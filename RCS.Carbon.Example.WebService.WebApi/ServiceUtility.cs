using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Text.RegularExpressions;

namespace RCS.Carbon.Example.WebService.WebApi;

/// <ignore/>
public static class ServiceUtility
{
	/// <ignore/>
	public static string NiceObj(object? value, int maxlen = 80)
	{
		if (value == null) return "NULL";
		if (value is string s)
		{
			return NiceStr(s, maxlen);
		}
		var t = value.GetType();
		if (t.IsArray)
		{
			return $"{t.GetElementType()!.Name}[{((Array)value).Length}]";
		}
		return NiceStr(value.ToString(), maxlen);
	}

	/// <ignore/>
	public static string NiceStr(string? s, int maxlen = 40)
	{
		if (s == null) return "NULL";
		if (s.Length == 0) return "BLANK";
		string sfx = "";
		if (s.Length > maxlen)
		{
			s = s[..maxlen];
			sfx = "\u2026";
		}
		s = s.Replace("\t", @"\t").Replace("\n", @"\n").Replace("\r", @"\r");
		s = Regex.Replace(s, "[\x00-\x1f]", ".");
		return s + sfx;
	}

	/// <ignore/>
	public static IEnumerable<string> ListStringLines(string s)
	{
		using var reader = new StringReader(s);
		string? line = reader.ReadLine();
		while (line != null)
		{
			yield return line;
			line = reader.ReadLine();
		}
	}

	/// <ignore/>
	public static ulong HashApiKey(string apiKey)
	{
		var buff1 = Encoding.UTF8.GetBytes(apiKey);
		var buff2 = XxHash64.Hash(buff1, 1111111111111111111L);
		return BitConverter.ToUInt64(buff2);
	}
}
