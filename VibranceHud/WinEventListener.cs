using System;
using System.Runtime.InteropServices;

namespace VibranceHud;

public sealed class WinEventListener : IDisposable
{
	private delegate void WinEventProc(nint hook, uint evt, nint hwnd, int idObject, int idChild, uint thread, uint time);

	public const uint EventForeground = 3u;

	public const uint EventObjectReorder = 32772u;

	private const uint WINEVENT_OUTOFCONTEXT = 0u;

	private const uint WINEVENT_SKIPOWNPROCESS = 2u;

	private const int OBJID_WINDOW = 0;

	private const int CHILDID_SELF = 0;

	private readonly WinEventProc _callback;

	private readonly Action<nint> _onEvent;

	private readonly bool _dedupe;

	private readonly Func<DateTime> _now;

	private nint _hook;

	private nint _lastWindow;

	private DateTime _lastReported = DateTime.MinValue;

	private bool _pretendHooked;

	private bool _disposed;

	public static TimeSpan RepeatWindow => ColourWatchdog.ForegroundSettle;

	public bool IsHooked
	{
		get
		{
			if (_hook == IntPtr.Zero)
			{
				return _pretendHooked;
			}
			return true;
		}
	}

	[DllImport("user32.dll")]
	private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventProc callback, uint idProcess, uint idThread, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern bool UnhookWinEvent(nint hWinEventHook);

	public WinEventListener(uint first, uint last, Action<nint> onEvent, Func<DateTime>? now = null)
		: this(first, last, onEvent, hook: true, now)
	{
	}

	internal static WinEventListener HookedForTest(uint first, uint last, Action<nint> onEvent)
	{
		return new WinEventListener(first, last, onEvent, hook: false)
		{
			_pretendHooked = true
		};
	}

	internal WinEventListener(uint first, uint last, Action<nint> onEvent, bool hook, Func<DateTime>? now = null)
	{
		_onEvent = onEvent;
		_callback = OnWinEvent;
		_now = now ?? ((Func<DateTime>)(() => DateTime.UtcNow));
		_dedupe = first == 3 && last == 3;
		if (!hook)
		{
			return;
		}
		try
		{
			_hook = SetWinEventHook(first, last, IntPtr.Zero, _callback, 0u, 0u, 2u);
		}
		catch
		{
			_hook = IntPtr.Zero;
		}
	}

	internal void RaiseForTest(nint hwnd, int idObject, int idChild)
	{
		Handle(hwnd, idObject, idChild);
	}

	private void OnWinEvent(nint hook, uint evt, nint hwnd, int idObject, int idChild, uint thread, uint time)
	{
		Handle(hwnd, idObject, idChild);
	}

	private void Handle(nint hwnd, int idObject, int idChild)
	{
		if (hwnd == IntPtr.Zero || idObject != 0 || idChild != 0)
		{
			return;
		}
		if (_dedupe)
		{
			DateTime dateTime = _now();
			if (hwnd == _lastWindow && dateTime - _lastReported < RepeatWindow)
			{
				return;
			}
			_lastWindow = hwnd;
			_lastReported = dateTime;
		}
		try
		{
			_onEvent(hwnd);
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		_pretendHooked = false;
		if (_hook != IntPtr.Zero)
		{
			try
			{
				UnhookWinEvent(_hook);
			}
			catch
			{
			}
			_hook = IntPtr.Zero;
		}
	}
}
