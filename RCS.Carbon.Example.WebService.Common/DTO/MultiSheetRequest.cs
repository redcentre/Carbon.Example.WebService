namespace RCS.Carbon.Example.WebService.Common.DTO;

/// <summary>
/// Encapsulates paramters for a ????
/// </summary>
public sealed class MultiSheetRequest
{
	/// <summary>
	/// An array of one or more report names to generate. The report names are relative to the TOC's job name, so an
	/// example report name could be like "Tables/Exec/PPTX Reports/UK/Charts/Broadband/PP_BC ActivelyLooking".
	/// </summary>
	public string[] ReportNames { get; set; }

	/// <summary>
	/// The option filter expression to apply to all reports.
	/// </summary>
	public string? Filter { get; set; }

	/// <summary>
	/// The optional weight to apply to all reports.
	/// </summary>
	public string? Weight { get; set; }

	/// <summary>
	/// The parallel processing maximum count, clamped to be from 1 to the number of processors.
	/// </summary>
	public int ParallelMax { get; set; } = 4;
}
