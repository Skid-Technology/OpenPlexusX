using System;
using System.Windows.Forms;
using VibranceHud.Crosshair;
using VibranceHud.Games;

namespace VibranceHud.Fullscreen;

public sealed class FullscreenWatch : IDisposable
{
	public const int BackstopTickMs = 60000;

	public static readonly TimeSpan RepollDelay = FullscreenState.EnterHold + TimeSpan.FromMilliseconds(100.0);

	public static readonly TimeSpan LeaveRepollDelay = FullscreenState.LeaveHold + TimeSpan.FromMilliseconds(100.0);

	private readonly FullscreenState _state;

	private readonly Func<Action<nint>, WinEventListener> _listenerFactory;

	private readonly bool _realTimers;

	private WinEventListener? _listener;

	private Timer? _repollTimer;

	private Timer? _backstopTimer;

	private bool _last;

	private bool _started;

	private bool _disposed;

	internal Func<bool>? FullscreenAllowed { get; set; }

	public string? LatchedGame => _state.LatchedGame;

	internal bool RepollArmed { get; private set; }

	internal bool HookRefused
	{
		get
		{
			if (_listener != null)
			{
				return !_listener.IsHooked;
			}
			return false;
		}
	}

	internal bool BackstopArmed { get; private set; }

	public event Action<bool, string?>? Changed;

	public event Action<string?>? LatchChanged;

	public void GameClosed(string? processName)
	{
		if (!_disposed && _state.GameClosed(processName))
		{
			Raise(LatchChanged, _state.LatchedGame);
		}
	}

	public FullscreenWatch(FullscreenState state)
		: this(state, DefaultListener, realTimers: true)
	{
	}

	internal FullscreenWatch(FullscreenState state, Func<Action<nint>, WinEventListener> listenerFactory, bool realTimers)
	{
		_state = state ?? throw new ArgumentNullException("state");
		_listenerFactory = listenerFactory ?? throw new ArgumentNullException("listenerFactory");
		_realTimers = realTimers;
	}

	private static WinEventListener DefaultListener(Action<nint> onForeground)
	{
		return new WinEventListener(3u, 3u, onForeground);
	}

	public static FullscreenWatch ForThisMachine()
	{
		return new FullscreenWatch(new FullscreenState(CrosshairService.IsExclusiveFullscreen, ForegroundWindowProcess.Name, SupportedGames.AllProcessNames, () => Environment.TickCount64));
	}

	public static FullscreenWatch ForAnyFullScreenWindow()
	{
		return new FullscreenWatch(FullscreenState.ForAnyFullScreenWindow(AnyFullScreenWindow.IsInFront, ForegroundWindowProcess.Name, () => Environment.TickCount64));
	}

	public void Start()
	{
		if (_disposed || _started)
		{
			return;
		}
		_started = true;
		_listener = _listenerFactory((nint _) =>
		{
			Poll();
		});
		Poll();
		BackstopArmed = true;
		if (_realTimers)
		{
			_backstopTimer = new Timer
			{
				Interval = 60000
			};
			_backstopTimer.Tick += (object? _, EventArgs _) =>
			{
				Poll();
			};
			_backstopTimer.Start();
		}
	}

	public void PollNow()
	{
		Poll();
	}

	public void Stop()
	{
		_started = false;
		BackstopArmed = false;
		_listener?.Dispose();
		_listener = null;
		DisarmRepoll();
		if (_repollTimer != null)
		{
			_repollTimer.Dispose();
			_repollTimer = null;
		}
		if (_backstopTimer != null)
		{
			_backstopTimer.Stop();
			_backstopTimer.Dispose();
			_backstopTimer = null;
		}
	}

	internal void TickForTest()
	{
		Poll();
	}

	internal void FireRepoll()
	{
		DisarmRepoll();
		Poll();
	}

	private void Poll()
	{
		if (_disposed)
		{
			return;
		}
		string latchedGame = _state.LatchedGame;
		bool flag = _state.Poll();
		if (flag && FullscreenAllowed != null)
		{
			try
			{
				flag = FullscreenAllowed();
			}
			catch
			{
				flag = false;
			}
		}
		if (_state.CandidatePending || _state.LeavePending)
		{
			ArmRepoll(_state.LeavePending);
		}
		else
		{
			DisarmRepoll();
		}
		if (flag != _last)
		{
			_last = flag;
			Raise(Changed, flag, _state.Game);
		}
		if (!string.Equals(latchedGame, _state.LatchedGame, StringComparison.OrdinalIgnoreCase))
		{
			Raise(LatchChanged, _state.LatchedGame);
		}
	}

	private static void Raise(Action<bool, string?>? handlers, bool now, string? game)
	{
		if (handlers == null)
		{
			return;
		}
		Delegate[] invocationList = handlers.GetInvocationList();
		foreach (Delegate obj in invocationList)
		{
			try
			{
				((Action<bool, string>)obj)(now, game);
			}
			catch
			{
			}
		}
	}

	private static void Raise(Action<string?>? handlers, string? game)
	{
		if (handlers == null)
		{
			return;
		}
		Delegate[] invocationList = handlers.GetInvocationList();
		foreach (Delegate obj in invocationList)
		{
			try
			{
				((Action<string>)obj)(game);
			}
			catch
			{
			}
		}
	}

	private void ArmRepoll(bool leaving = false)
	{
		RepollArmed = true;
		if (!_realTimers)
		{
			return;
		}
		if (_repollTimer == null)
		{
			_repollTimer = new Timer();
			_repollTimer.Tick += (object? _, EventArgs _) =>
			{
				FireRepoll();
			};
		}
		_repollTimer.Interval = (int)(leaving ? LeaveRepollDelay : RepollDelay).TotalMilliseconds;
		_repollTimer.Stop();
		_repollTimer.Start();
	}

	private void DisarmRepoll()
	{
		RepollArmed = false;
		_repollTimer?.Stop();
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			Stop();
		}
	}
}
