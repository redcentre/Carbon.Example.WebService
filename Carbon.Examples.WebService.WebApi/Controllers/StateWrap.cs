using System;
using RCS.Carbon.Licensing.Shared;
using RCS.Carbon.Tables;

namespace Carbon.Examples.WebService.WebApi.Controllers;

public sealed class StateWrap : IDisposable
{
	readonly string _sid;
	readonly bool _save;

	public StateWrap(string sessionId, ILicensingProvider licensingProvider, bool saveState = false)
	{
		_sid = sessionId;
		_save = saveState;
		Engine = new CrossTabEngine(licensingProvider);
		string?[] state = SessionManager.LoadState(_sid);
		Engine.RestoreState(state);
	}

	public void Dispose()
	{
		if (_save)
		{
			string[] state = Engine.SaveState();
			SessionManager.SaveState(_sid, state);
		}
	}

	public CrossTabEngine Engine { get; }
}
