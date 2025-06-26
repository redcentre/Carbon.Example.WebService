using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCS.Carbon.Example.WebService.Common.DTO;
using RCS.Carbon.Shared;
using RCS.Licensing.Provider.Shared;
using TAB = RCS.Carbon.Tables;

namespace RCS.Carbon.Example.WebService.WebApi.Controllers;

partial class SessionController
{
	readonly JsonSerializerOptions jsonOpts1 = new() { WriteIndented = true };

	async Task<ActionResult<SessionInfo>> StartSessionFreeImpl(AuthenticateFreeRequest request)
	{
		var engine = new TAB.CrossTabEngine(LicProv);
		LicenceInfo licence = await engine.GetFreeLicence(request.Email, request.SkipCache);
		string sessionId = MakeSessionId();
		SessionManager.StartSession(sessionId, licence);
		var sessinfo = LicToInfo(licence, sessionId);
		string[] state = engine.SaveState();
		SessionManager.SaveState(sessionId, state);
		Logger.LogInformation(103, "Start Free Session {SessionId} Id {LicenceId} Name {LicenceName}", sessionId, licence.Id, licence.Name);
		return Ok(sessinfo);
	}

	async Task<ActionResult<SessionInfo>> StartSessionIdImpl(AuthenticateIdRequest request)
	{
		// Optional single session enforcement for user id.
		if (Config.GetValue<bool>("CarbonApi:EnforceSingleSession"))
		{
			var sessions = SessionManager.FindSessionsForId(request.Id);
			if (sessions.Length > 0)
			{
				string message = sessions.Length == 1 ?
					$"A session for User Id {request.Id} is already active." :
					$"{sessions.Length} sessions for User Id {request.Id} are already active.";
				string[] ids = [.. sessions.Select(s => s.SessionId)];
				return BadRequest(new ErrorResponse(ErrorResponseCode.DuplicateSession, message, "The data property contains a string array of the sessionIds that are already active.", ids));
			}
		}
		// Perform the Carbon engine login and session start for a user id.
		try
		{
			var engine = new TAB.CrossTabEngine(LicProv);
			LicenceInfo licence = await engine.GetLicenceId(request.Id, request.Password, request.SkipCache);
			string sessionId = MakeSessionId();
			SessionManager.StartSession(sessionId, licence);
			var sessinfo = LicToInfo(licence, sessionId);
			string[] state = engine.SaveState();
			SessionManager.SaveState(sessionId, state);
			Logger.LogInformation(100, "Login Session {SessionId} Id {LicenceId} Name {LicenceName}", sessionId, licence.Id, licence.Name);
			return Ok(sessinfo);
		}
		catch (Exception ex)
		{
			// Different providers throw different exception types, so we must catch them all.
			return BadRequest(new ErrorResponse(ErrorResponseCode.GetLicenceIdFailed, ex.Message));
		}
	}

	async Task<ActionResult<SessionInfo>> StartSessionNameImpl(AuthenticateNameRequest request)
	{
		// Optional single session enforcement for user name.
		if (Config.GetValue<bool>("CarbonApi:EnforceSingleSession"))
		{
			var sessions = SessionManager.FindSessionsForName(request.Name);
			if (sessions.Length > 0)
			{
				string message = sessions.Length == 1 ?
					$"A session for User Name {request.Name} is already active." :
					$"{sessions.Length} sessions for User Name {request.Name} are already active.";
				string[] ids = [.. sessions.Select(s => s.SessionId)];
				return BadRequest(new ErrorResponse(ErrorResponseCode.DuplicateSession, message, "The data property contains a string array of the sessionIds that are already active.", ids));
			}
		}
		// Perform the Carbon engine login and session start for a user name.
		try
		{
			var engine = new TAB.CrossTabEngine(LicProv);
			LicenceInfo licence = await engine.GetLicenceName(request.Name, request.Password);
			string sessionId = MakeSessionId();
			SessionManager.StartSession(sessionId, licence);
			var sessinfo = LicToInfo(licence, sessionId);
			string[] state = engine.SaveState();
			SessionManager.SaveState(sessionId, state);
			Logger.LogInformation(102, "Login Session {SessionName} Name {LicenceName}", sessionId, licence.Name);
			return Ok(sessinfo);
		}
		catch (Exception ex)
		{
			// Different providers throw different exception types, so we must catch them all.
			return BadRequest(new ErrorResponse(ErrorResponseCode.GetLicenceNameFailed, ex.Message));
		}
	}

	async Task<ActionResult<int>> ForceSessionsImpl(string idlist)
	{
		string[] ids = idlist.Split(',');
		bool[] flags = [.. ids.Select(id => SessionManager.EndSession(id))];
		int count = flags.Count(f => f);
		long total = ids.Select(id => SessionManager.DeleteState(id)).Sum();
		Logger.LogInformation(104, "Force {IdList} count {Count} bytes {Total}", idlist, count, total);
		return await Task.FromResult(count);
	}

	async Task<ActionResult<bool>> EndSessionImpl(string sessionId)
	{
		bool success = SessionManager.EndSession(sessionId);
		long total = SessionManager.DeleteState(sessionId);
		Logger.LogInformation(110, "End Session {SessionId} {Total}", sessionId, total);
		SessionCleanup();
		return await Task.FromResult(success);
	}

	async Task<ActionResult> ReadSessionImpl([FromRoute] string id)
	{
		SessionItem? session = SessionManager.FindSession(id, false);
		if (session == null)
		{
			return NotFound(new ErrorResponse(ErrorResponseCode.SessionNotFound, $"Session Id {id} not found"));
		}
		var ss = new SessionStatus()
		{
			SessionId = session.SessionId,
			UserId = session.UserId,
			UserName = session.UserName,
			CreatedUtc = session.CreatedUtc,
			LastActivityUtc = session.LastActivityUtc,
			LastActivity = session.LastActivity,
			ActivityCount = session.ActivityCount,
			OpenCustomerName = session.OpenCustomerName,
			OpenJobName = session.OpenJobName,
			OpenReportName = session.OpenReportName,
			OpenVartreeName = session.OpenVartreeName
		};
		await Task.CompletedTask;
		return Ok(ss);
	}

	/// <summary>
	/// There is a 1-in-5 chance that a session cleanup will happen.
	/// </summary>
	int SessionCleanup()
	{
		if (Random.Shared.NextDouble() > 0.2) return -1;
		int days = Config.GetValue<int>("CarbonApi:SessionCleanupDays");
		int count = SessionManager.Cleanup(days);
		if (count == 0)
		{
			Logger.LogInformation(140, "No sessions older than {Days} days to cleanup", days);
		}
		else
		{
			Logger.LogInformation(141, "Cleaned {Count} sessions older than {Days} days", count, days);
		}
		return count;
	}

	async Task<ActionResult<SessionStatus[]>> ListSessionsImpl()
	{
		var list = SessionManager.ListSessions().Select(s => new SessionStatus()
		{
			SessionId = s.Item1,
			ActivityCount = s.Item2.ActivityCount,
			CreatedUtc = s.Item2.CreatedUtc,
			LastActivity = s.Item2.LastActivity,
			LastActivityUtc = s.Item2.LastActivityUtc,
			UserId = s.Item2.UserId,
			UserName = s.Item2.UserName,
			OpenCustomerName = s.Item2.OpenCustomerName,
			OpenJobName = s.Item2.OpenJobName,
			OpenReportName = s.Item2.OpenReportName,
			OpenVartreeName = s.Item2.OpenVartreeName
		}).ToArray();
		return await Task.FromResult(list);
	}

	async Task<int> ChangePasswordImpl(ChangePasswordRequest request)
	{
		return await LicProv.ChangePassword(request.UserId, request.OldPassword, request.Newpassword);
	}

	async Task<int> UpdateAccountImpl(UpdateAccountRequest request)
	{
		return await LicProv.UpdateAccount(request.UserId, request.UserName, request.Comment, request.Email);
	}

	static SessionInfo LicToInfo(LicenceInfo licence, string sessionId) => new SessionInfo()
	{
		SessionId = sessionId,
		Id = licence.Id,
		Name = licence.Name,
		Email = licence.Email,
		Roles = licence.Roles ?? [],
		VartreeNames = licence.VartreeNames ?? [],
		SessionCusts = [.. licence.Customers.Select(c => new SessionCust()
		{
			Id = c.Id,
			Name = c.Name,
			DisplayName = c.DisplayName,
			AgencyId = c.AgencyId,
			Info = c.Info,
			Logo = c.Logo,
			Url = c.Url,
			Sequence = c.Sequence,
			StorageKey = c.StorageKey,
			ParentAgency = c.ParentAgency == null ? null : new SessionAgency() { Id = c.ParentAgency.Id, Name = c.ParentAgency.Name },
			SessionJobs = [.. c.Jobs.Select(j => new SessionJob()
			{
				Id = j.Id,
				Name = j.Name,
				DisplayName = j.DisplayName,
				VartreeNames = j.VartreeNames ?? [],
				RealCloudVartreeNames = j.RealCloudVartreeNames ?? [],
				IsAccessible = j.IsAccessible,
				Description = j.Description,
				Info = j.Info,
				Logo = j.Logo,
				Url = j.Url,
				Sequence = j.Sequence
			})]
		})],
		ProcessorCount = Environment.ProcessorCount,
		OS = Environment.OSVersion.ToString()
	};

	/// <ignore/>
	public const int SessionIdLength = 10;

	/// <summary>
	/// Generates a random Session Id which can take about 10^15 values assuming that the
	/// Guid hash codes are equidistributed, which is likely because it is suspected that 
	/// they are crypto-strong. A session Id contains about 50 bits of entropy.
	/// </summary>
	static string MakeSessionId()
	{
		const string NiceChars = "123456789ABCDEFGHKLMNPQRSTVWXYZ";
		var chars = from i in Enumerable.Range(0, SessionIdLength)
					let r = Guid.NewGuid().GetHashCode() & 0x7fffffff
					let x = r % NiceChars.Length
					select NiceChars[x];
		return new string([.. chars]);
	}
}
