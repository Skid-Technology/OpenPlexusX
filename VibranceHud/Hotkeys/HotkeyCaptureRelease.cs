using System;

namespace VibranceHud.Hotkeys;

public sealed class HotkeyCaptureRelease : IDisposable
{
	private readonly HotkeyCaptureGate _gate;

	private readonly Action _releaseAll;

	private readonly Action _registerAll;

	private bool _released;

	private bool _disposed;

	public bool IsReleased => _released;

	public HotkeyCaptureRelease(HotkeyCaptureGate gate, Action releaseAll, Action registerAll)
	{
		_gate = gate ?? throw new ArgumentNullException("gate");
		_releaseAll = releaseAll ?? throw new ArgumentNullException("releaseAll");
		_registerAll = registerAll ?? throw new ArgumentNullException("registerAll");
		_gate.Began += OnBegan;
		_gate.Ended += OnEnded;
	}

	private void OnBegan()
	{
		if (_disposed || _released)
		{
			return;
		}
		_released = true;
		try
		{
			_releaseAll();
		}
		catch
		{
		}
	}

	private void OnEnded()
	{
		if (_disposed || !_released)
		{
			return;
		}
		_released = false;
		try
		{
			_registerAll();
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_gate.Began -= OnBegan;
			_gate.Ended -= OnEnded;
		}
	}
}
