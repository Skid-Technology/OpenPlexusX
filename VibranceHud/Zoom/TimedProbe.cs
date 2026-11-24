using System;

namespace VibranceHud.Zoom;

internal sealed class TimedProbe
{
	private readonly Func<bool> _probe;

	private readonly Func<long> _nowMs;

	private readonly long _ttlMs;

	private bool _value;

	private long _at;

	private bool _have;

	public TimedProbe(Func<bool> probe, Func<long> nowMs, long ttlMs)
	{
		_probe = probe;
		_nowMs = nowMs;
		_ttlMs = ttlMs;
	}

	public bool Get()
	{
		long num = _nowMs();
		if (_have && num - _at < _ttlMs)
		{
			return _value;
		}
		_value = _probe();
		_at = num;
		_have = true;
		return _value;
	}
}
