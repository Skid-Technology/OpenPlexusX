using System;
using System.IO;
using System.Linq;

namespace VibranceHud;

public static class DisplayHelp
{
	public enum Vendor
	{
		Unknown,
		Nvidia,
		Amd,
		Intel
	}

	private static readonly string[] NvidiaPanels = new string[2] { "C:\\Program Files\\NVIDIA Corporation\\Control Panel Client\\nvcplui.exe", "C:\\Windows\\System32\\nvcplui.exe" };

	private static readonly string[] AmdPanels = new string[2] { "C:\\Program Files\\AMD\\CNext\\CNext\\RadeonSoftware.exe", "C:\\Program Files\\AMD\\CNext\\CCCSlim\\RadeonSoftware.exe" };

	public static Vendor Installed()
	{
		if (!OperatingSystem.IsWindows())
		{
			return Vendor.Unknown;
		}
		if (NvidiaPanels.Any(SafeExists))
		{
			return Vendor.Nvidia;
		}
		if (AmdPanels.Any(SafeExists))
		{
			return Vendor.Amd;
		}
		return Vendor.Unknown;
	}

	public static string? PanelPath()
	{
		return NvidiaPanels.FirstOrDefault(SafeExists) ?? AmdPanels.FirstOrDefault(SafeExists);
	}

	public static string HowToAddACustomMode()
	{
		return HowToAddACustomMode(Installed());
	}

	public static string HowToAddACustomMode(Vendor vendor)
	{
		return vendor switch
		{
			Vendor.Nvidia => "You can add it yourself in NVIDIA Control Panel: Display, Change resolution, Customise, Create Custom Resolution.", 
			Vendor.Amd => "You can add it yourself in AMD Software: Settings, Display, Custom Resolutions.", 
			_ => "Your graphics driver's control panel can usually add it as a custom resolution.", 
		};
	}

	private static bool SafeExists(string path)
	{
		try
		{
			return File.Exists(path);
		}
		catch
		{
			return false;
		}
	}
}
