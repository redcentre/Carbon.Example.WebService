using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
		var accinfo = LicToInfo(licence, sessionId);
		string[] state = engine.SaveState();
		SessionManager.SaveState(sessionId, state);
		Logger.LogInformation(103, "{RequestSequence} Start Free Session {SessionId} Id {LicenceId} Name {LicenceName}", RequestSequence, sessionId, licence.Id, licence.Name);
		return Ok(accinfo);
	}

	async Task<ActionResult<SessionInfo>> LoginIdImpl(AuthenticateIdRequest request)
	{
		try
		{
			var engine = new tab.CrossTabEngine(LicProv);
			LicenceInfo licence = await engine.LoginId(request.Id, request.Password, request.SkipCache);
			string sessionId = MakeSessionId();
			SessionManager.StartSession(sessionId, licence);
			var accinfo = LicToInfo(licence, sessionId);
			string[] state = engine.SaveState();
			SessionManager.SaveState(sessionId, state);
			Logger.LogInformation(100, "{RequestSequence} Login Session {SessionId} Id {LicenceId} Name {LicenceName}", RequestSequence, sessionId, licence.Id, licence.Name);
			return Ok(accinfo);
		}
		catch (CarbonException ex)
		{
			return BadRequest(new ErrorResponse(ex.Code, ex.Message));
		}
	}

	async Task<ActionResult<SessionInfo>> AuthenticateNameImpl(AuthenticateNameRequest request)
	{
		try
		{
			var engine = new tab.CrossTabEngine(LicProv);
			LicenceInfo licence = await engine.GetLicenceName(request.Name, request.Password);
			string sessionId = MakeSessionId();
			SessionManager.StartSession(sessionId, licence);
			var accinfo = LicToInfo(licence, sessionId);
			string[] state = engine.SaveState();
			SessionManager.SaveState(sessionId, state);
			Logger.LogInformation(100, "{RequestSequence} Login Session {SessionName} Name {LicenceName}", RequestSequence, sessionId, licence.Name);
			return Ok(accinfo);
		}
		catch (CarbonException ex)
		{
			return BadRequest(new ErrorResponse(ex.Code, ex.Message));
		}
	}

	async Task<ActionResult<bool>> EndSessionImpl(string sessionId)
	{
		bool success = SessionManager.EndSession(sessionId);
		long total = SessionManager.DeleteState(sessionId);
		double kb = total / 1024.0;
		Logger.LogInformation(104, "{RequestSequence} {Sid} End Session {SessionId} {Total}", RequestSequence, Sid, sessionId, total);
		SessionCleanup();
		return await Task.FromResult(success);
	}

	async Task<ActionResult<int>> LogoffSessionImpl(string sessionId)
	{
		var engine = new tab.CrossTabEngine(LicProv);
		SessionItem? si = SessionManager.FindSession(SessionId, false);
		int count = -1;
		if (si != null)
		{
			if (si.UserId != null)
			{
				count = await engine.LogoutId(si.UserId);
			}
			bool success = SessionManager.EndSession(sessionId);
			long total = SessionManager.DeleteState(sessionId);
			string showuserid = si.UserId ?? "NULL";
			Logger.LogInformation(102, "{RequestSequence} {Sid} Logoff Session {SessionId} Count {Count} User Id {UserId} Success {Success} State {Total}", RequestSequence, Sid, sessionId, count, showuserid, success, total);
			SessionCleanup();
			return Ok(count);
		}
		else
		{
			Logger.LogWarning(103, "{RequestSequence} {Sid} Logoff Session {SessionId} not found", RequestSequence, Sid, sessionId);
			return Ok(-1);
		}
	}

	async Task<ActionResult<int>> ReturnSessionImpl(string sessionId)
	{
		var engine = new tab.CrossTabEngine(LicProv);
		SessionItem? si = SessionManager.FindSession(SessionId, false);
		int count = -1;
		if (si != null)
		{
			if (si.UserId != null)
			{
				count = await engine.ReturnId(si.UserId);
			}
			bool success = SessionManager.EndSession(sessionId);
			long total = SessionManager.DeleteState(sessionId);
			string showuserid = si.UserId ?? "NULL";
			Logger.LogInformation(102, "{RequestSequence} {Sid} Return Session {SessionId} Count {Count} User Id {UserId} Success {Success} State {Total}", RequestSequence, Sid, sessionId, count, showuserid, success, total);
			SessionCleanup();
			return Ok(count);
		}
		else
		{
			Logger.LogWarning(103, "{RequestSequence} {Sid} Return Session {SessionId} not found", RequestSequence, Sid, sessionId);
			return Ok(-1);
		}
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
			Logger.LogInformation("No sessions older than {Days} to cleanup", days);
		}
		else
		{
			Logger.LogInformation("Cleaned {Count} sessions older than {Days}", count, days);
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
				Description = j.Description,
				Info = j.Info,
				Logo = j.Logo,
				Url = j.Url,
				Sequence = j.Sequence
			}).ToArray()
		}).ToArray()
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
