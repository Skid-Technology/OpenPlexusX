using System;
using SharpDX;

namespace VibranceHud;

public static class DxInitFailureMapper
{
	public static (DxInitFailureKind Kind, string Short, string Hint) Map(int hresult)
	{
		return hresult switch
		{
			-2005270526 => (Kind: DxInitFailureKind.NoOutputs, Short: "No display found", Hint: "Make sure your monitor is plugged in and Windows is fully loaded before opening PlexusX."), 
			-2005270524 => (Kind: DxInitFailureKind.DriverIssue, Short: "Display driver doesn't support DX11", Hint: "Update your GPU driver from the manufacturer's website (nvidia.com / amd.com / intel.com)."), 
			-2005270523 => (Kind: DxInitFailureKind.DriverIssue, Short: "GPU was reset", Hint: "Your GPU driver restarted. Click Retry - this often works the second time."), 
			-2005270522 => (Kind: DxInitFailureKind.DriverIssue, Short: "GPU recovered from an error", Hint: "Your GPU recovered from a hang. Click Retry."), 
			-2005270496 => (Kind: DxInitFailureKind.DriverIssue, Short: "GPU driver crashed", Hint: "Reinstall or update your GPU driver."), 
			-2005270494 => (Kind: DxInitFailureKind.CaptureInUse, Short: "Another app is capturing this display", Hint: "Close or stop other screen-capture sources (OBS Display Capture, ShadowPlay, overlay tools), then reopen PlexusX."), 
			-2147024882 => (Kind: DxInitFailureKind.OutOfMemory, Short: "Not enough GPU memory", Hint: "Close other 3D apps (game, browser with hardware accel, recording software) and try again."), 
			-2005270489 => (Kind: DxInitFailureKind.SdkIssue, Short: "DX11 runtime missing", Hint: "Install the DirectX End-User Runtime from microsoft.com (search 'DirectX End-User Runtime')."), 
			_ => (Kind: DxInitFailureKind.Unknown, Short: "DX11 init failed", Hint: $"Restart PlexusX to retry. If it keeps failing, report the code 0x{hresult:X} in a bug report."), 
		};
	}

	public static (DxInitFailureKind Kind, string Short, string Hint) Map(SharpDXException ex)
	{
		return Map(ex?.HResult ?? 0);
	}

	public static (DxInitFailureKind Kind, string Short, string Hint) MapGeneric(Exception ex)
	{
		if (ex == null)
		{
			return (Kind: DxInitFailureKind.Unknown, Short: "DX11 init failed", Hint: "Restart PlexusX to retry.");
		}
		if (ex.InnerException is SharpDXException ex2)
		{
			return Map(ex2);
		}
		return (Kind: DxInitFailureKind.Unknown, Short: "DX11 init failed", Hint: "Restart PlexusX to retry. (" + ex.GetType().Name + ")");
	}

	public static string HintForKind(DxInitFailureKind kind)
	{
		return Map(kind switch
		{
			DxInitFailureKind.NoOutputs => -2005270526, 
			DxInitFailureKind.DriverIssue => -2005270524, 
			DxInitFailureKind.SdkIssue => -2005270489, 
			DxInitFailureKind.OutOfMemory => -2147024882, 
			DxInitFailureKind.CaptureInUse => -2005270494, 
			_ => -2147467259, 
		}).Hint;
	}
}
