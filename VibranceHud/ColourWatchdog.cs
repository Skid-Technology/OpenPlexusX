using System;

namespace VibranceHud;

public sealed class ColourWatchdog
{
	public static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(400.0);

	public static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60.0);

	public static readonly TimeSpan ForegroundSettle = TimeSpan.FromSeconds(2.0);

	public const int FailuresBeforeBackingOff = 5;

	private readonly Action _reapply;

	private readonly Func<bool> _isHdrActive;

	private readonly Func<bool> _isNeutral;

	private readonly Func<DateTime> _now;

	private readonly Action<Exception>? _onGaveUp;

	private readonly Action? _worldChanged;

	private DateTime _lastApply = DateTime.MinValue;

	private bool _stopped;

	private int _consecutiveFailures;

	private bool _backedOff;

	private bool _saidSo;

	private DateTime _lastForeground = DateTime.MinValue;

	private DateTime? _foregroundCheckDue;

	internal TimeSpan? ForegroundCheckDelay
	{
		get
		{
			if (!_stopped && _foregroundCheckDue.HasValue)
			{
				return TimeSpan.FromTicks(Math.Max(0L, (_foregroundCheckDue.Value - _now()).Ticks));
			}
			return null;
		}
	}

	internal bool HasBackedOff => _backedOff;

	public ColourWatchdog(Action reapply, Func<bool> isHdrActive, Func<bool> isNeutral, Func<DateTime> now, Action<Exception>? onGaveUp = null, Action? onWorldChanged = null)
	{
		_reapply = reapply;
		_isHdrActive = isHdrActive;
		_isNeutral = isNeutral;
		_now = now;
		_onGaveUp = onGaveUp;
		_worldChanged = onWorldChanged;
	}

	public void OnDisplayChanged()
	{
		Reassert(force: true, forget: true);
	}

	public void OnResume()
	{
		Reassert(force: true, forget: true);
	}

	public void OnSessionUnlock()
	{
		Reassert(force: true, forget: true);
	}

	public void OnForegroundChanged()
	{
		if (!_stopped)
		{
			DateTime dateTime = _now();
			DateTime dateTime2 = _lastForeground + ForegroundSettle;
			DateTime dateTime3 = _lastApply + Debounce;
			if (dateTime3 > dateTime2)
			{
				dateTime2 = dateTime3;
			}
			if (dateTime < dateTime2)
			{
				_foregroundCheckDue = dateTime2;
				return;
			}
			_foregroundCheckDue = null;
			_lastForeground = dateTime;
			Reassert(force: true, forget: false);
		}
	}

	public void Tick()
	{
		if (_stopped)
		{
			return;
		}
		DateTime? foregroundCheckDue = _foregroundCheckDue;
		if (foregroundCheckDue.HasValue)
		{
			DateTime valueOrDefault = foregroundCheckDue.GetValueOrDefault();
			if (_now() >= valueOrDefault)
			{
				_foregroundCheckDue = null;
				OnForegroundChanged();
				return;
			}
		}
		Reassert(force: false, forget: false);
	}

	public void Stop()
	{
		_stopped = true;
		_foregroundCheckDue = null;
	}

	private void Reassert(bool force, bool forget)
	{
		if (_stopped)
		{
			return;
		}
		if (force)
		{
			_backedOff = false;
		}
		else if (_backedOff)
		{
			return;
		}
		try
		{
			if (_isNeutral() || _isHdrActive())
			{
				return;
			}
			DateTime dateTime = _now();
			if (!(dateTime - _lastApply < (force ? Debounce : TickInterval)))
			{
				_lastApply = dateTime;
				if (forget)
				{
					_worldChanged?.Invoke();
				}
				_reapply();
				_consecutiveFailures = 0;
			}
		}
		catch (Exception obj)
		{
			_consecutiveFailures++;
			if (_consecutiveFailures < 5)
			{
				return;
			}
			_backedOff = true;
			if (_saidSo)
			{
				return;
			}
			_saidSo = true;
			try
			{
				_onGaveUp?.Invoke(obj);
			}
			catch
			{
			}
		}
	}
}
