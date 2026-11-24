using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace VibranceHud;

internal sealed class StoreStartupTask : IStartupRegistration
{
	public const string TaskId = "PlexusXStartup";

	private volatile StartupState _state;

	public StartupState State => _state;

	public async Task<StartupState> RefreshAsync()
	{
		try
		{
			_state = await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_state = StartupState.Unknown;
		}
		return _state;
	}

	public async Task<StartupState> TurnOnAsync()
	{
		try
		{
			_state = await EnableAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			_state = StartupState.Unknown;
		}
		return _state;
	}

	public void TurnOff()
	{
		if (!StartupRegistration.IsRefusedByWindows(_state))
		{
			_state = StartupState.Disabled;
		}
		TurnOffQuietlyAsync();
	}

	private async Task TurnOffQuietlyAsync()
	{
		try
		{
			_state = await DisableAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<StartupState> ReadAsync()
	{
		return From((await StartupTask.GetAsync("PlexusXStartup")).State);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<StartupState> EnableAsync()
	{
		StartupTask startupTask = await StartupTask.GetAsync("PlexusXStartup");
		return (startupTask.State != StartupTaskState.Disabled) ? From(startupTask.State) : From(await startupTask.RequestEnableAsync());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<StartupState> DisableAsync()
	{
		StartupTask startupTask = await StartupTask.GetAsync("PlexusXStartup");
		startupTask.Disable();
		return From(startupTask.State);
	}

	private static StartupState From(StartupTaskState state)
	{
		switch (state)
		{
		case StartupTaskState.Enabled:
		case StartupTaskState.EnabledByPolicy:
			return StartupState.Enabled;
		case StartupTaskState.Disabled:
			return StartupState.Disabled;
		case StartupTaskState.DisabledByUser:
			return StartupState.DisabledByUser;
		case StartupTaskState.DisabledByPolicy:
			return StartupState.DisabledByPolicy;
		default:
			return StartupState.Unknown;
		}
	}
}
