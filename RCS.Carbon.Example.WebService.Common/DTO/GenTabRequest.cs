using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class GenTabRequest
	{
#pragma warning disable CS8618     // Empty ctor required for JSON serialization                                                   
		public GenTabRequest()
		{
		}
#pragma warning restore CS8618

		public GenTabRequest(string? name, string top, string side, string? filter, string? weight, XSpecProperties sprops, XDisplayProperties dprops)
		{
			Name = name;
			Top = top;
			Side = side;
			Filter = filter;
			Weight = weight;
			SProps = sprops;
			DProps = dprops;
		}

		public string? Name { get; set; }
		public string Top { get; set; }
		public string Side { get; set; }
		public string? Filter { get; set; }
		public string? Weight { get; set; }
		public XSpecProperties SProps { get; set; }
		public XDisplayProperties DProps { get; set; }
	}
}
