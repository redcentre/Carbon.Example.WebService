using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RCS.Carbon.Example.WebService.Common;
using RCS.Carbon.Example.WebService.WebApi.Controllers;

namespace RCS.Carbon.Example.WebService.WebApi;

/// <ignore/>
[ServiceFilter(typeof(ILoggerFactory))]
public class GeneralActionFilterAttribute : ActionFilterAttribute
{
	/// <ignore/>
	public const string RequestStartItemKey = "started";

	const string HeaderElapsed = "x-service-elapsed";
	static ILogger? logger;

	/// <ignore/>
	public GeneralActionFilterAttribute(ILoggerFactory logfac)
	{
		// A single static instance of the filter logger is created on first demand
		// to reduce pressure because they are getting constructed all the time.
		logger ??= logfac.CreateLogger("FILT");
	}

	/// <ignore/>
	public override void OnActionExecuting(ActionExecutingContext context)
	{
		base.OnActionExecuting(context);
		var req = context.HttpContext.Request;
		context.HttpContext.Items[RequestStartItemKey] = DateTime.Now;
		string? sessionId = GetSesssId(req);
		string method = req.Method;
		string url = req.Path.ToString();
		SessionManager.UpdateActivity(sessionId ?? "-", $"{method} {url}");
	}

	/// <ignore/>
	public override void OnResultExecuting(ResultExecutingContext context)
	{
		bool? browsable = context.ActionDescriptor.EndpointMetadata.OfType<BrowsableAttribute>().FirstOrDefault()?.Browsable;
		DateTime started;
		double secs = 0.0;
		if (context.HttpContext.Items.TryGetValue(RequestStartItemKey, out object? val2))
		{
			if (val2 is DateTime dt)
			{
				started = dt;
				secs = DateTime.Now.Subtract(started).TotalSeconds;
				context.HttpContext.Response.Headers[HeaderElapsed] = secs.ToString("F3");
			}
		}
		string? sessionId = GetSesssId(context.HttpContext.Request);
		int errcode = 0;
		int status = context.HttpContext.Response.StatusCode;   // Default
		if (status >= 400)
		{
			if (context.Result is ObjectResult or)
			{
				status = or.StatusCode ?? status;
				object? orval = or.Value;
				if (orval is ErrorResponse er)
				{
					errcode = er.Code;
				}
			}
		}
		// These items may have been set by the /error controller.
		string? errorMethod = context.HttpContext.Items.TryGetValue("ErrorMethod", out var m) ? m?.ToString() : null;
		if (errorMethod != null)
		{
			string? errorPath = context.HttpContext.Items.TryGetValue("ErrorPath", out m) ? m?.ToString() : null;
			string? errorType = context.HttpContext.Items.TryGetValue("ErrorType", out m) ? m?.ToString() : null;
			string? message = context.HttpContext.Items.TryGetValue("Message", out m) ? m?.ToString() : null;
			string? errorMessage = context.HttpContext.Items.TryGetValue("ErrorMessage", out m) ? m?.ToString() : null;
			string? errorStack = context.HttpContext.Items.TryGetValue("ErrorStack", out m) ? m?.ToString() : null;
			logger!.LogError(752, "{Status} {Method} {Path} [{Seconds:F2}] {Message} {Code} {ErrorType} {ErrorMessage}", status, errorMethod, errorPath, secs, message, errcode, errorType, errorMessage);
		}
		else
		{
			if (browsable != false)
			{
				if (status >= 500)
				{
					logger!.LogError(753, "{Status} {Method} {Path} [{Seconds:F2}] {Code}", status, context.HttpContext.Request.Method, context.HttpContext.Request.Path, secs, errcode);
				}
				else if (status >= 400)
				{
					logger!.LogWarning(754, "{Status} {Method} {Path} [{Seconds:F2}] {Code}", status, context.HttpContext.Request.Method, context.HttpContext.Request.Path, secs, errcode);
				}
				else
				{
					logger!.LogInformation(756, "{Status} {Method} {Path} [{Seconds:F2}]", status, context.HttpContext.Request.Method, context.HttpContext.Request.Path, secs);
				}
			}
		}
		base.OnResultExecuting(context);
	}

	static string? GetSesssId(HttpRequest request)
	{
		if (request.Headers.TryGetValue(CarbonServiceClient.SessionIdHeaderKey, out StringValues values))
		{
			if (values.Count == 1 && values[0]?.Length == SessionController.SessionIdLength)
			{
				return values[0];
			}
		}
		return null;
	}

	readonly string TrafficFile = Path.Combine(Path.GetTempPath(), "_carbon_web_traffic.txt");

	StreamWriter MakeWriter() => new(TrafficFile, true);
}
