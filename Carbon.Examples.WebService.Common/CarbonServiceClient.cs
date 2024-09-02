using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RCS.Carbon.Shared;

namespace Carbon.Examples.WebService.Common
{
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

		readonly string _baseAddress;
		readonly int _timeoutSecs;
		HttpClient Client { get; }
		readonly JsonSerializerOptions JOpts = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true, WriteIndented = true };

		/// <summary>
		/// Constructs a Carbon service client.
		/// </summary>
		/// <param name="baseAddress">Base Url address of the web service.</param>
		/// <param name="timeoutSeconds">TODO</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="baseAddress"/> is null.</exception>
		public CarbonServiceClient(string baseAddress, int timeoutSeconds = 20)
		{
			_baseAddress = baseAddress ?? throw new ArgumentNullException(nameof(baseAddress));
			_timeoutSecs = timeoutSeconds;
			if (!_baseAddress.EndsWith("/"))
			{
				_baseAddress += "/";
			}
			Client = new HttpClient
			{
				BaseAddress = new Uri(_baseAddress),
				Timeout = TimeSpan.FromSeconds(_timeoutSecs)
			};
			JOpts.Converters.Add(new JsonStringEnumConverter());
		}

		public void Dispose()
		{
			Client.Dispose();
		}

		/// <summary>
		/// Base address specified when the client was constructed.
		/// </summary>
		public Uri BaseAddress => Client.BaseAddress;

		/// <summary>
		/// Gets the session Id that is currently active. Null if there is no active session.
		/// </summary>
		public string? SessionId => Session?.SessionId;

		/// <summary>
		/// Gets account licence summary information associated with the session. Null if there no active session.
		/// </summary>
		public SessionInfo? Session { get; private set; }

		/// <summary>
		/// SPECIAL METHOD -- Static helper method that allows arbitrary apps to end a Carbon service session then
		/// issue a Licensing logoff or return.
		/// </summary>
		/// <remarks>
		/// The method was initially created for the Carbon dekstop app so that it could start a short Process to
		/// call this method during the main Window closing event where asynchronous calls are ineffective. See the
		/// notes in the app's controller Shutdown method and Program class.
		/// </remarks>
		/// <param name="baseUri">The base address of the licensing web service.</param>
		/// <param name="sessionId">The Carbon web service session Id.</param>
		/// <param name="returnId">True to issuea a licensing <b>return</b>, False for a <b>logoff</b>.</param>
		/// <returns>The number of licensing borrows remaining, or -1 if processing failed.</returns>
		public static async Task<int> EndSessionExternal(string baseUri, string sessionId, bool returnId)
		{
			if (!baseUri.EndsWith("/"))
			{
				baseUri += "/";
			}
			using var client = new HttpClient();
			client.BaseAddress = new Uri(baseUri);
			client.DefaultRequestHeaders.Add(SessionIdHeaderKey, sessionId);
			string action = returnId ? "return" : "logoff";
			HttpResponseMessage response = await client.DeleteAsync($"session/end/{action}");
			response.EnsureSuccessStatusCode();
			string json = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<int>(json);
		}

		#region Special Calls

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
			if (top == null) throw new ArgumentNullException(nameof(top));
			if (side == null) throw new ArgumentNullException(nameof(side));
			if (sprops == null) throw new ArgumentNullException(nameof(sprops));
			if (dprops == null) throw new ArgumentNullException(nameof(dprops));
			var data = new GenTabRequest(name, top, side, filter, weight, sprops, dprops);
			return await InnerPostText($"report/gentab/text/{format}", data);
		}

		public async Task<string> ReportGenTabXml(string? name, string top, string side, string? filter, string? weight, XSpecProperties sprops, XDisplayProperties dprops)
		{
			if (top == null) throw new ArgumentNullException(nameof(top));
			if (side == null) throw new ArgumentNullException(nameof(side));
			if (sprops == null) throw new ArgumentNullException(nameof(sprops));
			if (dprops == null) throw new ArgumentNullException(nameof(dprops));
			var data = new GenTabRequest(name, top, side, filter, weight, sprops, dprops);
			return await InnerPostText($"report/gentab/xml", data);
		}

		public async Task<XlsxResponse> ReportGenTabExcelBlob(string? name, string top, string side, string? filter, string? weight, XSpecProperties sprops, XDisplayProperties dprops)
		{
			if (top == null) throw new ArgumentNullException(nameof(top));
			if (side == null) throw new ArgumentNullException(nameof(side));
			if (sprops == null) throw new ArgumentNullException(nameof(sprops));
			if (dprops == null) throw new ArgumentNullException(nameof(dprops));
			var data = new GenTabRequest(name, top, side, filter, weight, sprops, dprops);
			return await InnerPost<XlsxResponse>($"report/gentab/excel/blob", data);
		}

		public async Task<string> ReportGenTabPandas(int format, string? name, string top, string side, string? filter, string? weight, XSpecProperties sprops, XDisplayProperties dprops)
		{
			if (top == null) throw new ArgumentNullException(nameof(top));
			if (side == null) throw new ArgumentNullException(nameof(side));
			if (sprops == null) throw new ArgumentNullException(nameof(sprops));
			if (dprops == null) throw new ArgumentNullException(nameof(dprops));
			var data = new GenTabRequest(name, top, side, filter, weight, sprops, dprops);
			return await InnerPostText($"report/gentab/pandas/{format}", data);
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Multi-threads may attempt to alter the client request headers for overlapping
		/// logon or logoff, so they need to be locked (found during stress testing).
		/// </summary>
		void EnsureIdHeader()
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

		async Task<T> InnerGet<T>(string uri)
		{
			HttpResponseMessage? response = null;
			try
			{
				response = await Client.GetAsync(uri);
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
			HttpResponseMessage? response = null;
			try
			{
				string json = JsonSerializer.Serialize(data);
				var content = new StringContent(json, Encoding.UTF8, "application/json");
				response = await Client.PostAsync(uri, content);
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

		void AnalyzeBadResponse(HttpResponseMessage response, string json)
		{
			JsonElement e;
			int i;
			var elem = JsonSerializer.Deserialize<JsonElement>(json);
			// Is this a Carbon ErrorResponse class?
			int? code = elem.TryGetProperty("code", out e) ? e.TryGetInt32(out i) ? i : (int?)null : null;
			string? message = elem.TryGetProperty("message", out e) ? e.GetString() : null;
			if (code != null && message != null)
			{
				throw new CarbonServiceException(code.Value, message);
			}
			// Is this an Azure failure response?
			string? type = elem.TryGetProperty("type", out e) ? e.GetString() : null;
			string? title = elem.TryGetProperty("title", out e) ? e.GetString() : null;
			int? status = elem.TryGetProperty("status", out e) ? e.TryGetInt32(out i) ? i : (int?)null : null;
			if (type != null && title != null && status != null)
			{
				// The 'errors' could be extracted here
				throw new CarbonServiceException(status.Value, title);
			}
			throw new CarbonServiceException(666, $"Response status {response.StatusCode} unknown response body");
		}

		#endregion
	}
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