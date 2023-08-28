using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Carbon.Examples.WebService.Common;
using Carbon.Examples.WebService.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Carbon.Examples.WebService.WebApi;

/// <ignore/>
[ServiceFilter(typeof(ILoggerFactory))]
public class GeneralActionFilterAttribute : ActionFilterAttribute
{
	/// <ignore/>
	public const string RequestSequenceItemKey = "count";
	/// <ignore/>
	public const string RequestStartItemKey = "started";
	/// <ignore/>
	public const string EmptySid = "---";

	static int requestSequence = 1000;
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
		++requestSequence;
		var req = context.HttpContext.Request;
		context.HttpContext.Items[RequestSequenceItemKey] = requestSequence;
		context.HttpContext.Items[RequestStartItemKey] = DateTime.Now;
		string? sessionId = GetSesssId(req);
		string sid = sessionId?[..3] ?? "---";
		if (browsable != false)
		{
			logger!.LogDebug("{RequestSequence} {Sid} {Method} {Path}", requestSequence, sid, req.Method, req.Path);
		}
#if TRAFFIC
            object? argreq = context.ActionArguments.TryGetValue("request", out var o) ? o : null;
            using (var writer = MakeWriter())
            {
                writer.WriteLine(new string('=', 120));
                writer.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {requestSequence} {sid} {req.Method} {req.Path}");
                if (argreq != null)
                {
                    writer.WriteLine($"{argreq.GetType().Name}");
                    string json = JsonSerializer.Serialize(argreq, JsonOpts);
                    writer.WriteLine(json);
                }
            }
#endif
		string method = req.Method;
		string url = req.Path.ToString();
		SessionManager.UpdateActivity(sessionId ?? "-", $"{method} {url}");
	}

	/// <ignore/>
	public override void OnResultExecuting(ResultExecutingContext context)
	{
		bool? browsable = context.ActionDescriptor.EndpointMetadata.OfType<BrowsableAttribute>().FirstOrDefault()?.Browsable;
		int requestSequence = -1;
		DateTime started;
		double secs = 0.0;
		if (context.HttpContext.Items.TryGetValue(RequestSequenceItemKey, out object? val1))
		{
			if (val1 is int i)
			{
				requestSequence = i;
			}
		}
		if (context.HttpContext.Items.TryGetValue(RequestStartItemKey, out object? val2))
		{
			if (val2 is DateTime dt)
			{
				started = dt;
				secs = DateTime.Now.Subtract(started).TotalSeconds;
				context.HttpContext.Response.Headers.Add(HeaderElapsed, secs.ToString("F3"));
			}
		}
		string? sessionId = GetSesssId(context.HttpContext.Request);
		string sid = sessionId?[..3] ?? EmptySid;
		int code = 0;
		int status = context.HttpContext.Response.StatusCode;   // Default
		string contentType = "-";
		string? showtext;
		if (context.Result is ObjectResult or)
		{
			status = or.StatusCode ?? status;
			object? orval = or.Value;
			if (orval is ErrorResponse er)
			{
				code = er.Code;
				showtext = er.Message;
			}
			else
			{
				showtext = ServiceUtility.NiceObj(orval);
			}
		}
		else if (context.Result is ContentResult cr)
		{
			status = cr.StatusCode ?? status;
			contentType = cr.ContentType ?? contentType;
			showtext = $"Content({cr.Content?.Length})";
		}
		else if (context.Result is JsonResult jr)
		{
			status = jr.StatusCode ?? status;
			contentType = jr.ContentType ?? contentType;
			showtext = ServiceUtility.NiceObj(jr.Value);
		}
		else
		{
			//System.Diagnostics.Trace.WriteLine($"#### context.Result is {context.Result?.GetType().Name}");
			showtext = ServiceUtility.NiceObj(context.Result);
		}
		if (browsable != false)
		{
			logger!.LogDebug("{RequestSequence} {Sid} {Status} {ContentType} [{Seconds}] {Code} {SampleResponse}", requestSequence, sid, status, contentType, secs.ToString("F2"), code, showtext);
		}
#if TRAFFIC
            using (var writer = MakeWriter())
            {
                writer.WriteLine(new string('-', 120));
                writer.WriteLine($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} {requestSequence} {sid} {context.HttpContext.Response.StatusCode} {secs:F2} {code}");
                if (showobj != null)
                {
                    writer.WriteLine($"{showobj.GetType().Name}");
                    string json = JsonSerializer.Serialize(showobj, JsonOpts);
                    writer.WriteLine(json);
                }
            }
#endif
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
