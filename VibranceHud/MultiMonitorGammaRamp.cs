using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VibranceHud.Monitors;

namespace VibranceHud;

public sealed class MultiMonitorGammaRamp : IGammaRamp, IDisposable
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct DISPLAY_DEVICE
	{
		public int cb;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string DeviceName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceString;

		public int StateFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceID;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string DeviceKey;
	}

	private const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 1;

	private const int DISPLAY_DEVICE_MIRRORING_DRIVER = 8;

	private readonly Func<IReadOnlyList<string>> _enumerateDevices;

	private readonly Func<string, ushort[], bool> _setRampOnDevice;

	private readonly Func<string, ushort[]?> _readRampOnDevice;

	private readonly Func<string, (int GammaPercent, int WarmthPercent)> _adjustmentFor;

	private ushort[]? _lastBase;

	private readonly Dictionary<string, ushort[]?> _theirOwn = new Dictionary<string, ushort[]>();

	private readonly Dictionary<string, (ushort[] Wanted, ushort[] Landed, string Step)> _softened = new Dictionary<string, (ushort[], ushort[], string)>();

	private readonly Dictionary<string, ushort[]> _refusedEveryStep = new Dictionary<string, ushort[]>();

	private readonly HashSet<string> _coloured = new HashSet<string>();

	private bool _saidGoodbye;

	internal static readonly double[] SofteningSteps = new double[3] { 0.5, 0.25, 0.125 };

	internal const string HandedBackAlready = "PlexusX has already handed the screen back to Windows";

	private bool _lastBaseAllOrNothing;

	internal const string AllOrNothingRefused = "Windows refused the Dark Boost ramp on a display";

	internal const string AllOrNothingNotHeld = "a display did not hold the Dark Boost ramp it was given";

	internal const string AllOrNothingNoDisplay = "no display to hold the Dark Boost ramp";

	internal const int MaxDip = 255;

	public MonitorSelection? Selection { get; set; }

	public Action<string, ushort[]>? BeforeDeviceColour { get; set; }

	public Action<string>? DeviceReleased { get; set; }

	public bool LastApplyFailed { get; private set; }

	public int DeviceWrites { get; private set; }

	public string LastRefusal { get; private set; } = "";

	public int DisplaysThatRefusedTheLastWrite { get; private set; }

	public int DisplaysThatWereSoftened { get; private set; }

	public GammaDelivery LastDelivery { get; private set; } = GammaDelivery.Full;

	public Action? BeforeFirstColour { get; set; }

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

	[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
	private static extern nint CreateDC(string? lpszDriver, string lpszDevice, string? lpszOutput, nint lpInitData);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(nint hdc);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetDeviceGammaRamp(nint hDC, ushort[] ramp);

	[DllImport("gdi32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetDeviceGammaRamp(nint hDC, ushort[] ramp);

	public MultiMonitorGammaRamp(Func<string, (int, int)> adjustmentFor)
		: this(Devices, SetRampOn, adjustmentFor, ReadRampOn)
	{
	}

	internal MultiMonitorGammaRamp(Func<IReadOnlyList<string>> enumerateDevices, Func<string, ushort[], bool> setRampOnDevice, Func<string, (int, int)> adjustmentFor, Func<string, ushort[]?>? readRampOnDevice = null)
	{
		_enumerateDevices = enumerateDevices;
		_setRampOnDevice = setRampOnDevice;
		_adjustmentFor = adjustmentFor;
		_readRampOnDevice = readRampOnDevice ?? ((Func<string, ushort[]>)((string _) => (ushort[]?)null));
	}

	public void Apply(ushort[] ramp)
	{
		Deliver(ramp, allOrNothing: false);
	}

	public string? ApplyAllOrNothing(ushort[] ramp)
	{
		return Deliver(ramp, allOrNothing: true);
	}

	private string? Deliver(ushort[]? ramp, bool allOrNothing)
	{
		if (_saidGoodbye)
		{
			LastRefusal = "PlexusX has already handed the screen back to Windows";
			ColourTrace.Refused("GAMMA RAMP", "PlexusX has already handed the screen back to Windows");
			return "PlexusX has already handed the screen back to Windows";
		}
		LastRefusal = "";
		DisplaysThatRefusedTheLastWrite = 0;
		DisplaysThatWereSoftened = 0;
		allOrNothing &= ramp != null;
		_lastBase = ramp;
		_lastBaseAllOrNothing = allOrNothing;
		string text = null;
		string step = "";
		bool flag = false;
		bool flag2 = false;
		foreach (string item3 in _enumerateDevices())
		{
			string text2 = ((Selection == null) ? item3 : Selection.KeyFor(item3));
			if (Selection != null && !Selection.IncludesDevice(item3))
			{
				ReleaseSelectedDevice(item3, text2);
				continue;
			}
			flag = true;
			(int, int) tuple = _adjustmentFor(item3);
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			ushort[] array = _readRampOnDevice(item3);
			ushort[] array2 = TheirOwn(text2, array);
			bool flag3 = _coloured.Contains(text2);
			ushort[] array3;
			bool flag4;
			if (ramp == null && NoAdjustment(item, item2))
			{
				if (Selection != null && !flag3)
				{
					flag2 = true;
					continue;
				}
				if (!flag3 && array != null && LooksLikeARamp(array))
				{
					flag2 = true;
					continue;
				}
				array3 = ((flag3 && array2 != null) ? array2 : GammaCurve.Identity());
				flag4 = true;
			}
			else
			{
				if (Selection != null && array2 == null)
				{
					DisplaysThatRefusedTheLastWrite++;
					if (allOrNothing)
					{
						text = "Windows refused the Dark Boost ramp on a display";
						break;
					}
					continue;
				}
				array3 = MonitorColourAdjust.Compose(ramp ?? GammaCurve.Identity(), item, item2);
				flag4 = false;
				if (!_coloured.Contains(text2))
				{
					if (array2 != null)
					{
						BeforeDeviceColour?.Invoke(text2, (ushort[])array2.Clone());
					}
					_coloured.Add(text2);
					BeforeFirstColour?.Invoke();
				}
			}
			if (array != null && SameRamp(array, array3))
			{
				if (flag4)
				{
					ReleaseRecord(text2);
				}
				flag2 = true;
				continue;
			}
			if (!flag4 && !allOrNothing && array != null && _softened.TryGetValue(text2, out (ushort[], ushort[], string) value) && SameRamp(value.Item1, array3) && SameRamp(array, value.Item2))
			{
				DisplaysThatWereSoftened++;
				step = value.Item3;
				flag2 = true;
				continue;
			}
			DeviceWrites++;
			bool flag5 = _setRampOnDevice(item3, array3);
			if ((flag5 & flag4) && Selection != null)
			{
				ushort[] array4 = _readRampOnDevice(item3);
				flag5 = array4 != null && SameRamp(array4, array3);
			}
			if (!flag5 && !flag4 && !allOrNothing && !AlreadyRefusedEveryStep(text2, array3))
			{
				if (TryGentler(item3, array3, out ushort[] landed, out string step2))
				{
					_softened[text2] = ((ushort[])array3.Clone(), landed, step2);
					DisplaysThatWereSoftened++;
					step = step2;
					flag5 = true;
					ColourTrace.Refused("GAMMA RAMP", "Windows refused the full ramp on " + item3 + "; a gentler one (" + step2 + ") went through");
				}
				else
				{
					_refusedEveryStep[text2] = (ushort[])array3.Clone();
				}
			}
			if (flag5 && !flag4)
			{
				_refusedEveryStep.Remove(text2);
			}
			flag2 |= flag5;
			if (flag4 & flag5)
			{
				ReleaseRecord(text2);
			}
			if (!flag5)
			{
				DisplaysThatRefusedTheLastWrite++;
				ColourTrace.Refused("GAMMA RAMP", "Windows refused the ramp on " + item3);
			}
			if (!allOrNothing)
			{
				continue;
			}
			if (!flag5)
			{
				text = "Windows refused the Dark Boost ramp on a display";
			}
			else
			{
				ushort[] array5 = _readRampOnDevice(item3);
				if (array5 == null || !SameRamp(array5, array3))
				{
					DisplaysThatRefusedTheLastWrite++;
					text = "a display did not hold the Dark Boost ramp it was given";
				}
			}
			if (text != null)
			{
				break;
			}
		}
		LastApplyFailed = flag && !flag2;
		if (allOrNothing && text == null && !flag)
		{
			text = "no display to hold the Dark Boost ramp";
		}
		if (LastApplyFailed)
		{
			LastRefusal = "Windows refused the gamma ramp on every display";
		}
		else if (text != null)
		{
			LastRefusal = text;
		}
		else if (DisplaysThatRefusedTheLastWrite > 0)
		{
			LastRefusal = "Windows refused the gamma ramp on " + DisplaysThatRefusedTheLastWrite + " of your displays";
		}
		MultiMonitorGammaRamp multiMonitorGammaRamp = this;
		GammaDelivery lastDelivery;
		if (LastApplyFailed || DisplaysThatRefusedTheLastWrite > 0 || text != null)
		{
			lastDelivery = new GammaDelivery(GammaDeliveryKind.Refused, Math.Max(1, DisplaysThatRefusedTheLastWrite));
		}
		else
		{
			lastDelivery = ((DisplaysThatWereSoftened > 0) ? new GammaDelivery(GammaDeliveryKind.Softened, DisplaysThatWereSoftened, step) : GammaDelivery.Full);
		}
		multiMonitorGammaRamp.LastDelivery = lastDelivery;
		if (!allOrNothing)
		{
			return null;
		}
		return text;
	}

	private bool TryGentler(string device, ushort[] wanted, out ushort[] landed, out string step)
	{
		landed = Array.Empty<ushort>();
		step = "";
		ushort[] array = GammaCurve.Identity();
		if (wanted.Length != array.Length)
		{
			return false;
		}
		double[] softeningSteps = SofteningSteps;
		foreach (double num in softeningSteps)
		{
			ushort[] array2 = Toward(array, wanted, num);
			if (SameRamp(array2, array))
			{
				return false;
			}
			if (_setRampOnDevice(device, array2))
			{
				landed = array2;
				step = "1/" + Math.Round(1.0 / num);
				return true;
			}
		}
		return false;
	}

	internal static ushort[] Toward(ushort[] identity, ushort[] wanted, double strength)
	{
		ushort[] array = new ushort[wanted.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = (ushort)Math.Clamp(Math.Round((double)(int)identity[i] + (double)(wanted[i] - identity[i]) * strength), 0.0, 65535.0);
		}
		return array;
	}

	private ushort[]? TheirOwn(string device, ushort[]? reading)
	{
		if (_coloured.Contains(device) && _theirOwn.TryGetValue(device, out ushort[] value))
		{
			return value;
		}
		ushort[] array = ((reading != null && LooksLikeARamp(reading)) ? ((ushort[])reading.Clone()) : null);
		_theirOwn[device] = array;
		return array;
	}

	internal static bool LooksLikeARamp(ushort[] ramp)
	{
		if (ramp.Length != 768)
		{
			return false;
		}
		for (int i = 0; i < 3; i++)
		{
			int num = i * 256;
			for (int j = 1; j < 256; j++)
			{
				if (ramp[num + j] + 255 < ramp[num + j - 1])
				{
					return false;
				}
			}
			if (ramp[num + 256 - 1] - ramp[num] < 16383)
			{
				return false;
			}
		}
		return true;
	}

	private static bool NoAdjustment(int gammaPercent, int warmthPercent)
	{
		return MonitorColourAdjust.IsNeutral(Math.Clamp(gammaPercent, 50, 150), Math.Clamp(warmthPercent, 0, 100));
	}

	private bool AlreadyRefusedEveryStep(string device, ushort[] wanted)
	{
		if (_refusedEveryStep.TryGetValue(device, out ushort[] value))
		{
			return SameRamp(value, wanted);
		}
		return false;
	}

	private static bool SameRamp(ushort[] a, ushort[] b)
	{
		if (a.Length != b.Length)
		{
			return false;
		}
		for (int i = 0; i < a.Length; i++)
		{
			if (a[i] != b[i])
			{
				return false;
			}
		}
		return true;
	}

	public void Reset()
	{
		Deliver(null, allOrNothing: false);
	}

	public void RefreshMonitors()
	{
		Deliver(_lastBase, _lastBaseAllOrNothing);
	}

	internal void AdoptRecoveryBaselines(IReadOnlyDictionary<string, ushort[]> baselines)
	{
		foreach (var (text2, array2) in baselines)
		{
			if (LooksLikeARamp(array2))
			{
				_theirOwn[text2] = (ushort[])array2.Clone();
				_coloured.Add(text2);
			}
		}
	}

	private void ReleaseRecord(string key)
	{
		_coloured.Remove(key);
		_theirOwn.Remove(key);
		_softened.Remove(key);
		_refusedEveryStep.Remove(key);
		DeviceReleased?.Invoke(key);
	}

	private void ReleaseSelectedDevice(string device, string key)
	{
		if (!_coloured.Contains(key) || !_theirOwn.TryGetValue(key, out ushort[] value) || value == null)
		{
			return;
		}
		ushort[] array = _readRampOnDevice(device);
		if (array != null && SameRamp(array, value))
		{
			ReleaseRecord(key);
			return;
		}
		DeviceWrites++;
		if (_setRampOnDevice(device, value))
		{
			ushort[] array2 = _readRampOnDevice(device);
			if (array2 != null && SameRamp(array2, value))
			{
				ReleaseRecord(key);
			}
		}
	}

	public void Dispose()
	{
		if (_saidGoodbye)
		{
			return;
		}
		_saidGoodbye = true;
		ushort[] array = GammaCurve.Identity();
		foreach (string item in _enumerateDevices())
		{
			if (Selection != null)
			{
				ReleaseSelectedDevice(item, Selection.KeyFor(item));
				continue;
			}
			bool flag = _theirOwn.TryGetValue(item, out ushort[] value);
			if (flag && !_coloured.Contains(item))
			{
				ushort[] array2 = _readRampOnDevice(item);
				if (array2 == null || !LooksLikeARamp(array2))
				{
					_setRampOnDevice(item, array);
				}
			}
			else
			{
				_setRampOnDevice(item, (flag && value != null) ? value : array);
			}
		}
	}

	internal static IReadOnlyList<string> Devices()
	{
		List<string> list = new List<string>();
		DISPLAY_DEVICE lpDisplayDevice = new DISPLAY_DEVICE
		{
			cb = Marshal.SizeOf<DISPLAY_DEVICE>()
		};
		for (uint num = 0u; EnumDisplayDevices(null, num, ref lpDisplayDevice, 0u); num++)
		{
			if ((lpDisplayDevice.StateFlags & 1) != 0 && (lpDisplayDevice.StateFlags & 8) == 0)
			{
				list.Add(lpDisplayDevice.DeviceName);
			}
			lpDisplayDevice = new DISPLAY_DEVICE
			{
				cb = Marshal.SizeOf<DISPLAY_DEVICE>()
			};
		}
		return list;
	}

	internal static bool SetRampOn(string deviceName, ushort[] ramp)
	{
		nint num = CreateDC(null, deviceName, null, IntPtr.Zero);
		if (num == IntPtr.Zero)
		{
			return false;
		}
		try
		{
			return SetDeviceGammaRamp(num, ramp);
		}
		finally
		{
			DeleteDC(num);
		}
	}

	internal static ushort[]? ReadRampOn(string deviceName)
	{
		nint num = CreateDC(null, deviceName, null, IntPtr.Zero);
		if (num == IntPtr.Zero)
		{
			return null;
		}
		try
		{
			ushort[] array = new ushort[768];
			return GetDeviceGammaRamp(num, array) ? array : null;
		}
		finally
		{
			DeleteDC(num);
		}
	}
}
