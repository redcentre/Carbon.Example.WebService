namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class SaveReportRequest
	{
		public SaveReportRequest(string name, string? sub)
		{
			Name = name;
			Sub = sub;
		}

		public string Name { get; set; }
		public string? Sub { get; set; }
	}
}
