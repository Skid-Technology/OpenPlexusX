using System;
using System.Threading;

namespace VibranceHud.Audio;

internal sealed class RestorePointSaver
{
	private readonly Action _save;

	private readonly int _quietMs;

	private readonly Func<int, Action, IDisposable> _startTimer;

	private readonly object _gate = new object();

	private IDisposable? _timer;

	private int _generation;

	private bool _dirty;

	public RestorePointSaver(Action save, int quietMs = 400, Func<int, Action, IDisposable>? startTimer = null)
	{
		_save = save;
		_quietMs = quietMs;
		_startTimer = startTimer ?? new Func<int, Action, IDisposable>(DefaultTimer);
	}

	public void Captured(float volume, AppSettings settings)
	{
		lock (_gate)
		{
			settings.AudioEdgeRestoreVolume = volume;
			if (!settings.AudioEdgeRestorePending)
			{
				settings.AudioEdgeRestorePending = true;
				CancelTimer();
				_dirty = false;
				_save();
				return;
			}
			_dirty = true;
			if (_timer == null)
			{
				CancelTimer();
				int gen = _generation;
				_timer = _startTimer(_quietMs, () =>
				{
					OnDue(gen);
				});
			}
		}
	}

	public void Completed(AppSettings settings)
	{
		lock (_gate)
		{
			CancelTimer();
			_dirty = false;
			settings.AudioEdgeRestorePending = false;
			_save();
		}
	}

	public void Flush()
	{
		lock (_gate)
		{
			CancelTimer();
			if (_dirty)
			{
				_dirty = false;
				_save();
			}
		}
	}

	private void OnDue(int generation)
	{
		lock (_gate)
		{
			if (generation == _generation)
			{
				_timer?.Dispose();
				_timer = null;
				if (_dirty)
				{
					_dirty = false;
					_save();
				}
			}
		}
	}

	private void CancelTimer()
	{
		_generation++;
		_timer?.Dispose();
		_timer = null;
	}

	private static IDisposable DefaultTimer(int delayMs, Action callback)
	{
		return new Timer((object? _) =>
		{
			callback();
		}, null, delayMs, -1);
	}
}
