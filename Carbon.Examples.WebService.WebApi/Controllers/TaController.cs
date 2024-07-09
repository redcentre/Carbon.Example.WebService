using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RCS.Azure.Data.Common;
using RCS.Carbon.Export;
using RCS.Carbon.Shared;
using RCS.Carbon.Variables;
using TSAPI.Public.Queries;

namespace Carbon.Examples.WebService.WebApi.Controllers;

partial class TaController
{
	async Task<TADef[]> ListTaDefsImpl()
	{
		SessionItem si = SessionManager.FindSession(SessionId, true)!;
		var deflist = await AzProc.ListTaDefsAsync(si.OpenStorageKey!, si.OpenJobName!);
		LogInfo(670, "List TADef {CustomerName} {JobName} -> {Count}", si.OpenCustomerName, si.OpenJobName, deflist.Length);
		return deflist;
	}

	async Task<TADef> UpsertTaDefImpl(TADef def)
	{
		SessionItem si = SessionManager.FindSession(SessionId, true)!;
		var olddef = await AzProc.GetDefAsync(si.OpenStorageKey!, si.OpenJobName!, def.Uid);
		if (olddef == null)
		{
			def.CreatedUtc = DateTime.UtcNow;
			def.CreatedUserName = si.UserName;
			def.Name ??= "Untitled";
		}
		var updef = await AzProc.UpsertTaDef(si.OpenStorageKey!, si.OpenJobName!, def);
		LogInfo(671, "Upsert TADef {CustomerName} {JobName} {Def}", si.OpenCustomerName, si.OpenJobName, def);
		return updef;
	}

	async Task<bool> DeleteTaDefImpl(Guid uid)
	{
		SessionItem si = SessionManager.FindSession(SessionId, true)!;
		return await AzProc.DeleteTaDef(si.OpenStorageKey!, si.OpenJobName!, uid);
	}

	/// <summary>
	/// An export just just runs the export process early so the user can preview the
	/// data that will be available via the same call to the LLM later. The user can
	/// tweak the export parameters to their satisfaction before the LLM analysis.
	/// No stats are updated.
	/// </summary>
	async Task<TSAPIData> ExportTaDefImpl(TADef def)
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
		var export = new ExportEngine();
		export.AttachJob(wrap.Engine);
		string varjoin = string.Join(",", def.VariableNames);
		var query = new InterviewsQuery()
		{
			Variables = def.VariableNames.ToList()
		};
		TSAPIData tsdata = await Task.Run(() => export.ExportTSAPI(query, def.Filter));
		if (tsdata.MetaData == null ||
			tsdata.MetaData.Variables == null ||
			tsdata.MetaData.Variables.Count == 0 ||
			tsdata.Interviews == null ||
			tsdata.Interviews.Length == 0) throw new ApplicationException("The job does not contain any matching data or variables to export");
		// The quite full and verbose data in TSAPI compilant format that comes from the
		// Cabon engine is 'reduced' to some simpler classes that are suitable for export
		// and analysis by clients using AI or LLM.
		TSAPIData expdata = new TSAPIData()
		{
			ExportMetadata = tsdata.MetaData.Variables.Select(v => new TsapiMetaVariable()
			{
				Id = int.Parse(v.VariableId),
				Name = v.Name,
				Type = v.Type,
				Label = v.Label.Text,
				Codes = v.Values.Values?.Select(x => new TsapiMetaCode() { Code = int.Parse(x.Code), Label = x.Label.Text }).ToArray() ?? Array.Empty<TsapiMetaCode>()
			}).ToArray(),
			ExportInterview = tsdata.Interviews.Select(i => new TsapiInterview()
			{
				Id = int.Parse(i.InterviewId),
				Complete = i.Complete,
				Responses = i.Responses.Select(r => new TsapiInterviewResponse()
				{
					VariableId = int.Parse(r.VariableId),
					Values = r.Data.Select(d => int.Parse(d.Value)).ToArray()
				}).ToArray()
			}).ToArray()
		};
		// Merge Ruby job metadata into the export metadata.
		foreach (var mv in expdata.ExportMetadata)
		{
			VVarInfo? vinfo = wrap.Engine.VarInfo(mv.Name);
			if (vinfo != null)
			{
				mv.AKA = string.IsNullOrEmpty(vinfo.AKA) ? null : vinfo.AKA;
				mv.VarType = vinfo.Type.ToString();
			}
		}
#if DEBUG
		string filename = $"export-{varjoin}-{def.Filter}";
		filename = Regex.Replace(filename, @"[^a-zA-Z0-9.-]", "_");
		while (filename.Contains("__")) filename = filename.Replace("__", "_");
		filename = filename.Trim("_- ".ToCharArray());
		string json = JsonSerializer.Serialize(tsdata, new JsonSerializerOptions() { WriteIndented = true });
		System.IO.File.WriteAllText(@"D:\temp\" + filename + "-tsdata.json", json);
		json = JsonSerializer.Serialize(expdata, new JsonSerializerOptions() { WriteIndented = true });
		System.IO.File.WriteAllText(@"D:\temp\" + filename + "-expdata.json", json);
#endif
		return expdata;
	}

	/// <summary>
	/// The export parameters are sent to the LLM service so it can make a reverse call to
	/// get the export data to be analysed.
	/// </summary>
	async Task<TADef> AnalyseTaDefImpl(TADef def)
	{
		SessionItem si = SessionManager.FindSession(SessionId, true)!;
		var client = new HttpClient();
		string llmToken = Config["LLM:BearerToken"]!;
		string llmModel = Config["LLM:PostModel"]!;
		string llmRole = Config["LLM:PostRole"]!;
		string llmUri = Config["LLM:Uri"]!;
		client.DefaultRequestHeaders.Add("Authorization", $"Bearer {llmToken}");
		var postdata = new
		{
			model = llmModel,
			messages = new[] { new { role = llmRole, content = def.Question } }
		};
		string postjson = JsonSerializer.Serialize(postdata);
		var postContent = new StringContent(postjson, Encoding.UTF8, MediaTypeNames.Application.Json);
		string varjoin = string.Join(",", def.VariableNames);
		var cts = new CancellationTokenSource();
		cts.CancelAfter(def.TimeoutSeconds * 1000);
		foreach (var header in client.DefaultRequestHeaders)
		{
			Trace.WriteLine($"{header.Key} = {string.Join(",", header.Value)}");
		}
		//Trace.WriteLine(postjson);
		//Trace.WriteLine($"varnames={string.Join(",", def.VariableNames)}");
		//Trace.WriteLine($"filters={def.Filter}");
		++def.AnalyseCount;
		def.AnalyseUtc = DateTime.UtcNow;
		def.AnalyseUserName = si.UserName;
		await AzProc.UpsertTaDef(si.OpenStorageKey!, si.OpenJobName!, def);
		string uri = string.Format(llmUri, Uri.EscapeDataString(varjoin), Uri.EscapeDataString(def.Filter));
		//Trace.WriteLine(uri);
		var resp = await client.PostAsync(uri, postContent, cts.Token);
		string json = await resp.Content.ReadAsStringAsync();
		if (resp.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new ApplicationException($"Failure response code {resp.StatusCode} - {json}");
		}
		var doc = JsonDocument.Parse(json);
		if (!doc.RootElement.TryGetProperty("choices", out JsonElement choices)) throw new ApplicationException("choices element not found");
		if (!choices.EnumerateArray().Any()) throw new ApplicationException("choices first element not found");
		JsonElement choice0 = choices.EnumerateArray().First();
		if (!choice0.TryGetProperty("message", out JsonElement message)) throw new ApplicationException("message element not found");
		if (!message.TryGetProperty("content", out JsonElement content)) throw new ApplicationException("content element not found");
		string md = content.GetString() ?? "No response";
		def.AnswerHtml = Markdig.Markdown.ToHtml(md);
		await AzProc.UpsertTaDef(si.OpenStorageKey!, si.OpenJobName!, def);
		Trace.WriteLine(def.AnswerHtml);
		return def;
	}
}
