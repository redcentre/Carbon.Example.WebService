namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class ReadFileRequest
{
#pragma warning disable CS8618     // Empty ctor required for JSON serialization                                                   
	public ReadFileRequest()
	{
	}
#pragma warning restore CS8618

	public ReadFileRequest(string name)
	{
		Name = name;
	}

	public string Name { get; set; }
}
