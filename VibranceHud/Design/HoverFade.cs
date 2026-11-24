using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace VibranceHud.Design;

public sealed class HoverFade
{
	private static class Ticker
	{
		private static readonly List<HoverFade> Active = new List<HoverFade>();

		private static Timer? _timer;

		public static void Enrol(HoverFade fade)
		{
			if (!Active.Contains(fade))
			{
				Active.Add(fade);
			}
			if (_timer == null)
			{
				_timer = new Timer
				{
					Interval = 16
				};
				_timer.Tick += (object? _, EventArgs _) =>
				{
					Tick();
				};
			}
			_timer.Start();
		}

		private static void Tick()
		{
			for (int num = Active.Count - 1; num >= 0; num--)
			{
				if (!Active[num].Step())
				{
					Active.RemoveAt(num);
				}
			}
			if (Active.Count == 0)
			{
				_timer?.Stop();
			}
		}
	}

	internal const int DurationMs = 83;

	private static readonly Stopwatch Clock = Stopwatch.StartNew();

	internal static Func<double> NowMs = () => Clock.Elapsed.TotalMilliseconds;

	internal static Func<bool> MotionAllowed = () => SystemInformation.UIEffectsEnabled;

	private readonly Control _owner;

	private float _value;

	private float _from;

	private float _target;

	private double _startMs;

	public float Value
	{
		get
		{
			if (_value == _target)
			{
				return _value;
			}
			double num = (NowMs() - _startMs) / 83.0;
			if (num >= 1.0)
			{
				_value = _target;
				return _value;
			}
			_value = _from + (_target - _from) * (float)StandardEase(num);
			return _value;
		}
	}

	internal static double StandardEase(double x)
	{
		if (x <= 0.0)
		{
			return 0.0;
		}
		if (x >= 1.0)
		{
			return 1.0;
		}
		double num = 0.0;
		double num2 = 1.0;
		double num3 = x;
		for (int i = 0; i < 40; i++)
		{
			num3 = (num + num2) / 2.0;
			if (B(num3, 0.33, 0.67) < x)
			{
				num = num3;
			}
			else
			{
				num2 = num3;
			}
		}
		return B(num3, 0.0, 1.0);
		static double B(double t, double p1, double p2)
		{
			return 3.0 * (1.0 - t) * (1.0 - t) * t * p1 + 3.0 * (1.0 - t) * t * t * p2 + t * t * t;
		}
	}

	public HoverFade(Control owner)
	{
		_owner = owner;
	}

	public void Set(bool hot)
	{
		float num = (hot ? 1f : 0f);
		if (_target == num)
		{
			return;
		}
		if (!MotionAllowed())
		{
			Snap(hot);
			return;
		}
		_from = Value;
		_startMs = NowMs();
		_target = num;
		if (_value != _target)
		{
			Ticker.Enrol(this);
		}
	}

	public void Snap(bool hot)
	{
		_target = (_value = (_from = (hot ? 1f : 0f)));
	}

	private bool Step()
	{
		if (_owner.IsDisposed)
		{
			return false;
		}
		_owner.Invalidate();
		return _value != _target;
	}
}
