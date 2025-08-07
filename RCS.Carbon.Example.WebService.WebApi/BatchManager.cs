using System.Collections.Generic;

namespace RCS.Carbon.Example.WebService.WebApi;

/// <summary>
/// A static wrapper class for a dictionary of Platinum report batches that live for the life of the process.
/// </summary>
static class BatchManager
{
	static readonly Dictionary<string, BatchData> batches = [];

	public static void Add(BatchData data) => batches.Add(data.Response.Id, data);
	
	public static BatchData? Get(string id) => batches.GetValueOrDefault(id);

	public static string[] ListIds() => [.. batches.Keys];

	public static bool Remove(string id) => batches.Remove(id);

	public static IEnumerable<BatchData> ListBatches()
	{
		foreach (var kvp in batches) yield return kvp.Value;
	}
}
