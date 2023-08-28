using System.Linq;

namespace Carbon.Examples.WebService.Common
{
	public sealed class MultiOxtRequest
	{
		public string[] ReportNames { get; set; }
		public FilterPair[] Filters { get; set; }
		public bool TableOnly { get; set; }
		public int ParallelCount { get; set; }
		public override string ToString() => string.Format("([{0}],[{1}],{2},{3})",
				string.Join(",", ReportNames ?? Enumerable.Empty<string>()),
				string.Join(",", Filters?.Select(f => f.ToString()) ?? Enumerable.Empty<string>()),
				TableOnly,
				ParallelCount
			);
	}

	public sealed class FilterPair
	{
		public FilterPair(string label, string? syntax, bool isPeriod = false)
		{
			Label = label;
			Syntax = syntax;
			IsPeriod = isPeriod;
		}
		public string Label { get; set; }
		public string? Syntax { get; set; }
		public bool IsPeriod { get; set; }
		public override string ToString() => $"({Label},{Syntax},{(IsPeriod ? 'P' : 'F')})";
	}
}
