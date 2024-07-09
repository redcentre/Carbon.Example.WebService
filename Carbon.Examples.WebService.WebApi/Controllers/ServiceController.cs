using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Carbon.Examples.WebService.Common;
using Microsoft.AspNetCore.Mvc;
using RCS.Carbon.Licensing.Shared;
using RCS.Carbon.Shared;
using RCS.Carbon.Tables;
using RCS.Carbon.Variables;

namespace Carbon.Examples.WebService.WebApi.Controllers;

partial class ServiceController
{
	async Task<ActionResult<int>> MockErrorImpl(int number)
	{
		if (number == Guid.NewGuid().GetHashCode())
		{
			return await Task.FromResult(number);   // Buy a lottery ticket if this happens!
		}
		throw new Exception($"This is a fake error for request number {number}");
	}

	async Task<ActionResult<ServiceInfo>> GetServiceInfoImpl()
	{
		var asm = typeof(Program).Assembly;
		var an = asm.GetName();
		var casm = typeof(VEngine).Assembly;
		var can = casm.GetName();
		ILicensingProvider lic = (ILicensingProvider)HttpContext.RequestServices.GetService(typeof(ILicensingProvider))!;
		var info = new ServiceInfo()
		{
			Version = an.Version!.ToString(),
			Build = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
			FileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version,
			CarbonVersion = can.Version!.ToString(),
			CarbonFileVersion = casm.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version,
			CarbonBuild = casm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
			Copyright = asm.GetCustomAttribute<AssemblyCopyrightAttribute>()!.Copyright,
			Company = asm.GetCustomAttribute<AssemblyCompanyAttribute>()!.Company,
			Product = asm.GetCustomAttribute<AssemblyProductAttribute>()!.Product,
			Title = asm.GetCustomAttribute<AssemblyTitleAttribute>()!.Title,
			Description = asm.GetCustomAttribute<AssemblyDescriptionAttribute>()!.Description,
			HostMachine = Environment.MachineName,
			HostAccount = Environment.UserName,
			ProcessorCount = Environment.ProcessorCount,
			TempFolder = Path.GetTempPath(),
			LicensingProvider = lic.Name,
			LicensingSummary = lic.ConfigSummary
		};
		return await Task.FromResult(info);
	}

	async Task<ActionResult<CacheInfo>> GetCacheInfoImpl(int maxnames)
	{
		var info = VEngine.GetCacheInfo(maxnames);
		return await Task.FromResult(info);
	}

	async Task<ActionResult<int>> DeleteCacheFilesImpl(string glob)
	{
		int count = VEngine.DeleteCacheInfo(glob);
		return await Task.FromResult(count);
	}

	async Task<ActionResult<bool>> StartLogImpl()
	{
		VarLib.StartLog();
		return await Task.FromResult(true);
	}

	async Task<ActionResult<bool>> EndLogImpl()
	{
		VarLib.EndLog();
		return await Task.FromResult(true);
	}

	async Task<ActionResult<bool>> ClearLogImpl()
	{
		VarLib.ClearLog();
		return await Task.FromResult(true);
	}

	async Task<ActionResult<string>> ListLogImpl()
	{
		string body = VarLib.GetLog();
		return await Task.FromResult(body);
	}

	async Task<ActionResult<string[]>> ReadTiming1Impl(ReadTimingRequest1 request)
	{
		var watch = new Stopwatch();
		var lines = new List<string>();
		var svc = new BlobServiceClient(request.AzConnect);
		var cc = svc.GetBlobContainerClient(request.Container);
		string[] names = request.Names.Split(",;".ToCharArray());
		foreach (string name in names)
		{
			watch.Restart();
			try
			{
				int lineCount = 0;
				var bc = cc.GetBlockBlobClient(name);
				var props = bc.GetProperties();
				string tempfile = Path.Combine(Path.GetTempPath(), Path.GetFileName(name));
				if (request.UseCache)
				{
					using var instream = bc.OpenRead();
					using var outstream = new FileStream(tempfile, FileMode.Create, FileAccess.Write);
					instream.CopyTo(outstream);
				}
				for (int i = 0; i < request.Count; i++)
				{
					if (!request.UseCache)
					{
						using var reader = new StreamReader(bc.OpenRead());
						while (!reader.EndOfStream)
						{
							++lineCount;
							string? line = reader.ReadLine();
						}
					}
					else
					{
						using var reader = new StreamReader(tempfile);
						while (!reader.EndOfStream)
						{
							++lineCount;
							string? line = reader.ReadLine();
						}
					}
				}
				double kb = props.Value.ContentLength / 1024.0;
				lines.Add($"{name} ({kb:F1} KB) -> {lineCount} lines loops {request.Count} [{watch.Elapsed.TotalSeconds:F3}]");
			}
			catch (Exception ex)
			{
				lines.Add($"{name} -> {ex.Message}");
			}
		}
		return await Task.FromResult(lines.ToArray());
	}

	async Task<ActionResult<string[]>> ReadTiming2Impl(ReadTimingRequest2 request)
	{
		var engine = new CrossTabEngine(LicProv);
		await engine.LoginId("G1234567", "37Reddot2");
		string[] lines = engine.LoadTest(request.Customer, request.Job, request.Vars, request.Count);
		return lines;
	}

	async Task<ActionResult<bool>> LogTestImpl()
	{
		LogTrace(900, "This is a Trace message");
		LogDebug(901, "This is a Debug message");
		LogInfo(902, "This is an Information message");
		LogWarn(903, "This is a Warning message");
		LogError(904, new ApplicationException("This is a fake error"), "This is an Error message");
		return await Task.FromResult(true);
	}
}
