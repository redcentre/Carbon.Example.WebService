using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RCS.Carbon.Example.WebService.Common.DTO;
using RCS.Carbon.Shared;
using TSAPI.Public.Domain.Interviews;
using TSAPI.Public.Domain.Metadata;

namespace RCS.Carbon.Example.WebService.Common;

/// <summary>
/// A class that encapsulates all processing against the Carbon web service. All requests and response data
/// is strongly-typed as .NET classes. The machinery of making web requests and interpreting the response codes
/// and bodies is silently internally handled correctly.
/// </summary>
public sealed partial class CarbonServiceClient : IDisposable
{
	/// <summary>
	/// The Session Id string required for access to the web service must be provided in request
	/// headers using this key. The value is shared widely throughout the suite.
	/// </summary>
	public const string SessionIdHeaderKey = "x-session-id";
	public const string ApiKeyHeaderKey = "x-api-key";

	readonly string _baseAddress;
	readonly int _timeoutSecs;
	HttpClient Client { get; }
	readonly JsonSerializerOptions JOpts = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

	/// <summary>
	/// Constructs a Carbon service client.
	/// </summary>
	/// <param name="baseAddress">Base Url address of the Carbon example web service.</param>
	/// <param name="timeoutSeconds">The request timeout seconds for the web service client.</param>
	/// <param name="customHander">An optional custom message handler that can be used to intercept
	/// requests and responses in the http pipeline. Clients can use this handler to add custom headers
	/// or validation to requests, and to globally inspect responses and perform custom processing.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="baseAddress"/> is null.</exception>
	public CarbonServiceClient(string baseAddress, int timeoutSeconds = 20, HttpMessageHandler? customHander = null)
	{
		_baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
		_timeoutSecs = timeoutSeconds;
		if (!_baseAddress.EndsWith('/'))
		{
			_baseAddress += "/";
		}
		Client = customHander == null ? new HttpClient() : new HttpClient(customHander);
		Client.BaseAddress = new Uri(_baseAddress);
		Client.Timeout = TimeSpan.FromSeconds(_timeoutSecs);
		JOpts.Converters.Add(new JsonStringEnumConverter());
	}

	public void Dispose()
	{
		Client.Dispose();
	}

	/// <summary>
	/// Base address specified when the client was constructed.
	/// </summary>
	public Uri? BaseAddress => Client?.BaseAddress;

	/// <summary>
	/// Gets the session Id that is currently active. Null if there is no active session.
	/// </summary>
	public string? SessionId => Session?.SessionId;

	/// <summary>
	/// Gets account licence summary information associated with the session. Null if there no active session.
	/// </summary>
	public SessionInfo? Session { get; private set; }

	string? _apiKey;
	/// <summary>
	/// The API Key can be set by clients who know a value that is registered for the service.
	/// When a valid value is set, it is sent in requests in the <c>x-api-key</c> header.
	/// A valid API Key allows access to certain endpoints that don't use sessions and are useful
	/// for utility applications querying the service.
	/// </summary>
	public string? ApiKey
	{
		get => _apiKey;
		set
		{
			_apiKey = value;
			if (Client.DefaultRequestHeaders.TryGetValues(ApiKeyHeaderKey, out var values))
			{
				Client.DefaultRequestHeaders.Remove(ApiKeyHeaderKey);
			}
			if (_apiKey != null)
			{
				Client.DefaultRequestHeaders.Add(ApiKeyHeaderKey, ApiKey);
			}
		}
	}

	#region Special Calls

	/// <summary>
	/// Exports TSAPI compliant metadata from a job (survey).
	/// </summary>
	/// <param name="varnames">Variables names to export.</param>
	/// <param name="filter">Filter expression for exported data.</param>
	/// <returns>A TSAPI compliant <c>SurveyMetadata</c> object.</returns>
	[CLSCompliant(false)]
	public async Task<SurveyMetadata> TsapiMetadata(string[] varnames, string filter)
	{
		ArgumentNullException.ThrowIfNull(varnames);
		ArgumentNullException.ThrowIfNull(filter);
		if (varnames.Length < 2) throw new ArgumentException("At least two variable names must be provided", nameof(varnames));
		string vjoin = string.Join("&", varnames.Select(v => $"varnames={v}"));
		return await InnerGet<SurveyMetadata>($"tsapi/metadata?{vjoin}&filter={filter}");
	}

	/// <summary>
	/// Exports TSAPI compliant interviews from a job (survey).
	/// </summary>
	/// <param name="varnames">Variables names to export.</param>
	/// <param name="filter">Filter expression for exported data.</param>
	/// <returns>An array of TSAPI compliant <c>Interview</c> objects.</returns>
	[CLSCompliant(false)]
	public async Task<Interview[]> TsapiInterview(string[] varnames, string filter)
	{
		ArgumentNullException.ThrowIfNull(varnames);
		ArgumentNullException.ThrowIfNull(filter);
		if (varnames.Length < 2) throw new ArgumentException("At least two variable names must be provided", nameof(varnames));
		string vjoin = string.Join("&", varnames.Select(v => $"varnames={v}"));
		return await InnerGet<Interview[]>($"tsapi/interview?{vjoin}&filter={filter}");
	}

	/// <summary>
	/// Processes a JSON data containing Python pandas library compliant data values through cross-tabulation
	/// processing and returns JSON in the shape of a pandas dataframe.
	/// </summary>
	/// <param name="data">TODO</param>
	/// <returns>TODO</returns>
	public async Task<string> PandasAlphacodes(string data)
	{
		var content = new StringContent(data, Encoding.UTF8, "application/json");
		var response = await Client.PostAsync("python/crosstab/alphacodes", content);
		response.EnsureSuccessStatusCode();
		return await response.Content.ReadAsStringAsync();
	}

	/// <summary>
	/// Generates a crosstab report as plain text in different formats.
	/// </summary>
	/// <include file='CarbonServiceClientDoc.xml' path='doc/members[@name="GenTabParams"]/*'/>
	/// <returns>The string body of a crosstab report as plain text.</returns>
	/// <exception cref="ArgumentNullException">Thrown if null value passed in <paramref name="top"/> or <paramref name="side"/> or <paramref name="sprops"/> or <paramref name="dprops"/>.</exception>
	/// <remarks>
	/// The <paramref name="format"/> value must be set to one of the enumeration values: TSV, CSV, SSV, XML, HTML, OXT, OXTNums, MultiCube.
	/// Only these report formats can be represented as plain text and returned by this endpoint.
	/// The response content-type is always text/plain.
	/// </remarks>
	public async Task<string> ReportGenTabText(XOutputFormat format, string? name, string top, string side, string? filter, string? weight, XSpecProperties sprops, XDisplayProperties dprops)
	{
		ArgumentNullException.ThrowIfNull(top);
		ArgumentNullException.ThrowIfNull(side);
		ArgumentNullException.ThrowIfNull(sprops);
		ArgumentNullException.ThrowIfNull(dprops);
		var data = new GenTabRequest(name, top, side, filter, weight, sprops, dprops);
		return await InnerPostText($"report/gentab/text/{format}", data);
	}

	public async Task<XlsxResponse> ReportGenTabExcelBlob(string? name, string top, string side, string? filter, string? weight, XSpecProperties sprops, XDisplayProperties dprops)
	{
		ArgumentNullException.ThrowIfNull(top);
		ArgumentNullException.ThrowIfNull(side);
		ArgumentNullException.ThrowIfNull(sprops);
		ArgumentNullException.ThrowIfNull(dprops);
		var data = new GenTabRequest(name, top, side, filter, weight, sprops, dprops);
		return await InnerPost<XlsxResponse>($"report/gentab/excel/blob", data);
	}

	public async Task<string> ReportGenTabPandas(int format, string? name, string top, string side, string? filter, string? weight, XSpecProperties sprops, XDisplayProperties dprops)
	{
		ArgumentNullException.ThrowIfNull(top);
		ArgumentNullException.ThrowIfNull(side);
		ArgumentNullException.ThrowIfNull(sprops);
		ArgumentNullException.ThrowIfNull(dprops);
		var data = new GenTabRequest(name, top, side, filter, weight, sprops, dprops);
		return await InnerPostText($"report/gentab/pandas/{format}", data);
	}

	#endregion

	#region Helpers

	/// <summary>
	/// Multi-threads may attempt to alter the client request headers for overlapping
	/// logon or logoff, so they need to be locked (found during stress testing).
	/// </summary>
	void EnsureSessionIdHeader()
	{
		lock (Client.DefaultRequestHeaders)
		{
			if (Client.DefaultRequestHeaders.TryGetValues(SessionIdHeaderKey, out var values))
			{
				Client.DefaultRequestHeaders.Remove(SessionIdHeaderKey);
			}
			if (Session != null)
			{
				Client.DefaultRequestHeaders.Add(SessionIdHeaderKey, Session.SessionId);
			}
		}
	}

	async Task<T?> InnerGet<T>(string uri, bool canNotFound = false)
	{
		HttpResponseMessage? response = null;
		try
		{
			response = await Client.GetAsync(uri);
			if (response.StatusCode == System.Net.HttpStatusCode.NotFound && canNotFound)
			{
				return default;
			}
			string respjson = await response.Content.ReadAsStringAsync();
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				AnalyzeBadResponse(response, respjson);
				var error = JsonSerializer.Deserialize<ErrorResponse>(respjson, JOpts)!;
				throw new CarbonServiceException(error.Code, error.Message);
			}
			return JsonSerializer.Deserialize<T>(respjson, JOpts)!;
		}
		catch (HttpRequestException ex)
		{
			int code = (ex.InnerException is SocketException sex) ? sex.ErrorCode : -1;
			throw new CarbonServiceException(code, ex.Message);
		}
		catch (JsonException jex)
		{
			Trace.WriteLine(jex.Message);
			throw new CarbonServiceException(666, $"The GET response from '{_baseAddress}{uri}' status {response?.StatusCode} is not in a recognised format. The address may be incorrect or the service is faulting.");
		}
	}

	async Task<T> InnerPost<T>(string uri, object data)
	{
		HttpResponseMessage? response = null;
		try
		{
			string json = JsonSerializer.Serialize(data);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			response = await Client.PostAsync(uri, content);
			string respjson = await response.Content.ReadAsStringAsync();
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				AnalyzeBadResponse(response, respjson);
			}
			return JsonSerializer.Deserialize<T>(respjson, JOpts)!;
		}
		catch (HttpRequestException ex)
		{
			int code = (ex.InnerException is SocketException sex) ? sex.ErrorCode : -1;
			throw new CarbonServiceException(code, ex.Message);
		}
		catch (JsonException jex)
		{
			Trace.WriteLine(jex.Message);
			throw new CarbonServiceException(666, $"The POST response from '{_baseAddress}{uri}' status {response?.StatusCode} is not in a recognised format. The address may be incorrect or the service is faulting.");
		}
	}

	async Task<string> InnerPostText(string uri, object data)
	{
		try
		{
			string json = JsonSerializer.Serialize(data);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			HttpResponseMessage? response = await Client.PostAsync(uri, content);
			string body = await response.Content.ReadAsStringAsync();
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				AnalyzeBadResponse(response, body);
			}
			return body;
		}
		catch (HttpRequestException ex)
		{
			int code = (ex.InnerException is SocketException sex) ? sex.ErrorCode : -1;
			throw new CarbonServiceException(code, ex.Message);
		}
	}

	async Task<T> InnerDelete<T>(string uri)
	{
		HttpResponseMessage? response = null;
		try
		{
			response = await Client.DeleteAsync(uri);
			string respjson = await response.Content.ReadAsStringAsync();
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				AnalyzeBadResponse(response, respjson);
				var error = JsonSerializer.Deserialize<ErrorResponse>(respjson, JOpts)!;
				throw new CarbonServiceException(error.Code, error.Message);
			}
			return JsonSerializer.Deserialize<T>(respjson, JOpts)!;
		}
		catch (HttpRequestException ex)
		{
			int code = (ex.InnerException is SocketException sex) ? sex.ErrorCode : -1;
			throw new CarbonServiceException(code, ex.Message);
		}
		catch (JsonException jex)
		{
			Trace.WriteLine(jex.Message);
			throw new CarbonServiceException(666, $"The DELETE response from '{_baseAddress}{uri}' status {response?.StatusCode} is not in a recognised format. The address may be incorrect or the service is faulting.");
		}
	}

	static void AnalyzeBadResponse(HttpResponseMessage response, string json)
	{
		var elem = JsonSerializer.Deserialize<JsonElement>(json);
		// Is this a Carbon ErrorResponse class?
		int? code = elem.TryGetProperty("code", out JsonElement e) ? e.TryGetInt32(out int i) ? i : null : null;
		string? message = elem.TryGetProperty("message", out e) ? e.GetString() : null;
		if (code != null && message != null)
		{
			var ex = new CarbonServiceException(code.Value, message);
			if (code is 301)
			{
				// This is a duplicate session failure which can be treaed as a special
				// case to return the existing session Ids so the caller can force those
				// sessions to end if desirable.
				string[] sessIds = [.. elem.GetProperty("data").EnumerateArray().Select(x => x.GetString()!)];
				ex.SetDataStrings(sessIds);
			}
			throw ex;
		}
		// Is this an Azure failure response?
		string? type = elem.TryGetProperty("type", out e) ? e.GetString() : null;
		string? title = elem.TryGetProperty("title", out e) ? e.GetString() : null;
		int? status = elem.TryGetProperty("status", out e) ? e.TryGetInt32(out i) ? i : null : null;
		if (type != null && title != null && status != null)
		{
			// The 'errors' could be extracted here
			throw new CarbonServiceException(status.Value, title);
		}
		throw new CarbonServiceException(666, $"Response status {response.StatusCode} unknown response body");
	}

	#endregion
}
/*
 {
  ""type"": ""https://tools.ietf.org/html/rfc7231#section-6.5.1"",
  ""title"": ""One or more validation errors occurred."",
  ""status"": 400,
  ""traceId"": ""00-ece035e9f5b789a8348cc10232814a5c-e46343b3cd7c2f2f-00"",
  ""errors"": {
""Filters[0].Syntax"": [

  ""The Syntax field is required.""
    ],
    ""Filters[1].Syntax"": [

  ""The Syntax field is required.""
    ],
    ""Filters[2].Syntax"": [

  ""The Syntax field is required.""
    ]
  }
}
*/