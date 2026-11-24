using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Updates;

namespace VibranceHud;

public sealed class BackgroundUpdateChecker : IDisposable
{
	internal const int CheckIntervalMs = 7200000;

	private readonly AppSettings _settings;

	private readonly SettingsStore _store;

	private readonly NotifyIcon _trayIcon;

	private readonly System.Windows.Forms.Timer _timer;

	private readonly Task? _startupUpdateStep;

	private bool _isRunning;

	private int _ticksBegun;

	private int _manifestChecks;

	internal int TicksBegun => Volatile.Read(in _ticksBegun);

	internal int ManifestChecks => Volatile.Read(in _manifestChecks);

	internal Func<bool> SafeNowCheck { get; set; } = SafeToRestart.IsNow;

	internal static bool ShouldTick(FlavourPolicy policy)
	{
		return policy.SelfUpdates;
	}

	public BackgroundUpdateChecker(AppSettings settings, SettingsStore store, NotifyIcon trayIcon, Task? startupUpdateStep = null)
	{
		_settings = settings;
		_store = store;
		_trayIcon = trayIcon;
		_startupUpdateStep = startupUpdateStep;
		_timer = new System.Windows.Forms.Timer
		{
			Interval = 7200000
		};
		_timer.Tick += async (object? s, EventArgs e) =>
		{
			await TickAsync();
		};
	}

	public void Start()
	{
		_timer.Start();
		FirstTickAsync();
	}

	internal async Task FirstTickAsync()
	{
		await Task.Delay(3000);
		if (_startupUpdateStep != null)
		{
			try
			{
				await _startupUpdateStep;
			}
			catch
			{
			}
		}
		await TickAsync();
	}

	private async Task TickAsync()
	{
		Interlocked.Increment(ref _ticksBegun);
		if (!ShouldTick(FlavourPolicy.Current) || _isRunning)
		{
			return;
		}
		_isRunning = true;
		try
		{
			if (!RemoteSwitches.IsOn("updates") || UpdateService.InstallerStartedThisRun)
			{
				return;
			}
			if (!string.IsNullOrWhiteSpace(_settings.PendingUpdateVersion))
			{
				await InstallIfNobodyIsBusyAsync();
				return;
			}
			Interlocked.Increment(ref _manifestChecks);
			ReleaseInfo release = RefusedUpdate.Unless(release: await UpdateService.TryGetSignedUpdateAsync(), settings: _settings);
			if (!(release == null) && SafeNowCheck())
			{
				string text = await UpdateService.DownloadAndStageAsync(release);
				if (!string.IsNullOrWhiteSpace(text))
				{
					UpdateService.RecordPending?.Invoke(release, text);
					await InstallIfNobodyIsBusyAsync();
				}
			}
		}
		catch
		{
		}
		finally
		{
			_isRunning = false;
		}
	}

	private async Task InstallIfNobodyIsBusyAsync()
	{
		if (!SafeNowCheck())
		{
			return;
		}
		string refusedBefore = _settings.UpdateRefusedSha256 + "|" + _settings.UpdateRefusedNoticedSha256;
		await UpdateService.RunPendingUpdateIfAnyAsync(_settings);
		if (!(refusedBefore != _settings.UpdateRefusedSha256 + "|" + _settings.UpdateRefusedNoticedSha256))
		{
			return;
		}
		try
		{
			_store.Save(_settings);
		}
		catch
		{
		}
	}

	internal Task TickForTest()
	{
		return TickAsync();
	}

	public void Dispose()
	{
		_timer.Stop();
		_timer.Dispose();
	}
}
