namespace RCS.Carbon.Example.WebService.Common.DTO;

/// <summary>
/// Encapsulates the response for a ????
/// </summary>
public sealed class MultiSheetResponse
{
	/// <summary>
	/// The full Uri of the blob containing the results of multi-sheet processing.
	/// </summary>
	public string ExcelBlobUri { get; set; }

	/// <summary>
	/// The elapsed time to generate the Excel document.
	/// </summary>
	public double Seconds { get; set; }

	/// <summary>
	/// A set of errors collected during report generation.
	/// </summary>
	public MutilSheetError[] Errors { get; set; }

	/// <summary>
	/// The parallel processing maximum count that was used.
	/// </summary>
	public int ParallelUsed { get; set; }
}

public sealed class MutilSheetError
{
	public int Index { get; set; }
	public string Type { get; set; }
	public string Message { get; set; }
}
