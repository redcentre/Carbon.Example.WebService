using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lines = System.Collections.Generic.List<string>;

namespace Carbon.Examples.WebService.WebApi;

/// <summary>
/// A utility class to convert objects to-and-from a simple custom serialized format
/// based upon name=value text lines.
/// </summary>
public static partial class TextConvert
{
	public const string TextSerializeDateFormat = "s";

	public static bool CanReadType(Type _) => true;

	public static bool CanWriteType(Type? _) => true;

	#region Serialize Members

	/// <summary>
	/// Serializes an object into a multi-line string. This method is called by the API output formatter.
	/// </summary>
	public static string SerializeObject(object? value)
	{
		var lines = new Lines();
		SerializeObject(value, lines);
		return string.Join("\n", lines);
	}

	public static void SerializeObject(object? value, Lines lines)
	{
		if (value == null) return;
		SerializeToLines((dynamic?)value, lines);
	}

	/// <summary>
	/// This service only returns response type ResponseWrap«T» and it's the only type that will be passed into this method.
	/// The known simple properties are serialized first, then the generic T object type in property 'Data' is serialized by either
	/// default processing or dynamic dispatch to a custom serializer.
	/// </summary>
	static void SerializeToLines(object value, Lines lines)
	{
		lines.Add($"# Text serialize type {value.GetType().FullName}");
		var props = value.GetType().GetProperties();
		foreach (var prop in props)
		{
			object? val = prop.GetValue(value);
			if (val is string[] ss)
			{
				if (ss.Any(s => s?.Contains(',') == true)) throw new InvalidOperationException("String array values containing commas cannot be serialized as text/plain format.");
				lines.Add($"{prop.Name}={string.Join(',', ss)}");
			}
			else if (val is int[] ii)
			{
				lines.Add($"{prop.Name}={string.Join(',', ii)}");
			}
			else if (val is long[] ll)
			{
				lines.Add($"{prop.Name}={string.Join(',', ll)}");
			}
			else if (val is DateTime dt)
			{
				lines.Add($"{prop.Name}={dt:s}");
			}
			else if (val is byte[] buff)
			{
				lines.Add($"{prop.Name}={Convert.ToHexString(buff)}");
			}
			else if (prop.PropertyType.IsValueType)
			{
				lines.Add($"{prop.Name}={val}");
			}
			else
			{
				lines.Add($"{prop.Name}={val}");    // Lowest default (which might be useless)
			}
		}
	}

	#endregion

	#region Deserialize Members

	public static T? Deserialize<T>(string body, bool throwOnError) => (T?)Deserialize(typeof(T), body, throwOnError);

	/// <summary>
	/// This method is called by the API input formatter to deserialize a request from name=value text formatting.
	/// </summary>
	public static object? Deserialize(Type type, string body, bool throwOnError)
	{
		var list = new Lines();
		using (var reader = new StringReader(body))
		{
			string? line = reader.ReadLine();
			while (line != null)
			{
				list.Add(line);
				line = reader.ReadLine();
			}
		}
		var lines = list.ToArray();
		// If the project linking to this class adds any partial methods which
		// follow the naming convention Deserialize{TypeName} then that method
		// will be invoked to perform custom deserialization of the lines to a
		// specific object instance type.
		string custMethodName = $"Deserialize{type.Name}";
		MethodInfo? mi = typeof(TextConvert).GetMethod(custMethodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
		if (mi != null)
		{
			return mi.Invoke(null, [lines]);
		}
		// The fallback deserialization processing simply uses Name=Value pairs
		// to set the matching property values of a fabricated class instance.
		return DeserializeFromLines(type, lines, throwOnError);
	}

	/// <summary>
	/// Text lines deserialize processing constructs an instance of the desired type
	/// (assuming it's possible) then sets the property values by parsing text lines in the format
	/// <c>PropertyName=InvarianStringValue</c>. Lines starting with # are ignored. Certain types
	/// such as string[] and DateTime have special handling.
	/// </summary>
	static object DeserializeFromLines(Type type, string[] lines, bool throwOnError)
	{
		if (type.IsValueType || type.IsAbstract || type.IsInterface)
		{
			throw new NotSupportedException($"Text deserialize of type {type.Name}");
		}
		// ╔══════════════════════════════════════════════════════════════════════════╗
		// ║  Note that there is an inconvenient and delicate possible flaw around    ║
		// ║  here. An instance of the target Type is dynamically created and all     ║
		// ║  the properties will have their default values. If the incoming lines    ║
		// ║  to be deserialized do not cover all of the object properties, then      ║
		// ║  those not set will remain having their default values, which may be     ║
		// ║  undesirable or cause bugs. For now, the web service caller must         ║
		// ║  provide all of the propery values necessary to create a completely      ║
		// ║  filled object to their desired level. This issue was raised in Feb      ║
		// ║  2025 when Ruby C++ code would GET and PUT User records. It's being      ║
		// ║  looked at as a background task.                                         ║
		// ╚══════════════════════════════════════════════════════════════════════════╝
		var target = Activator.CreateInstance(type) ?? throw new NotSupportedException($"An instance of type {type.Name} cannot be activated");
		foreach (string line in lines)
		{
			if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) { continue; }
			Match m = Regex.Match(line, @"^(\w+)=(.*)$");
			if (!m.Success)
			{
				if (throwOnError)
				{
					throw new InvalidOperationException($"Text deserialize line unrecognised format: {line}");
				}
				continue;
			}
			string propname = m.Groups[1].Value;
			string rawvalue = m.Groups[2].Value;
			PropertyInfo? pinf = type.GetProperty(propname, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
			if (pinf == null)
			{
				if (throwOnError)
				{
					throw new InvalidOperationException($"Property Name={propname} not found on Type={type.Name}: {line}");
				}
				continue;
			}
			if (rawvalue.Length > 0)
			{
				object? value = null;
				// This is not general purpose code and deseralization of other
				// special types will need to be added here when required.
				if (pinf.PropertyType == typeof(string[]))
				{
					// Special case: string[] is split from a comma-joined list.
					value = rawvalue.Split(',');
				}
				if (pinf.PropertyType == typeof(int[]))
				{
					value = rawvalue.Split(',').Select(x => int.Parse(x)).ToArray();
				}
				if (pinf.PropertyType == typeof(long[]))
				{
					value = rawvalue.Split(',').Select(x => long.Parse(x)).ToArray();
				}
				else if (pinf.PropertyType == typeof(DateTime) || pinf.PropertyType == typeof(DateTime?))
				{
					// Special case: DateTime is converted from Universal sortable format.
					value = DateTime.ParseExact(rawvalue, TextSerializeDateFormat, DateTimeFormatInfo.InvariantInfo);
				}
				else
				{
					var conv = TypeDescriptor.GetConverter(pinf.PropertyType);
					if (conv.CanConvertFrom(typeof(string)) && conv.CanConvertTo(typeof(string)))
					{
						value = conv.ConvertFromInvariantString(rawvalue);
					}
				}
				if (value != null)
				{
					pinf.SetValue(target, value);
				}
			}
		}
		return target;
	}

	#endregion
}
