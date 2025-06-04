namespace RCS.Carbon.Example.WebService.Common.DTO;

public enum PandasFormat
{
	Type1,
	Type2,
	Type3
}

public sealed class GenTabPandasRequest
{
#pragma warning disable CS8618     // Empty ctor required for JSON serialization                                                   
	public GenTabPandasRequest()
	{
	}
#pragma warning restore CS8618

	public GenTabPandasRequest(PandasFormat type, string top, string side, string? filter, string? weight)
	{
		Format = type;
		Top = top;
		Side = side;
		Filter = filter;
		Weight = weight;
	}

	public PandasFormat Format { get; set; }
	public string Top { get; set; }
	public string Side { get; set; }
	public string? Filter { get; set; }
	public string? Weight { get; set; }
}
