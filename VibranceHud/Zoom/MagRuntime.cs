using System;
using System.Runtime.InteropServices;

namespace VibranceHud.Zoom;

internal sealed class MagRuntime
{
	internal static class Native
	{
		[DllImport("Magnification.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool MagInitialize();

		[DllImport("Magnification.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool MagUninitialize();

		[DllImport("Magnification.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool MagSetFullscreenTransform(float magLevel, int xOffset, int yOffset);

		[DllImport("Magnification.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool MagSetFullscreenUseBitmapSmoothing([MarshalAs(UnmanagedType.Bool)] bool fUseBitmapSmoothing);
	}

	private readonly Func<bool> _init;

	private readonly Func<bool> _uninit;

	private readonly Func<bool, bool>? _smooth;

	private int _holders;

	private bool _smoothOn;

	public static MagRuntime Shared { get; } = new MagRuntime(Native.MagInitialize, Native.MagUninitialize, Native.MagSetFullscreenUseBitmapSmoothing);

	public int Holders => _holders;

	public string Smoothing { get; private set; } = "not asked";

	public bool SmoothingOn => _smoothOn;

	internal MagRuntime(Func<bool> init, Func<bool> uninit, Func<bool, bool>? smooth = null)
	{
		_init = init;
		_uninit = uninit;
		_smooth = smooth;
	}

	public bool Take()
	{
		if (_holders == 0)
		{
			if (!TryStart())
			{
				return false;
			}
			Smoothing = "not asked";
			_smoothOn = false;
		}
		_holders++;
		return true;
	}

	public bool TakeForColour()
	{
		if (_holders == 0)
		{
			if (!TryStart())
			{
				return false;
			}
			Smoothing = "not asked";
			_smoothOn = false;
		}
		_holders++;
		return true;
	}

	public void Give()
	{
		if (_holders == 0 || --_holders != 0)
		{
			return;
		}
		_smoothOn = false;
		try
		{
			_uninit();
		}
		catch
		{
		}
	}

	public bool SetSmoothing(bool on)
	{
		if (_smooth == null || _holders == 0 || Smoothing == "missing" || on == _smoothOn)
		{
			return false;
		}
		try
		{
			bool flag = _smooth(on);
			if (on)
			{
				_smoothOn = flag;
				Smoothing = (flag ? "on" : "refused");
			}
			else if (flag)
			{
				_smoothOn = false;
				Smoothing = "off";
			}
			else
			{
				Smoothing = "off refused";
			}
		}
		catch (EntryPointNotFoundException)
		{
			_smoothOn = false;
			Smoothing = "missing";
		}
		catch (DllNotFoundException)
		{
			_smoothOn = false;
			Smoothing = "missing";
		}
		catch
		{
			Smoothing = "error";
			if (on)
			{
				_smoothOn = true;
			}
		}
		return true;
	}

	public bool ClearSmoothing()
	{
		if (_smooth == null || _holders == 0 || Smoothing == "missing")
		{
			return false;
		}
		try
		{
			if (_smooth(arg: false))
			{
				_smoothOn = false;
				Smoothing = "off";
			}
			else
			{
				Smoothing = "off refused";
			}
		}
		catch (EntryPointNotFoundException)
		{
			_smoothOn = false;
			Smoothing = "missing";
		}
		catch (DllNotFoundException)
		{
			_smoothOn = false;
			Smoothing = "missing";
		}
		catch
		{
			Smoothing = "error";
		}
		return true;
	}

	private bool TryStart()
	{
		try
		{
			return _init();
		}
		catch (DllNotFoundException)
		{
			return false;
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
	}
}
