using System;
using System.Threading;

namespace VibranceHud;

public sealed class DebouncedAction : IDisposable
{
	private readonly Action _action;

	private readonly Timer _timer;

	public int DelayMs { get; }

	public DebouncedAction(Action action, int delayMs)
	{
		_action = action;
		_timer = new Timer((object? _) =>
		{
			Run();
		}, null, -1, -1);
		DelayMs = delayMs;
	}

	private void Run()
	{
		try
		{
			_action();
		}
		catch (Exception ex)
		{
			try
			{
				CrashLog.Write(ex);
			}
			catch
			{
			}
		}
	}

	public void Trigger()
	{
		_timer.Change(DelayMs, -1);
	}

	public void Dispose()
	{
		_timer.Dispose();
	}
}
