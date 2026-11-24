using System;
using System.Threading.Tasks;

namespace VibranceHud;

public static class StartupRegistration
{
	private sealed class NoStartup : IStartupRegistration
	{
		internal static readonly NoStartup Instance = new NoStartup();

		public StartupState State => StartupState.Unknown;

		public Task<StartupState> RefreshAsync()
		{
			return Task.FromResult(StartupState.Unknown);
		}

		public Task<StartupState> TurnOnAsync()
		{
			return Task.FromResult(StartupState.Unknown);
		}

		public void TurnOff()
		{
		}
	}

	public static IStartupRegistration For(FlavourPolicy policy)
	{
		return For(policy, StoreHooks.StartupTask, () => new RunKeyStartup(StartupManager.IsEnabled, StartupManager.SetEnabled));
	}

	internal static IStartupRegistration For(FlavourPolicy policy, Func<IStartupRegistration>? storeTask, Func<IStartupRegistration> runKey)
	{
		if (policy.Startup == StartupMechanism.RunKey)
		{
			return runKey();
		}
		try
		{
			return storeTask?.Invoke() ?? NoStartup.Instance;
		}
		catch
		{
			return NoStartup.Instance;
		}
	}

	public static string? ExplainState(StartupState state)
	{
		return state switch
		{
			StartupState.DisabledByUser => "Off in Task Manager. Turn it on there.", 
			StartupState.DisabledByPolicy => "Your PC's admin has turned this off.", 
			_ => null, 
		};
	}

	public static bool IsRefusedByWindows(StartupState state)
	{
		if ((uint)(state - 3) <= 1u)
		{
			return true;
		}
		return false;
	}

	public static bool SavedPreferenceAfter(bool wanted, StartupState after)
	{
		if (wanted)
		{
			return !IsRefusedByWindows(after);
		}
		return false;
	}

	public static bool ShowsOn(StartupState state, bool savedPreference)
	{
		return state switch
		{
			StartupState.Enabled => true, 
			StartupState.Unknown => savedPreference, 
			_ => false, 
		};
	}
}
