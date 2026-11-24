using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using VibranceHud.Support;

namespace VibranceHud.Zoom;

public sealed class FullscreenZoom : IDisposable
{
	internal const string MagnifierOpen = "Close Windows Magnifier to use Zoom";

	internal const string ExclusiveScreen = "Zoom needs Borderless or Windowed";

	internal const string WontStart = "Windows would not start the zoom";

	private const double InMs = 90.0;

	private const double OutMs = 70.0;

	private readonly MagRuntime _runtime;

	private readonly Func<float, int, int, bool> _set;

	private readonly Func<Rectangle> _bounds;

	private readonly Func<bool> _magnifierRunning;

	private readonly Func<bool> _exclusive;

	private bool _leaseHeld;

	private bool _pendingIdentity;

	private float _current = 1f;

	private float _from = 1f;

	private float _to = 1f;

	private double _elapsedMs;

	private double _durationMs;

	private bool _animating;

	private bool _keyDown;

	private int _gen;

	private int _animGen;

	private bool _dirty;

	private bool _logNext;

	private bool _disarmOwed;

	private const double FirstStepMs = 8.0;

	internal const float WarmLevel = 1.001f;

	private readonly Action<string> _log;

	private readonly Func<bool> _smoothEdges;

	private static readonly TimedProbe MagnifierProbe = new TimedProbe(MagnifierIsRunning, () => Environment.TickCount64, 2000L);

	public bool Engaged
	{
		get
		{
			if (!(_current > 1f) && !_animating)
			{
				return _pendingIdentity;
			}
			return true;
		}
	}

	public bool Armed => _leaseHeld;

	public string WhyRefused { get; private set; } = "";

	public bool NeedsTicks
	{
		get
		{
			if (!_animating)
			{
				return _pendingIdentity;
			}
			return true;
		}
	}

	public FullscreenZoom(Func<bool>? exclusiveFullscreen = null, Func<bool>? smoothEdges = null)
		: this(MagRuntime.Shared, RealSet, () => Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080), CachedMagnifierCheck, exclusiveFullscreen ?? ((Func<bool>)(() => false)), ZoomLog.Note, smoothEdges)
	{
	}

	internal FullscreenZoom(MagRuntime runtime, Func<float, int, int, bool> set, Func<Rectangle> primaryBounds, Func<bool>? magnifierRunning = null, Func<bool>? exclusive = null, Action<string>? log = null, Func<bool>? smoothEdges = null)
	{
		_runtime = runtime;
		_log = log ?? ((Action<string>)((string _) =>
		{
		}));
		_smoothEdges = smoothEdges ?? ((Func<bool>)(() => false));
		_set = set;
		_bounds = primaryBounds;
		_magnifierRunning = magnifierRunning ?? ((Func<bool>)(() => false));
		_exclusive = exclusive ?? ((Func<bool>)(() => false));
	}

	public bool Arm()
	{
		try
		{
			if (_leaseHeld)
			{
				_disarmOwed = false;
				return true;
			}
			bool flag = _runtime.Holders == 0;
			bool flag2 = _runtime.Take();
			_log(flag ? ("arm MagInitialize=" + flag2 + " smooth=" + _runtime.Smoothing) : "arm shared runtime");
			if (!flag2)
			{
				return false;
			}
			_leaseHeld = true;
			_disarmOwed = false;
			bool flag3 = SafeBool(_magnifierRunning);
			if (!flag3 && !SafeBool(_exclusive) && !_animating && !_dirty && _current <= 1f)
			{
				ZoomEmergency.WeZoomed = true;
				_dirty = true;
				_logNext = true;
				ScaleForZoom();
				WriteLevel(1.001f);
				_logNext = true;
				EndAtIdentity();
			}
			else if (!flag3 && !_animating && !_dirty && !_pendingIdentity && _current <= 1f)
			{
				Smooth(on: false);
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public void Press(float targetLevel, bool smooth)
	{
		try
		{
			WhyRefused = "";
			if (SafeBool(_magnifierRunning))
			{
				Refuse("Close Windows Magnifier to use Zoom");
				return;
			}
			if (SafeBool(_exclusive))
			{
				Refuse("Zoom needs Borderless or Windowed");
				return;
			}
			if (!_leaseHeld)
			{
				bool flag = _runtime.Holders == 0;
				bool flag2 = _runtime.Take();
				if (flag)
				{
					_log("MagInitialize=" + flag2 + " smooth=" + _runtime.Smoothing);
				}
				if (!flag2)
				{
					Refuse("Windows would not start the zoom");
					return;
				}
				_leaseHeld = true;
			}
			ZoomEmergency.WeZoomed = true;
			_disarmOwed = false;
			_keyDown = true;
			_gen++;
			_pendingIdentity = false;
			if (!(targetLevel > 1f))
			{
				targetLevel = 2f;
			}
			_dirty = true;
			ScaleForZoom();
			if (!smooth)
			{
				_animating = false;
				WriteLevel(targetLevel);
				return;
			}
			Begin(targetLevel, 90.0);
			_elapsedMs = 8.0;
			float num = ZoomMath.Ease(_from, _to, 4f / 45f);
			if (num <= 1f)
			{
				num = 1.0001f;
			}
			_logNext = true;
			WriteLevel(num);
		}
		catch
		{
		}
	}

	public void Release(bool smooth)
	{
		try
		{
			_keyDown = false;
			_gen++;
			if (_dirty || _animating || _pendingIdentity || !(_current <= 1f))
			{
				if (!smooth || _current <= 1.0001f)
				{
					_animating = false;
					EndAtIdentity();
				}
				else
				{
					Begin(1f, 70.0);
				}
			}
		}
		catch
		{
		}
	}

	public void Tick(TimeSpan elapsed)
	{
		try
		{
			if (_animating)
			{
				if (_to > 1f && (!_keyDown || _animGen != _gen))
				{
					_animating = false;
					EndAtIdentity();
					return;
				}
				_elapsedMs += elapsed.TotalMilliseconds;
				float num = ((_durationMs <= 0.0) ? 1f : ((float)Math.Min(1.0, _elapsedMs / _durationMs)));
				if (num >= 1f)
				{
					_animating = false;
					if (_to <= 1f)
					{
						EndAtIdentity();
					}
					else
					{
						WriteLevel(_to);
					}
					return;
				}
				float num2 = ZoomMath.Ease(_from, _to, num);
				if (num2 <= 1f)
				{
					num2 = 1.0001f;
				}
				if (num2 != _current)
				{
					WriteLevel(num2);
				}
			}
			else if (_pendingIdentity)
			{
				EndAtIdentity();
			}
		}
		catch
		{
		}
	}

	public void ResetNow()
	{
		try
		{
			_animating = false;
			_keyDown = false;
			_gen++;
			if ((_dirty || _current > 1f || _pendingIdentity) && !EndAtIdentity() && !EndAtIdentity())
			{
				_disarmOwed = true;
			}
			else
			{
				GiveTheLease();
			}
		}
		catch
		{
		}
	}

	public void Dispose()
	{
		ResetNow();
	}

	private void Begin(float to, double ms)
	{
		_from = _current;
		_to = to;
		_elapsedMs = 0.0;
		_durationMs = ms;
		_animGen = _gen;
		_animating = true;
	}

	private bool WriteLevel(float level)
	{
		(int X, int Y) tuple = ZoomMath.CentreOffsets(_bounds(), level);
		int item = tuple.X;
		int item2 = tuple.Y;
		bool flag = SafeSet(level, item, item2);
		if (flag)
		{
			_current = level;
		}
		return flag;
	}

	private bool EndAtIdentity()
	{
		bool flag = SafeSet(1f, 0, 0);
		if (flag)
		{
			_current = 1f;
			_pendingIdentity = false;
			_dirty = false;
			Smooth(on: false);
			if (_disarmOwed)
			{
				_disarmOwed = false;
				GiveTheLease();
				return flag;
			}
		}
		else
		{
			_pendingIdentity = true;
		}
		return flag;
	}

	private void Smooth(bool on)
	{
		long timestamp = Stopwatch.GetTimestamp();
		if (_runtime.SetSmoothing(on))
		{
			long num = (Stopwatch.GetTimestamp() - timestamp) * 1000 / Stopwatch.Frequency;
			_log("MagSetFullscreenUseBitmapSmoothing(" + on + ")=" + _runtime.Smoothing + " ms=" + num);
		}
	}

	private void ScaleForZoom()
	{
		if (SafeBool(_smoothEdges))
		{
			Smooth(on: true);
			return;
		}
		long timestamp = Stopwatch.GetTimestamp();
		if (_runtime.ClearSmoothing())
		{
			long num = (Stopwatch.GetTimestamp() - timestamp) * 1000 / Stopwatch.Frequency;
			_log("MagSetFullscreenUseBitmapSmoothing(False)=" + _runtime.Smoothing + " ms=" + num);
		}
	}

	private void GiveTheLease()
	{
		if (_leaseHeld)
		{
			if (!SafeBool(_magnifierRunning))
			{
				Smooth(on: false);
			}
			_leaseHeld = false;
			_runtime.Give();
			_log("disarm holders=" + _runtime.Holders);
		}
	}

	private void Refuse(string why)
	{
		WhyRefused = why;
		_log("refused: " + why);
	}

	private bool SafeSet(float z, int x, int y)
	{
		long timestamp = Stopwatch.GetTimestamp();
		bool flag;
		try
		{
			flag = _set(z, x, y);
		}
		catch
		{
			flag = false;
		}
		long value = (Stopwatch.GetTimestamp() - timestamp) * 1000 / Stopwatch.Frequency;
		if (!_animating || !flag || _logNext)
		{
			_logNext = false;
			Action<string> log = _log;
			IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
			DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(36, 5, invariantCulture);
			handler.AppendLiteral("MagSetFullscreenTransform(");
			handler.AppendFormatted(z, "0.###");
			handler.AppendLiteral(", ");
			handler.AppendFormatted(x);
			handler.AppendLiteral(", ");
			handler.AppendFormatted(y);
			handler.AppendLiteral(")=");
			handler.AppendFormatted(flag);
			handler.AppendLiteral(" ms=");
			handler.AppendFormatted(value);
			log(string.Create(invariantCulture, ref handler));
		}
		return flag;
	}

	private static bool SafeBool(Func<bool> f)
	{
		try
		{
			return f();
		}
		catch
		{
			return false;
		}
	}

	private static bool RealSet(float z, int x, int y)
	{
		try
		{
			return MagRuntime.Native.MagSetFullscreenTransform(z, x, y);
		}
		catch
		{
			return false;
		}
	}

	private static bool CachedMagnifierCheck()
	{
		return MagnifierProbe.Get();
	}

	private static bool MagnifierIsRunning()
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName("Magnify");
			bool result = processesByName.Length != 0;
			Process[] array = processesByName;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose();
			}
			return result;
		}
		catch
		{
			return false;
		}
	}
}
