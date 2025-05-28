namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class LoadReportRequest
	{
		public LoadReportRequest(string name)
		{
			Name = name;
		}

		public string Name { get; set; }
	}
}
