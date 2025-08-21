using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Example.WebService.Common.DTO;
using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.UnitTests;

[TestClass]
public class HttpTests : TestBase
{
	static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

	[TestMethod]
	public async Task T100_Http_Story()
	{
		JsonOpts.Converters.Add(new JsonStringEnumConverter());
		using var client = new HttpClient();
		client.BaseAddress = new System.Uri(baseUri);

		async Task<T> CarbonPostAsync<T>(string url, object request)
		{
			HttpResponseMessage hrm = await client.PostAsJsonAsync(url, request);
			string json = await hrm.Content.ReadAsStringAsync();
			//System.Diagnostics.Trace.WriteLine(json);
			//hrm.EnsureSuccessStatusCode();
			if (hrm.StatusCode == System.Net.HttpStatusCode.BadRequest)
			{
				JsonElement e;
				int i;
				var elem = JsonSerializer.Deserialize<JsonElement>(json);
				int? code = elem.TryGetProperty("code", out e) ? e.TryGetInt32(out i) ? i : null : null;
				if (code is 301)
				{
					// Special case - force session end and get a fresh one.
					string[] sessIds = [.. elem.GetProperty("data").EnumerateArray().Select(x => x.GetString()!)];
					string sessjoin = string.Join(",", sessIds);
					var hrm2 = await client.DeleteAsync($"session/force/{sessjoin}");
					hrm2.EnsureSuccessStatusCode();
					hrm = await client.PostAsJsonAsync(url, request);
					hrm.EnsureSuccessStatusCode();
					json = await hrm.Content.ReadAsStringAsync();
				}
			}
			hrm.EnsureSuccessStatusCode();
			return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
		}

		Sep1("Login");
		var authReq = new AuthenticateIdRequest(userId, userPass);
		var sinfo = await CarbonPostAsync<SessionInfo>("session/start/authenticate/id", authReq);
		Trace($"Login → {sinfo}");
		client.DefaultRequestHeaders.Add("x-session-id", sinfo.SessionId);

		Sep1("Open Job");
		var openReq = new OpenCloudJobRequest()
		{
			CustomerName = custName,
			JobName = jobName,
			TocType = JobTocType.ExecUser,
			GetDisplayProps = true,
			GetDrills = true,
			GetAxisTreeNames = true,
			GetVartreeNames = true
		};
		var openresp = await CarbonPostAsync<OpenCloudJobResponse>("job/open", openReq);
		Trace($"Open job → {openresp}");

		var formats = new XOutputFormat[]
		{
			//XOutputFormat.None,	// Gives a 204 NoContent response
			XOutputFormat.TSV,
			XOutputFormat.CSV,
			XOutputFormat.SSV,
			XOutputFormat.XLSX,
			XOutputFormat.HTML,
			XOutputFormat.OXT,
			XOutputFormat.OXTNums,
			//XOutputFormat.Diamond,	// Throws a not supported by Carbon
			XOutputFormat.MultiCube,
			XOutputFormat.Pandas
		};

		var genreq = new GenTabRequest()
		{
			Name = "Mock Report",
			DProps = openresp.DProps!,
			SProps = new XSpecProperties(),
			Top = "Age",
			Side = "Region",
			Filter = null,
			Weight = null
		};
		foreach (var format in formats)
		{
			Sep1($"GenTab {format} ({(int)format})");
			genreq.DProps.Output.Format = format;
			string[] lines = await CarbonPostAsync<string[]>("report/gentab", genreq);
			DumpLines(lines);
		}

		Sep1("Close Job");
		HttpResponseMessage hrm = await client.DeleteAsync("job/close");
		hrm.EnsureSuccessStatusCode();
		string json = await hrm.Content.ReadAsStringAsync();
		bool closed = JsonSerializer.Deserialize<bool>(json);
		Trace($"Job closed → {closed}");

		Sep1("Logoff (return)");
		hrm = await client.DeleteAsync("session/end");
		hrm.EnsureSuccessStatusCode();
		json = await hrm.Content.ReadAsStringAsync();
		bool ended = JsonSerializer.Deserialize<bool>(json);
		Trace($"Session end → {ended}");
	}

}
