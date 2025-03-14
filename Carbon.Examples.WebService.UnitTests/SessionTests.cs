using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Carbon.Examples.WebService.Common;

namespace Carbon.Examples.WebService.UnitTests
{
    [TestClass]
	public class SessionTests : TestBase
	{
		[TestMethod]
		public async Task T010_LoginId_NoUser()
		{
			using var client = MakeClient();
			var pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.StartSessionId("NOUSER", "BADPASS"));
			Trace($"ex Message ........ {pex.Message}");
			Trace($"ex Code ........... {pex.Code}");
		}

		[TestMethod]
		public async Task T020_LoginId_BadPass()
		{
			using var client = MakeClient();
			var pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.StartSessionId(TestAccountId, "BADPASS"));
			Trace($"ex Message ........ {pex.Message}");
			Trace($"ex Code ........... {pex.Code}");
		}

		[TestMethod]
		public async Task T030_LoginId_Out()
		{
			using var client = MakeClient();
			SessionInfo sinfo = await client.StartSessionName("GregK", "Cats4Sleeping");
			Trace($"Login → {sinfo}");
			DumpSessinfo(sinfo);
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
			//Assert.IsTrue(count == (sinfo.LoginCount - 1));
		}

		[TestMethod]
		public async Task T040_LoginName_Out()
		{
			using var client = MakeClient();
			SessionInfo sinfo = await client.StartSessionName(TestAccountName, TestAccountPassword);
			Trace($"Login → {sinfo}");
			Dumpobj(sinfo);
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}

		[TestMethod]
		public async Task T050_OpenJob()
		{
			using var client = MakeClient();
			SessionInfo sinfo = await client.StartSessionName(TestAccountName, TestAccountPassword);
			Trace($"Login → {sinfo}");
			OpenCloudJobResponse jobresp = await client.OpenCloudJob("rcsruby", "demo", null, true, true, true, JobTocType.ExecUser, true);
			Trace($"OpenCloudJob {jobresp.DProps} {jobresp.DrillFilters} {jobresp.VartreeNames} {jobresp.AxisTreeNames}");
			bool ended = await client.EndSession();
			Trace($"EndSession → {ended}");
		}
	}
}