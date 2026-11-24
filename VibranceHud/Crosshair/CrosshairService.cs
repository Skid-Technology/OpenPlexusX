using System;
using System.Runtime.InteropServices;

namespace VibranceHud.Crosshair;

public sealed class CrosshairService : IDisposable
{
	private const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;

	private CrosshairWindow? _window;

	private readonly Func<CrosshairWindow> _newWindow;

	public bool IsVisible { get; private set; }

	public CrosshairConfig Config { get; private set; } = new CrosshairConfig();

	[DllImport("shell32.dll")]
	private static extern int SHQueryUserNotificationState(out int state);

	public CrosshairService()
		: this(() => new CrosshairWindow())
	{
	}

	internal CrosshairService(Func<CrosshairWindow> newWindow)
	{
		_newWindow = newWindow;
	}

	public static bool IsExclusiveFullscreen()
	{
		try
		{
			int state;
			return SHQueryUserNotificationState(out state) == 0 && state == 3;
		}
		catch
		{
			return false;
		}
	}

	public void Apply(CrosshairConfig config)
	{
		Config = config;
		if (IsVisible)
		{
			_window?.Apply(config);
		}
	}

	public void Show()
	{
		if (_window == null)
		{
			_window = _newWindow();
		}
		_ = _window.Handle;
		_window.Apply(Config);
		if (!_window.Visible)
		{
			_window.Show();
		}
		IsVisible = true;
	}

	public void Hide()
	{
		_window?.Hide();
		IsVisible = false;
	}

	public void Toggle()
	{
		if (IsVisible)
		{
			Hide();
		}
		else
		{
			Show();
		}
	}

	public void Dispose()
	{
		_window?.Dispose();
		_window = null;
		IsVisible = false;
	}
}
