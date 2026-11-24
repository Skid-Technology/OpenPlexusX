using System;
using System.Collections.Generic;

namespace VibranceHud.Display;

public sealed class ShapeFit
{
	public enum Outcome
	{
		NotNeeded,
		Applied,
		Failed
	}

	public readonly record struct Change(bool Written, ScalingMode Wrote, ScalingMode? Before, Outcome Result = Outcome.Applied);

	private readonly Func<string?, IReadOnlyList<DisplayMode>> _supportedModes;

	private readonly Func<string?, ScalingMode?> _stored;

	private readonly Func<ScalingMode, bool, string?, bool> _set;

	private readonly Dictionary<string, ScalingMode> _before = new Dictionary<string, ScalingMode>(StringComparer.OrdinalIgnoreCase);

	private readonly object _gate = new object();

	public static ShapeFit Shared { get; } = new ShapeFit((string? d) => DisplayController.SupportedModes(d), (string? d) => ScalingModeController.Stored(d), (ScalingMode mode, bool persist, string? d) => ScalingModeController.TrySet(mode, persist, d));

	public ShapeFit(Func<string?, IReadOnlyList<DisplayMode>> supportedModes, Func<string?, ScalingMode?> stored, Func<ScalingMode, bool, string?, bool> set)
	{
		_supportedModes = supportedModes;
		_stored = stored;
		_set = set;
	}

	private static string Key(string? deviceName)
	{
		if (!string.IsNullOrWhiteSpace(deviceName))
		{
			return deviceName;
		}
		return "";
	}

	public ScalingMode? ReadBefore(string? deviceName)
	{
		try
		{
			return _stored(deviceName);
		}
		catch
		{
			return null;
		}
	}

	public Change Fill(int width, int height, string? deviceName, ScalingMode? before)
	{
		try
		{
			if (CustomModeLimits.NeedsStretching(width, height, _supportedModes(deviceName)))
			{
				return Write(ScalingMode.FullScreen, deviceName, before);
			}
			ScalingMode value;
			lock (_gate)
			{
				if (!_before.TryGetValue(Key(deviceName), out value))
				{
					return new Change(Written: false, ScalingMode.FullScreen, before, Outcome.NotNeeded);
				}
			}
			return Write(value, deviceName, before);
		}
		catch
		{
			return new Change(Written: false, ScalingMode.FullScreen, before, Outcome.Failed);
		}
	}

	private Change Write(ScalingMode mode, string? deviceName, ScalingMode? before)
	{
		bool flag = _set(mode, arg2: false, deviceName);
		return new Change(flag, mode, before, (flag && Matches(mode, deviceName)) ? Outcome.Applied : Outcome.Failed);
	}

	private bool Matches(ScalingMode mode, string? deviceName)
	{
		ScalingMode? scalingMode = ReadBefore(deviceName);
		if (scalingMode.HasValue)
		{
			ScalingMode valueOrDefault = scalingMode.GetValueOrDefault();
			return valueOrDefault == mode;
		}
		return true;
	}

	public Outcome Keep(Change change, string? deviceName)
	{
		if (change.Result != Outcome.Applied)
		{
			return change.Result;
		}
		if (!change.Written)
		{
			return Outcome.NotNeeded;
		}
		try
		{
			if (!_set(change.Wrote, arg2: true, deviceName) || !Matches(change.Wrote, deviceName))
			{
				return Outcome.Failed;
			}
		}
		catch
		{
			return Outcome.Failed;
		}
		if (change.Wrote == ScalingMode.FullScreen)
		{
			Remember(change.Before, deviceName);
		}
		else
		{
			lock (_gate)
			{
				_before.Remove(Key(deviceName));
			}
		}
		return Outcome.Applied;
	}

	public Outcome Undo(Change change, string? deviceName)
	{
		if (!change.Written)
		{
			return Outcome.NotNeeded;
		}
		ScalingMode? before = change.Before;
		if (before.HasValue)
		{
			ScalingMode valueOrDefault = before.GetValueOrDefault();
			try
			{
				return (_set(valueOrDefault, arg2: false, deviceName) && Matches(valueOrDefault, deviceName)) ? Outcome.Applied : Outcome.Failed;
			}
			catch
			{
				return Outcome.Failed;
			}
		}
		return Outcome.Failed;
	}

	public void Remember(ScalingMode? before, string? deviceName)
	{
		if (!before.HasValue)
		{
			return;
		}
		ScalingMode valueOrDefault = before.GetValueOrDefault();
		if (valueOrDefault == ScalingMode.FullScreen)
		{
			return;
		}
		lock (_gate)
		{
			_before.TryAdd(Key(deviceName), valueOrDefault);
		}
	}

	public void Forget(string? deviceName)
	{
		lock (_gate)
		{
			_before.Remove(Key(deviceName));
		}
	}
}
