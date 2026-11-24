using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VibranceHud.Capabilities;
using VibranceHud.Licensing;

namespace VibranceHud.Support;

public static class BugReportClient
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10.0);

	public const int MinText = 10;

	public const int MaxText = 2000;

	public const int MaxContact = 200;

	private static string Url => ActivationEndpoint.Origin + "/api/app/bug-report";

	public static IReadOnlyDictionary<string, string> Facts(MachineCapabilities caps, OverlayMode overlay, ReassertMeter? meter, string version)
	{
		return new Dictionary<string, string>
		{
			["gammaRamp"] = caps.GammaRamp.ToString(),
			["elevated"] = (caps.Elevated ? "yes" : "no"),
			["hdr"] = (caps.HdrActive ? "on" : "off"),
			["gpu"] = caps.Gpu.ToString(),
			["driverVibrance"] = (caps.DriverVibrance ? "yes" : "no"),
			["monitors"] = caps.MonitorCount.ToString(),
			["overlay"] = overlay.ToString(),
			["windows"] = Environment.OSVersion.Version.ToString(),
			["overlayMode"] = RuntimeFacts.Overlay,
			["magInitialized"] = (RuntimeFacts.MagInitialized ? "yes" : "no"),
			["screenEffectHeld"] = (RuntimeFacts.ScreenEffectHeld ? "yes" : "no"),
			["saturation"] = RuntimeFacts.Saturation.ToString(CultureInfo.InvariantCulture),
			["gammaRefusedWhy"] = caps.RefusedWhy.ToString(),
			["gammaByDevice"] = DescribeDevices(caps.Devices),
			["rampOutcome"] = RuntimeFacts.RampDelivery.ReportValue,
			["rampSoftenedStep"] = RuntimeFacts.RampDelivery.Step,
			["gdiIcmGammaRange"] = WindowsRampRule.ReadGammaRange()?.ToString() ?? "default",
			["exclusiveFullscreenSeen"] = (RuntimeFacts.ExclusiveFullscreenSeen ? "yes" : "no"),
			["reassertCount"] = (meter?.Reasserts ?? 0).ToString(CultureInfo.InvariantCulture),
			["reassertMaxMs"] = (meter?.MaxMs ?? 0.0).ToString("0.00", CultureInfo.InvariantCulture),
			["flavour"] = "store",
			["version"] = version ?? string.Empty
		};
	}

	internal static string DescribeDevices(IReadOnlyList<DeviceGamma> devices)
	{
		if (devices == null || devices.Count == 0)
		{
			return "none";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (DeviceGamma device in devices)
		{
			string text = device.Device ?? "";
			int num = text.LastIndexOf('\\');
			if (num >= 0)
			{
				text = text.Substring(num + 1);
			}
			string text2 = text + ":" + device.Result;
			if (stringBuilder.Length + ((stringBuilder.Length != 0) ? 1 : 0) + text2.Length > 60)
			{
				break;
			}
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(text2);
		}
		if (stringBuilder.Length != 0)
		{
			return stringBuilder.ToString();
		}
		return "none";
	}

	public static bool IsEnough(string? text)
	{
		return (text ?? "").Trim().Length >= 10;
	}

	public static string BuildBody(string version, string? text, string? contact, IReadOnlyDictionary<string, string> facts)
	{
		string text2 = (text ?? string.Empty).Trim();
		if (text2.Length > 2000)
		{
			text2 = text2.Substring(0, 2000);
		}
		string text3 = (contact ?? string.Empty).Trim();
		if (text3.Length > 200)
		{
			text3 = text3.Substring(0, 200);
		}
		return JsonSerializer.Serialize(new
		{
			version = (version ?? string.Empty),
			text = text2,
			contact = ((text3.Length == 0) ? null : text3),
			facts = facts
		});
	}

	public static async Task<bool> SendAsync(string body, CancellationToken cancel = default(CancellationToken))
	{
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = Timeout
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd("PlexusX");
			using StringContent content = new StringContent(body, Encoding.UTF8, "application/json");
			using HttpResponseMessage httpResponseMessage = await client.PostAsync(Url, content, cancel).ConfigureAwait(continueOnCapturedContext: false);
			return httpResponseMessage.IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}
}
