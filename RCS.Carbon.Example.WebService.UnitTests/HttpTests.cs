using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using RCS.Carbon.Example.WebService.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.UnitTests
{
	[TestClass]
	public class HttpTests : TestBase
	{
		static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

		[TestMethod]
		public async Task T100_Http_Story()
		{
			using var client = new HttpClient();
			client.BaseAddress = new System.Uri("http://localhost:5086/");

			async Task<T> CarbonPostAsync<T>(string url, object request)
			{
				HttpResponseMessage hrm = await client.PostAsJsonAsync(url, request);
				string json = await hrm.Content.ReadAsStringAsync();
				//System.Diagnostics.Trace.WriteLine(json);
				hrm.EnsureSuccessStatusCode();
				return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
			}

			Sep1("Login");
			var loginReq = new AuthenticateIdRequest("16499372", "C6H12O6");
			var sinfo = await CarbonPostAsync<SessionInfo>("session/start/login/id", loginReq);
			Trace($"Login → {sinfo}");
			client.DefaultRequestHeaders.Add("x-session-id", sinfo.SessionId);

			Sep1("Open Job");
			var openReq = new OpenCloudJobRequest()
			{
				CustomerName = "client1rcs",
				JobName = "demo",
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
				XOutputFormat.XML,
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
				string[] lines = await CarbonPostAsync<string[]>("job/gentab", genreq);
				DumpLines(lines);
			}

			Sep1("Close Job");
			HttpResponseMessage hrm = await client.DeleteAsync("job/close");
			hrm.EnsureSuccessStatusCode();
			string json = await hrm.Content.ReadAsStringAsync();
			bool closed = JsonSerializer.Deserialize<bool>(json);
			Trace($"Job closed → {closed}");

			Sep1("Logoff (return)");
			hrm = await client.DeleteAsync("session/end/return");
			hrm.EnsureSuccessStatusCode();
			json = await hrm.Content.ReadAsStringAsync();
			int count = JsonSerializer.Deserialize<int>(json);
			Trace($"Session return → {count}");
		}

	}
}
