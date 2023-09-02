using System;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace Carbon.Examples.WebService.Logging;

public static class WebLog
{
	public static void Startup(IConfiguration configuration)
	{
		string appconnect = configuration["CarbonApi:ApplicationStorageConnect"]!;
		string logtable = configuration["CarbonApi:LogTableName"] ?? "ServiceLog";
		string? logpk = configuration["CarbonApi:LogPartitionKey"];
		if (string.IsNullOrEmpty(logpk))
		{
			logpk = Environment.MachineName;
		}
		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(configuration)
			.WriteTo.AzureTableStorage(
				appconnect,
				storageTableName: logtable,
				documentFactory: new WebDocgen(logpk)
			)
			.CreateLogger();
		//Serilog.Debugging.SelfLog.Enable(m => System.Diagnostics.Trace.WriteLine(m));
	}

	public static void Shutdown()
	{
		Log.CloseAndFlush();
	}

	[MessageTemplateFormatMethod("messageTemplate")]
	public static void Verbose(string messageTemplate, params object?[] propertyValues) => Log.Verbose(messageTemplate, propertyValues);

	[MessageTemplateFormatMethod("messageTemplate")]
	public static void Debug(string messageTemplate, params object?[] propertyValues) => Log.Debug(messageTemplate, propertyValues);

	[MessageTemplateFormatMethod("messageTemplate")]
	public static void Info(string messageTemplate, params object?[] propertyValues) => Log.Information(messageTemplate, propertyValues);

	[MessageTemplateFormatMethod("messageTemplate")]
	public static void Warn(string messageTemplate, params object?[] propertyValues) => Log.Warning(messageTemplate, propertyValues);

	[MessageTemplateFormatMethod("messageTemplate")]
	public static void Error(Exception? error, string messageTemplate, params object?[] propertyValues) => Log.Error(error, messageTemplate, propertyValues);

	[MessageTemplateFormatMethod("messageTemplate")]
	public static void Fatal(Exception? error, string messageTemplate, params object?[] propertyValues) => Log.Fatal(error, messageTemplate, propertyValues);
}