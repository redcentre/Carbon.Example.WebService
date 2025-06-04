using System;

namespace RCS.Carbon.Example.WebService.Common;

public delegate void ClientLogEventHandler(object sender, ClientLogEventArgs e);

public sealed class ClientLogEventArgs : EventArgs
{
	public ClientLogEventArgs(int type, string message)
	{
		Type = type;
		Message = message;
	}
	public int Type { get; }
	public string Message { get; }
}
