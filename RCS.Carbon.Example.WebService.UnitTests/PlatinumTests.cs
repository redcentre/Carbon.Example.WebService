using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Example.WebService.Common.DTO;
using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.UnitTests;

[TestClass]
public class PlatinumTests : TestBase
{
	[TestMethod]
	public async Task T100_Platinum()
	{
		var sprops = new XSpecProperties();
		WriteTemp("_platest_sprops_ctor", sprops);
		var dprops = new XDisplayProperties();
		WriteTemp("_platest_drops_ctor", dprops);
		using var client = MakeClient();
		await GuardedSession(userId, userPass, client);
		OpenCloudJobResponse jobresp = await client.OpenCloudJob(custName, jobName, null, true);
		Trace($"OpenCloudJob {jobresp.DProps} {jobresp.DrillFilters} {jobresp.VartreeNames} {jobresp.AxisTreeNames}");
		
		jobresp.DProps!.Output.Format = XOutputFormat.TSV;
		WriteTemp("_platest_dprops_jobopen", jobresp.DProps);
		string[] lines = await client.GenTab("GenTab TSV and Platinum", genTop, genSide, null, null, sprops, jobresp.DProps);
		Sep1("TSV");
		DumpLines(lines);
		var data = await client.GeneratePlatinum();
		WriteTemp("_platest_GeneratePlatinum", data);

		var pprops = new PlatinumProperties();
		pprops.Description = "Created by GenTabPlatinum";
		pprops.Cells.Frequencies.Visible = true;
		pprops.Cells.ColumnPercents.Visible = true;
		pprops.Cells.RowPercents.Visible = true;
		var platreq = new GenTabPlatinumRequest("GenTab Platinum", "occupation", "income", null, null, null, sprops, pprops);
		data = await client.GenTabPlatinum(platreq);
		WriteTemp("_platest_GenTabPlatinum", data);

		bool closed = await client.CloseJob();
		Trace($"Closed → {closed}");
		
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	static readonly JsonSerializerOptions seropts = new JsonSerializerOptions() { WriteIndented = true };

	static void WriteTemp(string name, object value)
	{
		string s = JsonSerializer.Serialize(value, seropts);
		string fullname = Path.Combine(Path.GetTempPath(), name + ".json");
		File.WriteAllText(fullname, s);
	}
}