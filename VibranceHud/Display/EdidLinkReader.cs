using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VibranceHud.Display;

internal static class EdidLinkReader
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

	private const int EDD_GET_DEVICE_INTERFACE_NAME = 1;

	[DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplayDevicesW")]
	private static extern bool EnumDisplayDevices(string? lpDevice, int iDevNum, ref DISPLAY_DEVICE dd, int flags);

	public static string? Hint(string? deviceName)
	{
		try
		{
			string text = deviceName ?? Screen.PrimaryScreen?.DeviceName ?? "";
			if (text.Length == 0)
			{
				return null;
			}
			DISPLAY_DEVICE dd = new DISPLAY_DEVICE
			{
				cb = Marshal.SizeOf<DISPLAY_DEVICE>()
			};
			if (!EnumDisplayDevices(text, 0, ref dd, 1))
			{
				return null;
			}
			string[] array = (dd.DeviceID ?? "").Split('#');
			if (array.Length < 3)
			{
				return null;
			}
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			using RegistryKey registryKey2 = registryKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\DISPLAY\\" + array[1], writable: false);
			if (registryKey2 == null)
			{
				return null;
			}
			byte[] current = null;
			List<byte[]> list = new List<byte[]>();
			string[] subKeyNames = registryKey2.GetSubKeyNames();
			foreach (string text2 in subKeyNames)
			{
				using RegistryKey registryKey3 = registryKey2.OpenSubKey(text2 + "\\Device Parameters", writable: false);
				if (registryKey3?.GetValue("EDID") is byte[] array2)
				{
					list.Add(array2);
					if (string.Equals(text2, array[2], StringComparison.OrdinalIgnoreCase))
					{
						current = array2;
					}
				}
			}
			int num = 0;
			foreach (DisplayMode item in DisplayController.SupportedModes(text))
			{
				if (item.RefreshHz > num)
				{
					num = item.RefreshHz;
				}
			}
			return EdidLink.CeilingHint(num, current, list);
		}
		catch
		{
			return null;
		}
	}
}
