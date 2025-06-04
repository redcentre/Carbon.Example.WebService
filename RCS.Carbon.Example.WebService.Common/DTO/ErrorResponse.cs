namespace RCS.Carbon.Example.WebService.Common.DTO;

public enum ErrorResponseCode
{
	DuplicateSession = 301,
	GetLicenceIdFailed = 302,
	GetLicenceNameFailed = 303,
	DatabaseReadNotFound = 311,
	DatabaseDeleteNotFound = 312,
	OpenJobFailed = 321,
	JsonRootNotString = 331,
	JsonPropertyNotArray = 332,
	JsonRootBadFormat = 333,
	JsonBadNAme = 334,
	PandasLackParameters = 335,
	PandasBadCredentials = 336,
	PandasGenTabFailed = 338,
	NoSessionFound = 381,
	NotRoleAuthorised = 382,
	NoSessionHeader = 383,
	RequestFailed = 391,
	RequestFailedNoDetail = 392
}

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

	public ErrorResponse(ErrorResponseCode code, string message)
		: this(code, message, null, null)
	{
	}

	public ErrorResponse(ErrorResponseCode code, string message, string? details)
		: this(code, message, details, null)
	{
	}

	public ErrorResponse(ErrorResponseCode code, string message, string? details, object? data)
	{
		Code = (int)code;
		Message = message;
		Details = details;
		Data = data;
	}

	/// <summary>
	/// Numeric error code. The .NET class constructor takes an enumeration for documentation and safety,
	/// but the property is an integer for the convenience of clients on any platform.
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
