#define FAILS

using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Shared;
using Carbon.Examples.WebService.Common;

namespace Carbon.Examples.WebService.UnitTests
{
    [TestClass]
    public class Stories : TestBase
    {
        [TestMethod]
        public async Task T100_TOC_Story()
        {
            using var client = MakeClient();
            Sep1("LoginId");
            SessionInfo sessinfo = await client.StartSessionId(TestAccountId, TestAccountPassword);
            //Assert.IsNotNull(sessinfo);
            DumpSessinfoShort(sessinfo);

            Sep1("OpenCloudJob");
            OpenCloudJobResponse openresp = await client.OpenCloudJob(CustomerName1, JobName1, null, true, true, true, JobTocType.ExecUser, true);
            GenNode[] tocflat = GenNode.WalkNodes(openresp.Toc).ToArray();
            Trace($"TOC roots {openresp.Toc.Length} total {tocflat.Length} • Vartree {openresp.VartreeNames.Length} • Axis {openresp.AxisTreeNames?.Length} • Drill {openresp.DrillFilters.Length} • DProps format {openresp.DProps.Output.Format}");

            GenNode GetUserNode() => GenNode.WalkNodes(openresp.Toc).First(n => n.Value1 == sessinfo.Name);
            void DumpUserToc() => DumpToc(new GenNode[] { GetUserNode() });

            Sep1("Original TOC");
            DumpUserToc();

/*
    0- Age x Region=T Tables/User/greg
    0- Junk-1=T Tables/User/greg
    0- SubFolderB=F Tables/User/greg
    1- Gender x Occupation=T Tables/User/greg/SubFolderB
    0- Folder Two=F Tables/User/greg
    1- Gender x Junk=T Tables/User/greg/Folder Two
    1- Gender x Occupation=T Tables/User/greg/Folder Two
    1- Sub3=F Tables/User/greg/Folder Two
    2- Report3=T Tables/User/greg/Folder Two/Sub3
*/

            //Sep1("Delete not found");
            //GenericResponse gr1 = await client.DeleteInToc("Tables/User/greg/NOT FOUND.cbt");
            //Dumpobj(gr1);

            //Sep1("Delete Occupation");
            //GenericResponse gr2 = await client.DeleteInUserToc("Tables/User/greg/Folder Two/Gender x Occupation.cbt");
            //Dumpobj(gr2);

            //Sep1("Delete Junk");
            //GenericResponse gr3 = await client.DeleteInUserToc("Tables/User/greg/Folder Two/Gender x Junk.cbt");
            //Dumpobj(gr3);

            //Sep1("Delete Report3");
            //GenericResponse gr4 = await client.DeleteInUserToc("Tables/User/greg/Folder Two/Sub3/Report3.cbt");
            //Dumpobj(gr4);

            //Sep1("Delete Sub3");
            //GenericResponse gr5 = await client.DeleteInUserToc("Tables/User/greg/Folder Two/Sub3");
            //Dumpobj(gr5);

            Sep1("Delete Folder Two");
            GenericResponse gr6 = await client.DeleteInUserToc("Tables/User/greg/Folder Two");
            Dumpobj(gr6);

            //GenNode unode = GetUserNode();
            //GenNode[] uflat = GenNode.WalkNodes(new GenNode[] { unode }).ToArray();
            //var delnode = uflat.FirstOrDefault(n => n.Value1 == "Folder Two");
            //if (delnode != null)
            //{
            //    gr = await client.DeleteInToc($"{delnode.Value2}/{delnode.Value1}");
            //    Dumpobj(gr);
            //}

            Sep1("After TOC");
            GenNode[] tocafter = await client.ListExecUserToc(true);
            DumpToc(tocafter);

            Sep1("CloseJob");
            bool ended = await client.CloseJob();
            Assert.IsTrue(ended);
            Trace($"ReturnClose job → {ended}");

            Sep1("ReturnSession");
			ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}

		[TestMethod]
        public async Task T200_Big_Story()
        {
            CarbonServiceException pex;
            ArgumentNullException anex;
            OpenCloudJobResponse resp;

            using var client = MakeClient();
#if FAILS
            Sep1("Bad Id");
            pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.StartSessionId("NOUSER", "BADPASS"));
            Trace(pex.Message);
            Assert.AreEqual("User Id 'NOUSER' not found", pex.Message);

            Sep1("Null Id");
            anex = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.StartSessionId(null, null));
            Trace(anex.Message);
            Assert.AreEqual("Value cannot be null. (Parameter 'id')", anex.Message);

            Sep1("Null Pass");
            anex = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.StartSessionId(TestAccountId, null));
            Trace(anex.Message);
            Assert.AreEqual("Value cannot be null. (Parameter 'password')", anex.Message);

            Sep1("Bad Id");
            pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.StartSessionId(TestAccountId, "BADPASS"));
            Trace(pex.Message);
            Assert.AreEqual("User '" + TestAccountName + "' Id '" + TestAccountId + "' incorrect password", pex.Message);
#endif
            Sep1("LoginId");
            SessionInfo sessinfo = await client.StartSessionId(TestAccountId, TestAccountPassword);
            Assert.IsNotNull(sessinfo);
            DumpSessinfo(sessinfo);

            Sep1("List Sessions");
            var sesslist = await client.ListSessions();
            Assert.IsTrue(sesslist.Length > 0);
            Trace($"Session list count → {sesslist.Length}");
            var sess = sesslist.First(x => x.SessionId == sessinfo.SessionId);
            Assert.IsNotNull(sess);
            Dumpobj(sess);
#if FAILS
            Sep1("OpenCloudJob null cust");
            anex = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.OpenCloudJob(null, null));
            Trace(anex.Message);
            Assert.AreEqual("Value cannot be null. (Parameter 'customerName')", anex.Message);

            Sep1("OpenCloudJob null job");
            anex = await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => client.OpenCloudJob(CustomerName1, null));
            Trace(anex.Message);
            Assert.AreEqual("Value cannot be null. (Parameter 'jobName')", anex.Message);

            Sep1("OpenCloudJob blank cust");
            pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.OpenCloudJob("", JobName1));
            Trace(pex.Message);
            Assert.AreEqual("Open cloud Customer '' Job 'demo' Vartree '' failed - Customer '' is not a registered storage account", pex.Message);

            Sep1("OpenCloudJob blank job");
            pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.OpenCloudJob(CustomerName1, ""));
            Trace(pex.Message);
            Assert.AreEqual("Open cloud Customer 'client1rcs' Job '' Vartree '' failed - Job '' is not accessible in customer 'client1rcs'.", pex.Message);
#endif
            Sep1("OpenCloudJob");
            resp = await client.OpenCloudJob(CustomerName1, JobName1, null, true, true, true, JobTocType.ExecUser, true);
            Assert.IsNotNull(resp);
            Assert.IsNotNull(resp.Toc);
            Assert.IsNotNull(resp.DProps);
            Assert.IsNotNull(resp.VartreeNames);
            //Assert.IsNotNull(resp.AxisTreeNames);		// NOT SURE WHERE THESE COME FROM YET
            Assert.IsNotNull(resp.DrillFilters);
            Trace($"TOC {resp.Toc.Length} • Vartree {resp.VartreeNames.Length} • Axis {resp.AxisTreeNames?.Length} • Drill {resp.DrillFilters.Length} • DProps format {resp.DProps.Output.Format}");

            Sep1("Vartree List");
            string[] vtnames = await client.ListVartrees();
            Assert.IsTrue(vtnames.Length > 0);
            Dumpobj(vtnames);

            //Sep1("Vartree not found");
            //var vnodes = await client.VartreeAsNodes("NOVARTREE");
            //Assert.IsNull(vnodes);

            //Sep1("Vartree");
            //GenNode[] vtroots = await client.VartreeAsNodes("VarTree");
            //Assert.IsTrue(vtroots.Length > 0);
            //Trace($"Vartree root nodes → {vtroots.Length}");
            //DumpNodes(vtroots, 12);
            //Assert.AreEqual("Case", vtroots[0].Children[0].Value1);

            //Sep1("Axis Tree List");
            //string[] axnames = await client.ListAxisTrees();
            //Assert.IsTrue(axnames.Length > 0);
            //Dumpobj(axnames);
#if FAILS
            Sep1("Axis not found");
            //pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.AxisTreeAsNodes("NOAXIS"));
            //Trace(pex.Message);
            //Assert.AreEqual("Blob 'NOAXIS.atr' does not exist", pex.Message);
#endif
            //Sep1("Axis Tree");
            //GenNode[] axroots = await client.AxisTreeAsNodes("Test");
            //Assert.IsTrue(axroots.Length > 0);
            //Trace($"Axis tree root nodes → {axroots.Length}");
            //DumpNodes(axroots, 12);
            //Assert.AreEqual("Banner1", axroots[0].Children[0].Name);
#if FAILS
            Sep1("Axis not found");
            //pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.GetVarMeta("NOVAR"));
            //Trace(pex.Message);
            //Assert.AreEqual(@"Blob 'CaseData\novar.met' does not exist", pex.Message);
#endif
            //Sep1("Varmeta Age");
            //VarMeta vmage = await client.GetVarMeta("Age");
            //Assert.IsNotNull(vmage);
            //foreach (var meta in vmage.Metadata) Trace($"META {meta.Key}={meta.Value}");
            //Trace($"Age root nodes → {vmage.Nodes.Length}");
            //DumpNodes(vmage.Nodes, 12);
            //Assert.AreEqual("15-25", vmage.Nodes[0].Children[0].Description);

            //Sep1("Varmeta BIM");
            //VarMeta vmbim = await client.GetVarMeta("BIM");
            //Assert.IsNotNull(vmbim);
            //Assert.IsNotNull(vmbim.Metadata);
            //Assert.IsNotNull(vmbim.Nodes);
            //foreach (var meta in vmage.Metadata) Trace($"META {meta.Key}={meta.Value}");
            //Trace($"BIM root nodes → {vmbim.Nodes.Length}");
            //DumpNodes(vmbim.Nodes, 12);
            //Assert.AreEqual("BrandX", vmbim.Nodes[0].Children[0].Description);

            Sep1("TOC Simple");
            GenNode[] tocroots = await client.ListSimpleToc(true);
            Trace($"TOC Simple root nodes → {tocroots.Length}");
            DumpNodes(tocroots, 12);

            Sep1("TOC ExecUser");
            tocroots = await client.ListExecUserToc(true);
            Trace($"TOC ExecUser root nodes → {tocroots.Length}");
            DumpNodes(tocroots, 12);

            Sep1("TOC Full");
            tocroots = await client.ListFullToc(true);
            Trace($"TOC Full root nodes → {tocroots.Length}");
            DumpNodes(tocroots, 12);

            //Sep1("TOC (legacy)");
            //GenNode[] tocoldroots = await client.ListLegacyToc();
            //Trace($"TOC legacy root nodes → {tocoldroots.Length}");
            //DumpNodes(tocoldroots, 12);
            //var trknode = GenNode.WalkNodes(tocoldroots).FirstOrDefault(n => n.Type == "Table");
            //Assert.IsNotNull(trknode);
            //string name = $"{trknode.Description}/{trknode.Name}.rpt";
            //string[] trklines = await client.ReadFileAsLines(name);
            //Assert.IsNotNull(trklines);
            //Sep1($"Dump {name} ({trklines.Length})");
            //DumpLines(trklines, 12);


            Sep1("GenTab Age x Region TSV (default)");
            var sprops = new XSpecProperties();
            var dprops = new XDisplayProperties();
            dprops.Output.Format = XOutputFormat.TSV;
#if FAILS
            //string[] linesx = await client.GenTab(null, "FOO", "BAR", null, null, sprops, dprops);
            // Weird crash send to RS
#endif
            string[] lines = await client.GenTab(null, Top1, Side1, null, null, sprops, dprops);
            DumpLines(lines);
            Assert.IsTrue(lines[3].StartsWith("\t15-25"));
            Assert.IsTrue(lines[4].StartsWith("NE\t479"));
            Assert.IsTrue(lines[5].StartsWith("\t24.82%"));
            Assert.IsTrue(lines[6].StartsWith("\t18.99%"));
            var dprops2 = await client.GetProps();
            Assert.AreEqual(XOutputFormat.TSV, dprops2.Output.Format);
            Assert.IsTrue(dprops2.Cells.RowPercents.Visible);
            Assert.IsTrue(dprops2.Cells.ColumnPercents.Visible);

            Sep1("GenTab Age x Region TSV (freq only)");
            dprops.Cells.RowPercents.Visible = false;
            dprops.Cells.ColumnPercents.Visible = false;
            lines = await client.GenTab(null, Top1, Side1, null, null, sprops, dprops);
            DumpLines(lines);
            Assert.IsTrue(lines[3].StartsWith("\t15-25"));
            Assert.IsTrue(lines[4].StartsWith("NE\t479"));
            Assert.IsTrue(lines[5].StartsWith("SE\t459"));
            Assert.IsTrue(lines[6].StartsWith("SW\t523"));
            dprops2 = await client.GetProps();
            Assert.AreEqual(XOutputFormat.TSV, dprops2.Output.Format);
            Assert.IsFalse(dprops2.Cells.RowPercents.Visible);
            Assert.IsFalse(dprops2.Cells.ColumnPercents.Visible);

            //Sep1("Reformat as TSV");
            //dprops2.Columns.Groups.Visible = false;
            //dprops2.Columns.Labels.Visible = true;
            //dprops2.Rows.Groups.Visible = false;
            //dprops2.Rows.Labels.Visible = true;
            //dprops2.Output.Format = XOutputFormat.SSV;
            //lines = await client.ReformatTable(dprops2);
            //DumpLines(lines);
            //Assert.IsTrue(Regex.IsMatch(lines[3], @"^\s+\+------\+"));
            //Assert.IsTrue(Regex.IsMatch(lines[4], @"^\s+\|15-25 \|"));
            //Assert.IsTrue(Regex.IsMatch(lines[5], @"^\s+\|      \|"));
            //Assert.IsTrue(Regex.IsMatch(lines[6], @"^-+\+------\+"));
            //Assert.IsTrue(Regex.IsMatch(lines[7], @"^NE\s+\|479   \|"));

            Sep1("Pandas input");
            var postdata = new
            {
                top = new string[] { "Female", "Male", "Male", "Male", "Male", "Female", "Female", "Male", "Male", "Female" },
                side = new double[] { 30, 64, 30, 18, 30, 64, 30, 79, 64, 19 }
            };
            string json = JsonSerializer.Serialize(postdata);
            Trace(json);
            Sep1("Pandas output");
            string pandas = await client.PandasAlphacodes(json);
            Trace(NiceJson(pandas));

            Sep1("CloseJob");
            bool ended = await client.CloseJob();
            Assert.IsTrue(ended);
            Trace($"ReturnClose job → {ended}");

            Sep1("ReturnSession");
			ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}
	}
}