using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class MultiOxtResponse
{
	public Guid Id { get; set; }
	public DateTime Created { get; set; }
	public string ProgressMessage { get; set; }
	public bool IsCancelled { get; set; }
	public int ParallelCount { get; set; }
	public RubyMultiOxtItem[] Items { get; set; }
	public override string ToString() => string.Format("({0},{1},[{2}],{3},{4})",
			Id,
			ProgressMessage,
			string.Join(",", Items?.Select(i => i.ToString()) ?? Enumerable.Empty<string>()),
			ParallelCount,
			IsCancelled
		);
}

public sealed class RubyMultiOxtItem
{
	public string ReportName { get; set; }
	public double? Seconds { get; set; }
	public int? Titles_RowCount { get; set; }
	public bool? SigShowLetters { get; set; }
	public bool? DispColLetters { get; set; }
	public bool? DispRowLetters { get; set; }
	public string[] OxtLines { get; set; }
	public string ErrorType { get; set; }
	public string ErrorMessage { get; set; }

	public IEnumerable<string[]> WalkValues()
	{
		//int skip = 1 + (Titles_RowCount ?? 4) + 1 + 1 + (SigShowLetters == true && DispColLetters == true ? 1 : 0);
		foreach (string line in OxtLines.Skip(2))   // NOTE The new OXTNums lines always need a skip of 2 lines
		{
			yield return line.Split('\t').ToArray();
		}
	}

	public override string ToString() => ErrorType == null ? $"{GetType().Name}({ReportName},{Seconds:F2},{Titles_RowCount},{SigShowLetters},{DispColLetters},{DispRowLetters},#{OxtLines?.Length})" : $"({ErrorType}:{ErrorMessage})";
}
