using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RCS.Azure.Data.Common;
using RCS.Carbon.Export;
using RCS.Carbon.Shared;

namespace Carbon.Examples.WebService.WebApi.Controllers;

partial class TaController
{
	async Task<TADef[]> ListTaDefsImpl(string cust, string job)
	{
		Logger.LogInformation(670, "List TADef {CustomerName} {JobName}", cust, job);
		return await AzProc.ListTaDefsAsync(GetKey(cust), job);
	}

	async Task<TADef> UpsertTaDefImpl(string cust, string job, TADef def)
	{
		Logger.LogInformation(671, "Upsert TADef {CustomerName} {JobName} {Def}", cust, job, def);
		return await AzProc.UpsertTaDef(GetKey(cust), job, def);
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
				Codes = v.Values.Values.Select(x => new TsapiMetaCode() { Code = int.Parse(x.Code), Label = x.Label.Text }).ToArray()
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
		//string json = JsonSerializer.Serialize(tsdata, new JsonSerializerOptions() { WriteIndented = true });
		//string filename = $"export-{varjoin}-{def.Filter}.json";
		//filename = Regex.Replace(filename, @"[^a-zA-Z0-9.-]", "_");
		//System.IO.File.WriteAllText(@"D:\temp\" + filename, json);  //####
		return expdata;
	}
}
