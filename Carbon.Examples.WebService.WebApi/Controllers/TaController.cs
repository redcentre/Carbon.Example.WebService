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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RCS.Azure.Data.Common;
using RCS.Carbon.Export;
using RCS.Carbon.Shared;
using RCS.Carbon.Variables;

namespace Carbon.Examples.WebService.WebApi.Controllers;

partial class TaController
{
	async Task<TADef[]> ListTaDefsImpl(string cust, string job)
	{
		Logger.LogInformation(670, "List TADef {CustomerName} {JobName}", cust, job);
		return await AzProc.ListTaDefsAsync(GetKey(cust), job);
	}

	async Task<TADef> UpsertTaDefImpl(string cust, string job, string userName, TADef def)
	{
		Logger.LogInformation(671, "Upsert TADef {CustomerName} {JobName} {Def} {User}", cust, job, def, userName);
		return await AzProc.UpsertTaDef(GetKey(cust), job, def, userName);
	}

	async Task<bool> DeleteTaDefImpl(string customerName, string jobName, string taName)
	{
		return await AzProc.DeleteTaDef(GetKey(customerName), jobName, taName);
	}

	async Task<TSAPIData> ExportTaDefImpl(TADef def)
	{
		using var wrap = new StateWrap(SessionId, LicProv, false);
		var export = new ExportEngine();
		export.AttachJob(wrap.Engine);
		string varjoin = string.Join(",", def.VariableNames);
		TSAPIData tsdata = await Task.Run(() => export.ExportTSAPIVarFilter(varjoin, def.Filter));
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

	async Task<TADef> AnalyseTaDefImpl(TADef def)
	{
		var client = new HttpClient();
		client.DefaultRequestHeaders.Add("Authorization", "Bearer hl-iVzjP0HLN4CtsHJrk4aP3wpeyF4UGg0cw3vBUWLtiJw=");
		var postdata = new
		{
			model = "doesnt-matter",
			messages = new [] { new { role = "user", content = def.Question } }
		};
		string postjson = JsonSerializer.Serialize(postdata);
		var postContent = new StringContent(postjson, Encoding.UTF8, MediaTypeNames.Application.Json);
		string varjoin = string.Join(",", def.VariableNames);
		var cts = new CancellationTokenSource();
		cts.CancelAfter(60000);
		foreach (var header in client.DefaultRequestHeaders)
		{
			Trace.WriteLine($"{header.Key} = {string.Join(",", header.Value)}");
		}
		Trace.WriteLine(postjson);
		Trace.WriteLine($"varnames={string.Join(",", def.VariableNames)}");
		Trace.WriteLine($"filters={def.Filter}");
		string uri = $"https://bayesprice.helix.ml/v1/chat/completions?varnames={Uri.EscapeDataString(varjoin)}&filters={Uri.EscapeDataString(def.Filter)}";
		Trace.WriteLine(uri);
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
		def.Answer = content.GetString() ?? "Empty response";
		Trace.WriteLine(def.Answer);
		return def;
	}
}
