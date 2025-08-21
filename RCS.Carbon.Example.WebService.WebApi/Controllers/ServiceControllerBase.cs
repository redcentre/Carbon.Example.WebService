using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using RCS.Azure.Data.Processor;
using RCS.Carbon.Example.WebService.Common;
using RCS.Carbon.Example.WebService.Common.DTO;
using RCS.Carbon.Shared;
using RCS.Licensing.Provider.Shared;

namespace RCS.Carbon.Example.WebService.WebApi.Controllers;

/// <ignore/>
public abstract class ServiceControllerBase : ControllerBase
{
	/// <summary>
	/// All derived controllers can use this logger service.
	/// </summary>
	protected static ILogger Logger;

	protected IConfiguration Config { get; private set; }

	protected ILicensingProvider LicProv { get; private set; }

	protected const string ReportVDirPrefix = "service-report";
	protected const string PlatinumbatchVDirPrefix = "service-platinum-batch";

	/// <ignore/>
	public ServiceControllerBase(ILoggerFactory logfac, IConfiguration config, ILicensingProvider licprov)
	{
		Logger ??= logfac.CreateLogger("WEBC");
		Config = config;
		LicProv = licprov;
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
			void Chuck(string message) => throw new CarbonServiceException(1000, $"Header '{CarbonServiceClient.SessionIdHeaderKey}' {message}'. Request {req.Method} {req.Path}.");
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
	/// This property can only be used for the duration of a request, not in background
	/// work like what happens in the Platinum and OXT multi report processing.
	/// </summary>
	protected StorageProcessor AzProc => LazyInitializer.EnsureInitialized(ref _azproc, () =>
	{
		var azp = new StorageProcessor(
			Config["CarbonApi:ApplicationStorageConnect"]!,
			Config["CarbonApi:AppContainerName"]!
		);
		var m = Regex.Match(azp.StorageConnect, @"AccountName=(\w+).+AccountKey=([^;]+)");
		Logger.LogDebug(600, "Created {Name} {AccName} {AccKey}… {AppCon}", azp.GetType().Name, m.Groups[1].Value, m.Groups[2].Value.Substring(0, 8), azp.ContainerName);
		return azp;
	});

	// Important factored-out code that converts a job's display table into an XLSX workbook
	// and uploads it so the url can be used to display it in client apps.

	protected async Task<XlsxResponse> MakeXlsxAndUpload(StateWrap wrap, string reason)
	{
		var watch = new Stopwatch();
		watch.Start();
		byte[] blob = wrap.Engine.TableAsExcelBlob();
		double xlsxsecs = watch.Elapsed.TotalSeconds;
		Logger.LogDebug(610, "Make XLSX {BlobLength} [{XlsxSecs:F2}] - {Reason}", blob.Length, xlsxsecs, reason);
		watch.Restart();
		var sess = SessionManager.FindSession(SessionId, true);
		string repname = sess.OpenReportName ?? "UnsavedReport";
		string upname = Path.ChangeExtension(repname, ".xlsx");
		var azblob = await AzProc.UploadBufferForReport(sess.UserId, sess.OpenCustomerName, sess.OpenJobName, upname, ReportVDirPrefix, blob);
		double upsecs = watch.Elapsed.TotalSeconds;
		Logger.LogDebug(612, "Upload {BlobUri} [{upsecs:F2}]", azblob.Uri, upsecs);
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