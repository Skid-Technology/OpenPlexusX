using System;
using System.Collections.Generic;
using VibranceHud.Controls;

namespace VibranceHud.Display;

public sealed class CustomModeRegistrar
{
	private readonly ICustomModeDriver _driver;

	private readonly Func<string?, IReadOnlyList<DisplayMode>> _supportedModes;

	private readonly Func<string?, DisplayMode?> _current;

	private readonly Func<DisplayMode, string?, bool> _restore;

	private readonly Func<string, TimeSpan, bool> _confirm;

	private readonly Func<string?, ScalingMode?> _readScaling;

	private readonly Func<ScalingMode, bool, string?, bool> _setScaling;

	private readonly IDesktopLayout _layout;

	private readonly Func<string?, string?> _linkHint;

	private readonly ShapeFit? _fit;

	[ThreadStatic]
	private static bool _switching;

	public bool HoldsTheRealLayout => _layout is DesktopLayout.RealDesktopLayout;

	public bool Available => Guarded(() => _driver.Available) == true;

	public CustomModeRegistrar(ICustomModeDriver driver, Func<string?, IReadOnlyList<DisplayMode>>? supportedModes = null, Func<string?, DisplayMode?>? current = null, Func<DisplayMode, string?, bool>? restore = null, Func<string, TimeSpan, bool>? confirm = null, Func<string?, ScalingMode?>? readScaling = null, Func<ScalingMode, bool, string?, bool>? setScaling = null, IDesktopLayout? layout = null, Func<string?, string?>? linkHint = null, ShapeFit? fit = null)
	{
		_driver = driver;
		_fit = fit;
		_layout = layout ?? DesktopLayout.None;
		_linkHint = linkHint ?? ((Func<string, string>)((string? _) => (string?)null));
		_supportedModes = supportedModes ?? ((Func<string, IReadOnlyList<DisplayMode>>)((string? d) => DisplayController.SupportedModes(d)));
		_current = current ?? ((Func<string, DisplayMode?>)((string? d) => DisplayController.Current(d)));
		_restore = restore ?? ((Func<DisplayMode, string, bool>)((DisplayMode m, string? d) => DisplayController.Restore(m, d)));
		_confirm = confirm ?? new Func<string, TimeSpan, bool>(AskWithTheCountdown);
		_readScaling = readScaling ?? ((Func<string, ScalingMode?>)((string? d) => ScalingModeController.Stored(d)));
		_setScaling = setScaling ?? ((Func<ScalingMode, bool, string, bool>)((ScalingMode m, bool p, string? d) => ScalingModeController.TrySet(m, p, d)));
	}

	public static CustomModeRegistrar Real()
	{
		return new CustomModeRegistrar(new NvidiaCustomModeDriver(), null, null, null, null, null, null, DesktopLayout.Real(), EdidLinkReader.Hint, ShapeFit.Shared);
	}

	public static CustomModeRegistrar Disabled()
	{
		return new CustomModeRegistrar(new NoCustomModeDriver(), (string? _) => Array.Empty<DisplayMode>(), (string? _) => (DisplayMode?)null, (DisplayMode _, string? _) => false, (string _, TimeSpan _) => false, (string? _) => (ScalingMode?)null, (ScalingMode _, bool _, string? _) => false);
	}

	public string? LinkHint(string? deviceName)
	{
		return GuardedValue(() => _linkHint(deviceName), null);
	}

	public IReadOnlyList<DisplayMode> SavedSizes(string? deviceName)
	{
		if (Guarded(() => _driver.Available) != true)
		{
			return Array.Empty<DisplayMode>();
		}
		return GuardedValue(() => _driver.Saved(deviceName) ?? Array.Empty<DisplayMode>(), Array.Empty<DisplayMode>());
	}

	public bool SwitchToSaved(int width, int height, int hz, string? deviceName)
	{
		if (_switching)
		{
			return false;
		}
		_switching = true;
		try
		{
			DisplayMode? displayMode = null;
			foreach (DisplayMode item in SavedSizes(deviceName))
			{
				if (item.Width == width && item.Height == height && (hz <= 0 || item.RefreshHz <= 0 || item.RefreshHz == hz))
				{
					displayMode = item;
					break;
				}
			}
			if (displayMode.HasValue)
			{
				DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
				IReadOnlyList<DisplayMode> reported = GuardedValue(() => _supportedModes(deviceName), Array.Empty<DisplayMode>());
				int judgedHz = ((hz > 0) ? hz : valueOrDefault.RefreshHz);
				if (CustomModeLimits.Judge(width, height, judgedHz, reported) != CustomModeVerdict.Fine)
				{
					return false;
				}
				DisplayMode? native = NativeOf(reported);
				if (Guarded(() => _driver.IsOnTheNativeSignal(deviceName, width, height, judgedHz, native)) != true)
				{
					return false;
				}
				DisplayMode? displayMode2 = GuardedValue(() => _current(deviceName), null);
				if (displayMode2.HasValue)
				{
					DisplayMode valueOrDefault2 = displayMode2.GetValueOrDefault();
					if (valueOrDefault2.Width == width && valueOrDefault2.Height == height)
					{
						return true;
					}
					int rate;
					if (hz > 0)
					{
						rate = hz;
					}
					else
					{
						rate = ((valueOrDefault.RefreshHz > 0) ? valueOrDefault.RefreshHz : valueOrDefault2.RefreshHz);
					}
					IReadOnlyList<MonitorPlacement> layoutBefore = GuardedValue(() => _layout.Read(), Array.Empty<MonitorPlacement>());
					ScalingMode? scalingMode = GuardedValue(() => _readScaling(deviceName), null);
					if (CustomModeLimits.NeedsStretching(width, height, reported) && scalingMode != ScalingMode.FullScreen)
					{
						return false;
					}
					if (Guarded(() => _driver.Trial(deviceName, width, height, rate, native)) != true || !IsShowing(width, height, deviceName))
					{
						UndoEverything(valueOrDefault2, deviceName, null, stretched: false, layoutBefore);
						return false;
					}
					return true;
				}
				return false;
			}
			return false;
		}
		catch
		{
			return false;
		}
		finally
		{
			_switching = false;
		}
	}

	public CustomModeAttempt TryApply(int width, int height, int hz, string? deviceName, TimeSpan grace)
	{
		try
		{
			return Attempt(width, height, hz, deviceName, grace);
		}
		catch
		{
			return new CustomModeAttempt(CustomModeOutcome.NotAvailable, CustomModeVerdict.Fine, hz, Stretched: false);
		}
	}

	private CustomModeAttempt Attempt(int width, int height, int hz, string? deviceName, TimeSpan grace)
	{
		IReadOnlyList<DisplayMode> reported = GuardedValue(() => _supportedModes(deviceName), Array.Empty<DisplayMode>());
		CustomModeVerdict customModeVerdict = CustomModeLimits.Judge(width, height, hz, reported);
		if (customModeVerdict != CustomModeVerdict.Fine)
		{
			return new CustomModeAttempt(CustomModeOutcome.OutOfRange, customModeVerdict, hz, Stretched: false);
		}
		if (Guarded(() => _driver.Available) != true)
		{
			return NotAvailable(hz);
		}
		DisplayMode? displayMode = GuardedValue(() => _current(deviceName), null);
		if (displayMode.HasValue)
		{
			DisplayMode original = displayMode.GetValueOrDefault();
			int rate = CustomModeLimits.NativeRateFor(hz, reported, original.RefreshHz);
			IReadOnlyList<MonitorPlacement> readOnlyList = GuardedValue(() => _layout.Read(), Array.Empty<MonitorPlacement>());
			ScalingMode? scalingMode = GuardedValue(() => _readScaling(deviceName), null);
			bool flag = false;
			DisplayMode? native = NativeOf(reported);
			bool? flag2 = Guarded(() => _driver.Trial(deviceName, width, height, rate, native));
			if (!flag2.HasValue)
			{
				UndoEverything(original, deviceName, scalingMode, flag, readOnlyList);
				return NotAvailable(rate);
			}
			if (flag2 == false)
			{
				UndoEverything(original, deviceName, scalingMode, flag, readOnlyList);
				return new CustomModeAttempt(CustomModeOutcome.DriverRefused, customModeVerdict, rate, flag);
			}
			if (!IsShowing(width, height, deviceName))
			{
				UndoEverything(original, deviceName, scalingMode, flag, readOnlyList);
				return new CustomModeAttempt(CustomModeOutcome.DriverRefused, customModeVerdict, rate, flag);
			}
			bool flag3 = CustomModeLimits.NeedsStretching(width, height, reported);
			string message = $"Switched to {width} x {height}" + ((rate > 0) ? $" at {rate} Hz" : "") + ".";
			if (Guarded(() => _confirm(message, grace)) == true)
			{
				if (Guarded(() => _driver.Save(deviceName)) != true)
				{
					return new CustomModeAttempt(CustomModeOutcome.AppliedButNotSaved, customModeVerdict, rate, flag);
				}
				if (flag3 && IsShowing(width, height, deviceName))
				{
					flag = SetScalingChecked(ScalingMode.FullScreen, persist: true, deviceName);
					NoteTheStretch(width, height, flag, deviceName);
					if (flag)
					{
						_fit?.Remember(scalingMode, deviceName);
					}
				}
				if (flag3 && !flag)
				{
					return new CustomModeAttempt(CustomModeOutcome.ScalingFailed, customModeVerdict, rate, Stretched: false);
				}
				return new CustomModeAttempt(CustomModeOutcome.Applied, customModeVerdict, rate, flag);
			}
			bool valueOrDefault = Guarded(() => _driver.Revert(deviceName)) == true;
			bool flag4 = IsShowing(original.Width, original.Height, deviceName) || (Guarded(() => _restore(original, deviceName)) == true && IsShowing(original.Width, original.Height, deviceName));
			bool flag5 = RestoreScaling(scalingMode, flag, deviceName);
			PutTheLayoutBack(readOnlyList);
			return new CustomModeAttempt((valueOrDefault & flag4 & flag5) ? CustomModeOutcome.Reverted : CustomModeOutcome.RevertFailed, customModeVerdict, rate, flag);
		}
		return NotAvailable(hz);
	}

	private void PutTheLayoutBack(IReadOnlyList<MonitorPlacement> before)
	{
		if (before.Count == 0)
		{
			return;
		}
		IReadOnlyList<MonitorPlacement> now = GuardedValue(() => _layout.Read(), Array.Empty<MonitorPlacement>());
		IReadOnlyList<MonitorPlacement> moved = DesktopLayout.WhatMoved(before, now);
		if (moved.Count != 0)
		{
			Guarded(() => _layout.Place(moved));
		}
	}

	private bool IsShowing(int width, int height, string? deviceName)
	{
		DisplayMode? displayMode = GuardedValue(() => _current(deviceName), null);
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			if (valueOrDefault.Width == width)
			{
				return valueOrDefault.Height == height;
			}
		}
		return false;
	}

	private static DisplayMode? NativeOf(IReadOnlyList<DisplayMode> reported)
	{
		return NvidiaCustomModeDriver.NativeModeOf(reported);
	}

	private static CustomModeAttempt NotAvailable(int hz)
	{
		return new CustomModeAttempt(CustomModeOutcome.NotAvailable, CustomModeVerdict.Fine, hz, Stretched: false);
	}

	private void UndoEverything(DisplayMode original, string? deviceName, ScalingMode? previousScaling, bool stretched, IReadOnlyList<MonitorPlacement> layoutBefore)
	{
		Guarded(() => _driver.Revert(deviceName));
		if (!IsShowing(original.Width, original.Height, deviceName))
		{
			Guarded(() => _restore(original, deviceName));
		}
		RestoreScaling(previousScaling, stretched, deviceName);
		PutTheLayoutBack(layoutBefore);
	}

	private void NoteTheStretch(int width, int height, bool stretched, string? deviceName)
	{
		try
		{
			string status = "refused";
			if (stretched)
			{
				string text = "unknown";
				try
				{
					text = _readScaling(deviceName)?.ToString() ?? "unknown";
				}
				catch
				{
				}
				status = "written, driver now holds " + text;
			}
			NvapiFallbackLog.NoteCustomModeStep($"stretch {width}x{height}", status);
		}
		catch
		{
		}
	}

	private bool SetScalingChecked(ScalingMode mode, bool persist, string? deviceName)
	{
		if (Guarded(() => _setScaling(mode, persist, deviceName)) == true)
		{
			ScalingMode? scalingMode = GuardedValue(() => _readScaling(deviceName), null);
			if (scalingMode.HasValue)
			{
				ScalingMode valueOrDefault = scalingMode.GetValueOrDefault();
				return valueOrDefault == mode;
			}
			return true;
		}
		return false;
	}

	private bool RestoreScaling(ScalingMode? previous, bool stretched, string? deviceName)
	{
		if (!stretched)
		{
			return true;
		}
		if (previous.HasValue)
		{
			ScalingMode valueOrDefault = previous.GetValueOrDefault();
			return SetScalingChecked(valueOrDefault, persist: false, deviceName);
		}
		return false;
	}

	private static bool AskWithTheCountdown(string message, TimeSpan grace)
	{
		bool confirmed = false;
		using RevertConfirmForm revertConfirmForm = new RevertConfirmForm(message, Math.Max(1, (int)Math.Ceiling(grace.TotalSeconds)));
		revertConfirmForm.Confirmed += () =>
		{
			confirmed = true;
		};
		revertConfirmForm.ShowDialog();
		return confirmed;
	}

	private static bool? Guarded(Func<bool> call)
	{
		try
		{
			return call();
		}
		catch
		{
			return null;
		}
	}

	private static T GuardedValue<T>(Func<T> call, T fallback)
	{
		try
		{
			return call();
		}
		catch
		{
			return fallback;
		}
	}
}
