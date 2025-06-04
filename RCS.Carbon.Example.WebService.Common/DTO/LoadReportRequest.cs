namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class LoadReportRequest
{
	public LoadReportRequest(string name)
	{
		Name = name;
	}

	public string Name { get; set; }
}
