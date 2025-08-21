using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Example.WebService.Common.DTO;

namespace RCS.Carbon.Example.WebService.UnitTests;

[TestClass]
public class MultiTests : TestBase
{
	/// <summary>
	/// Test the new Mmilti-sheet endpoint created for the Platinum team in August 2025.
	/// </summary>
	[TestMethod]
	public async Task T10_Multi_Sheet()
	{
		using var client = MakeClient();
		await GuardedSession(userId, userPass, client);
		OpenCloudJobResponse jobresp = await client.OpenCloudJob(custName, jobName, null, true);
		Trace($"OpenCloudJob {jobresp.DProps} {jobresp.DrillFilters} {jobresp.VartreeNames} {jobresp.AxisTreeNames}");

		var request = new MultiSheetRequest()
		{
			ReportNames = [
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC ActivelyLooking",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_10 Driver BRa Val",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_10 Driver C Val",
				"Tables/Exec/Standard Tables/UK/TV Set/TVS WillingPayTV",
				"Tables/Exec/Standard Tables/UK/TV Set/TVS WillingCommit",
				"Tables/Exec/Standard Tables/UK/Cinema/V WillingPayTV"
			],
			Filter = null//"bb_Age(1)"
		};
		MultiSheetResponse result = await client.MultiSheet(request);
		Trace($"Mutil sheet uri → {result.ExcelBlobUri} [{result.Seconds:F1}]");

		bool closed = await client.CloseJob();
		Trace($"Closed → {closed}");

		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	/// <summary>
	/// Generates lots of platinum reports using the parallel multi-engine workarond technique.
	/// </summary>
	[TestMethod]
	public async Task T200_MultiPlatinum()
	{
		using var client = MakeClient();
		await GuardedSession(userId, userPass, client);
		var meta = await client.GetServiceInfo();
		Trace($"Service {meta.Version} {meta.CarbonVersion} {meta.HostMachine} {client.BaseAddress}");
		OpenCloudJobResponse jobresp = await client.OpenCloudJob(custName, jobName, null, true);
		Trace($"OpenCloudJob {jobresp.DProps} {jobresp.DrillFilters} {jobresp.VartreeNames} {jobresp.AxisTreeNames}");

		var request = new MultiPlatinumRequest()
		{
			ParallelMax = 5,
			SaveAsBlobs = true,
			Top = "UNUSED",
			Side = "UNUSED",
			Filter = "bb_age(1)",
			Weight = null,
			ReportNames = [
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC ActivelyLooking",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_10 Driver BRa Val",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_10 Driver C Val",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_10 Driver P Val",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_10 Driver PP Val",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_10 Driver Pros Val",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_107 Driver BRg slow",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_113 Driver BRg No1",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_19 Driver BRa Speed",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_19 Driver BRg Speed",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_19 Driver C Speed",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_19 Driver P Speed",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_19 Driver PP Speed",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_19 Driver Pros Speed",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_20 Driver BRa WiFi",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_20 Driver BRg WiFi",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_20 Driver C WiFi",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_20 Driver P WiFi",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_20 Driver PP WiFi",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_20 Driver Pros WiFi",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_21 Driver BRa Reliable",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_21 Driver BRg Reliable",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_21 Driver C Reliable",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_21 Driver P Reliable",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_21 Driver PP Reliable",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_21 Driver Pros Reliable",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_6 Driver BRa Service",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_6 Driver C Service",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_6 Driver P Service",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_6 Driver PP Service",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_6 Driver Pros Service",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_9 Driver BRa Trst",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_9 Driver BRg Trst",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_9 Driver C Trst",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_9 Driver P Trst",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_9 Driver PP Trst",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC BI_9 Driver Pros Trst",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Committed Customers",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Con Comp P",
				"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Con Comp Pa",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Con Comp Pg",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Con Sky Prospect",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Con Sky Pure",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Customers At Risk",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Desire Comp P",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Desire Comp Pa",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Desire Comp Pg",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Desire Sky Prospect",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Desire Sky Pure",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC GI_1 Driver BRg bstBB",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC GI_2 Driver BRg cmpWM",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC GuildPartner Game",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC MainProvider Game",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC PmtAware Esprt",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC PmtAware Game",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Spon Aware Comp P",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Spon Aware Comp Pa",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Spon Aware Comp Pg",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Spon Aware Sky Prospect",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC Spon Aware Sky Pure",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_1st",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_1st Comp",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_1st CompIM",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_1st P",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_AVGNUM",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_AVGNUM_Comp",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_AVGNUM_CompIM",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_AVGNUM_P",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_Top3 Comp",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_Top3 CompIM",
				//"Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC SponAware_Top3 P"
			]
		};

		string id = await client.MultiPlatinumStart(request);
		Trace($"Platinum batch Id = {id}");

		MultiPlatinumResponse? response = null;
		bool done = false;
		while (!done)
		{
			await Task.Delay(10000);
			response = await client.MultiPlatinumQuery(id);
			Trace($"{response.WaitingReportCount} {response.RunningReportCount} {response.CompletedReportCount} {response.FailedReportCount}");
			done = response.AllReportsCompleted;
		}

		Trace($"Started {response!.StartTime:s} ({response!.ElapsedSeconds:F1}) - Used cores {response.ParallelMax}");
		foreach (var report in response.Reports!)
		{
			if (report.IsFailed)
			{
				Trace($"|  {report.Name} • {report.StartTime:s} ({report.ElapsedSeconds:F1}) {report.FailureType} -> {report.FailureMessages?.FirstOrDefault()}");
			}
			else
			{
				if (report.BlobUri != null)
				{
					Trace($"|  {report.Name} • {report.StartTime:s} ({report.ElapsedSeconds:F1}) - {report.BlobUri}");
				}
				else
				{
					Trace($"|  {report.Name} • {report.StartTime:s} ({report.ElapsedSeconds:F1}) - {report.Data}");
				}
				string filename = MakeTempFile(SafeName(report.Name)) + ".json";
				if (report.Data != null)
				{
					string json = JsonSerializer.Serialize(report.Data, Jopts);
					File.WriteAllText(filename, json);
				}
			}
		}

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