using System;
using System.Collections.Generic;

namespace VibranceHud.Hotkeys;

public sealed class HotkeyCaptureGate
{
	public static readonly HotkeyCaptureGate Shared = new HotkeyCaptureGate();

	private readonly object _lock = new object();

	private readonly HashSet<object> _listening = new HashSet<object>();

	public bool IsCapturing
	{
		get
		{
			lock (_lock)
			{
				return _listening.Count > 0;
			}
		}
	}

	public event Action? Began;

	public event Action? Ended;

	internal void Enter(object picker)
	{
		bool flag;
		lock (_lock)
		{
			flag = _listening.Count == 0;
			_listening.Add(picker);
		}
		if (flag)
		{
			Began?.Invoke();
		}
	}

	internal void Leave(object picker)
	{
		bool flag;
		lock (_lock)
		{
			if (!_listening.Remove(picker))
			{
				return;
			}
			flag = _listening.Count == 0;
		}
		if (flag)
		{
			Ended?.Invoke();
		}
	}
}
