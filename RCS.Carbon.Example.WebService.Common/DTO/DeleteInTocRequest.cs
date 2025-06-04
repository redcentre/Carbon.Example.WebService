namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class DeleteInUserTocRequest
{
	public DeleteInUserTocRequest()
	{
	}

	public DeleteInUserTocRequest(string name)
	{
		Name = name;
	}

	public string Name { get; set; }
}
