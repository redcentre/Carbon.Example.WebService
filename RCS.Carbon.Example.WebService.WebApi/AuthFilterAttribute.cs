using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RCS.Carbon.Example.WebService.Common;
using RCS.Carbon.Example.WebService.Common.DTO;

namespace RCS.Carbon.Example.WebService.WebApi;

/// <ignore/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuthFilterAttribute : Attribute, IAuthorizationFilter
{
	readonly string[] requiredRoles;
	string[] registeredApiKeys;
	static ILogger logger;

	/// <ignore/>
	public AuthFilterAttribute(params string[] requiredRoles)
	{
		this.requiredRoles = requiredRoles ?? [];
	}

	/// <summary>
	/// This filter runs before anyting in the general action filter. An error must be fully
	/// logged here because the action filter won't do it.
	/// </summary>
	public void OnAuthorization(AuthorizationFilterContext context)
	{
		if (logger == null)
		{
			var logfac = (ILoggerFactory)context.HttpContext.RequestServices.GetService(typeof(ILoggerFactory))!;
			logger = logfac.CreateLogger("AUTH");
			var config = (IConfiguration)context.HttpContext.RequestServices.GetService(typeof(IConfiguration))!;
			registeredApiKeys = config.GetSection("CarbonApi:RegisteredApiKeys").Get<string[]>()!;
		}
		HttpRequest req = context.HttpContext.Request;
		var mi = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo;     // Note this tricky cast is needed
		bool allowApiKey = mi.GetCustomAttribute<AllowApiKeyAttribute>() != null;
		string? apiKey = req.Headers.TryGetValue(CarbonServiceClient.ApiKeyHeaderKey, out var svals) ? svals.FirstOrDefault() : null;
		string? sessionId = req.Headers.TryGetValue(CarbonServiceClient.SessionIdHeaderKey, out svals) ? svals.FirstOrDefault() : null;
		if (apiKey != null)
		{
			// ┌───────────────────────────────────────────────────────────────┐
			// │  The client has passed the x-api-key header with a magic      │
			// │  value that can be used to invoke certain endpoints. This is  │
			// │  useful for utilities, such as listing sessions for example.  │
			// └───────────────────────────────────────────────────────────────┘
			if (!allowApiKey)
			{
				logger.LogWarning(704, "Api Key {ApiKey} not usable for {Method} {Path}", CarbonServiceClient.ApiKeyHeaderKey, req.Method, req.Path);
				context.Result = new ObjectResult(new ErrorResponse(ErrorResponseCode.NoSessionFound, $"Api Key '{apiKey}' not usable for {req.Method} {req.Path}")) { StatusCode = StatusCodes.Status403Forbidden };
				return;
			}
			if (!registeredApiKeys.Contains(apiKey))
			{
				logger.LogWarning(703, "Api Key {ApiKey} not registered for {Method} {Path}", CarbonServiceClient.ApiKeyHeaderKey, req.Method, req.Path);
				context.Result = new ObjectResult(new ErrorResponse(ErrorResponseCode.NoSessionFound, $"Api Key '{apiKey}' not registered for {req.Method} {req.Path}")) { StatusCode = StatusCodes.Status403Forbidden };
				return;
			}
			var apiIdent = new GenericIdentity("Anonymous", "x-api-key");
			apiIdent.AddClaim(new Claim("ApiKey", apiKey));
			context.HttpContext.User = new GenericPrincipal(apiIdent, null);
			return;
		}
		// ┌───────────────────────────────────────────────────────────────┐
		// │  This is the traditional authorisation using x-session-id     │
		// │  header with a value from a previous authentication.          │
		// │  If the method has role restrictions then one of those roles  │
		// │  must be in the user's account record.                        │
		// └───────────────────────────────────────────────────────────────┘
		if (sessionId == null)
		{
			logger.LogWarning(702, "An authorisation request header is required for {Method} {Path}", req.Method, req.Path);
			context.Result = new ObjectResult(new ErrorResponse(ErrorResponseCode.NoSessionHeader, $"An authorisation request header is required for {req.Method} {req.Path}")) { StatusCode = StatusCodes.Status403Forbidden };
			return;
		}
		// The header Session Id must have an entry in the session manager
		// to indicate it's active, then the licensing name and roles can
		// be used to construct a context 'User' for the request.
		SessionItem? si = SessionManager.FindSession(sessionId);
		if (si == null)
		{
			logger.LogWarning(700, "No session {SessionId} exists for {Method} {Path}", sessionId, req.Method, req.Path);
			context.Result = new ObjectResult(new ErrorResponse(ErrorResponseCode.NoSessionFound, $"No session '{sessionId}' exist for {req.Method} {req.Path}")) { StatusCode = StatusCodes.Status403Forbidden };
			return;
		}
		// ╔══════════════════════════════════════════════════════════════════════════╗
		// ║  NOTE -- This example web service does not by default use RBAC (roles    ║
		// ║  based authentication). The feature can be enabled by changing an        ║
		// ║  attribute on the endpoints like this example:                           ║
		// ║                                                                          ║
		// ║     before  [AuthFilter]                                                 ║
		// ║     after   [AuthFilter("Import","DeleteReport")]                        ║
		// ║             public Task ... MyEndpoint(...)                              ║
		// ║                                                                          ║
		// ║  Two mock roles have been invented and applied to an endpoint. A user    ║
		// ║  account cannot use the endpoint unless is has at least one of those     ║
		// ║  role names listed in their licensing account record.                    ║
		// ╚══════════════════════════════════════════════════════════════════════════╝
		if (requiredRoles.Length > 0 && !requiredRoles.Intersect(si.Roles).Any())
		{
			string needsJoin = string.Join(",", requiredRoles);
			string hasJoin = string.Join(",", si.Roles);
			logger.LogWarning(701, "Not authorised for {Method} {Path}. Needs [{NeedsJoin}] has [{HasJoin}].", req.Method, req.Path, needsJoin, hasJoin);
			context.Result = new ObjectResult(new ErrorResponse(ErrorResponseCode.NotRoleAuthorised, $"Not authorised for {req.Method} {req.Path}. Needs [{needsJoin}] has [{hasJoin}].")) { StatusCode = StatusCodes.Status403Forbidden };
			return;
		}

		var sessIdent = new GenericIdentity(si.UserName!, "x-session-id");
		sessIdent.AddClaim(new Claim("SessionId", sessionId));
		context.HttpContext.User = new GenericPrincipal(sessIdent, si.Roles);
	}
}
