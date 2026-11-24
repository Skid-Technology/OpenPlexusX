using System;
using System.Collections.Generic;
using System.Linq;
using VibranceHud.Crosshair;
using VibranceHud.Display;

namespace VibranceHud.Presets;

public static class ConfigCode
{
	public sealed record Parts(ProfileCode? Look, CrosshairConfig? Crosshair, MonitorCode? Mode);

	public const string Prefix = "PXF-";

	private const char Separator = '.';

	public static string Encode(ProfileCode? look, CrosshairConfig? crosshair, MonitorCode? mode)
	{
		if (!look.HasValue && crosshair == null && !mode.HasValue)
		{
			throw new ArgumentException("A config has to carry at least one of the three.");
		}
		string[] value = new string[3]
		{
			(!look.HasValue) ? "" : ProfileCode.Encode(look.Value),
			(crosshair == null) ? "" : CrosshairCode.Encode(crosshair),
			(!mode.HasValue) ? "" : MonitorCode.Encode(mode.Value)
		};
		return "PXF-" + string.Join('.', value);
	}

	public static bool TryDecode(string? code, out Parts parts)
	{
		parts = new Parts(null, null, null);
		if (string.IsNullOrWhiteSpace(code))
		{
			return false;
		}
		string text = new string((from ch in ShareCodeText.Normalize(code)
			where !char.IsWhiteSpace(ch)
			select ch).ToArray());
		if (!text.StartsWith("PXF-", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string[] array = text.Substring("PXF-".Length).Split('.');
		if (array.Length != 3)
		{
			return false;
		}
		ProfileCode? look = null;
		CrosshairConfig crosshairConfig = null;
		MonitorCode? mode = null;
		if (array[0].Length > 0)
		{
			if (!ProfileCode.TryDecode(array[0], out var profile))
			{
				return false;
			}
			look = profile;
		}
		if (array[1].Length > 0)
		{
			if (!CrosshairCode.TryDecode(array[1], out CrosshairConfig crosshair))
			{
				return false;
			}
			crosshairConfig = crosshair;
		}
		if (array[2].Length > 0)
		{
			if (!MonitorCode.TryDecode(array[2], out var mode2))
			{
				return false;
			}
			mode = mode2;
		}
		if (!look.HasValue && crosshairConfig == null && !mode.HasValue)
		{
			return false;
		}
		parts = new Parts(look, crosshairConfig, mode);
		return true;
	}

	public static IReadOnlyList<string> Describe(Parts parts)
	{
		List<string> list = new List<string>();
		if (parts.Look.HasValue)
		{
			list.Add("Colour");
		}
		if (parts.Crosshair != null)
		{
			list.Add("Crosshair");
		}
		MonitorCode? mode = parts.Mode;
		if (mode.HasValue)
		{
			list.Add(mode.GetValueOrDefault().Label());
		}
		return list;
	}
}
