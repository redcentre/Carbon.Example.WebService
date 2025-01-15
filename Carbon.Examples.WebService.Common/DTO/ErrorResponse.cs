namespace Carbon.Examples.WebService.Common
{
	/// <summary>
	/// A standard response for all errors from the web service.
	/// </summary>
	public sealed class ErrorResponse
	{
#pragma warning disable CS8618     // Empty ctor required for JSON serialization                                                   
		public ErrorResponse()
		{
		}
#pragma warning restore CS8618

		public ErrorResponse(int code, string message)
			: this(code, message, null)
		{
		}

		public ErrorResponse(int code, string message, string? details)
		{
			Code = code;
			Message = message;
			Details = details;
		}

		public ErrorResponse(int code, string message, string? details, object? data)
		{
			Code = code;
			Message = message;
			Details = details;
			Data = data;
		}

		/// <summary>
		/// Numeric error code.
		/// </summary>
		public int Code { get; set; }

		/// <summary>
		/// Error summary message.
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// Optional detailed error information.
		/// </summary>
		public string? Details { get; set; }

		/// <summary>
		/// Optional arbitrary data associated with the error that will be serialized into the error response body.
		/// </summary>
		public object? Data { get; set; }

		public override string ToString() => $"{GetType().Name}({Code},{Message},{Details})";

	}
}
