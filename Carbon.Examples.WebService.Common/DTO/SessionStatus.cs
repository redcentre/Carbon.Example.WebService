using System;

namespace Carbon.Examples.WebService.Common
{
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
}
