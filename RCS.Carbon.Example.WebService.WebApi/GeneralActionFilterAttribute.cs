using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using RCS.Carbon.Example.WebService.Common;
using RCS.Carbon.Example.WebService.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

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
		bool? browsable = context.ActionDescriptor.EndpointMetadata.OfType<BrowsableAttribute>().FirstOrDefault()?.Browsable;
		var req = context.HttpContext.Request;
		context.HttpContext.Items[RequestStartItemKey] = DateTime.Now;
		string? sessionId = GetSesssId(req);
		if (browsable != false)
		{
			logger!.LogDebug(750, "{Method} {Path}", req.Method, req.Path);
		}
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
				secs = Math.Round(secs, 3);
				context.HttpContext.Response.Headers[HeaderElapsed] = secs.ToString("F3");
			}
		}
		string? sessionId = GetSesssId(context.HttpContext.Request);
		int errcode = 0;
		string? errmsg = null;
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
					errmsg = er.Message;
				}
				else
				{
					errmsg = ServiceUtility.NiceObj(orval);
				}
			}
		}
		string? message = context.HttpContext.Items.TryGetValue("Message", out var m) ? m.ToString() : null;
		string? errorPath = context.HttpContext.Items.TryGetValue("ErrorPath", out m) ? m.ToString() : null;
		string? errorType = context.HttpContext.Items.TryGetValue("ErrorType", out m) ? m.ToString() : null;
		string? errorMessage = context.HttpContext.Items.TryGetValue("ErrorMessage", out m) ? m.ToString() : null;
		string? errorStack = context.HttpContext.Items.TryGetValue("ErrorStack", out m) ? m.ToString() : null;
		if (browsable != false)
		{
			if (status >= 500)
			{
				logger!.LogError(752, "{Method} {Path} {Status} {Seconds} {Message} {Code} {ErrorPath} {ErrorType} {ErrorMessage} {ErrorStack}", context.HttpContext.Request.Method, context.HttpContext.Request.Path, status, secs, message, errcode, errorPath, errorType, errorMessage, errorStack);
			}
			else if (status >= 400)
			{
				logger!.LogWarning(754, "{Method} {Path} {Status} {Seconds} {Message} {Code} {ErrorPath} {ErrorType} {ErrorMessage} {ErrorStack}", context.HttpContext.Request.Method, context.HttpContext.Request.Path, status, secs, message, errcode, errorPath, errorType, errorMessage, errorStack);
			}
			else
			{
				logger!.LogInformation(756, "{Method} {Path} {Status} {Seconds} {Message}", context.HttpContext.Request.Method, context.HttpContext.Request.Path, status, secs, message);
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

	readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions() { WriteIndented = true };

	readonly string TrafficFile = Path.Combine(Path.GetTempPath(), "_carbon_web_traffic.txt");

	StreamWriter MakeWriter() => new(TrafficFile, true);
}
