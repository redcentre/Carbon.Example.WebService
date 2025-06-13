using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Azure.Data.Common;
using RCS.Carbon.Example.WebService.Common.DTO;

namespace RCS.Carbon.Example.WebService.UnitTests;

[TestClass]
public class DashboardTests : TestBase
{
	[TestMethod]
	public async Task T010_MultiOxt_Sync()
	{
		Sep1("AuthenticateName");
		using var client = MakeClient();
		SessionInfo sinfo = await GuardedSession(userName, userPass, client, false);
		Trace($"StartSessionName → {sinfo}");

		Sep1("OpenCloudJob");
		//OpenCloudJobResponse jobinfo = await client.OpenCloudJob(custName, "skyuk");
		OpenCloudJobResponse jobinfo = await client.OpenCloudJob(custName, jobName);

		Sep1("Before cache");
		var info = await client.GetCacheInfo(200);
		Dumpobj(info);

		Sep1("MultiOxt");
		//var multireq = new MultiOxtRequest()
		//{
		//	Filters = new FilterPair[] { new FilterPair("MASTER FILTER", "BB_Master(1)", false), new FilterPair("Age Groups", "BB_Age(3)", false) },
		//	ReportNames = BB_Chart_Tables_ReportNames,
		//	TableOnly = true
		//};
		var multireq = new MultiOxtRequest()
		{
			Filters = [new FilterPair("Quarter", null, true), new FilterPair("Quarter", null, true), new FilterPair("Custom", null, false), new FilterPair("Male", "GEN(1)", false)],
			ReportNames = KPIReportNames,
			TableOnly = true
		};
		Dumpobj(multireq);
		MultiOxtResponse multiresp = await client.MultiOxt(multireq);
		DumpMultiOxtResponse(multiresp);

		Sep1("After cache");
		info = await client.GetCacheInfo(200);
		Dumpobj(info);

		await client.CloseJob();
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	[TestMethod]
	public async Task T020_MultiOxt_Start()
	{
		using var client = MakeClient();
		Sep1("Before cache");
		var info = await client.GetCacheInfo(200);
		Dumpobj(info);
		Sep1("AuthenticateName");
		SessionInfo sinfo = await GuardedSession(userName, userPass, client, false);
		Trace($"StartSessionName → {sinfo}");

		Sep1("OpenCloudJob");
		OpenCloudJobResponse jobinfo = await client.OpenCloudJob(custName, jobName);

		Sep1("MultiOxt");
		var multireq = new MultiOxtRequest()
		{
			Filters = new FilterPair[] { new FilterPair("MASTER FILTER", "Age(1)", false), new FilterPair("Age Groups", "Age(2)", false), new FilterPair("Age Groups", "Age(3)", false) },
			ReportNames = BB_Chart_Tables_ReportNames,
			TableOnly = true
		};
		//var multireq = new MultiOxtRequest()
		//{
		//	Filters = new FilterPair[] { new FilterPair("Quarter", null, true), new FilterPair("Quarter", null, true), new FilterPair("Custom", null, false), new FilterPair("Male", "GEN(1)", false) },
		//	ReportNames = KPIReportNames,
		//	TableOnly = true
		//};
		Dumpobj(multireq);
		Guid id = await client.MultiOxtStart(multireq);
		MultiOxtResponse? resp = null;
		while (resp?.Items == null)
		{
			await Task.Delay(5000);
			resp = await client.MultiOxtQuery(id);
			Trace($"POLL {resp.ProgressMessage}");
		}
		if (resp.IsCancelled)
		{
			Trace("CANCELLED");
		}
		else
		{
			DumpMultiOxtResponse(resp);
		}

		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");

		Sep1("After cache");
		info = await client.GetCacheInfo(200);
		Dumpobj(info);
	}

	[TestMethod]
	public async Task T024_GetCacheInfo()
	{
		using var client = MakeClient();
		SessionInfo sinfo = await GuardedSession(userName, userPass, client, false);
		Trace($"StartSessionName → {sinfo}");
		var info = await client.GetCacheInfo(100);
		Dumpobj(info);
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	[TestMethod]
	public async Task T030_List_Dashboards()
	{
		using var client = MakeClient();
		SessionInfo sinfo = await GuardedSession(userName, userPass, client, false);
		Trace($"StartSessionName → {sinfo}");
		OpenCloudJobResponse jobinfo = await client.OpenCloudJob(custName, jobName);

		var dashlist = await client.ListDashboards(custName, jobName);
		Trace($"List count → {dashlist.Length}");
		foreach (var dash in dashlist)
		{
			Trace($"| {dash.Name} • {dash.DisplayName} • {dash.Comment}");
		}

		bool closed = await client.CloseJob();
		Trace($"CloseJob → {closed}");
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	[TestMethod]
	public async Task T040_KPI_Upsert()
	{
		using var client = MakeClient();
		SessionInfo sinfo = await GuardedSession(userName, userPass, client, false);
		Trace($"StartSessionName → {sinfo}");
		OpenCloudJobResponse jobinfo = await client.OpenCloudJob(custName, jobName);

		var dashlist = await client.ListDashboards(custName, jobName);
		Trace($"Pre upsert count → {dashlist.Length}");
		foreach (var dash in dashlist)
		{
			Trace($"| {dash.Name} • {dash.DisplayName} • {dash.Comment}");
		}

		string dashfile1 = @"K:\u1\Excel\BigInteger factoring speeds.xlsx";
		FileInfo dashinfo = new FileInfo(dashfile1);
		var azdash = new UpsertDashboardRequest()
		{
			CustomerName = custName,
			JobName = jobName,
			UserName = userName,
			Name = dashfile1,
			DisplayName = "Factoring Speeds",
			CreatedUtc = dashinfo.CreationTimeUtc,
			ModifiedUtc = dashinfo.LastWriteTimeUtc,
			Comment = "This is a comment for the factoring speeds dashboard",
			IsShared = true,
			Buffer = File.ReadAllBytes(dashfile1)
		};
		var updash = await client.UpsertDashboard(azdash);
		updash.Buffer = null;
		Dumpobj(updash);

		dashlist = await client.ListDashboards(custName, jobName);
		Trace($"Post upsert count → {dashlist.Length}");
		Assert.IsTrue(dashlist.Count(d => d.Name == updash.Name) == 1);

		bool closed = await client.CloseJob();
		Trace($"CloseJob → {closed}");
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	static readonly string[] KPIReportNames =
	{
		@"\DashboardSource\Dashboard_KPI\z1_KPI_BrandAwa_Summary",
		@"\DashboardSource\Dashboard_KPI\z2_KPI_BIM",
		@"\DashboardSource\Dashboard_KPI\z3_Funnel_BrandX",
		@"\DashboardSource\Dashboard_KPI\z4_Funnel_BrandY",
		@"\DashboardSource\Dashboard_KPI\z5_Share_BBL",
		@"\DashboardSource\Dashboard_KPI\z6_Seasonal",
		@"\DashboardSource\Dashboard_KPI\z7_RegionalSales",
		@"\DashboardSource\Dashboard_KPI\z8_BrandXProvenRecallMonth",
		@"\DashboardSource\Dashboard_KPI\z9_BrandXProvenRecallQrt"
	};

	static readonly string[] BB_Chart_Tables_ReportNames =
	{
		@"\DashboardSource\Chart Tables\Broadband\BC Spon Aware Comp P",
		@"\DashboardSource\Chart Tables\Broadband\BC Con Comp P",
		@"\DashboardSource\Chart Tables\Broadband\BC Desire Comp P",
		@"\DashboardSource\Chart Tables\Broadband\BC ITP Comp P",
		@"\DashboardSource\Chart Tables\Broadband\BC Spon Aware Sky Prospect",
		@"\DashboardSource\Chart Tables\Broadband\BC Con Sky Prospect",
		@"\DashboardSource\Chart Tables\Broadband\BC Desire Sky Prospect",
		@"\DashboardSource\Chart Tables\Broadband\BC ITP Sky Prospect",
		@"\DashboardSource\Chart Tables\Broadband\BC Spon Aware Sky Pure",
		@"\DashboardSource\Chart Tables\Broadband\BC Con Sky Pure",
		@"\DashboardSource\Chart Tables\Broadband\BC Desire Sky Pure",
		@"\DashboardSource\Chart Tables\Broadband\BC ITP Sky Pure",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_21 Driver PP Reliable",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_19 Driver PP Speed",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_20 Driver PP WiFi",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_4 Driver PP Looks",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_6 Driver PP Service",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_2 Driver PP Leader",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_21 Driver P Reliable",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_19 Driver P Speed",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_20 Driver P WiFi",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_4 Driver P Looks",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_6 Driver P Service",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_2 Driver P Leader",
		@"\DashboardSource\Chart Tables\Broadband\BC Committed Customers",
		@"\DashboardSource\Chart Tables\Broadband\BC Customers At Risk",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_21 Driver C Reliable",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_20 Driver C WiFi",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_19 Driver C Speed",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_4 Driver C Looks",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_6 Driver C Service",
		@"\DashboardSource\Chart Tables\Broadband\BC BI_2 Driver C Leader"
	};
}