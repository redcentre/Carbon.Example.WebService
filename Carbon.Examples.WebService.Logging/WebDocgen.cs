using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Azure.Data.Tables;
using Serilog.Events;
using Serilog.Sinks.AzureTableStorage;

namespace Carbon.Examples.WebService.Logging;

internal class WebDocgen : IDocumentFactory
{
	readonly string _pk;

	public WebDocgen(string partitionKey)
	{
		_pk = partitionKey;
	}

	public TableEntity Create(LogEvent logEvent, AzureTableStorageSinkOptions options, IKeyGenerator keyGenerator)
	{
		var row = new TableEntity()
		{
			PartitionKey = _pk,
			RowKey = DateTime.UtcNow.Ticks.ToString(),
			Timestamp = logEvent.Timestamp
		};
		//foreach (var prop in logEvent.Properties)
		//{
		//	var key = prop.Key;
		//	var value = ConvertValue(prop.Value, null, options.FormatProvider);
		//	Trace.WriteLine($"@@@@ {key} {prop.Value} -> {value?.GetType().Name} | {value}");
		//	if (!logEvent.Properties.Any(p => TakePropNames.Contains(p.Key))) continue;
		//	if (prop.Value is StructureValue sv)
		//	{
		//		if (key == "EventId")
		//		{
		//			string ids = Regex.Match((string)value!, @"\bId:\s*(\d+)").Groups[1].Value;
		//			//row[key] = int.Parse(ids);
		//		}
		//	}
		//	else
		//	{
		//		//row[key] = value;
		//	}
		//}

		T? GetScalarVal<T>(IReadOnlyDictionary<string, LogEventPropertyValue> props, AzureTableStorageSinkOptions options, string key)
		{
			if (!props.TryGetValue(key, out var lepv)) return default;
			if (lepv == null) return default;
			var scalar = (ScalarValue)lepv;
			return (T?)scalar.Value;
		}
		string? GetStructVal(IReadOnlyDictionary<string, LogEventPropertyValue> props, AzureTableStorageSinkOptions options, string key)
		{
			if (!props.TryGetValue(key, out var lepv)) return default;
			if (lepv == null) return default;
			var sv = (StructureValue)lepv;
			return sv?.ToString();
		}
		T? AddVal<T>(T? value, string key, string? columnName = null)
		{
			if (value != null) row[columnName ?? key] = value;
			return value;
		}
		T? GetAdd<T>(IReadOnlyDictionary<string, LogEventPropertyValue> props, AzureTableStorageSinkOptions options, string key, string? columnName = null)
		{
			return AddVal<T>(GetScalarVal<T>(logEvent.Properties, options, key), key, columnName);
		}
		GetAdd<int>(logEvent.Properties, options, "RequestSequence", "Sequence");
		GetAdd<string>(logEvent.Properties, options, "Method");
		GetAdd<string>(logEvent.Properties, options, "Path");
		GetAdd<int>(logEvent.Properties, options, "Status");
		GetAdd<int>(logEvent.Properties, options, "ThreadId");
		GetAdd<int>(logEvent.Properties, options, "ProcessId");
		GetAdd<string>(logEvent.Properties, options, "Sid");
		string? evid = GetStructVal(logEvent.Properties, options, "EventId");
		var m = Regex.Match(evid ?? string.Empty, @"\bId:\s*(\d+)");
		if (m.Success)
		{
			row["EventId"] = int.Parse(m.Groups[1].Value);
		}
		string? source = GetScalarVal<string>(logEvent.Properties, options, "SourceContext");
		AddVal(source, "SourceContext", "Source");
		GetAdd<double?>(logEvent.Properties, options, "Seconds");
		row["Level"] = (int)logEvent.Level;
		row["Message"] = source == "FILT" ? null : logEvent.RenderMessage(options.FormatProvider);
		GetAdd<int?>(logEvent.Properties, options, "Code");
		GetAdd<string>(logEvent.Properties, options, "ErrorPath");
		GetAdd<string>(logEvent.Properties, options, "ErrorType");
		GetAdd<string>(logEvent.Properties, options, "ErrorMessage");
		GetAdd<string>(logEvent.Properties, options, "ErrorStack");
		if (logEvent.Exception != null)
		{
			row["ErrorType"] = logEvent.Exception.GetType().Name;
			row["ErrorMessage"] = logEvent.Exception.Message;
			row["ErrorStackTrace"] = logEvent.Exception.StackTrace;
		}
		return row;
	}

	protected object? ConvertValue(LogEventPropertyValue value, string? format = null, IFormatProvider? formatProvider = null)
	{
		return value switch
		{
			ScalarValue scalarValue => SimplifyScalar(scalarValue.Value),
			DictionaryValue dictionaryValue => dictionaryValue.ToString(format, formatProvider),
			SequenceValue sequenceValue => sequenceValue.ToString(format, formatProvider),
			StructureValue structureValue => structureValue.ToString(format, formatProvider),
			_ => null
		};
	}

	private static object? SimplifyScalar(object? value)
	{
		return value switch
		{
			byte[] bytesValue => bytesValue,
			bool boolValue => boolValue,
			DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue,
			DateTime dateTimeValue => dateTimeValue,
			double doubleValue => doubleValue,
			Guid guidValue => guidValue,
			int intValue => intValue,
			long longValue => longValue,
			string stringValue => stringValue,
			_ => value?.ToString()
		};
	}
}
