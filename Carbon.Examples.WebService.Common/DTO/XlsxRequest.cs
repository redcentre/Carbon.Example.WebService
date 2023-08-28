namespace Carbon.Examples.WebService.Common
{
	public sealed class XlsxRequest
	{
		public XlsxRequest()
		{
		}

		public XlsxRequest(string reportName, bool isLoad)
		{
			ReportName = reportName;
			IsLoad = isLoad;
		}

		public string ReportName { get; set; }
		public bool IsLoad { get; set; }
	}
}
