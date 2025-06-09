using System;

namespace RCS.Carbon.Example.WebService.Common.DTO;

/// <summary>
/// A public class with summary information about a session held in the web service.
/// The internal <c>SessionItem</c> class in the service is stipped down to the following
/// properties for some public endpoints.
/// </summary>
public sealed class SessionStatus
{
	public const string AnonymousSessionId = "(anonymous)";
	public string? SessionId { get; set; }
	public string? UserId { get; set; }
	public string? UserName { get; set; }
	public DateTime CreatedUtc { get; set; }
	public DateTime? LastActivityUtc { get; set; }
	public string? LastActivity { get; set; }
	public int ActivityCount { get; set; }
	public string? OpenCustomerName { get; set; }
	public string? OpenJobName { get; set; }
	public string? OpenVartreeName { get; set; }
	public string? OpenReportName { get; set; }
}
