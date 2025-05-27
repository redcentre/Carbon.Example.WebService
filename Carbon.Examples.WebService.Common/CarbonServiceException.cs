using System;

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

		public int Code { get; }


		public string[]? GetDataStrings()
		{
			if (Data["Strings1"] is string joined)
			{
				if (joined.Length == 0) return [];
				return joined.Split(',');
			}
			return null;
		}

		public void SetDataStrings(string[]? data)
		{
			Data.Add("Strings1", data == null ? null : string.Join(",", data));
		}
	}
}
