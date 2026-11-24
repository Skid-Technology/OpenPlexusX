using System;
using VibranceHud.Capabilities;
using VibranceHud.Controls;

namespace VibranceHud.Display;

public sealed class ModeApplier
{
	internal sealed record Steps(Func<string?, DisplayMode?> Current, Func<int, int, int, string?, bool, DispChangeResult> Apply, Func<DisplayMode, string?, bool> Restore, Func<string, TimeSpan, bool> Confirm);

	private readonly Func<int, int, int, string?, TimeSpan, DispChangeResult> _apply;

	private readonly Func<bool, string?, TimeSpan, HdrSwitchOutcome>? _setHdr;

	public bool MovesARealScreen { get; }

	public ModeApplier(Func<int, int, int, string?, TimeSpan, DispChangeResult> apply, bool movesARealScreen, Func<bool, string?, TimeSpan, HdrSwitchOutcome>? setHdr = null)
	{
		_apply = apply;
		_setHdr = setHdr;
		MovesARealScreen = movesARealScreen;
	}

	public HdrSwitchOutcome SetHdr(bool enabled, string? deviceName, TimeSpan grace)
	{
		return _setHdr?.Invoke(enabled, deviceName, grace) ?? HdrSwitchOutcome.Failed;
	}

	public static ModeApplier Real()
	{
		return new ModeApplier((int width, int height, int hz, string? deviceName, TimeSpan grace) => ApplyThenFit(width, height, hz, deviceName, grace, RealSteps(), ShapeFit.Shared), movesARealScreen: true, HdrDetection.SwitchWithRevertReal);
	}

	private static Steps RealSteps()
	{
		return new Steps((string? d) => DisplayController.Current(d), (int w, int h, int hz, string? d, bool persist) => DisplayController.ApplyEx(w, h, hz, d, persist), (DisplayMode m, string? d) => DisplayController.Restore(m, d), (string message, TimeSpan grace) =>
		{
			bool confirmed = false;
			using RevertConfirmForm revertConfirmForm = new RevertConfirmForm(message, Math.Max(1, (int)Math.Ceiling(grace.TotalSeconds)));
			revertConfirmForm.Confirmed += () =>
			{
				confirmed = true;
			};
			revertConfirmForm.ShowDialog();
			return confirmed;
		});
	}

	internal static DispChangeResult ApplyThenFit(int width, int height, int hz, string? deviceName, TimeSpan grace, Steps steps, ShapeFit fit)
	{
		DisplayMode? displayMode = steps.Current(deviceName);
		if (displayMode.HasValue)
		{
			DisplayMode valueOrDefault = displayMode.GetValueOrDefault();
			ScalingMode? before = fit.ReadBefore(deviceName);
			DispChangeResult dispChangeResult = steps.Apply(width, height, hz, deviceName, arg5: false);
			if (dispChangeResult != DispChangeResult.Successful)
			{
				return dispChangeResult;
			}
			ShapeFit.Change change = fit.Fill(width, height, deviceName, before);
			if (change.Result == ShapeFit.Outcome.Failed)
			{
				steps.Restore(valueOrDefault, deviceName);
				fit.Undo(change, deviceName);
				return DispChangeResult.Failed;
			}
			if (!steps.Confirm($"Switched to {width} x {height}" + ((hz > 0) ? $" at {hz} Hz" : "") + ".", grace))
			{
				bool flag = steps.Restore(valueOrDefault, deviceName);
				ShapeFit.Outcome outcome = fit.Undo(change, deviceName);
				if (!flag || outcome == ShapeFit.Outcome.Failed)
				{
					return DispChangeResult.Failed;
				}
				return DispChangeResult.Reverted;
			}
			DispChangeResult dispChangeResult2 = steps.Apply(width, height, hz, deviceName, arg5: true);
			if (dispChangeResult2 != DispChangeResult.Successful)
			{
				steps.Restore(valueOrDefault, deviceName);
				fit.Undo(change, deviceName);
				return dispChangeResult2;
			}
			if (fit.Keep(change, deviceName) != ShapeFit.Outcome.Failed)
			{
				return dispChangeResult2;
			}
			return DispChangeResult.Failed;
		}
		return DispChangeResult.Failed;
	}

	public static ModeApplier Dead()
	{
		return new ModeApplier((int _, int _, int _, string? _, TimeSpan _) => DispChangeResult.Failed, movesARealScreen: false);
	}

	public DispChangeResult Apply(int width, int height, int hz, string? deviceName, TimeSpan grace)
	{
		return _apply(width, height, hz, deviceName, grace);
	}
}
