using System;
using System.Linq;
using System.Threading;
using RCS.Carbon.Example.WebService.Common.DTO;
using RCS.Carbon.Tables;

namespace RCS.Carbon.Example.WebService.WebApi;

/// <summary>
/// Encapsulates all the data for a single Platinum batch report processing which runs in the background.
/// </summary>
sealed class BatchData
{
	public BatchData(string sessionId, string userId, string customerName, string jobName, string? storageConnect, string? containerName, MultiPlatinumRequest request)
	{
		SessionId = sessionId;
		UserId = userId;
		CustomerName = customerName;
		JobName = jobName;
		StorageConnect = storageConnect;
		ContainerName = containerName;
		Request = request;
		Response = new MultiPlatinumResponse
		{
			Id = Guid.NewGuid().GetHashCode().ToString("X8")
		};
		if (Request.ParallelMax < 1) Response.ParallelMax = 1;
		else if (Request.ParallelMax > Environment.ProcessorCount) Response.ParallelMax = Environment.ProcessorCount;
		else Response.ParallelMax = Request.ParallelMax;
		Response.Reports = [.. Request.ReportNames.Select(rn => new PlatinumResponseItem() { Name = rn })];
		Cts = new CancellationTokenSource();
		StartedEvent = new AutoResetEvent(false);
		HoldDatas = new PlatinumData[Request.ReportNames.Length];
	}
	public string SessionId { get; }
	public string UserId { get; }
	public string CustomerName { get; }
	public string JobName { get; }
	public string? StorageConnect { get; }
	public string? ContainerName { get; }
	public MultiPlatinumRequest Request { get; }
	public MultiPlatinumResponse Response { get; }
	public CancellationTokenSource Cts { get; }
	public AutoResetEvent StartedEvent { get; }
	public PlatinumData?[] HoldDatas { get; }
}
