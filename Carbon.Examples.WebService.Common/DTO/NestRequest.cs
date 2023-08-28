using System.Collections.Generic;
using System.Linq;
using RCS.Carbon.Shared;

namespace Carbon.Examples.WebService.Common
{
	public sealed class NestRequest
	{
		public NestRequest()
		{
		}

		public NestRequest(IEnumerable<GenNode> axis, IEnumerable<string> variables)
		{
			Axis = axis.ToArray();
			Variables = variables.ToArray();
		}

		public GenNode[] Axis { get; set; }

		public string[] Variables { get; set; }
	}
}
