using System;
using System.Collections.Generic;
using VibranceHud.Customize;
using VibranceHud.Monitors;
using VibranceHud.Zoom;

namespace VibranceHud.Startup;

public static class ColdStartRestore
{
	public static bool ShouldResetTheTaskbar(bool lastRunDied, bool aStyleWasTurnedOn, bool styleStillWanted, bool styleStillAllowed)
	{
		if (lastRunDied & aStyleWasTurnedOn)
		{
			return !(styleStillWanted & styleStillAllowed);
		}
		return false;
	}

	public static void PutTheScreenBack(Action gammaLinear, Action magIdentity, Action driverBaseline, Action taskbarNormal, bool resetTaskbar)
	{
		Guarded(gammaLinear);
		Guarded(magIdentity);
		Guarded(driverBaseline);
		if (resetTaskbar)
		{
			Guarded(taskbarNormal);
		}
	}

	public static void RunIfTheLastRunDied(AppSettings settings, SettingsStore store)
	{
		RunIfTheLastRunDied(DirtyExitMarker.Shared, settings, () =>
		{
			store.Save(settings);
		}, () => RecoveryGamma(settings, () =>
		{
			store.Save(settings);
		}), () => new MagOverlay().HeldForTheGoodbye(), TrayApplicationContext.CreateVibranceController, TaskbarNormal, null, ZoomEmergency.Reset);
	}

	internal static void RunIfTheLastRunDied(DirtyExitMarker marker, AppSettings settings, Action save, Func<IDisposable> openGammaRamp, Func<IDisposable> openMagnifier, Func<IVibranceController> openDriver, Action taskbarNormal, Func<IReadOnlyList<ColourMonitor>>? monitorInventory = null, Action? zoomIdentity = null)
	{
		if (!marker.ClaimPreviousRunWasDirty())
		{
			return;
		}
		PutTheScreenBack(() =>
		{
			if (settings.GammaRestorePending)
			{
				openGammaRamp().Dispose();
				AppSettings appSettings = settings;
				Dictionary<string, ushort[]> ownedMonitorGamma = settings.OwnedMonitorGamma;
				appSettings.GammaRestorePending = ownedMonitorGamma != null && ownedMonitorGamma.Count > 0;
				save();
			}
		}, () =>
		{
			openMagnifier().Dispose();
			zoomIdentity?.Invoke();
		}, () =>
		{
			if (!settings.NvapiRestorePending)
			{
				return;
			}
			if (settings.OwnedMonitorVibrance != null)
			{
				MonitorSelection selection = new MonitorSelection(settings, monitorInventory ?? new Func<IReadOnlyList<ColourMonitor>>(ColourMonitorInventory.Read));
				new SelectedVibranceController(openDriver(), selection, settings, save).RestoreOwned();
				settings.NvapiRestorePending = settings.OwnedMonitorVibrance.Count > 0;
				save();
				return;
			}
			if (settings.NvapiRestoreLevels.Count > 0 || DriverVibranceBaseline.RestoreOnExit(settings.NvapiRestoreLevel).HasValue)
			{
				IVibranceController driver = openDriver();
				int? driverVibranceWeLastWrote = settings.DriverVibranceWeLastWrote;
				if (driverVibranceWeLastWrote.HasValue)
				{
					int valueOrDefault = driverVibranceWeLastWrote.GetValueOrDefault();
					if (OutsideDriverChange.HoldsALevelOtherThan(driver, valueOrDefault))
					{
						goto IL_0131;
					}
				}
				int? driverVibranceWeLastWrote2 = DriverVibranceBaseline.RestoreEveryDisplay(driver, settings.NvapiRestoreLevels, settings.NvapiRestoreLevel);
				if (driverVibranceWeLastWrote2.HasValue)
				{
					settings.DriverVibranceWeLastWrote = driverVibranceWeLastWrote2;
				}
			}
			goto IL_0131;
			IL_0131:
			settings.NvapiRestorePending = false;
			save();
		}, taskbarNormal, ShouldResetTheTaskbar(lastRunDied: true, AStyleWasTurnedOn(settings), CustomizeService.ShouldReapplyTaskbarStyle(settings), styleStillAllowed: true));
	}

	public static void LeavingBeforeTheTray(AppSettings settings)
	{
		LeavingBeforeTheTray(DirtyExitMarker.Shared, settings, TaskbarNormal);
	}

	internal static void LeavingBeforeTheTray(DirtyExitMarker marker, AppSettings settings, Action taskbarNormal)
	{
		if (ShouldResetTheTaskbar(marker.ClaimPreviousRunWasDirty(), AStyleWasTurnedOn(settings), CustomizeService.ShouldReapplyTaskbarStyle(settings), styleStillAllowed: false))
		{
			Guarded(taskbarNormal);
		}
	}

	private static bool AStyleWasTurnedOn(AppSettings settings)
	{
		return CustomizeService.StyleFromName(settings.TaskbarStyleName) != TaskbarStyle.Normal;
	}

	private static void TaskbarNormal()
	{
		CustomizeService.ApplyTaskbarStyle(new TaskbarSettings
		{
			Style = TaskbarStyle.Normal
		});
	}

	public static Action GammaLinearNow(AppSettings settings, SettingsStore store)
	{
		return () =>
		{
			RecoveryGamma(settings, () =>
			{
				store.Save(settings);
			}).Dispose();
			AppSettings appSettings = settings;
			Dictionary<string, ushort[]> ownedMonitorGamma = settings.OwnedMonitorGamma;
			appSettings.GammaRestorePending = ownedMonitorGamma != null && ownedMonitorGamma.Count > 0;
		};
	}

	public static Action DriverBaselineNow(AppSettings settings, SettingsStore store)
	{
		return () =>
		{
			if (settings.OwnedMonitorVibrance != null)
			{
				MonitorSelection selection = new MonitorSelection(settings, ColourMonitorInventory.Read);
				new SelectedVibranceController(TrayApplicationContext.CreateVibranceController(), selection, settings, () =>
				{
					store.Save(settings);
				}).RestoreOwned();
			}
			else if (settings.NvapiRestoreLevels.Count > 0 || DriverVibranceBaseline.RestoreOnExit(settings.NvapiRestoreLevel).HasValue)
			{
				DriverVibranceBaseline.RestoreEveryDisplay(TrayApplicationContext.CreateVibranceController(), settings.NvapiRestoreLevels, settings.NvapiRestoreLevel);
			}
		};
	}

	private static MultiMonitorGammaRamp RecoveryGamma(AppSettings settings, Action save)
	{
		MultiMonitorGammaRamp multiMonitorGammaRamp = new MultiMonitorGammaRamp((string _) => (100, 0));
		if (settings.OwnedMonitorGamma != null)
		{
			multiMonitorGammaRamp.Selection = new MonitorSelection(settings, ColourMonitorInventory.Read);
			multiMonitorGammaRamp.AdoptRecoveryBaselines(settings.OwnedMonitorGamma);
			multiMonitorGammaRamp.DeviceReleased = (string id) =>
			{
				settings.OwnedMonitorGamma.Remove(id);
				save();
			};
		}
		return multiMonitorGammaRamp;
	}

	private static void Guarded(Action call)
	{
		try
		{
			call();
		}
		catch
		{
		}
	}
}
