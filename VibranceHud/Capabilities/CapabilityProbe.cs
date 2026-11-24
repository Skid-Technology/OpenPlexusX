using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;

namespace VibranceHud.Capabilities;

public static class CapabilityProbe
{
	private sealed class PerDeviceGammaTarget : IPerDeviceGammaProbeTarget
	{
		public IReadOnlyList<string> Devices()
		{
			return MultiMonitorGammaRamp.Devices();
		}

		public bool TrySet(string device, ushort[] ramp)
		{
			return MultiMonitorGammaRamp.SetRampOn(device, ramp);
		}

		public ushort[]? TryGet(string device)
		{
			return MultiMonitorGammaRamp.ReadRampOn(device);
		}
	}

	private struct POINT
	{
		public int X;

		public int Y;
	}

	private sealed class ScreenGammaTarget : IGammaProbeTarget
	{
		[DllImport("user32.dll")]
		private static extern nint GetDC(nint hWnd);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(nint hWnd, nint hDC);

		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetDeviceGammaRamp(nint hDC, ushort[] ramp);

		[DllImport("gdi32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetDeviceGammaRamp(nint hDC, ushort[] ramp);

		public bool TrySet(ushort[] ramp)
		{
			nint dC = GetDC(IntPtr.Zero);
			if (dC == IntPtr.Zero)
			{
				return false;
			}
			try
			{
				return SetDeviceGammaRamp(dC, ramp);
			}
			finally
			{
				ReleaseDC(IntPtr.Zero, dC);
			}
		}

		public ushort[]? TryGet()
		{
			nint dC = GetDC(IntPtr.Zero);
			if (dC == IntPtr.Zero)
			{
				return null;
			}
			try
			{
				ushort[] array = new ushort[768];
				return GetDeviceGammaRamp(dC, array) ? array : null;
			}
			finally
			{
				ReleaseDC(IntPtr.Zero, dC);
			}
		}
	}

	internal const int SameCurveTolerance = 512;

	internal const float TestGamma = 6f;

	private const int MONITOR_DEFAULTTONEAREST = 2;

	internal static GammaSupport Classify(ushort[] written, ushort[]? readback, ushort[] identity)
	{
		if (readback == null || written.Length == 0 || readback.Length != written.Length)
		{
			return GammaSupport.Untested;
		}
		double num = MeanAbsoluteDifference(readback, written);
		double num2 = MeanAbsoluteDifference(readback, identity);
		if (num <= 512.0)
		{
			return GammaSupport.Working;
		}
		if (num2 <= 512.0)
		{
			return GammaSupport.Refused;
		}
		return GammaSupport.Clamped;
	}

	private static double MeanAbsoluteDifference(ushort[] a, ushort[] b)
	{
		long num = 0L;
		for (int i = 0; i < a.Length; i++)
		{
			num += Math.Abs(a[i] - b[i]);
		}
		return (double)num / (double)a.Length;
	}

	internal static GammaRefusal WhyRefused(GammaSupport result, bool writeAccepted, bool elevated, bool hdrActive, ushort[]? before, ushort[] identity)
	{
		if (result != GammaSupport.Refused)
		{
			return GammaRefusal.None;
		}
		if (hdrActive)
		{
			return GammaRefusal.HdrOn;
		}
		if (!elevated)
		{
			return GammaRefusal.NotElevated;
		}
		if (!writeAccepted)
		{
			return GammaRefusal.WriteFailed;
		}
		if (before != null && before.Length == identity.Length && MeanAbsoluteDifference(before, identity) > 512.0)
		{
			return GammaRefusal.RampHeldByOther;
		}
		return GammaRefusal.ReadbackIdentity;
	}

	internal static ushort[] TestRamp()
	{
		return GammaCurve.Build(6f);
	}

	internal static ushort[] GentleRamp()
	{
		return GammaCurve.Build(1.15f);
	}

	private static bool GentleGetsThrough(Func<ushort[], bool> trySet, Func<ushort[]?> tryGet)
	{
		ushort[] array = GentleRamp();
		if (trySet(array))
		{
			return Classify(array, tryGet(), GammaCurve.Identity()) == GammaSupport.Working;
		}
		return false;
	}

	internal static GammaSupport ProbeGamma(IGammaProbeTarget target)
	{
		bool couldRead;
		return ProbeGamma(target, out couldRead);
	}

	internal static GammaSupport ProbeGamma(IGammaProbeTarget target, out bool couldRead)
	{
		couldRead = false;
		if (target == null)
		{
			return GammaSupport.Untested;
		}
		ushort[] array = null;
		try
		{
			array = target.TryGet();
			couldRead = array != null;
			if (array == null)
			{
				return GammaSupport.Refused;
			}
			ushort[] array2 = TestRamp();
			GammaSupport gammaSupport = (target.TrySet(array2) ? Classify(array2, target.TryGet(), GammaCurve.Identity()) : GammaSupport.Refused);
			if (gammaSupport == GammaSupport.Refused && GentleGetsThrough(target.TrySet, target.TryGet))
			{
				return GammaSupport.Clamped;
			}
			return gammaSupport;
		}
		catch
		{
			return GammaSupport.Untested;
		}
		finally
		{
			try
			{
				if (array != null)
				{
					target.TrySet(array);
				}
			}
			catch
			{
			}
		}
	}

	internal static IReadOnlyList<DeviceGamma> ProbeDevices(IPerDeviceGammaProbeTarget target, bool elevated, bool hdrActive)
	{
		bool anyReadable;
		return ProbeDevices(target, elevated, hdrActive, out anyReadable);
	}

	internal static IReadOnlyList<DeviceGamma> ProbeDevices(IPerDeviceGammaProbeTarget target, bool elevated, bool hdrActive, out bool anyReadable)
	{
		anyReadable = false;
		List<DeviceGamma> list = new List<DeviceGamma>();
		if (target == null)
		{
			return list;
		}
		IReadOnlyList<string> readOnlyList;
		try
		{
			readOnlyList = target.Devices() ?? Array.Empty<string>();
		}
		catch
		{
			return list;
		}
		ushort[] identity = GammaCurve.Identity();
		foreach (string device in readOnlyList)
		{
			ushort[] array = null;
			try
			{
				array = target.TryGet(device);
				if (array != null)
				{
					anyReadable = true;
					ushort[] array2 = TestRamp();
					bool flag = target.TrySet(device, array2);
					ushort[] readback = (flag ? target.TryGet(device) : null);
					GammaSupport gammaSupport = (flag ? Classify(array2, readback, identity) : GammaSupport.Refused);
					if (gammaSupport == GammaSupport.Refused && GentleGetsThrough((ushort[] r) => target.TrySet(device, r), () => target.TryGet(device)))
					{
						gammaSupport = GammaSupport.Clamped;
					}
					list.Add(new DeviceGamma(device, gammaSupport, WhyRefused(gammaSupport, flag, elevated, hdrActive, array, identity)));
				}
				else
				{
					list.Add(new DeviceGamma(device, GammaSupport.Untested, GammaRefusal.None));
				}
			}
			catch
			{
				list.Add(new DeviceGamma(device, GammaSupport.Untested, GammaRefusal.None));
			}
			finally
			{
				try
				{
					if (array != null)
					{
						target.TrySet(device, array);
					}
				}
				catch
				{
				}
			}
		}
		return list;
	}

	public static MachineCapabilities Run(bool driverVibrance, OverlayMode overlayPath, IGammaProbeTarget? gammaTarget = null, IPerDeviceGammaProbeTarget? perDeviceTarget = null)
	{
		GammaProbeMemory measured;
		return Run(driverVibrance, overlayPath, null, out measured, gammaTarget, perDeviceTarget);
	}

	public static MachineCapabilities Run(bool driverVibrance, OverlayMode overlayPath, GammaProbeMemory? remembered, out GammaProbeMemory? measured, IGammaProbeTarget? gammaTarget = null, IPerDeviceGammaProbeTarget? perDeviceTarget = null)
	{
		measured = null;
		bool elevated = SafeElevated();
		bool flag = SafeHdr();
		IPerDeviceGammaProbeTarget perDeviceGammaProbeTarget = perDeviceTarget ?? new PerDeviceGammaTarget();
		perDeviceTarget = perDeviceGammaProbeTarget;
		IReadOnlyList<string> devices;
		try
		{
			devices = perDeviceGammaProbeTarget.Devices() ?? Array.Empty<string>();
		}
		catch
		{
			devices = Array.Empty<string>();
		}
		string key = GammaProbeMemory.KeyFor(devices, elevated, flag, AppInfo.VersionText);
		if (remembered != null && remembered.Reusable(key))
		{
			measured = remembered;
			return new MachineCapabilities(remembered.Ramp, flag, SafeVendor(), driverVibrance, SafeMonitorCount(), SafeMixedDpi(), elevated, overlayPath, remembered.Why, remembered.Devices);
		}
		GammaSupport gammaSupport = GammaSupport.Untested;
		bool couldRead = false;
		try
		{
			gammaSupport = ProbeGamma(gammaTarget ?? new ScreenGammaTarget(), out couldRead);
		}
		catch
		{
		}
		IReadOnlyList<DeviceGamma> readOnlyList = Array.Empty<DeviceGamma>();
		bool anyReadable = false;
		try
		{
			readOnlyList = ProbeDevices(perDeviceTarget ?? new PerDeviceGammaTarget(), elevated, flag, out anyReadable);
		}
		catch
		{
		}
		GammaRefusal gammaRefusal = GammaRefusal.None;
		foreach (DeviceGamma item in readOnlyList)
		{
			if (item.Why != GammaRefusal.None)
			{
				gammaRefusal = item.Why;
				break;
			}
		}
		if (gammaRefusal == GammaRefusal.None)
		{
			gammaRefusal = WhyRefused(gammaSupport, writeAccepted: true, elevated, flag, null, GammaCurve.Identity());
		}
		if (gammaSupport == GammaSupport.Refused && !couldRead && !anyReadable)
		{
			gammaRefusal = GammaRefusal.ScreenUnreadable;
		}
		if (gammaSupport != GammaSupport.Refused)
		{
			measured = new GammaProbeMemory(key, gammaSupport, gammaRefusal, new List<DeviceGamma>(readOnlyList));
		}
		return new MachineCapabilities(gammaSupport, flag, SafeVendor(), driverVibrance, SafeMonitorCount(), SafeMixedDpi(), elevated, overlayPath, gammaRefusal, readOnlyList);
	}

	private static bool SafeHdr()
	{
		try
		{
			return HdrDetection.AnyDisplayInHdr();
		}
		catch
		{
			return false;
		}
	}

	private static GpuVendor SafeVendor()
	{
		try
		{
			return GpuDetection.PrimaryVendor();
		}
		catch
		{
			return GpuVendor.Unknown;
		}
	}

	private static int SafeMonitorCount()
	{
		try
		{
			return Math.Max(1, Screen.AllScreens.Length);
		}
		catch
		{
			return 1;
		}
	}

	private static bool SafeMixedDpi()
	{
		try
		{
			Screen[] allScreens = Screen.AllScreens;
			if (allScreens.Length < 2)
			{
				return false;
			}
			uint num = DpiOf(allScreens[0]);
			for (int i = 1; i < allScreens.Length; i++)
			{
				if (DpiOf(allScreens[i]) != num)
				{
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private static uint DpiOf(Screen screen)
	{
		if (GetDpiForMonitor(MonitorFromPoint(new POINT
		{
			X = screen.Bounds.Left + 1,
			Y = screen.Bounds.Top + 1
		}, 2), 0, out var x, out var _) != 0)
		{
			return 96u;
		}
		return x;
	}

	private static bool SafeElevated()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	[DllImport("user32.dll")]
	private static extern nint MonitorFromPoint(POINT pt, int flags);

	[DllImport("shcore.dll")]
	private static extern int GetDpiForMonitor(nint monitor, int type, out uint x, out uint y);
}
