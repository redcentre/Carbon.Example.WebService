using System;

namespace RCS.Carbon.Example.WebService.Common.DTO;

public sealed class SessionStatus
{
	public string? SessionId { get; set; }
	public string? UserId { get; set; }
	public string? UserName { get; set; }
	public DateTime CreatedUtc { get; set; }
	public DateTime? LastActivityUtc { get; set; }
	public string? LastActivity { get; set; }
	public int ActivityCount { get; set; }
}
