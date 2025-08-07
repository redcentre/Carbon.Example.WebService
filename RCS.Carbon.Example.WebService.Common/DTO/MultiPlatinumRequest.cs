using System;

namespace RCS.Carbon.Example.WebService.Common.DTO;

/// <summary>
/// Request parameters to generate multiple Platinum reports in parallel.
/// </summary>
public sealed class MultiPlatinumRequest
{
	/// <summary>
	/// An array of one or more report names to generate. The report names are relative to the TOC's job name, so an
	/// example report name could be like "Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC ActivelyLooking".
	/// </summary>
	public string[] ReportNames { get; set; }

	/// <summary>
	/// The top variable &#x26d4; RESERVED FOR FUTURE USE
	/// </summary>
	public string Top { get; set; }

	/// <summary>
	/// The side variable &#x26d4; RESERVED FOR FUTURE USE
	/// </summary>
	public string Side { get; set; }

	/// <summary>
	/// The optional filter expression to apply to all reports.
	/// </summary>
	public string? Filter { get; set; }

	/// <summary>
	/// The optional weight expression to apply to all reports &#x26d4; RESERVED FOR FUTURE USE
	/// </summary>
	public string? Weight { get; set; }

	/// <summary>
	/// The maximum parallism count. If the value is LE 1 then reports are processed sequentially.
	/// If the value is GT the number of cores then the core count is used. The default value is 4.
	/// </summary>
	public int ParallelMax { get; set; } = 4;

	/// <summary>
	/// False (the default) to return the reports in an array.
	/// True to save the resulting reports as blobs in the artefacts container,
	/// which might be useful if large numbers of reports are being processed and
	/// it's not feasible to return all of the results in an array response.
	/// </summary>
	public bool SaveAsBlobs { get; set; }
}
