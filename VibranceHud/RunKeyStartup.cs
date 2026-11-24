using System;
using System.Threading.Tasks;

namespace VibranceHud;

internal sealed class RunKeyStartup : IStartupRegistration
{
	private readonly Func<bool> _isEnabled;

	private readonly Func<bool, bool> _setEnabled;

	public StartupState State
	{
		get
		{
			if (!_isEnabled())
			{
				return StartupState.Disabled;
			}
			return StartupState.Enabled;
		}
	}

	internal RunKeyStartup(Func<bool> isEnabled, Func<bool, bool> setEnabled)
	{
		_isEnabled = isEnabled;
		_setEnabled = setEnabled;
	}

	public Task<StartupState> RefreshAsync()
	{
		return Task.FromResult(State);
	}

	public Task<StartupState> TurnOnAsync()
	{
		_setEnabled(arg: true);
		return Task.FromResult(State);
	}

	public void TurnOff()
	{
		_setEnabled(arg: false);
	}
}
