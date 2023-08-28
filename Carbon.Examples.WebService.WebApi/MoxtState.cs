using System;
using System.Threading;
using Carbon.Examples.WebService.Common;

namespace RCS.RubyCloud.WebService;

/// <summary>
/// A class to encapsulate the state of a single Multi-OXT processing run.
/// </summary>
[Serializable]
public sealed class MoxtState : IDisposable
{
	public MoxtState(MultiOxtRequest request)
	{
		Request = request;
	}
	public DateTime Created { get; } = DateTime.UtcNow;
	public MultiOxtRequest Request { get; }
	public Guid Id { get; } = Guid.NewGuid();
	public string ShowId => Id.ToString().Substring(0, 4);
	public string SessionId { get; set; }
	public int ParallelCount { get; set; }
	public string ProgressMessage { get; set; }
	public RubyMultiOxtItem[] Items { get; set; }
	public CancellationTokenSource CancelSource { get; set; } = new CancellationTokenSource();

	public void Dispose()
	{
		CancelSource?.Dispose();
	}
}