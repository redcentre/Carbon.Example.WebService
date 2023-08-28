using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Principal;
using Carbon.Examples.WebService.Common;
using Carbon.Examples.WebService.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Carbon.Examples.WebService.WebApi;

/// <ignore/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AuthFilterAttribute : Attribute, IAuthorizationFilter
{
	readonly string[] requiredRoles;

	/// <ignore/>
	public AuthFilterAttribute(params string[] requiredRoles)
	{
		this.requiredRoles = requiredRoles ?? Array.Empty<string>();
	}

	/// <summary>
	/// This filter runs before anyting in the general action filter. An error must be fully
	/// logged here because the action filter won't do it.
	/// </summary>
	public void OnAuthorization(AuthorizationFilterContext context)
	{
		ILoggerFactory logfac = (ILoggerFactory)context.HttpContext.RequestServices.GetService(typeof(ILoggerFactory))!;
		ILogger logger = logfac.CreateLogger("AUTH");
		HttpRequest req = context.HttpContext.Request;
		static ObjectResult MakeAuthFail(int code, string message) => new(new ErrorResponse(code, message)) { StatusCode = StatusCodes.Status403Forbidden };
		var mi = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo;     // Note this tricky cast is needed
		var attr = mi.GetCustomAttributes<AuthFilterAttribute>();
		if (attr != null)
		{
			string? key = context.HttpContext.Request.Headers.TryGetValue(CarbonServiceClient.SessionIdHeaderKey, out var svals) ? svals.FirstOrDefault() : null;
			if (key?.Length == SessionController.SessionIdLength)
			{
				// The header Session Id must have an entry in the session manager
				// to indicate it's active, then the licensing name and roles can
				// be used to construct a context 'User' for the request.
				SessionItem? si = SessionManager.FindSession(key);
				if (si == null)
				{
					logger.LogWarning(700, "No session '{SessionId}' exists for {Method} {Path}", key, req.Method, req.Path);
					context.Result = MakeAuthFail(3, $"No session '{key}' exist for {req.Method} {req.Path}");
					return;
				}

				if (requiredRoles.Length > 0 && !requiredRoles.Intersect(si.Roles).Any())
				{
					string needsJoin = string.Join(",", requiredRoles);
					string hasJoin = string.Join(",", si.Roles);
					logger.LogWarning(700, "Not authorised for {Method} {Path}. Needs [{NeedsJoin}] has [{HasJoin}].", req.Method, req.Path, needsJoin, hasJoin);
					context.Result = MakeAuthFail(3, $"Not authorised for {req.Method} {req.Path}. Needs [{needsJoin}] has [{hasJoin}].");
					return;
				}

				var ident = new GenericIdentity(si.UserName!, "SessionId");
				ident.AddClaim(new Claim("AuthKey", key));
				context.HttpContext.User = new GenericPrincipal(ident, si.Roles);
			}
			else
			{
				logger.LogWarning(700, "Header '{Key}' is required for {Method} {Path}", CarbonServiceClient.SessionIdHeaderKey, req.Method, req.Path);
				context.Result = MakeAuthFail(2, $"Header key '{CarbonServiceClient.SessionIdHeaderKey}' is required for {req.Method} {req.Path}");
				return;
			}
		}
	}
}
