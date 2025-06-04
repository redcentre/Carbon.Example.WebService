using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class RunSpecRequest
{
	public string Name { get; set; }
	public XDisplayProperties DProps { get; set; }
	public TableSpec Spec { get; set; }

}
