using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Example.WebService.Common;
using RCS.Carbon.Example.WebService.Common.DTO;
using RCS.Carbon.Example.WebService.Database;
using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.UnitTests;

public class TestBase
{
	protected const string AppId = "UnitTests";
	protected readonly JsonSerializerOptions Jopts = new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
	public TestContext TestContext { get; set; }
	protected string baseUri;
	protected string userId;
	protected string userName;
	protected string userPass;
	protected string custName;
	protected string jobName;
	protected string genTop;
	protected string genSide;
	protected string report;
	protected bool skipCache;

	protected IConfiguration Config { get; }

	public TestBase()
	{
		var args = Environment.GetCommandLineArgs();
		Config = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddUserSecrets("RCS.Carbon.Example.WebService")
			.Build();
		baseUri = Config["UnitTests:BaseUri"]!;
		userId = Config["UnitTests:UserId"]!;
		userName = Config["UnitTests:UserName"]!;
		userPass = Config["UnitTests:UserPass"]!;
		custName = Config["UnitTests:CustName"]!;
		jobName = Config["UnitTests:JobName"]!;
		genTop = Config["UnitTests:Top"]!;
		genSide = Config["UnitTests:Side"]!;
		report = Config["UnitTests:Report"]!;
		skipCache = Config.GetValue<bool>("UnitTests:SkipCache");
	}

	protected CarbonServiceClient MakeClient()
	{
		var client = new CarbonServiceClient(Config["UnitTests:BaseUri"]!, 300);
		Trace($"MakeClient → {client.BaseAddress}");
		return client;
	}

	protected DbCore MakeDb() => new DbCore(Config["CarbonApi:ApplicationStorageConnect"]!, Config["CarbonApi:DatabaseTableName"]!);

	protected async Task<SessionInfo> GuardedSession(string idOrName, string password, CarbonServiceClient client, bool useId = true)
	{
		try
		{
			var sinfo = useId ? await client.StartSessionId(idOrName, password, skipCache) : await client.StartSessionName(idOrName, password, appId: AppId);
			Trace($"Login OK → {sinfo}");
			return sinfo;
		}
		catch (CarbonServiceException ex) when (ex.Code is 301 or 302)
		{
			Trace(ex.Message);
			string[] sessIds = ex.GetDataStrings()!;
			string join = string.Join(',', sessIds);
			int count = await client.ForceSessions(join);
			Trace($"ForceSessions({join}) → {count}");
			Assert.AreEqual(sessIds.Length, count);
			var sinfo = useId ? await client.StartSessionId(idOrName, password, skipCache) : await client.StartSessionName(idOrName, password, appId: AppId);
			Trace($"Login RETRY → {sinfo}");
			return sinfo;
		}
	}

	protected void Dumpobj(object value)
	{
		if (value == null)
		{
			Trace("NULL");
			return;
		}
		string json = JsonSerializer.Serialize(value, new JsonSerializerOptions() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
		Trace(json);
		System.Diagnostics.Trace.Write(json);
	}

	protected void Sep1(string? title = null)
	{
		if (title != null)
		{
			int len = title.Length + 4;
			Trace("┌" + new string('─', len) + "┐");
			Trace("│  " + title + "  │");
			Trace("└" + new string('─', len) + "┘");
		}
	}

	protected void DumpSessinfoShort(SessionInfo? sessinfo)
	{
		if (sessinfo == null)
		{
			Trace($"SessionInfo NULL");
			return;
		}
		string? roles = sessinfo.Roles == null ? null : "[" + string.Join(",", sessinfo.Roles) + "]";
		string? custs = sessinfo.SessionCusts == null ? null : "[" + string.Join(",", sessinfo.SessionCusts.Select(c => c.Name)) + "]";
		Trace($"Session Id={sessinfo.Id} Name={sessinfo.Name} Roles={roles} Custs={custs}");

	}

	protected void DumpSessinfo(SessionInfo? sessinfo)
	{
		if (sessinfo == null)
		{
			Trace($"SessionInfo NULL");
			return;
		}
		Trace($"SessionId ..... {sessinfo.SessionId}");
		Trace($"Id ............ {sessinfo.Id}");
		Trace($"Name .......... {sessinfo.Name}");
		Trace($"Email ......... {sessinfo.Email}");
		Trace($"Roles ......... {string.Join(" + ", sessinfo.Roles!)}");
		foreach (var cust in sessinfo.SessionCusts!)
		{
			Trace($"|  {cust.Id} {cust.Name}");
			foreach (var job in cust.SessionJobs!)
			{
				string vtjoin = string.Join(" + ", job.VartreeNames!);
				Trace($"|  |  {job.Id} {job.Name} • {vtjoin}");
			}
		}
	}

	protected void DumpMultiOxtResponse(MultiOxtResponse multiresp)
	{
		Trace($"MultiOXT Id ............. {multiresp.Id}");
		Trace($"MultiOXT Created ........ {multiresp.Created}");
		Trace($"MultiOXT IsCancelled .... {multiresp.IsCancelled}");
		foreach (var item in multiresp.Items)
		{
			if (item.ErrorType != null)
			{
				Trace($"\u2588 {item.ReportName} → {item.ErrorType} {item.ErrorMessage}");
			}
			else
			{
				Trace($"\u2588 {item.ReportName} Lines={item.OxtLines.Length} DispColLetters={item.DispColLetters} DispRowLetters={item.DispRowLetters} SigShowLetters={item.SigShowLetters} TitlesRowCount={item.Titles_RowCount} [{item.Seconds:F2}]");
				foreach (string line in item.OxtLines)
				{
					Trace($"| {line}");
				}
			}
		}
	}

	protected string? Join(IEnumerable<string>? parts)
	{
		if (parts == null) return "NULL";
		return "[" + string.Join(",", parts) + "]";
	}

	protected void DumpNodes(IEnumerable<GenNode> roots, int max = int.MaxValue)
	{
		foreach (var node in GenNode.WalkNodes(roots).Take(max))
		{
			string pfx = string.Join("", Enumerable.Repeat("│  ", node.Level));
			Trace($"{pfx}{node}");
		}
	}

	protected string NiceJson(string json)
	{
		var doc = JsonDocument.Parse(json);
		return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
	}

	protected void DumpLines(IEnumerable<string> lines, int max = int.MaxValue)
	{
		foreach (string line in lines.Take(max)) Trace($"║ {line}");
	}

	protected void DumpToc(IEnumerable<GenNode> roots)
	{
		foreach (var node in GenNode.WalkNodes(roots))
		{
			string pfx = string.Join("", Enumerable.Repeat("|  ", node.Level));
			Trace($"{pfx}{node.Id},{node.ParentId},{node.Type},{node.Value2},{node.Value1}");
		}
	}

	protected static string SafeName(string value)
	{
		char[] badpath = Path.GetInvalidPathChars();
		char[] badfile = Path.GetInvalidFileNameChars();
		char[] badall = [.. badpath.Union(badfile)];
		string bads = new string(badall);
		string s = Regex.Replace(value, "[" + Regex.Escape(bads) + "]", m => "_");
		while (s.Contains("__"))
		{
			s = s.Replace("__", "_");
		}
		return s.Trim("_ ".ToCharArray());
	}

	//protected void Trace(string message) => System.Diagnostics.Trace.WriteLine(message);
	protected void Trace(string message) => TestContext.WriteLine(message);

	protected async Task<long> DownloadUrlToFile(string url, string filename)
	{
		using var http = new HttpClient();
		var stream1 = await http.GetStreamAsync(url);
		using var writer = new FileStream(filename, FileMode.Create, FileAccess.Write);
		await stream1.CopyToAsync(writer);
		return writer.Position;
	}

	protected static string MakeTempFile(string name) => Path.Combine(Path.GetTempPath(), name);
}