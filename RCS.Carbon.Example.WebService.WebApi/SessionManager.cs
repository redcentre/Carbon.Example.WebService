using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text.Json;
using System.Text.RegularExpressions;
using RCS.Licensing.Provider.Shared;

namespace RCS.Carbon.Example.WebService.WebApi;

/// <summary>
/// <para>
/// A static app-wide wrapper class over a dictionary that maintains the state of web service sessions.
/// The Carbon engine state for a session is stored as a set of text files with the session id in the
/// name prefix. Carbon's state is an opaque array of strings.
/// </para>
/// <para>
/// Because web services are stateless, the session Id is a key to loading the Carbon engine state at
/// the start of each request.
/// </para>
/// <para>
/// Session state is cached for a configurable number seconds (default is 60). This will help clients
/// who make bursts of requests, which is very likely for typical usage scenarios.
/// </para>
/// </summary>
static class SessionManager
{
	const string AnonymousSessionId = "(anonymous)";
	static readonly DirectoryInfo sessDir;
	static readonly FileInfo sessFile;
	static Dictionary<string, SessionItem>? map;
	static readonly JsonSerializerOptions jopts = new() { WriteIndented = true };

	/// <summary>
	/// The cache sliding seconds for sessions.
	/// </summary>
	public static int CacheSlidingSeconds { get; set; } = 60;

	static SessionManager()
	{
		sessDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "CarbonService"));
		sessFile = new FileInfo(Path.Combine(sessDir.FullName, "session-state-v1.json"));
	}

	#region Session Map

	static public bool StartSession(string sessionId, LicenceInfo licence)
	{
		var item = new SessionItem(sessionId)
		{
			UserId = licence.Id,
			UserName = licence.Name,
			Roles = licence.Roles ?? [],
			CustStorageKeys = licence.Customers.Select(c => new string[] { c.Name, c.StorageKey }).ToArray()
		};
		if (map!.TryGetValue(sessionId, out var sessionItem))
		{
			map[sessionId] = item;
			Trace($"Start (replace) - {item}");
			Save();
			return false;
		}
		else
		{
			map.Add(sessionId, item);
			Trace($"Start (add) - {item}");
			Save();
			return true;
		}
	}

	static public bool EndSession(string sessionId)
	{
		if (map!.TryGetValue(sessionId, out SessionItem? sessionItem))
		{
			map.Remove(sessionId);
			Trace($"End {sessionId}");
			Save();
			return true;
		}
		else
		{
			Trace($"End not found: {sessionId}");
			return false;
		}
	}

	static public bool SetCustomerJob(string sessionId, string? customerName, string? jobName, string? vartreeName)
	{
		map!.TryGetValue(sessionId, out SessionItem? item);
		if (item != null)
		{
			item.OpenCustomerName = customerName;
			item.OpenJobName = jobName;
			item.OpenVartreeName = vartreeName;
			item.ActivityCount++;
			item.LastActivity = $"Open {(customerName ?? "null")}:{(jobName ?? "null")}";
			item.LastActivityUtc = DateTime.UtcNow;
			Save();
			return true;
		}
		return false;
	}

	static public bool SetReportName(string sessionId, string? name)
	{
		map!.TryGetValue(sessionId, out SessionItem? item);
		if (item != null)
		{
			item.OpenReportName = name;
			item.ActivityCount++;
			item.LastActivity = $"Load {(name ?? "null")}";
			item.LastActivityUtc = DateTime.UtcNow;
			Save();
			return true;
		}
		return false;
	}

	static public bool UpdateActivity(string sessionId, string description)
	{
		string id = sessionId ?? AnonymousSessionId;
		map!.TryGetValue(id, out SessionItem? item);
		if (item == null)
		{
			if (!map.TryGetValue(AnonymousSessionId, out item))
			{
				item = new SessionItem(AnonymousSessionId);
				map.Add(AnonymousSessionId, item);
				Trace($"Add mock {AnonymousSessionId}");
			}
		}
		item.ActivityCount++;
		item.LastActivity = description;
		item.LastActivityUtc = DateTime.UtcNow;
		Trace($"Activity {id} {description}");
		Save();
		return true;
	}

	static public SessionItem? FindSession(string sessionId, bool throwIfNotFound = false)
	{
		if (map!.TryGetValue(sessionId, out var item)) return item;
		if (throwIfNotFound) throw new Exception($"Session id '{sessionId}' is not active");
		return null;
	}

	static public SessionItem[] FindSessionsForId(string userId)
	{
		return map!.Where(x => x.Value.UserId == userId).Select(x => x.Value).ToArray();
	}

	static public SessionItem[] FindSessionsForName(string userName)
	{
		return map!.Where(x => string.Compare(x.Value.UserName, userName, true) == 0).Select(x => x.Value).ToArray();
	}

	static public Tuple<string, SessionItem>[] ListSessions()
	{
		return map!.Select(m => Tuple.Create(m.Key, m.Value)).ToArray();
	}

	static public int Cleanup(int olderThanDays)
	{
		// Step 1 - Purge 'old' sessions
		int cleanCount = 0;
		foreach (var kvp in map!.ToArray())
		{
			if (kvp.Value.SessionId == AnonymousSessionId) continue;
			TimeSpan? span = DateTime.UtcNow - kvp.Value.LastActivityUtc;
			if (span?.TotalDays >= olderThanDays)
			{
				map!.Remove(kvp.Key);
				DeleteState(kvp.Key);
				++cleanCount;
			}
		}
		// Step 2 - Remove orphan map keys
		int orphanCount = 0;
		foreach (var kvp in map!.ToArray())
		{
			if (kvp.Value.SessionId == AnonymousSessionId) continue;
			FileInfo[] files = sessDir.GetFiles($"State-{kvp.Key}-*.*");
			if (files.Length == 0)
			{
				map!.Remove(kvp.Key);
				++orphanCount;
			}
		}
		if (cleanCount > 0 || orphanCount > 0)
		{
			Save();
		}
		return cleanCount;
	}

	#endregion

	#region State

	static readonly object stateLock = new object();

	/// <summary>
	/// We know at this point what sort of files are being serialized, so we put nice
	/// extensions on them to help browsing and debugging. In reality they could all
	/// be .foo files if no one ever looked at them.
	/// </summary>
	static readonly string[] StateExtensions = new string[] { ".json", ".json", ".ini", ".xml" };

	static public void SaveState(string sessionId, string?[] state)
	{
		lock (stateLock)
		{
			for (int i = 0; i < state.Length; i++)
			{
				string ext = StateExtensions.ElementAtOrDefault(i) ?? ".txt";
				string filename = Path.Combine(sessDir.FullName, $"State-{sessionId}-{i:D2}{ext}");
				File.WriteAllText(filename, state[i] ?? String.Empty);
			}
			var policy = new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromSeconds(CacheSlidingSeconds), RemovedCallback = CacheCallback };
			MemoryCache.Default.Set(sessionId, state, policy);
		}
		Trace($"CACHE SET {sessionId} {NiceState(state)} ({CacheSlidingSeconds})");
	}

	static public string?[] LoadState(string sessionId)
	{
		lock (stateLock)
		{
			string?[] results = (string?[])MemoryCache.Default.Get(sessionId);
			if (results != null)
			{
				Trace($"CACHE HIT {sessionId} {NiceState(results)}");
				return results;
			}
			FileInfo[] files = sessDir.GetFiles($"State-{sessionId}-*.*");
			var query = from f in files
						let m = Regex.Match(f.Name, @"-(\d\d)\.")
						let ix = int.Parse(m.Groups[1].Value)
						let state = File.ReadAllText(f.FullName)
						select new { ix, state };
			results = query.OrderBy(q => q.ix).Select(q => q.state.Length == 0 ? null : q.state).ToArray();
			Trace($"CACHE MISS {NiceState(results)}");
			return results;
		}
	}

	static public long DeleteState(string sessionId)
	{
		long total = 0;
		lock (stateLock)
		{
			FileInfo[] files = sessDir.GetFiles($"State-{sessionId}-*.*");
			foreach (var file in files)
			{
				try
				{
					total += file.Length;
					file.Delete();
					++total;
				}
				catch (Exception ex)
				{
					Trace($"{ex.Message}");
				}
			}
			object o = MemoryCache.Default.Remove(sessionId);
			Trace($"CACHE REMOVE {sessionId} {(o != null)} {total} KB");
			return total;
		}
	}

	static void CacheCallback(CacheEntryRemovedArguments args)
	{
		var cache = (MemoryCache)args.Source;
		var state = (string?[])args.CacheItem.Value;
		if (args.RemovedReason != CacheEntryRemovedReason.Removed)
		{
			Trace($"CACHE {args.RemovedReason} {args.CacheItem.Key} {NiceState(state)}");
		}
	}

	static string NiceState(string?[] state)
	{
		int total = state.Sum(s => s?.Length ?? 0);
		double kb = total / 1024.0;
		string join = string.Join(",", state.Select(s => s?.Length));
		return $"[{join}] {kb:F1} KB";
	}

	#endregion

	#region Session Persist

	static public void Load()
	{
		Trace("Load");
		if (!sessDir.Exists)
		{
			sessDir.Create();
			Trace($"Created {sessDir}");
		}
		if (sessFile.Exists)
		{
			try
			{
				lock (savelock)
				{
					string json = File.ReadAllText(sessFile.FullName);
					map = JsonSerializer.Deserialize<Dictionary<string, SessionItem>>(json);
					Trace($"Loaded {sessFile.FullName}");
				}
			}
			catch (Exception ex)
			{
				map = new Dictionary<string, SessionItem>();
				Trace($"Recover - {ex.Message}");
				Save();
			}
		}
		else
		{
			map = new Dictionary<string, SessionItem>();
			Trace($"Created");
			Save();
		}
	}

	readonly static object savelock = new();

	static void Save()
	{
		lock (savelock)
		{
			string json = JsonSerializer.Serialize(map, jopts);
			File.WriteAllText(sessFile.FullName, json);
		}
	}

	#endregion

	static void Trace(string _)
	{
		//System.Diagnostics.Trace.WriteLine($"SESS {message}");
	}
}
