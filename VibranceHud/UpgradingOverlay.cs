using System;

namespace VibranceHud;

public sealed class UpgradingOverlay : ISaturationOverlay, IDisplayOverlay, IDisposable
{
	private readonly Func<ISaturationOverlay?> _tryCreatePreferred;

	private readonly DxInitFailureKind _fallbackFailure;

	private readonly string _fallbackFailureMessage;

	private readonly int _fallbackFailureCode;

	private ISaturationOverlay _active;

	private float[]? _pendingMatrix;

	private bool _disposed;

	private bool _replayFailed;

	public OverlayMode ActiveMode => (_active as IDisplayOverlay)?.ActiveMode ?? OverlayMode.Dx;

	public bool CanUpgrade
	{
		get
		{
			if (!_disposed)
			{
				return ActiveMode != OverlayMode.Dx;
			}
			return false;
		}
	}

	public DxInitFailureKind LastFailure
	{
		get
		{
			if (ActiveMode == OverlayMode.Dx)
			{
				return DxInitFailureKind.None;
			}
			DxInitFailureKind dxInitFailureKind = (_active as IDisplayOverlay)?.LastFailure ?? DxInitFailureKind.None;
			if (dxInitFailureKind == DxInitFailureKind.None)
			{
				return _fallbackFailure;
			}
			return dxInitFailureKind;
		}
	}

	public string LastFailureMessage
	{
		get
		{
			if (ActiveMode == OverlayMode.Dx)
			{
				return "";
			}
			string text = (_active as IDisplayOverlay)?.LastFailureMessage ?? "";
			if (string.IsNullOrEmpty(text))
			{
				return _fallbackFailureMessage;
			}
			return text;
		}
	}

	public int LastFailureCode
	{
		get
		{
			if (ActiveMode == OverlayMode.Dx)
			{
				return 0;
			}
			int num = (_active as IDisplayOverlay)?.LastFailureCode ?? 0;
			if (num == 0)
			{
				return _fallbackFailureCode;
			}
			return num;
		}
	}

	public bool EffectIsLive => (_active as IDisplayOverlay)?.EffectIsLive ?? true;

	public bool LastApplySucceeded
	{
		get
		{
			if (!_replayFailed)
			{
				return _active.LastApplySucceeded;
			}
			return false;
		}
	}

	public UpgradingOverlay(ISaturationOverlay initial, Func<ISaturationOverlay?> tryCreatePreferred, DxInitFailureKind fallbackFailure = DxInitFailureKind.None, string fallbackFailureMessage = "", int fallbackFailureCode = 0)
	{
		_active = initial ?? throw new ArgumentNullException("initial");
		_tryCreatePreferred = tryCreatePreferred ?? throw new ArgumentNullException("tryCreatePreferred");
		_fallbackFailure = fallbackFailure;
		_fallbackFailureMessage = fallbackFailureMessage ?? "";
		_fallbackFailureCode = fallbackFailureCode;
	}

	public void Apply(float[] matrix)
	{
		_pendingMatrix = matrix;
		_replayFailed = false;
		_active.Apply(matrix);
	}

	public void Clear()
	{
		_pendingMatrix = null;
		_replayFailed = false;
		_active.Clear();
	}

	public void Forget()
	{
		_active.Forget();
	}

	public bool TryUpgrade()
	{
		if (!CanUpgrade)
		{
			return false;
		}
		ISaturationOverlay saturationOverlay;
		try
		{
			saturationOverlay = _tryCreatePreferred();
		}
		catch
		{
			return false;
		}
		if (saturationOverlay == null)
		{
			return false;
		}
		IDisplayOverlay obj2 = saturationOverlay as IDisplayOverlay;
		if (obj2 != null && obj2.ActiveMode != OverlayMode.Dx)
		{
			Dispose(saturationOverlay);
			return false;
		}
		ISaturationOverlay active = _active;
		_active = saturationOverlay;
		try
		{
			active.Clear();
		}
		catch
		{
		}
		Dispose(active);
		if (_pendingMatrix != null)
		{
			try
			{
				_active.Apply(_pendingMatrix);
			}
			catch
			{
				_replayFailed = true;
			}
		}
		return true;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			Dispose(_active);
		}
	}

	private static void Dispose(ISaturationOverlay overlay)
	{
		if (overlay is IDisposable disposable)
		{
			try
			{
				disposable.Dispose();
			}
			catch
			{
			}
		}
	}
}
