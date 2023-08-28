using System;
using Serilog.Events;
using Serilog.Sinks.AzureTableStorage;

namespace Carbon.Examples.WebService.Logging;

sealed class WebKeyGenerator : IKeyGenerator
{
	readonly string pk;

	public WebKeyGenerator(string? partitionKey)
	{
		pk = partitionKey ?? Environment.MachineName;
	}

	public string GeneratePartitionKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
	{
		return pk;
	}

	public string GenerateRowKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
	{
		return DateTime.UtcNow.Ticks.ToString();
	}
}
