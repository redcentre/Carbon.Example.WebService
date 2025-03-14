using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Shared;

namespace Carbon.Examples.WebService.UnitTests
{
	[TestClass]
	public class SpecTests : TestBase
	{
		[TestMethod]
		public async Task T100_GetNewSpec()
		{
			using var client = MakeClient();
			await client.StartSessionId(TestAccountId, TestAccountPassword);
			OpenCloudJobResponse jobresp = await client.OpenCloudJob(CustomerName1, JobName1, null, tocType: JobTocType.ExecUser);
			Sep1("Validate ctor spec");
			var ts = new TableSpec();
			Dumpobj(ts);
			GenericResponse gn = await client.ValidateSpec(ts);
			Dumpobj(gn);
			Sep1("Validate new spec");
			var spec = await client.GetNewSpec();
			Dumpobj(spec);
			gn = await client.ValidateSpec(spec.Spec);
			Dumpobj(gn);
			bool closed = await client.CloseJob();
			Trace($"Closed → {closed}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}

		[TestMethod]
		public async Task T120_GetEditSpec()
		{
			using var client = MakeClient();
			await client.StartSessionId(TestAccountId, TestAccountPassword);
			OpenCloudJobResponse jobresp = await client.OpenCloudJob(CustomerName1, JobName1, null, tocType: JobTocType.ExecUser);
			var loadreq = new LoadReportRequest("Tables/Exec/FolderA/AgeReg");
			var gr = await client.LoadReport(loadreq);
			Dumpobj(gr);
			SpecAggregate sa = await client.GetEditSpec();
			Dumpobj(sa);
			bool closed = await client.CloseJob();
			Trace($"Closed → {closed}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}
	}
}