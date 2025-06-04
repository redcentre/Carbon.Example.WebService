namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class SessionInfo
{
	public string? SessionId { get; set; }
	public string? Id { get; set; }
	public string? Name { get; set; }
	public string? Email { get; set; }
	public string[]? Roles { get; set; }
	public string[]? VartreeNames { get; set; }
	public SessionCust[]? SessionCusts { get; set; }
	public int ProcessorCount { get; set; }
	public string OS { get; set; }
	public override string ToString() => $"{GetType().Name}({SessionId},{Id},{Name},R{Roles?.Length}, V{VartreeNames?.Length},C{SessionCusts?.Length})";
}
