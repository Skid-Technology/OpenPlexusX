using System;
using System.Collections.Generic;

namespace VibranceHud.Remote;

internal sealed class PhoneBackoff
{
	private readonly object _gate = new object();

	private readonly Dictionary<string, (int Failures, DateTime BlockedUntil)> _bySource = new Dictionary<string, (int, DateTime)>();

	internal bool IsBlocked(string source, DateTime now)
	{
		lock (_gate)
		{
			(int, DateTime) value;
			return _bySource.TryGetValue(source, out value) && now < value.Item2;
		}
	}

	internal void RecordFailure(string source, DateTime now)
	{
		lock (_gate)
		{
			_bySource.TryGetValue(source, out (int, DateTime) value);
			int num = value.Item1 + 1;
			_bySource[source] = ((num >= 10) ? (0, now + PhoneRequest.BackoffWindow) : (num, value.Item2));
		}
	}

	internal void RecordSuccess(string source)
	{
		lock (_gate)
		{
			_bySource.Remove(source);
		}
	}
}
