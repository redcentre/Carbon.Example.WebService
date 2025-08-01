using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class GenTabPlatinumRequest
{
	public GenTabPlatinumRequest(string name, string top, string side, string? filter = null, string? weight = null, XSpecProperties? sprops = null, PlatinumProperties? pprops = null)
	{
		Name = name;
		Top = top;
		Side = side;
		Filter = filter;
		Weight = weight;
		SProps = sprops;
		PProps = pprops;
	}

	public string Name { get; set; }
	public string Top { get; set; }
	public string Side { get; set; }
	public string? Filter { get; set; }
	public string? Weight { get; set; }
	public XSpecProperties? SProps { get; set; }
	public PlatinumProperties? PProps { get; set; }
}
