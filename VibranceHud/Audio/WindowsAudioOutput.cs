using System;
using System.Runtime.InteropServices;

namespace VibranceHud.Audio;

public sealed class WindowsAudioOutput : IAudioOutput, IDisposable
{
	[ComImport]
	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	private class MMDeviceEnumerator
	{
	}

	[ComImport]
	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDeviceEnumerator
	{
		[PreserveSig]
		int EnumAudioEndpoints(int dataFlow, int stateMask, out nint devices);

		[PreserveSig]
		int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
	}

	[ComImport]
	[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IMMDevice
	{
		[PreserveSig]
		int Activate(ref Guid iid, int clsCtx, nint activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
	}

	[ComImport]
	[Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioMeterInformation
	{
		[PreserveSig]
		int GetPeakValue(out float peak);
	}

	[ComImport]
	[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IAudioEndpointVolume
	{
		[PreserveSig]
		int RegisterControlChangeNotify(nint notify);

		[PreserveSig]
		int UnregisterControlChangeNotify(nint notify);

		[PreserveSig]
		int GetChannelCount(out uint count);

		[PreserveSig]
		int SetMasterVolumeLevel(float level, ref Guid eventContext);

		[PreserveSig]
		int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);

		[PreserveSig]
		int GetMasterVolumeLevel(out float level);

		[PreserveSig]
		int GetMasterVolumeLevelScalar(out float level);
	}

	private const int ERender = 0;

	private const int EConsole = 0;

	private const int ClsCtxAll = 23;

	private static Guid _meterIid = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");

	private static Guid _volumeIid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

	private static Guid _noEvent = Guid.Empty;

	private readonly IAudioMeterInformation _meter;

	private readonly IAudioEndpointVolume _volume;

	public float Peak
	{
		get
		{
			try
			{
				float peak;
				return (_meter.GetPeakValue(out peak) == 0) ? peak : 0f;
			}
			catch
			{
				return 0f;
			}
		}
	}

	public float Volume
	{
		get
		{
			try
			{
				float level;
				return (_volume.GetMasterVolumeLevelScalar(out level) == 0) ? level : float.NaN;
			}
			catch
			{
				return float.NaN;
			}
		}
		set
		{
			try
			{
				_volume.SetMasterVolumeLevelScalar(Math.Clamp(value, 0f, 1f), ref _noEvent);
			}
			catch
			{
			}
		}
	}

	public WindowsAudioOutput()
	{
		Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)new MMDeviceEnumerator()).GetDefaultAudioEndpoint(0, 0, out IMMDevice device));
		Marshal.ThrowExceptionForHR(device.Activate(ref _meterIid, 23, IntPtr.Zero, out object iface));
		Marshal.ThrowExceptionForHR(device.Activate(ref _volumeIid, 23, IntPtr.Zero, out object iface2));
		_meter = (IAudioMeterInformation)iface;
		_volume = (IAudioEndpointVolume)iface2;
	}

	public void Dispose()
	{
		if (Marshal.IsComObject(_meter))
		{
			Marshal.ReleaseComObject(_meter);
		}
		if (Marshal.IsComObject(_volume))
		{
			Marshal.ReleaseComObject(_volume);
		}
	}
}
