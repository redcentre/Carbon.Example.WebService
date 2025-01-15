using System;
using System.Linq;

namespace Carbon.Examples.WebService.WebApi;

/// <summary>
/// Holds information about a single web service session.
/// </summary>
sealed class SessionItem
{
	public SessionItem(string sessionId)
	{
		SessionId = sessionId;
		CreatedUtc = DateTime.UtcNow;
	}

	public string SessionId { get; }
	public DateTime CreatedUtc { get; }
	// The following members are for activity tracking
	public DateTime? LastActivityUtc { get; set; }
	public string? LastActivity { get; set; }
	public int ActivityCount { get; set; }
	public string? OpenCustomerName { get; set; }
	public string? OpenJobName { get; set; }
	public string? OpenVartreeName { get; set; }
	public string? OpenReportName { get; set; }
	// Licence information for convenience
	public string? UserId { get; set; }
	public string? UserName { get; set; }
	public string[] Roles { get; set; } = Array.Empty<string>();
	public string[][]? CustStorageKeys { get; set; }

	public string? FindStorageKey(string customerName, bool throwIfNotFound)
	{
		string[]? custpair = CustStorageKeys?.FirstOrDefault(c => c[0] == customerName);
		if (custpair == null && throwIfNotFound) throw new Exception($"Customer name '{customerName}' is not found in the session. The account Id {UserId} does not have access to the customer.");
		string? sk = custpair?.ElementAtOrDefault(1);
		if (sk == null && throwIfNotFound) throw new Exception($"Customer name '{customerName}' does not have a storage key assigned.");
		return sk;
	}

	public string? OpenStorageKey => CustStorageKeys?.FirstOrDefault(c => c[0] == OpenCustomerName)?.ElementAtOrDefault(1);

	public override string ToString() => $"({SessionId},{CreatedUtc:s},{LastActivity},{OpenCustomerName},{OpenJobName},{OpenVartreeName},{OpenReportName})";
}
