using System;
using System.Linq;
using RCS.Carbon.Tables;

namespace RCS.Carbon.Example.WebService.Common.DTO;

/// <summary>
/// Response data containing the results of a Platinum report batch.
/// </summary>
public sealed class MultiPlatinumResponse
{
	/// <summary>
	/// A unique identifier that identifies and correlates the request and response. The Id is generated and
	/// returned when the request is made. The value can be used to track or cancel processing.
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// The report names to generate. The values are actually qualified with a path prefix to be in the format (example):
	/// "Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC ActivelyLooking".
	/// </summary>
	public string[] ReportNames { get; set; }

	/// <summary>
	/// The parallelism count that was actually used in report generation.
	/// </summary>
	public int ParallelMax { get; set; }

	/// <summary>
	/// The UTC time the report processing started. If this property has a value then report batch processing has started.
	/// </summary>
	public DateTime? StartTime { get; set; }

	/// <summary>
	/// The elapsed time in seconds for all report processing. If this property has a value then report batch processing has completed.
	/// </summary>
	public double? ElapsedSeconds { get; set; }

	/// <summary>
	/// Gets a flag indicating if the batch was cancelled.
	/// </summary>
	public bool IsCancelled { get; set; }

	/// <summary>
	/// Gets a flag indicating if the batch is completed. Note that this flag only indicates that the report processing loop
	/// completed. Some reports may still be in the IsWaiting state because the batch was cancelled. Some reports may have
	/// failed. Check other flag properties to determine the overall completion status of all reports in the batch.
	/// </summary>
	public bool IsCompleted => ElapsedSeconds != null;

	/// <summary>
	/// An aray of objects containing the status and results of the processing of each report in the batch.
	/// </summary>
	public PlatinumResponseItem[]? Reports { get; set; }

	/// <summary>
	/// Gets a flag indicating if all reports are waiting.
	/// </summary>
	public bool AllReportsWaiting => Reports == null || Reports.All(r => r.IsWaiting);

	/// <summary>
	/// Gets a flag indicating if any reports are running.
	/// </summary>
	public bool AnyReportsRunning => Reports != null && Reports.Any(r => r.IsRunning);

	/// <summary>
	/// Gets a flag indicating if all reports are completed (with either success or failure).
	/// </summary>
	public bool AllReportsCompleted => Reports != null && Reports.All(r => r.IsCompleted);

	/// <summary>
	/// Gets the number of reports in the waiting state.
	/// </summary>
	public int WaitingReportCount => Reports?.Count(x => x.IsWaiting) ?? 0;

	/// <summary>
	/// Gets the number of reports in the running state.
	/// </summary>
	public int RunningReportCount => Reports?.Count(x => x.IsRunning) ?? 0;

	/// <summary>
	/// Gets the number of reports in the completed state (with either success or failure).
	/// </summary>
	public int CompletedReportCount => Reports?.Count(x => x.IsCompleted) ?? 0;

	/// <summary>
	/// Gets the number of completed reports that failed due to unhandled error.
	/// </summary>
	public int FailedReportCount => Reports?.Count(x => x.IsFailed) ?? 0;
}

/// <summary>
/// Contains all of the processing values for a single report in the batch.
/// </summary>
public sealed class PlatinumResponseItem
{
	/// <summary>
	/// The name of the report.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The report in Platinum report object shape. Only contains a value when the
	/// <see cref="MultiPlatinumRequest.SaveAsBlobs"/> flag is False (the default) AND processing
	/// of the whole batch has completed AND this report completed with success.
	/// </summary>
	public PlatinumData? Data { get; set; }

	/// <summary>
	/// The Uri of a blob where the Platinum report object has been serialized. Only contains a value when the
	/// <see cref="MultiPlatinumRequest.SaveAsBlobs"/> flag is True AND the processing of this report has completed with success.
	/// </summary>
	public string? BlobUri { get; set; }

	/// <summary>
	/// The UTC time processing of this report started. If this property has a value then this report processing has started.
	/// </summary>
	public DateTime? StartTime { get; set; }

	/// <summary>
	/// The elapsed time in seconds of processing this report. If this property has a value then this report processing has completed.
	/// </summary>
	public double? ElapsedSeconds { get; set; }

	/// <summary>
	/// If report processing failed then this property contains the error type (currently the top Exception class name).
	/// </summary>
	public string? FailureType { get; set; }
	
	/// <summary>
	/// If report processing failed then this property contains the error messages unwound from the stack of Exceptions.
	/// The first element is from the top Exception. There will alway be at least one message in case of a failure.
	/// </summary>
	public string[]? FailureMessages { get; set; }

	/// <summary>
	/// The report is in waiting state.
	/// </summary>
	public bool IsWaiting => StartTime == null;

	/// <summary>
	/// The report is in running state.
	/// </summary>
	public bool IsRunning => StartTime != null && ElapsedSeconds == null;

	/// <summary>
	/// The report is in completed state (with either success or failure).
	/// </summary>
	public bool IsCompleted => StartTime != null && ElapsedSeconds != null;
	
	/// <summary>
	/// The report is completed with a failure due to an unhandled exception.
	/// </summary>
	public bool IsFailed => FailureType != null;
}