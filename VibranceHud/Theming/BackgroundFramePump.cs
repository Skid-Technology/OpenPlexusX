using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud.Theming;

internal sealed class BackgroundFramePump : IDisposable
{
	internal const int ParkedIntervalMs = 1000;

	internal const int SlowestFrameMs = 2000;

	internal static Func<Form, bool> InFront = DefaultInFront;

	private readonly Form _host;

	private readonly Action _onPictureChanged;

	private readonly Timer _timer = new Timer();

	private readonly Stopwatch _clock = Stopwatch.StartNew();

	private double _lastSeconds;

	private bool _disposed;

	private bool _movingOrSizing;

	internal bool Ticking
	{
		get
		{
			if (!_disposed)
			{
				return _timer.Enabled;
			}
			return false;
		}
	}

	internal int IntervalMs => _timer.Interval;

	internal BackgroundFramePump(Form host, Action onPictureChanged)
	{
		_host = host;
		_onPictureChanged = onPictureChanged;
		_timer.Interval = 1000;
		_timer.Tick += (object? _, EventArgs _) =>
		{
			Tick();
		};
		_host.Activated += Wake;
		_host.Deactivate += Wake;
		_host.VisibleChanged += Wake;
		_host.Resize += Wake;
		_host.HandleDestroyed += Wake;
		_host.ResizeBegin += OnMoveOrSizeBegin;
		_host.ResizeEnd += OnMoveOrSizeEnd;
		_host.Disposed += OnHostDisposed;
		Reassess();
	}

	private void Wake(object? sender, EventArgs e)
	{
		Reassess();
	}

	private void OnMoveOrSizeBegin(object? sender, EventArgs e)
	{
		_movingOrSizing = true;
		Reassess();
	}

	private void OnMoveOrSizeEnd(object? sender, EventArgs e)
	{
		_movingOrSizing = false;
		Reassess();
	}

	private void OnHostDisposed(object? sender, EventArgs e)
	{
		Dispose();
	}

	internal void Reassess()
	{
		if (_disposed)
		{
			return;
		}
		bool hasMotion = AppBackground.HasMotion;
		bool flag = _host.IsDisposed || _host.Disposing;
		bool flag2 = _host.Visible && _host.WindowState != FormWindowState.Minimized && !flag && _host.IsHandleCreated;
		bool flag3 = BackgroundMotionPolicy.ShouldPlay(hasMotion, _host.Visible, _host.WindowState == FormWindowState.Minimized, flag2 && InFront(_host), AppBackground.GameIsRunning(), flag, AppBackground.ReducedMotion, _movingOrSizing);
		bool motionAllowed = AppBackground.MotionAllowed;
		AppBackground.SetMotionGate(flag3);
		if (!flag3 && !_movingOrSizing)
		{
			AppBackground.ForgetKeptFrames();
		}
		if (flag3)
		{
			if (!motionAllowed)
			{
				_lastSeconds = _clock.Elapsed.TotalSeconds;
			}
			_timer.Interval = NextIntervalMs();
			_timer.Start();
		}
		else if ((hasMotion & flag2) && InFront(_host) && !_movingOrSizing)
		{
			_timer.Interval = 1000;
			_timer.Start();
		}
		else
		{
			_timer.Stop();
		}
	}

	private int NextIntervalMs()
	{
		return BackgroundFrameRate.IntervalMs(AppBackground.SecondsUntilNextFrame, AppBackground.CurrentFrameSeconds, AppBackground.ComposeMsPerFrame, 2000);
	}

	private void ArmForTheNextFrame()
	{
		_timer.Interval = NextIntervalMs();
	}

	internal void Tick()
	{
		if (_disposed)
		{
			return;
		}
		double totalSeconds = _clock.Elapsed.TotalSeconds;
		double seconds = Math.Min(totalSeconds - _lastSeconds, 0.5);
		_lastSeconds = totalSeconds;
		bool motionAllowed = AppBackground.MotionAllowed;
		Reassess();
		if (!AppBackground.MotionAllowed)
		{
			if (motionAllowed)
			{
				_onPictureChanged();
			}
			return;
		}
		bool flag = AppBackground.Advance(seconds);
		ArmForTheNextFrame();
		if (flag)
		{
			_onPictureChanged();
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_host.Activated -= Wake;
			_host.Deactivate -= Wake;
			_host.VisibleChanged -= Wake;
			_host.Resize -= Wake;
			_host.HandleDestroyed -= Wake;
			_host.ResizeBegin -= OnMoveOrSizeBegin;
			_host.ResizeEnd -= OnMoveOrSizeEnd;
			_host.Disposed -= OnHostDisposed;
			_timer.Stop();
			_timer.Dispose();
			AppBackground.SetMotionGate(mayPlay: false);
			AppBackground.ForgetKeptFrames();
		}
	}

	private static bool DefaultInFront(Form host)
	{
		if (!host.IsHandleCreated)
		{
			return false;
		}
		return GetForegroundWindow() == host.Handle;
	}

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();
}
