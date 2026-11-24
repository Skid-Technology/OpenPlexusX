using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud;

public sealed class CompositionKeeper : IDisposable
{
	private struct WNDCLASSEX
	{
		public int cbSize;

		public int style;

		public nint lpfnWndProc;

		public int cbClsExtra;

		public int cbWndExtra;

		public nint hInstance;

		public nint hIcon;

		public nint hCursor;

		public nint hbrBackground;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpszMenuName;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpszClassName;

		public nint hIconSm;
	}

	private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

	private const int WS_POPUP = int.MinValue;

	private const int WS_VISIBLE = 268435456;

	private const int WS_EX_TOPMOST = 8;

	private const int WS_EX_TRANSPARENT = 32;

	private const int WS_EX_TOOLWINDOW = 128;

	private const int WS_EX_NOACTIVATE = 134217728;

	private const int WS_EX_LAYERED = 524288;

	private const int LWA_ALPHA = 2;

	private const int SW_SHOWNOACTIVATE = 4;

	private static readonly nint HWND_TOPMOST = new IntPtr(-1);

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOACTIVATE = 16u;

	private const uint SWP_SHOWWINDOW = 64u;

	private const int ReassertIntervalMs = 500;

	private const uint GW_HWNDPREV = 3u;

	private static readonly WndProcDelegate s_wndProc = DefWindowProc;

	private static bool s_classRegistered;

	private const string KeeperClass = "PlexusXCompositionKeeper";

	private nint _hwnd;

	private WinEventListener? _foreground;

	private WinEventListener? _reorder;

	private Timer? _reassert;

	private bool _disposed;

	public bool IsActive => _hwnd != IntPtr.Zero;

	internal bool ListensRatherThanPolls
	{
		get
		{
			WinEventListener? foreground = _foreground;
			if (foreground != null && foreground.IsHooked)
			{
				WinEventListener? reorder = _reorder;
				if (reorder != null && reorder.IsHooked)
				{
					return _reassert == null;
				}
			}
			return false;
		}
	}

	internal bool PollsAsBackstop => _reassert != null;

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

	[DllImport("user32.dll")]
	private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern nint GetWindow(nint hWnd, uint uCmd);

	[DllImport("kernel32.dll")]
	private static extern nint GetModuleHandle(string? lpModuleName);

	public CompositionKeeper()
		: this(null)
	{
	}

	internal CompositionKeeper(Func<uint, uint, Action<nint>, WinEventListener>? listenerFactory)
	{
		try
		{
			EnsureClass();
			_hwnd = CreateWindowEx(134742184, "PlexusXCompositionKeeper", "PlexusX Composition Keeper", -1879048192, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
			if (_hwnd == IntPtr.Zero)
			{
				return;
			}
			SetLayeredWindowAttributes(_hwnd, 0u, 1, 2u);
			ShowWindow(_hwnd, 4);
			Reassert();
			_foreground = Listen(listenerFactory, 3u);
			_reorder = Listen(listenerFactory, 32772u);
			WinEventListener? foreground = _foreground;
			if (foreground != null && foreground.IsHooked)
			{
				WinEventListener? reorder = _reorder;
				if (reorder != null && reorder.IsHooked)
				{
					return;
				}
			}
			_reassert = new Timer
			{
				Interval = 500
			};
			_reassert.Tick += (object? s, EventArgs e) =>
			{
				Reassert();
			};
			_reassert.Start();
		}
		catch
		{
			Dispose();
		}
	}

	private WinEventListener Listen(Func<uint, uint, Action<nint>, WinEventListener>? factory, uint evt)
	{
		if (factory == null)
		{
			return new WinEventListener(evt, evt, (nint _) =>
			{
				Reassert();
			});
		}
		return factory(evt, evt, (nint _) =>
		{
			Reassert();
		});
	}

	private void Reassert()
	{
		if (_hwnd != IntPtr.Zero && GetWindow(_hwnd, 3u) != IntPtr.Zero)
		{
			SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 1, 1, 19u);
		}
	}

	private static void EnsureClass()
	{
		if (!s_classRegistered)
		{
			WNDCLASSEX lpwcx = new WNDCLASSEX
			{
				cbSize = Marshal.SizeOf<WNDCLASSEX>(),
				style = 0,
				lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
				hInstance = GetModuleHandle(null),
				lpszClassName = "PlexusXCompositionKeeper"
			};
			RegisterClassEx(ref lpwcx);
			s_classRegistered = true;
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		if (_foreground != null)
		{
			_foreground.Dispose();
			_foreground = null;
		}
		if (_reorder != null)
		{
			_reorder.Dispose();
			_reorder = null;
		}
		if (_reassert != null)
		{
			_reassert.Stop();
			_reassert.Dispose();
			_reassert = null;
		}
		if (_hwnd != IntPtr.Zero)
		{
			try
			{
				DestroyWindow(_hwnd);
			}
			catch
			{
			}
			_hwnd = IntPtr.Zero;
		}
	}
}
