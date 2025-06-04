using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RCS.Carbon.Example.WebService.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Shared;
using RCS.Carbon.Example.WebService.Common.DTO;

namespace RCS.Carbon.Example.WebService.UnitTests
{
	[TestClass]
	public class SpecTests : TestBase
	{
		[TestMethod]
		public async Task T100_GetNewSpec()
		{
			using var client = MakeClient();
			await GuardedSession(userId, userPass, client);
			OpenCloudJobResponse jobresp = await client.OpenCloudJob(custName, jobName, null, tocType: JobTocType.ExecUser);
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
			await GuardedSession(userId, userPass, client);
			OpenCloudJobResponse jobresp = await client.OpenCloudJob(custName, jobName, null, tocType: JobTocType.ExecUser);
			var loadreq = new LoadReportRequest(report);
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