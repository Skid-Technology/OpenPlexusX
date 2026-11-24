using System;
using System.Diagnostics;

namespace VibranceHud.Capabilities;

public static class Repair
{
	public static string RelaunchArguments => "--relaunch";

	public static RepairKind Plan(MachineCapabilities caps)
	{
		return Plan(caps, FlavourPolicy.Current);
	}

	public static RepairKind Plan(MachineCapabilities caps, FlavourPolicy policy)
	{
		GammaSupport gammaRamp = caps.GammaRamp;
		if ((uint)(gammaRamp - 2) > 1u)
		{
			return RepairKind.None;
		}
		if (caps.HdrActive)
		{
			return RepairKind.OpenHdrSettings;
		}
		if (caps.RefusedWhy == GammaRefusal.ScreenUnreadable)
		{
			return RepairKind.None;
		}
		if (!caps.Elevated)
		{
			if (!policy.OffersElevation)
			{
				return RepairKind.None;
			}
			return RepairKind.RestartAsAdministrator;
		}
		return RepairKind.None;
	}

	public static string ButtonText(RepairKind kind)
	{
		return kind switch
		{
			RepairKind.RestartAsAdministrator => "Fix this", 
			RepairKind.OpenHdrSettings => "Open HDR settings", 
			_ => "", 
		};
	}

	public static string Explain(RepairKind kind)
	{
		return kind switch
		{
			RepairKind.RestartAsAdministrator => "PlexusX will close and open again as administrator. Windows will ask you to confirm.", 
			RepairKind.OpenHdrSettings => "Opens the Windows HDR page. Turn HDR off there, then come back.", 
			_ => "", 
		};
	}

	public static bool Run(RepairKind kind, Action? beforeRestart = null)
	{
		return Run(kind, FlavourPolicy.Current, (ProcessStartInfo elevated) => Process.Start(elevated), beforeRestart);
	}

	internal static bool Run(RepairKind kind, FlavourPolicy policy, Func<ProcessStartInfo, Process?> start, Action? beforeRestart)
	{
		try
		{
			switch (kind)
			{
			case RepairKind.RestartAsAdministrator:
			{
				if (!policy.OffersElevation)
				{
					return false;
				}
				string processPath = Environment.ProcessPath;
				if (string.IsNullOrEmpty(processPath))
				{
					return false;
				}
				ProcessStartInfo arg = new ProcessStartInfo(processPath, RelaunchArguments)
				{
					UseShellExecute = true,
					Verb = "runas"
				};
				if (start(arg) == null)
				{
					return false;
				}
				beforeRestart?.Invoke();
				return true;
			}
			case RepairKind.OpenHdrSettings:
				Process.Start(new ProcessStartInfo("ms-settings:display")
				{
					UseShellExecute = true
				});
				return true;
			default:
				return false;
			}
		}
		catch
		{
			return false;
		}
	}
}
