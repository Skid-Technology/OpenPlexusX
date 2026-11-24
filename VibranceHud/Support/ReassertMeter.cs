using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace VibranceHud.Support;

public sealed class ReassertMeter
{
	public sealed record LayerStat(int Count, double LastMs, double MaxMs)
	{
		public static readonly LayerStat Never = new LayerStat(0, 0.0, 0.0);
	}

	public const string Driver = "driver";

	public const string Gamma = "gamma";

	public const string Overlay = "overlay";

	private readonly Func<long> _ticks;

	private readonly double _msPerTick;

	private readonly object _gate = new object();

	private readonly Dictionary<string, LayerStat> _layers = new Dictionary<string, LayerStat>(StringComparer.Ordinal);

	private readonly List<string> _order = new List<string>();

	public int Reasserts { get; private set; }

	public double LastMs { get; private set; }

	public double MaxMs { get; private set; }

	public ReassertMeter(Func<long>? ticks = null, long? frequency = null)
	{
		_ticks = ticks ?? new Func<long>(Stopwatch.GetTimestamp);
		long num = frequency ?? Stopwatch.Frequency;
		_msPerTick = 1000.0 / (double)((num <= 0) ? 1 : num);
	}

	public long Now()
	{
		return _ticks();
	}

	public double MsSince(long start)
	{
		double num = (double)(_ticks() - start) * _msPerTick;
		if (!(num > 0.0))
		{
			return 0.0;
		}
		return num;
	}

	public void Record(string layer, double ms)
	{
		if (string.IsNullOrEmpty(layer))
		{
			return;
		}
		if (double.IsNaN(ms) || ms < 0.0)
		{
			ms = 0.0;
		}
		lock (_gate)
		{
			if (!_layers.TryGetValue(layer, out LayerStat value))
			{
				value = LayerStat.Never;
				_order.Add(layer);
			}
			_layers[layer] = new LayerStat(value.Count + 1, ms, Math.Max(value.MaxMs, ms));
		}
	}

	public void Completed(double totalMs)
	{
		if (double.IsNaN(totalMs) || totalMs < 0.0)
		{
			totalMs = 0.0;
		}
		lock (_gate)
		{
			Reasserts++;
			LastMs = totalMs;
			if (totalMs > MaxMs)
			{
				MaxMs = totalMs;
			}
		}
	}

	public LayerStat For(string layer)
	{
		lock (_gate)
		{
			LayerStat value;
			return _layers.TryGetValue(layer, out value) ? value : LayerStat.Never;
		}
	}

	private static string Ms(double ms)
	{
		return ms.ToString("0.00", CultureInfo.InvariantCulture) + "ms";
	}

	public string Describe()
	{
		lock (_gate)
		{
			List<string> list = new List<string>();
			list.Add("reasserts " + Reasserts.ToString(CultureInfo.InvariantCulture) + "  last " + Ms(LastMs) + "  max " + Ms(MaxMs));
			List<string> list2 = list;
			foreach (string item in _order)
			{
				LayerStat layerStat = _layers[item];
				list2.Add("  " + item + ": n=" + layerStat.Count.ToString(CultureInfo.InvariantCulture) + " last=" + Ms(layerStat.LastMs) + " max=" + Ms(layerStat.MaxMs));
			}
			return string.Join(Environment.NewLine, list2);
		}
	}
}
