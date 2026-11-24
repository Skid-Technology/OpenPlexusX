using System;
using System.Collections.Generic;

namespace VibranceHud;

public sealed class RedetectingVibranceController : IVibranceController
{
	private IVibranceController _inner;

	private readonly Func<IVibranceController?> _tryCreate;

	private int _tries;

	internal IVibranceController Inner => _inner;

	public int CurrentLevel => _inner.CurrentLevel;

	public int DefaultLevel => _inner.DefaultLevel;

	public bool IsAvailable => _inner.IsAvailable;

	public VibranceDriverState DriverState => _inner.DriverState;

	public event Action? Changed;

	public RedetectingVibranceController(IVibranceController initial, Func<IVibranceController?> tryCreate)
	{
		_inner = initial;
		_tryCreate = tryCreate;
	}

	public void OnDisplayEvent()
	{
		bool flag = false;
		try
		{
			if (_inner.IsAvailable)
			{
				flag = _inner is VibranceController vibranceController && vibranceController.Redetect();
			}
			else if (_inner.DriverState == VibranceDriverState.DisplayNotOnNvidia && _tries < 3)
			{
				_tries++;
				IVibranceController vibranceController2 = _tryCreate();
				if (vibranceController2 != null && vibranceController2.IsAvailable)
				{
					_inner = vibranceController2;
					_tries = 0;
					flag = true;
				}
			}
		}
		catch
		{
		}
		if (!flag)
		{
			return;
		}
		try
		{
			Changed?.Invoke();
		}
		catch
		{
		}
	}

	public bool EveryDisplayIsAt(int level)
	{
		return _inner.EveryDisplayIsAt(level);
	}

	public void SetLevel(int level)
	{
		_inner.SetLevel(level);
	}

	public IReadOnlyList<KeyValuePair<string, int>> ReadEveryDisplayLevel()
	{
		return _inner.ReadEveryDisplayLevel();
	}

	public void SetLevelFor(string displayId, int level)
	{
		_inner.SetLevelFor(displayId, level);
	}
}
