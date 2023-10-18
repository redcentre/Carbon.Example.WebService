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
			var pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.LoginId("NOUSER", "BADPASS"));
			Trace($"ex Message ........ {pex.Message}");
			Trace($"ex Code ........... {pex.Code}");
		}

		[TestMethod]
		public async Task T020_LoginId_BadPass()
		{
			using var client = MakeClient();
			var pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.LoginId(TestAccountId, "BADPASS"));
			Trace($"ex Message ........ {pex.Message}");
			Trace($"ex Code ........... {pex.Code}");
		}

		[TestMethod]
		public async Task T030_LoginId_Out()
		{
			using var client = MakeClient();
			SessionInfo sinfo = await client.AuthenticateName("GregK", "Cats4Sleeping");
			Trace($"Login → {sinfo}");
			DumpSessinfo(sinfo);
			int count = await client.ReturnSession();
			Trace($"Return count → {count}");
			//Assert.IsTrue(count == (sinfo.LoginCount - 1));
		}

		[TestMethod]
		public async Task T040_LoginName_Out()
		{
			using var client = MakeClient();
			SessionInfo sinfo = await client.AuthenticateName(TestAccountName, TestAccountPassword);
			Trace($"Login → {sinfo}");
			int count = await client.LogoffSession();
			Trace($"Logoff count → {count}");
		}

		[TestMethod]
		public async Task T050_OpenJob()
		{
			using var client = MakeClient();
			SessionInfo sinfo = await client.AuthenticateName(TestAccountName, TestAccountPassword);
			Trace($"Login → {sinfo}");
			OpenCloudJobResponse jobresp = await client.OpenCloudJob("rcsruby", "demo", null, true, true, true, JobTocType.ExecUser, true);
			Trace($"OpenCloudJob {jobresp.DProps} {jobresp.DrillFilters} {jobresp.VartreeNames} {jobresp.AxisTreeNames}");
			int count = await client.LogoffSession();
			Trace($"Logoff count → {count}");
		}
	}
}