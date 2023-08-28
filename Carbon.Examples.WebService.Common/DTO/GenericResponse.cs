namespace Carbon.Examples.WebService.Common
{
	public sealed class GenericResponse
	{
		public GenericResponse()
		{
		}

		public GenericResponse(int code, string message)
		{
			Code = code;
			Message = message;
		}

		public int Code { get; set; }
		public string Message { get; set; }

		public override string ToString() => $"{GetType().Name}({Code},{Message})";
	}
}
