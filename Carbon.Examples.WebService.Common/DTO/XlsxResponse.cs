namespace Carbon.Examples.WebService.Common
{
	public sealed class XlsxResponse
	{
		public string ReportName { get; set; }
		public int ExcelBytes { get; set; }
		public string ExcelUri { get; set; }
		public bool ShowFrequencies { get; set; }
		public bool ShowColPercents { get; set; }
		public bool ShowRowPercents { get; set; }
		public bool ShowSignificance { get; set; }
		public string? OriginalFilter { get; set; }
		public double ExcelSecs { get; set; }
		public double UploadSecs { get; set; }
		public override string ToString() => $"({ReportName},{ExcelBytes},{ExcelUri},{ShowFrequencies},{ShowColPercents},{ShowRowPercents},{ShowSignificance},{OriginalFilter},{ExcelSecs},{UploadSecs})";
	}
}
