using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using VibranceHud.Capabilities;

namespace VibranceHud;

public static class CaptureDiagnostic
{
	private const int Pairs = 10;

	private const float TestSaturation = 3f;

	private const double ReachesThreshold = 1.4;

	private const double MissesThreshold = 1.1;

	private const int DxgiErrorUnsupported = -2005270524;

	internal const string DifferentChipWords = "This laptop's screen runs on a different graphics chip, so the test can't look. In OBS, try Display Capture with Capture Method 'Windows 10 (1903 and up)'";

	internal const string GenericWords = "Windows wouldn't let the test look at your screen";

	internal const string NoScreenOnAnyChipWords = "The test couldn't find your screen on any graphics chip, so it can't look. In OBS, try Display Capture with Capture Method 'Windows 10 (1903 and up)'";

	private const int DxgiErrorNotFound = -2005270526;

	public static CaptureProbe Probe(ISaturationOverlay overlay)
	{
		if (overlay == null)
		{
			return CaptureProbe.Failed("The colour effect isn't running, so there is nothing to test", "no overlay");
		}
		Factory1 factory = null;
		Adapter1 adapter = null;
		SharpDX.Direct3D11.Device device = null;
		Output output = null;
		Output1 output2 = null;
		try
		{
			Factory1 f;
			factory = (f = new Factory1());
			int index = FirstAdapterWithAnOutput(CountAdapters(f), (int i) => AdapterHasAScreen(f, i));
			adapter = factory.GetAdapter1(index);
			device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport);
			output = adapter.GetOutput(0);
			output2 = output.QueryInterface<Output1>();
			double[] array = new double[10];
			for (int num = 0; num < 10; num++)
			{
				overlay.Clear();
				Thread.Sleep(180);
				double num2 = MeanChroma(device, output2);
				overlay.Apply(SaturationMatrix.Build(3f));
				Thread.Sleep(180);
				double num3 = MeanChroma(device, output2);
				array[num] = ((num2 > 0.001) ? (num3 / num2) : 1.0);
			}
			overlay.Clear();
			Array.Sort(array);
			double num4 = (array[4] + array[5]) / 2.0;
			if (num4 >= 1.4)
			{
				return new CaptureProbe(Ran: true, num4, ReachesCapture: true, "the colour effect IS in the desktop-duplication path (Discord screen share sees it)");
			}
			if (num4 <= 1.1)
			{
				return new CaptureProbe(Ran: true, num4, ReachesCapture: false, "not in the desktop-duplication path (Discord screen share won't see it; OBS still will)");
			}
			return new CaptureProbe(Ran: true, num4, ReachesCapture: false, "inconclusive - the screen was changing too much while measuring");
		}
		catch (Exception ex)
		{
			return FailedBecause(ex);
		}
		finally
		{
			output2?.Dispose();
			output?.Dispose();
			device?.Dispose();
			adapter?.Dispose();
			factory?.Dispose();
		}
	}

	public static string BuildReport(AppSettings settings, bool driverVibrance, CaptureProbe probe)
	{
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("PlexusX capture report");
		stringBuilder.AppendLine("----------------------");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("app          : ");
		handler.AppendFormatted(AppInfo.VersionText);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder2);
		handler.AppendLiteral("windows      : ");
		handler.AppendFormatted(Environment.OSVersion.Version);
		handler.AppendLiteral(" (");
		handler.AppendFormatted(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit");
		handler.AppendLiteral(")");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("gpu          : ");
		handler.AppendFormatted(DescribeAdapters());
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("driver vibr. : ");
		handler.AppendFormatted(driverVibrance ? "available (NVIDIA)" : "not available");
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("display path : ");
		handler.AppendFormatted((settings.OverlayMode == OverlayMode.Dx) ? "DX11" : "Magnification");
		stringBuilder7.AppendLine(ref handler);
		if (settings.OverlayMode != OverlayMode.Dx)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder8 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder2);
			handler.AppendLiteral("dx11 failed  : ");
			handler.AppendFormatted(settings.DxFailure);
			handler.AppendLiteral(" - ");
			handler.AppendFormatted(settings.DxFailureMessage);
			stringBuilder8.AppendLine(ref handler);
			if (settings.DxFailureCode != 0)
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder9 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(17, 1, stringBuilder2);
				handler.AppendLiteral("dx11 code    : 0x");
				handler.AppendFormatted(settings.DxFailureCode, "X8");
				stringBuilder9.AppendLine(ref handler);
			}
		}
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder10 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("composited   : ");
		handler.AppendFormatted(settings.KeepDesktopComposited ? "forced on" : "off");
		stringBuilder10.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder11 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("streaming    : ");
		handler.AppendFormatted(settings.StreamingMode ? "on" : "off");
		stringBuilder11.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder12 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 2, stringBuilder2);
		handler.AppendLiteral("vibrance/sat : ");
		handler.AppendFormatted(settings.VibrancePercent);
		handler.AppendLiteral(" / ");
		handler.AppendFormatted(settings.SaturationPercent);
		stringBuilder12.AppendLine(ref handler);
		MachineCapabilities current = Machine.Current;
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder13 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("gamma ramp   : ");
		handler.AppendFormatted(current.GammaRamp);
		stringBuilder13.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder14 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("hdr          : ");
		handler.AppendFormatted(current.HdrActive ? "ON" : "off");
		stringBuilder14.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder15 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
		handler.AppendLiteral("monitors     : ");
		handler.AppendFormatted(current.MonitorCount);
		handler.AppendFormatted(current.MixedDpi ? " (mixed scaling)" : "");
		stringBuilder15.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder16 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("elevated     : ");
		handler.AppendFormatted(current.Elevated ? "yes" : "no");
		stringBuilder16.AppendLine(ref handler);
		stringBuilder.AppendLine();
		if (!probe.Ran)
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder17 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(31, 1, stringBuilder2);
			handler.AppendLiteral("capture test : could not run (");
			handler.AppendFormatted(probe.Note);
			handler.AppendLiteral(")");
			stringBuilder17.AppendLine(ref handler);
			if (!string.IsNullOrEmpty(probe.Detail))
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder18 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
				handler.AppendLiteral("  detail     : ");
				handler.AppendFormatted(probe.Detail);
				stringBuilder18.AppendLine(ref handler);
			}
		}
		else
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder19 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
			handler.AppendLiteral("capture test : ");
			handler.AppendFormatted(probe.Note);
			stringBuilder19.AppendLine(ref handler);
			stringBuilder.AppendLine("  measured   : " + probe.MedianRatio.ToString("F3", invariantCulture) + " (1.0 = capture saw no change, ~2.0 = capture saw the effect)");
		}
		return stringBuilder.ToString();
	}

	private static string DescribeAdapters()
	{
		try
		{
			using Factory1 factory = new Factory1();
			List<string> list = new List<string>();
			int num = 0;
			while (true)
			{
				Adapter1 adapter;
				try
				{
					adapter = factory.GetAdapter1(num);
				}
				catch (SharpDXException)
				{
					break;
				}
				using (adapter)
				{
					AdapterDescription1 description = adapter.Description1;
					if ((description.Flags & AdapterFlags.Software) == 0)
					{
						list.Add(description.Description.Trim());
					}
				}
				num++;
			}
			return (list.Count == 0) ? "none found" : string.Join(" + ", list);
		}
		catch (Exception ex2)
		{
			return "unavailable (" + ex2.GetType().Name + ")";
		}
	}

	public static CaptureProbe FailedBecause(Exception ex)
	{
		return FailedBecause(ex, CountHardwareAdapters());
	}

	internal static CaptureProbe FailedBecause(Exception ex, int hardwareAdapters)
	{
		string detail = ex.GetType().Name + ": " + Short(ex.Message) + $" (0x{ex.HResult:X8})";
		string why;
		if (ex.HResult == -2005270524)
		{
			why = "This laptop's screen runs on a different graphics chip, so the test can't look. In OBS, try Display Capture with Capture Method 'Windows 10 (1903 and up)'";
		}
		else
		{
			why = ((ex.HResult == -2005270526 && hardwareAdapters > 1) ? "The test couldn't find your screen on any graphics chip, so it can't look. In OBS, try Display Capture with Capture Method 'Windows 10 (1903 and up)'" : "Windows wouldn't let the test look at your screen");
		}
		return CaptureProbe.Failed(why, detail);
	}

	internal static int FirstAdapterWithAnOutput(int adapters, Func<int, bool> hasOutput)
	{
		for (int i = 0; i < adapters; i++)
		{
			if (hasOutput(i))
			{
				return i;
			}
		}
		return 0;
	}

	private static int CountAdapters(Factory1 factory)
	{
		int num = 0;
		while (true)
		{
			try
			{
				using (factory.GetAdapter1(num))
				{
				}
			}
			catch (SharpDXException)
			{
				return num;
			}
			num++;
		}
	}

	private static bool AdapterHasAScreen(Factory1 factory, int index)
	{
		try
		{
			using Adapter1 adapter = factory.GetAdapter1(index);
			if ((adapter.Description1.Flags & AdapterFlags.Software) != 0)
			{
				return false;
			}
			using (adapter.GetOutput(0))
			{
				return true;
			}
		}
		catch (SharpDXException)
		{
			return false;
		}
	}

	private static int CountHardwareAdapters()
	{
		try
		{
			using Factory1 factory = new Factory1();
			int num = 0;
			int num2 = 0;
			while (true)
			{
				Adapter1 adapter;
				try
				{
					adapter = factory.GetAdapter1(num2);
				}
				catch (SharpDXException)
				{
					break;
				}
				using (adapter)
				{
					if ((adapter.Description1.Flags & AdapterFlags.Software) == 0)
					{
						num++;
					}
				}
				num2++;
			}
			return num;
		}
		catch (Exception)
		{
			return 0;
		}
	}

	private static string Short(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			return s.Split('\n')[0].Trim();
		}
		return "";
	}

	private static double MeanChroma(SharpDX.Direct3D11.Device device, Output1 output1)
	{
		using OutputDuplication outputDuplication = output1.DuplicateOutput(device);
		SharpDX.DXGI.Resource resource = null;
		for (int i = 0; i < 40; i++)
		{
			if (resource != null)
			{
				break;
			}
			if (outputDuplication.TryAcquireNextFrame(250, out var _, out var desktopResourceOut).Success && desktopResourceOut != null)
			{
				resource = desktopResourceOut;
			}
			else
			{
				Thread.Sleep(25);
			}
		}
		if (resource == null)
		{
			return 0.0;
		}
		using (resource)
		{
			using Texture2D texture2D = resource.QueryInterface<Texture2D>();
			Texture2DDescription description = texture2D.Description;
			using Texture2D texture2D2 = new Texture2D(device, new Texture2DDescription
			{
				Width = description.Width,
				Height = description.Height,
				MipLevels = 1,
				ArraySize = 1,
				Format = description.Format,
				SampleDescription = new SampleDescription(1, 0),
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				CpuAccessFlags = CpuAccessFlags.Read,
				OptionFlags = ResourceOptionFlags.None
			});
			device.ImmediateContext.CopyResource(texture2D, texture2D2);
			DataBox dataBox = device.ImmediateContext.MapSubresource(texture2D2, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
			double num = 0.0;
			int num2 = 0;
			byte[] array = new byte[dataBox.RowPitch];
			for (int j = 0; j < description.Height; j += 4)
			{
				Marshal.Copy(dataBox.DataPointer + j * dataBox.RowPitch, array, 0, dataBox.RowPitch);
				for (int k = 0; k < description.Width; k += 4)
				{
					int num3 = k * 4;
					if (num3 + 2 >= array.Length)
					{
						break;
					}
					byte val = array[num3];
					byte val2 = array[num3 + 1];
					byte val3 = array[num3 + 2];
					int num4 = Math.Max(val3, Math.Max(val2, val));
					int num5 = Math.Min(val3, Math.Min(val2, val));
					num += (double)(num4 - num5);
					num2++;
				}
			}
			device.ImmediateContext.UnmapSubresource(texture2D2, 0);
			try
			{
				outputDuplication.ReleaseFrame();
			}
			catch
			{
			}
			return (num2 == 0) ? 0.0 : (num / (double)num2);
		}
	}
}
