using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Carbon.Examples.WebService.Common;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RCS.Carbon.Licensing.Shared;
using RCS.Carbon.Shared;
using tab = RCS.Carbon.Tables;

namespace Carbon.Examples.WebService.WebApi.Controllers;

partial class SessionController
{
	async Task<ActionResult<SessionInfo>> StartSessionFreeImpl(AuthenticateFreeRequest request)
	{
		var engine = new tab.CrossTabEngine(LicProv);
		LicenceInfo licence = await engine.GetFreeLicence(request.Email, request.SkipCache);
		string sessionId = MakeSessionId();
		SessionManager.StartSession(sessionId, licence);
		var sessinfo = LicToInfo(licence, sessionId);
		string[] state = engine.SaveState();
		SessionManager.SaveState(sessionId, state);
		LogInfo(103, "Start Free Session {SessionId} Id {LicenceId} Name {LicenceName}", sessionId, licence.Id, licence.Name);
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
				string[] ids = sessions.Select(s => s.SessionId).ToArray();
				return BadRequest(new ErrorResponse(301, message, "The data property contains a string array of the sessionIds that are already active.", ids));
			}
		}
		// Perform the Carbon engine login and session start for a user id.
		try
		{
			var engine = new tab.CrossTabEngine(LicProv);
			LicenceInfo licence = await engine.GetLicenceId(request.Id, request.Password, request.SkipCache);
			string sessionId = MakeSessionId();
			SessionManager.StartSession(sessionId, licence);
			var sessinfo = LicToInfo(licence, sessionId);
			string[] state = engine.SaveState();
			SessionManager.SaveState(sessionId, state);
			LogInfo(100, "Login Session {SessionId} Id {LicenceId} Name {LicenceName}", sessionId, licence.Id, licence.Name);
			return Ok(sessinfo);
		}
		catch (CarbonException ex)
		{
			return BadRequest(new ErrorResponse(ex.Code, ex.Message));
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
				string[] ids = sessions.Select(s => s.SessionId).ToArray();
				return BadRequest(new ErrorResponse(302, message, "The data property contains a string array of the sessionIds that are already active.", ids));
			}
		}
		// Perform the Carbon engine login and session start for a user name.
		try
		{
			var engine = new tab.CrossTabEngine(LicProv);
			LicenceInfo licence = await engine.GetLicenceName(request.Name, request.Password);
			string sessionId = MakeSessionId();
			SessionManager.StartSession(sessionId, licence);
			var sessinfo = LicToInfo(licence, sessionId);
			string[] state = engine.SaveState();
			SessionManager.SaveState(sessionId, state);
			LogInfo(102, "Login Session {SessionName} Name {LicenceName}", sessionId, licence.Name);
			return Ok(sessinfo);
		}
		catch (CarbonException ex)
		{
			return BadRequest(new ErrorResponse(ex.Code, ex.Message));
		}
	}

	async Task<ActionResult<int>> ForceSessionsImpl(string idlist)
	{
		string[] ids = idlist.Split(',');
		bool[] flags = ids.Select(id => SessionManager.EndSession(id)).ToArray();
		int count = flags.Count(f => f);
		long total = ids.Select(id => SessionManager.DeleteState(id)).Sum();
		LogInfo(104, "Force {IdList} count {Count} bytes {Total}", idlist, count, total);
		return await Task.FromResult(count);
	}

	async Task<ActionResult<bool>> EndSessionImpl(string sessionId)
	{
		bool success = SessionManager.EndSession(sessionId);
		long total = SessionManager.DeleteState(sessionId);
		LogInfo(110, "End Session {SessionId} {Total}", sessionId, total);
		SessionCleanup();
		return await Task.FromResult(success);
	}

	//async Task<ActionResult<int>> LogoffSessionImpl(string sessionId)
	//{
	//	var engine = new tab.CrossTabEngine(LicProv);
	//	SessionItem? si = SessionManager.FindSession(SessionId, false);
	//	int count = -1;
	//	if (si != null)
	//	{
	//		if (si.UserId != null)
	//		{
	//			count = await engine.LogoutId(si.UserId);
	//		}
	//		bool success = SessionManager.EndSession(sessionId);
	//		long total = SessionManager.DeleteState(sessionId);
	//		string showuserid = si.UserId ?? "NULL";
	//		LogInfo(112, "Logoff Session {SessionId} Count {Count} User Id {UserId} Success {Success} State {Total}", sessionId, count, showuserid, success, total);
	//		SessionCleanup();
	//		return Ok(count);
	//	}
	//	else
	//	{
	//		LogWarn(113, "Logoff Session {SessionId} not found", sessionId);
	//		return Ok(-1);
	//	}
	//}

	//async Task<ActionResult<int>> ReturnSessionImpl(string sessionId)
	//{
	//	var engine = new tab.CrossTabEngine(LicProv);
	//	SessionItem? si = SessionManager.FindSession(SessionId, false);
	//	int count = -1;
	//	if (si != null)
	//	{
	//		if (si.UserId != null)
	//		{
	//			count = await engine.ReturnId(si.UserId);
	//		}
	//		bool success = SessionManager.EndSession(sessionId);
	//		long total = SessionManager.DeleteState(sessionId);
	//		string showuserid = si.UserId ?? "NULL";
	//		LogInfo(114, "Return Session {SessionId} Count {Count} User Id {UserId} Success {Success} State {Total}", sessionId, count, showuserid, success, total);
	//		SessionCleanup();
	//		return Ok(count);
	//	}
	//	else
	//	{
	//		LogWarn(115, "Return Session {SessionId} not found", sessionId);
	//		return Ok(-1);
	//	}
	//}

	async Task<ActionResult<SessionStatus>> ReadSessionImpl([FromRoute] string id)
	{
		SessionItem? session = SessionManager.FindSession(id, false);
		if (session == null)
		{
			return null;
		}
		var ss = new SessionStatus()
		{
			SessionId = session.SessionId,
			UserId = session.UserId,
			UserName = session.UserName,
			CreatedUtc = session.CreatedUtc,
			LastActivityUtc = session.LastActivityUtc,
			LastActivity = session.LastActivity,
			ActivityCount = session.ActivityCount
		};
		return await Task.FromResult(ss);
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
			LogInfo(140, "No sessions older than {Days} to cleanup", days);
		}
		else
		{
			LogInfo(141, "Cleaned {Count} sessions older than {Days}", count, days);
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
			UserName = s.Item2.UserName
		}).ToArray();
		string json = JsonSerializer.Serialize(list, new JsonSerializerOptions() { WriteIndented = true });
		return await Task.FromResult(list);
	}

	async Task<GenericResponse> ChangePasswordImpl(ChangePasswordRequest request)
	{
		//var licreq = new lic.ChangePasswordRequest()
		//{
		//	UserId = request.UserId,
		//	OldPassword = request.OldPassword,
		//	NewPassword = request.Newpassword
		//};
		// TODO Implmenent ChangePasswordImpl in the licensing provider
		throw new NotImplementedException(nameof(ChangePasswordImpl));
		//lic.ErrorResponse resp = await Lic.ChangePassword(licreq);
		//return new GenericResponse(resp.Code, resp.Message);
	}

	async Task<GenericResponse> UpdateAccountImpl(UpdateAccountRequest request)
	{
		//var licreq = new lic.UpdateAccountRequest()
		//{
		//	UserId = request.UserId,
		//	UserName = request.UserName,
		//	Comment = request.Comment,
		//	Email = request.Email
		//};
		// TODO Implmenent UpdateAccountImpl in the licensing provider
		throw new NotImplementedException(nameof(UpdateAccountImpl));
		//lic.ErrorResponse resp = await Lic.UpdateAccount(licreq);
		//return new GenericResponse(resp.Code, resp.Message);
	}

	static SessionInfo LicToInfo(LicenceInfo licence, string sessionId) => new SessionInfo()
	{
		SessionId = sessionId,
		Id = licence.Id,
		Name = licence.Name,
		Email = licence.Email,
		Roles = licence.Roles ?? Array.Empty<string>(),
		VartreeNames = licence.VartreeNames ?? Array.Empty<string>(),
		SessionCusts = licence.Customers.Select(c => new SessionCust()
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
			SessionJobs = c.Jobs.Select(j => new SessionJob()
			{
				Id = j.Id,
				Name = j.Name,
				DisplayName = j.DisplayName,
				VartreeNames = j.VartreeNames ?? Array.Empty<string>(),
				RealCloudVartreeNames = j.RealCloudVartreeNames ?? Array.Empty<string>(),
				IsAccessible = j.IsAccessible,
				Description = j.Description,
				Info = j.Info,
				Logo = j.Logo,
				Url = j.Url,
				Sequence = j.Sequence
			}).ToArray()
		}).ToArray(),
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
		return new string(chars.ToArray());
	}
}
