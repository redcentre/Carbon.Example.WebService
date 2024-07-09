using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RCS.Azure.Data.Processor;
using RCS.Carbon.Licensing.Shared;
using RCS.Carbon.Shared;
using RCS.Carbon.Tables;

namespace Carbon.Examples.WebService.WebApi.Controllers;

/// <ignore/>
public abstract class ServiceControllerBase : ControllerBase
{
	/// <summary>
	/// All derived controllers can use this logger service.
	/// </summary>
	static ILogger Logger;

	protected IConfiguration Config { get; private set; }

	protected ILicensingProvider LicProv { get; private set; }

	/// <ignore/>
	public ServiceControllerBase(ILoggerFactory logfac, IConfiguration config, ILicensingProvider licprov)
	{
		Logger ??= logfac.CreateLogger("WEBC");
		Config = config;
		LicProv = licprov;
	}

	protected void LogTrace(EventId eventId, string message, params object?[] args) => LogCommon(eventId, Microsoft.Extensions.Logging.LogLevel.Trace, null, message, args);

	protected void LogDebug(EventId eventId, string message, params object?[] args) => LogCommon(eventId, Microsoft.Extensions.Logging.LogLevel.Debug, null, message, args);

	protected void LogInfo(EventId eventId, string message, params object?[] args) => LogCommon(eventId, Microsoft.Extensions.Logging.LogLevel.Information, null, message, args);

	protected void LogWarn(EventId eventId, string message, params object?[] args) => LogCommon(eventId, Microsoft.Extensions.Logging.LogLevel.Warning, null, message, args);

	protected void LogError(EventId eventId, Exception? error, string message, params object?[] args) => LogCommon(eventId, Microsoft.Extensions.Logging.LogLevel.Error, error, message, args);

	void LogCommon(EventId eventId, Microsoft.Extensions.Logging.LogLevel level, Exception? error, string message, params object?[] args)
	{
		string? sid = null;
		if (HttpContext.Request.Headers.TryGetValue(CarbonServiceClient.SessionIdHeaderKey, out StringValues values))
		{
			// Unchecked manually get the sid if it's there, and truncate it to 3 character slug (like below).
			sid = values.FirstOrDefault();
			if (sid?.Length > 3)
			{
				sid = sid[..3];
			}
		}
		using (Logger.BeginScope(new Dictionary<string, object?> { { "RequestSequence", RequestSequence }, { "Sid", sid } }))
		{
			Logger.Log(level, eventId, error, message, args);
		}
	}

	/// <summary>
	/// Get the Session Id out of the current request headers. The caller of this property knows that a session
	/// must be started, so a failure to get the value is considered a request failure.
	/// </summary>
	protected string SessionId
	{
		get
		{
			HttpRequest req = HttpContext.Request;
			[DoesNotReturn]
			void Chuck(string message) => throw new CarbonServiceException(1000, $"Header '{CarbonServiceClient.SessionIdHeaderKey}' {message}'. Request {req.Method} {req.Path}. Session ????.");
			if (!HttpContext.Request.Headers.TryGetValue(CarbonServiceClient.SessionIdHeaderKey, out StringValues values))
			{
				Chuck("not found");
			}
			if (values.Count != 1)
			{
				Chuck($"Value count {values.Count} expected 1");
			}
			string? id = values[0];
			if (id == null)
			{
				Chuck("Value is null");
			}
			if (id.Length != SessionController.SessionIdLength)
			{
				Chuck($"Value '{id}' not a session Id");
			}
			return id;
		}
	}

	/// <summary>
	/// Elapsed time since the standard filter attribute detected action starting.
	/// </summary>
	protected double? Secs
	{
		get
		{
			try
			{
				DateTime? start = HttpContext.Items.TryGetValue(GeneralActionFilterAttribute.RequestStartItemKey, out object? value) ? (DateTime?)value : null;
				return start == null ? null : DateTime.Now.Subtract(start.Value).TotalSeconds;
			}
			catch (ObjectDisposedException)
			{
				return null;
			}
		}
	}

	/// <summary>
	/// An abbreviated Session ID slug to help logging.
	/// </summary>
	protected string? Sid => SessionId?[..3];

	/// <summary>
	/// Attempts to get the request sequence out of the context items.
	/// </summary>
	protected int? RequestSequence => GetContextItemInt(GeneralActionFilterAttribute.RequestSequenceItemKey);

	protected string GetKey(string customerName)
	{
		SessionItem item = SessionManager.FindSession(SessionId, true)!;
		return item.FindStorageKey(customerName, true)!;
	}

	string? GetContextItemString(string key)
	{
		if (!HttpContext.Items.TryGetValue(key, out object? value)) return null;
		if (value is string s) return s;
		return null;
	}

	int? GetContextItemInt(string key)
	{
		if (!HttpContext.Items.TryGetValue(key, out object? value)) return null;
		if (value is int i) return i;
		return null;
	}

	StorageProcessor? _azproc;
	/// <summary>
	/// Lazy reference to a single instance of an RCS Azure data processor.
	/// Don't forget to look for the config values in development user secrets.
	/// </summary>
	protected StorageProcessor AzProc => LazyInitializer.EnsureInitialized(ref _azproc, () =>
	{
		var azp = new StorageProcessor(
			Config["CarbonApi:ApplicationStorageConnect"],
			Config["CarbonApi:ArtefactsContainerName"]
		);
		var m = Regex.Match(azp.ApplicationStorageConnect, @"AccountName=(\w+).+AccountKey=([^;]+)");
		LogDebug(600, "Created {Name} {AccName} {AccKey}… {ArtefactCon}", azp.GetType().Name, m.Groups[1].Value, m.Groups[2].Value.Substring(0, 8), azp.ArtefactsContainerName);
		return azp;
	});

	// Important factored-out code that converts a job's display table into an XLSX workbook
	// and uploads it so the url can be used to display it in client apps.

	protected async Task<XlsxResponse> MakeXlsxAndUpload(StateWrap wrap, string reason)
	{
		var watch = new Stopwatch();
		watch.Start();
		byte[] blob = XTableOutputManager.AsSingleXLSXBuffer(wrap.Engine.Job.DisplayTable);
		double xlsxsecs = watch.Elapsed.TotalSeconds;
		LogDebug(610, "Make XLSX {BlobLength} [{XlsxSecs:F2}] - {Reason}", blob.Length, xlsxsecs, reason);
		watch.Restart();
		var sess = SessionManager.FindSession(SessionId, true);
		string repname = sess.OpenReportName ?? "UnsavedReport";
		string upname = Path.ChangeExtension(repname, ".xlsx");
		var azblob = await AzProc.UploadBufferForReport(sess.UserId, sess.OpenCustomerName, sess.OpenJobName, upname, blob);
		double upsecs = watch.Elapsed.TotalSeconds;
		LogDebug(612, "Upload {BlobUri} [{upsecs:F2}]", azblob.Uri, upsecs);
		return new XlsxResponse()
		{
			ReportName = sess.OpenReportName!,
			ExcelBytes = blob.Length,
			ExcelSecs = xlsxsecs,
			UploadSecs = upsecs,
			ShowFrequencies = wrap.Engine.Job.DisplayTable.DisplayProps.Cells.Frequencies.Visible,
			ShowColPercents = wrap.Engine.Job.DisplayTable.DisplayProps.Cells.ColumnPercents.Visible,
			ShowRowPercents = wrap.Engine.Job.DisplayTable.DisplayProps.Cells.RowPercents.Visible,
			ShowSignificance = wrap.Engine.Job.DisplayTable.DisplayProps.Significance.Visible,
			OriginalFilter = wrap.Engine.Job.DisplayTable.TableSpec.Filter.Exp,
			ExcelUri = azblob.Uri.AbsoluteUri
		};
	}

	protected static void DumpNodes(IEnumerable<GenNode> nodes)
	{
		foreach (var node in GenNode.WalkNodes(nodes))
		{
			string pfx = string.Join("", Enumerable.Repeat("|  ", node.Level));
			Trace.WriteLine($"{pfx}{node}");
		}
	}

	protected static string Nicestr(string value) => value == null ? "NULL" : value.Length == 0 ? "BLANK" : $"'{value}'";
}