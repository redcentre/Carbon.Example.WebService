using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.UnitTests;

[TestClass]
public class StressTests : TestBase
{
	[TestMethod]
	public async Task T100_Session_Story()
	{
		var rand = new Random();
		int i = rand.Next(1000, 5000);
		Trace($"DELAY START {i}");
		await Task.Delay(i);
		async Task RandWait() => await Task.Delay(rand.Next(500, 1500));
		using var client = MakeClient();

		await RandWait();
		Sep1("Start Session");
		var sessinfo = await GuardedSession(userId, userPass, client);
		Assert.IsNotNull(sessinfo);
		Trace($"Session Id → {sessinfo.SessionId}");

		await RandWait();
		Sep1("List Sessions");
		var sesslist = await client.ListSessions();
		Trace($"Session list count → {sesslist.Length}");
		var sess = sesslist.First(x => x.SessionId == sessinfo.SessionId);
		Dumpobj(sess);

		await RandWait();
		Sep1("Service Info");
		var info = await client.GetServiceInfo();
		Dumpobj(info);

		await RandWait();
		Sep1("Open Cloud Job");
		var resp = await client.OpenCloudJob(custName, jobName);
		Trace($"Open job → {resp}");

		await RandWait();
		Sep1("Vartree List");
		string[] vtlist = await client.ListVartrees();
		Assert.IsNotNull(vtlist);
		Dumpobj(vtlist);
		string? vtname = vtlist.FirstOrDefault(v => string.Compare(v, "vartree", true) == 0) ?? vtlist.FirstOrDefault();
		Trace($"Use vartree name → {vtname}");

		await RandWait();
		Sep1("Vartree GenNode");
		if (vtlist.Length == 0)
		{
			Trace("SKIP VARTREE PNODES - NO VARTREES");
		}
		else
		{
			await client.SetVartreeName(vtname!);
			GenNode[] vtnodes = await client.VariableTreeAsNodes();
			foreach (GenNode node in GenNode.WalkNodes(vtnodes).Take(30))
			{
				string pfx = "".PadRight(2 * node.Level);
				Trace($"{pfx}{node}");
			}
		}

		await RandWait();
		Sep1("VarAsNodes Nodes (Simple)");
		GenNode[] nodes = await client.VarAsNodes(genTop);
		foreach (string line in GenNode.PrintTree(nodes))
		{
			Trace(line);
		}

		await RandWait();
		Sep1("VarAsNodes Nodes (Hierarchic)");
		nodes = await client.VarAsNodes("BIM");
		foreach (string line in GenNode.PrintTree(nodes))
		{
			Trace(line);
		}

		await RandWait();
		Sep1("GenTab OXT");
		var dprops = new XDisplayProperties();
		dprops.Output.Format = XOutputFormat.OXT;
		var sprops = new XSpecProperties();
		string[] lines = await client.GenTab(null, genTop, genSide, null, null, sprops, dprops);
		Dumpobj(lines.Take(20));

		await RandWait();
		Sep1("Return Session");
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	[TestMethod]
	public async Task T120_Session_Story_Stress()
	{
		var tasks = Enumerable.Range(0, 4).Select(n => T100_Session_Story()).ToArray();
		await Task.WhenAll(tasks);
	}

	[TestMethod]
	public async Task T200_Many_Reports()
	{
		var rand = new Random();
		using var client = MakeClient();
		Sep1("Start Session");
		async Task Show(XOutputFormat format, string top, string side)
		{
			int ms = rand.Next(1000, 5000);
			await Task.Delay(ms);
			var dprops = new XDisplayProperties();
			dprops.Output.Format = format;
			var sprops = new XSpecProperties();
			Sep1($"{format} | {top} | {side} ({ms}ms)");
			string[] lines = await client.GenTab(null, top, side, null, null, sprops, dprops);
			foreach (string s in lines.Take(10)) Trace(s);
		}
		var sessinfo = await GuardedSession(userId, userPass, client);
		await client.OpenCloudJob(custName, jobName);
		await Show(XOutputFormat.OXT, genTop, genSide);
		await Show(XOutputFormat.CSV, genTop, "Occupation");
		await Show(XOutputFormat.TSV, "Gender", genSide);
		await client.OpenCloudJob("rcspublic", "firstfleet");
		await Show(XOutputFormat.OXT, "Age7", "Crime11");
		await Show(XOutputFormat.OXT, "Occ15", "TrialYear");
		await Show(XOutputFormat.OXT, "Case", "CrimeValue21");
		Sep1("Return Session");
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	[TestMethod]
	public async Task T210_Many_Reports_Stress()
	{
		var tasks = Enumerable.Range(0, 6).Select(n => T200_Many_Reports()).ToArray();
		await Task.WhenAll(tasks);
	}
}