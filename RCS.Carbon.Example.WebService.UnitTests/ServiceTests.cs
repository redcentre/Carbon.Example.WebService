using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using RCS.Carbon.Example.WebService.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orthogonal.Common.Basic;
using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.UnitTests
{
	[TestClass]
	public class ServiceTests : TestBase
	{
		readonly XOutputFormat[] AllFormats = new[]
		{
				XOutputFormat.None,
				XOutputFormat.TSV,
				XOutputFormat.CSV,
				XOutputFormat.SSV,
				XOutputFormat.XLSX,
				XOutputFormat.XML,
				XOutputFormat.HTML,
				XOutputFormat.OXT,
				XOutputFormat.OXTNums,
				XOutputFormat.Diamond,
				XOutputFormat.MultiCube,
				XOutputFormat.Pandas
		};

		[TestMethod]
		public async Task T010_Service_Info()
		{
			using var client = MakeClient();
			var info = await client.GetServiceInfo();
			Dumpobj(info);
		}

		[TestMethod]
		public async Task T020_Mock_Error()
		{
			using var client = MakeClient();
			var ex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.MockError(123));
			Trace(ex.Message);
			Assert.AreEqual("This is a fake error for request number 123", ex.Message);
		}

		[TestMethod]
		public async Task T030_Bad_Service_Uri()
		{
			using var client = new CarbonServiceClient("http://notexist.com/carbon");
			var ex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.GetServiceInfo());
			Trace(ex.Message);
			Assert.AreEqual("The GET response from 'http://notexist.com/carbon/service/info' status NotFound is not in a recognised format. The address may be incorrect or the service is faulting.", ex.Message);
		}

		[TestMethod]
		public async Task T040_Job_GenTab_All_Formats()
		{
			using var client = MakeClient();
			var sinfo = await GuardedSession(userId, userPass, client);

			var resp = await client.OpenCloudJob(custName, jobName);
			Trace($"Open job → {resp}");

			var sprops = new XSpecProperties();
			var dprops = new XDisplayProperties();
			foreach (var format in AllFormats)
			{
				Sep1($"GenTab {format} ({(int)format})");
				dprops.Output.Format = format;
				try
				{
					string[] lines = await client.GenTab($"GenTab-{format}", genTop, genSide, null, null, sprops, dprops);
					DumpLines(lines);
				}
				catch (Exception ex)
				{
					Trace($"ERROR: {ex.GetType().Name} - {ex.Message}");
				}
			}
			bool closed = await client.CloseJob();
			Trace($"Closed job → {closed}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}

		[TestMethod]
		public async Task T100_Report_GenTab_All_Formats()
		{
			using var client = MakeClient();
			var sinfo = await GuardedSession(userId, userPass, client);
			var resp = await client.OpenCloudJob(custName, jobName);
			Trace($"Open job → {resp}");

			var sprops = new XSpecProperties();
			var dprops = new XDisplayProperties();
			var formats = new XOutputFormat[]
			{
				XOutputFormat.None,
				XOutputFormat.TSV,
				XOutputFormat.CSV,
				XOutputFormat.SSV,
				XOutputFormat.XLSX,
				XOutputFormat.XML,
				XOutputFormat.HTML,
				XOutputFormat.OXT,
				XOutputFormat.OXTNums,
				XOutputFormat.Diamond,
				XOutputFormat.MultiCube,
				XOutputFormat.Pandas
			};
			foreach (var format in formats)
			{
				Sep1($"GenTab {format} ({(int)format})");
				dprops.Output.Format = format;
				try
				{
					string report = await client.ReportGenTabText(format, $"Report-{format}", genTop, genSide, null, null, sprops, dprops);
					string s = NiceFormatter.Sample(report, 200);
					Trace(s);
				}
				catch (Exception ex)
				{
					Trace($"ERROR: {ex.GetType().Name} - {ex.Message}");
				}
			}
			bool closed = await client.CloseJob();
			Trace($"Closed job → {closed}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}

		[TestMethod]
		public async Task T120_Report_GenTab_XML()
		{
			using var client = MakeClient();
			var sinfo = await GuardedSession(userId, userPass, client);
			var resp = await client.OpenCloudJob(custName, jobName);
			Trace($"Open job → {resp}");

			var sprops = new XSpecProperties();
			var dprops = new XDisplayProperties();
			string xml = await client.ReportGenTabText(XOutputFormat.XML, $"Report-XML", genTop, genSide, null, null, sprops, dprops);
			Trace(xml);
			bool closed = await client.CloseJob();
			Trace($"Closed job → {closed}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}

		[TestMethod]
		public async Task T140_Report_GenTab_Excel_Blob()
		{
			using var client = MakeClient();
			var sinfo = await GuardedSession(userId, userPass, client);
			var resp = await client.OpenCloudJob(custName, jobName);
			Trace($"Open job → {resp}");

			var sprops = new XSpecProperties();
			var dprops = new XDisplayProperties();
			XlsxResponse xresp = await client.ReportGenTabExcelBlob($"Report-Excel-Blob", genTop, genSide, null, null, sprops, dprops);
			Dumpobj(xresp);
			string filename = Path.GetFileName(xresp.ExcelUri);
			using (var http = new HttpClient())
			{
				using (var stream = await http.GetStreamAsync(xresp.ExcelUri))
				{
					string oname = Path.Combine(Path.GetTempPath(), filename);
					using (var output = new FileStream(oname, FileMode.Create))
					{
						await stream.CopyToAsync(output);
					}
					Process.Start(new ProcessStartInfo(oname) { UseShellExecute = true });
				}
			}
			bool closed = await client.CloseJob();
			Trace($"Closed job → {closed}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}

		[TestMethod]
		public async Task T200_Report_GenTab_Pandas()
		{
			using var client = MakeClient();
			var sinfo = await GuardedSession(userId, userPass, client);
			var resp = await client.OpenCloudJob(custName, jobName);
			Trace($"Open job → {resp}");

			var sprops = new XSpecProperties();
			var dprops = new XDisplayProperties();
			for (int i = 1; i <= 3; i++)
			{
				Sep1($"Pandas {i}");
				string json = await client.ReportGenTabPandas(i, $"Report-Pandas", genTop, genSide, null, null, sprops, dprops);
				Trace(json);
			}
			bool closed = await client.CloseJob();
			Trace($"Closed job → {closed}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}
	}
}