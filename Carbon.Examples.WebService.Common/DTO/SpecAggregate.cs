using System.Linq;
using RCS.Carbon.Shared;

namespace Carbon.Examples.WebService.Common
{
	public sealed class SpecAggregate
	{
		public GenNode[] VariableTree { get; set; }
		public GenNode[]? AxisTree { get; set; }
		public GenNode[]? FunctionTree { get; set; }
		public TableSpec Spec { get; set; }

		public override string ToString()
		{
			return string.Format("SpecAggregate({0},{1},{2},{3})",
				GenNode.WalkNodes(VariableTree)?.Count(),
				GenNode.WalkNodes(AxisTree)?.Count(),
				GenNode.WalkNodes(FunctionTree)?.Count(),
				Spec
			);
		}

	}
}
