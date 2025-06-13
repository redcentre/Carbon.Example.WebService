using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RCS.Carbon.Example.WebService.Common;
using RCS.Carbon.Example.WebService.Common.DTO;

namespace RCS.Carbon.Example.WebService.UnitTests;

[TestClass]
public class SessionTests : TestBase
{
	[TestMethod]
	public async Task T010_SessionId_NoUser()
	{
		using var client = MakeClient();
		var pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.StartSessionId("NOUSER", "BADPASS"));
		Trace($"ex Message ........ {pex.Message}");
		Trace($"ex Code ........... {pex.Code}");
	}

	[TestMethod]
	public async Task T020_SessionId_BadPass()
	{
		using var client = MakeClient();
		var pex = await Assert.ThrowsExceptionAsync<CarbonServiceException>(() => client.StartSessionId(userId, "BADPASS"));
		Trace($"ex Message ........ {pex.Message}");
		Trace($"ex Code ........... {pex.Code}");
	}

	[TestMethod]
	public async Task T030_SessionId_Out()
	{
		using var client = MakeClient();
		SessionInfo sinfo = await GuardedSession(userId, userPass, client);
		Trace($"Session → {sinfo}");
		DumpSessinfo(sinfo);
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	[TestMethod]
	public async Task T040_SessionName_Out()
	{
		using var client = MakeClient();
		SessionInfo sinfo = await client.StartSessionName(userName, userPass);
		Trace($"Session → {sinfo}");
		Dumpobj(sinfo);
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}

	[TestMethod]
	public async Task T050_OpenJob()
	{
		using var client = MakeClient();
		SessionInfo sinfo = await client.StartSessionName(userName, userPass);
		Trace($"Session → {sinfo}");
		OpenCloudJobResponse jobresp = await client.OpenCloudJob(custName, jobName, null, true, true, true, JobTocType.ExecUser, true);
		Trace($"OpenCloudJob {jobresp.DProps} {jobresp.DrillFilters} {jobresp.VartreeNames} {jobresp.AxisTreeNames}");
		bool ended = await client.EndSession();
		Trace($"EndSession → {ended}");
	}
}