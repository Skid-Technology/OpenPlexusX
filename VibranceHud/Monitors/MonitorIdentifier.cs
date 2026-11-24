using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VibranceHud.Controls;

namespace VibranceHud.Monitors;

public sealed class MonitorIdentifier : IDisposable
{
	private readonly Func<ColourMonitorGroup, int, IMonitorIdentifyWindow> _factory;

	private readonly Action<Action>? _expire;

	private readonly List<IMonitorIdentifyWindow> _windows = new List<IMonitorIdentifyWindow>();

	private Timer? _timer;

	private int _generation;

	private bool _disposed;

	public MonitorIdentifier(Func<ColourMonitorGroup, int, IMonitorIdentifyWindow>? factory = null, Action<Action>? expire = null)
	{
		_factory = factory ?? ((Func<ColourMonitorGroup, int, IMonitorIdentifyWindow>)((ColourMonitorGroup g, int n) => new MonitorNumberWindow(g.Bounds, n)));
		_expire = expire;
	}

	public void Identify(IReadOnlyList<ColourMonitorGroup> groups)
	{
		if (_disposed)
		{
			return;
		}
		CloseWindows();
		int generation = ++_generation;
		try
		{
			for (int i = 0; i < groups.Count; i++)
			{
				IMonitorIdentifyWindow monitorIdentifyWindow = _factory(groups[i], i + 1);
				_windows.Add(monitorIdentifyWindow);
				monitorIdentifyWindow.ShowNumber();
			}
			if (_windows.Count == 0)
			{
				return;
			}
			if (_expire != null)
			{
				_expire(Expire);
				return;
			}
			_timer = new Timer
			{
				Interval = 2000
			};
			_timer.Tick += (object? _, EventArgs _) =>
			{
				Expire();
			};
			_timer.Start();
		}
		catch
		{
			CloseWindows();
			throw;
		}
		void Expire()
		{
			if (generation == _generation)
			{
				CloseWindows();
			}
		}
	}

	private void CloseWindows()
	{
		_timer?.Stop();
		_timer?.Dispose();
		_timer = null;
		foreach (IMonitorIdentifyWindow window in _windows)
		{
			window.Dispose();
		}
		_windows.Clear();
	}

	public void Dispose()
	{
		_disposed = true;
		_generation++;
		CloseWindows();
	}
}
