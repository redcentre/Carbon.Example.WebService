using RCS.Carbon.Shared;

namespace RCS.Carbon.Example.WebService.Common
{
	public sealed class OpenCloudJobResponse
	{
#pragma warning disable CS8618     // Empty ctor required for JSON serialization                                                   
		public OpenCloudJobResponse()
		{
		}
#pragma warning restore CS8618

		public OpenCloudJobResponse(XDisplayProperties? dprops, string[]? vartreeNames, string[]? axisTreeNames, GenNode[]? toc, GenNode[]? drillFilters)
		{
			DProps = dprops;
			VartreeNames = vartreeNames;
			AxisTreeNames = axisTreeNames;
			Toc = toc;
			DrillFilters = drillFilters;
		}

		public XDisplayProperties? DProps { get; set; }
		public string[]? VartreeNames { get; set; }
		public string[]? AxisTreeNames { get; set; }
		public GenNode[]? Toc { get; set; }
		public GenNode[]? DrillFilters { get; set; }
		public bool ShowCaseFilter { get; set; }
		public bool ShowAxisLocks { get; set; }
		public bool TreesDescOnly { get; set; }
		public int Cases { get; set; }
		public bool TryAzureTemp { get; set; }

		public override string ToString() => $"{GetType().Name}(V{VartreeNames?.Length},A{AxisTreeNames?.Length},T{Toc?.Length},D{DrillFilters?.Length},C{Cases},{ShowCaseFilter},{ShowAxisLocks},{TreesDescOnly},{TryAzureTemp},{DProps})";
	}
}
