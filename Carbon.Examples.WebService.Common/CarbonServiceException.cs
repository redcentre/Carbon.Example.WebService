using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Carbon.Examples.WebService.Common
{
	[Serializable]
	public sealed class CarbonServiceException : Exception
	{
		public CarbonServiceException(int code, string message)
			: base(message)
		{
			Code = code;
		}

		public CarbonServiceException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			Code = info.GetInt32(nameof(Code));
		}

		public int Code { get; }

		[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue(nameof(Code), Code);
			base.GetObjectData(info, context);
		}
	}
}
