using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Monitors;

public static class MonitorPanicRestore
{
	private enum Mode
	{
		None,
		Originals,
		Defaults
	}

	private sealed record Plan(AppSettings.PanelSettings Panel, Mode Mode, int? Brightness, int? Contrast, int? Blue, int? Red, int? Green);

	public const string Restored = "Monitor restored";

	public const string Refused = "Your monitor refused - use its menu: Reset";

	public const string NothingSaved = "Colours reset. PlexusX had nothing saved for the monitor itself - if it still looks wrong, use the monitor's own menu.";

	public static string KeyFor(MonitorCapability cap, Func<int, string?> deviceName)
	{
		return deviceName(cap.Index) + "|" + cap.Description;
	}

	public static bool DidNothingToThePanels(PanicResult r)
	{
		if (r.PanelsRestored == 0 && r.PanelsReset == 0)
		{
			return r.PanelsRefused == 0;
		}
		return false;
	}

	public static string BalloonText(PanicResult? result)
	{
		if (!(result == null) && result.PanelsRefused <= 0)
		{
			if (!DidNothingToThePanels(result))
			{
				return "Monitor restored";
			}
			return "Colours reset. PlexusX had nothing saved for the monitor itself - if it still looks wrong, use the monitor's own menu.";
		}
		return "Your monitor refused - use its menu: Reset";
	}

	internal static bool BlueLooksAlreadyCut(MonitorCapability cap)
	{
		if (!cap.SupportsRgbGain || !cap.SupportsRedGain || !cap.SupportsGreenGain)
		{
			return false;
		}
		if (!cap.BlueGain.IsUsable || !cap.RedGain.IsUsable || !cap.GreenGain.IsUsable)
		{
			return false;
		}
		int num = cap.BlueGain.ToPercent(cap.BlueGain.Current);
		int num2 = Math.Min(cap.RedGain.ToPercent(cap.RedGain.Current), cap.GreenGain.ToPercent(cap.GreenGain.Current));
		return (double)num < (double)num2 * 0.7;
	}

	public static IReadOnlyList<string> KeysFor(IEnumerable<MonitorCapability> caps, Func<int, string?> deviceName)
	{
		return caps.Select((MonitorCapability c) => KeyFor(c, deviceName)).ToList();
	}

	private static bool Owned(bool supported, PanelRange now, int wrote)
	{
		if (supported && wrote >= 0 && now.IsUsable)
		{
			return now.Current == wrote;
		}
		return false;
	}

	private static bool HasWriteRecord(AppSettings.PanelSettings p)
	{
		if (p.WroteBrightness < 0 && p.WroteContrast < 0 && p.WroteBlueGain < 0 && p.WroteRedGain < 0)
		{
			return p.WroteGreenGain >= 0;
		}
		return true;
	}

	private static bool DescriptionIsUnique(string key, IReadOnlyCollection<string> connected)
	{
		string d = Desc(key);
		if (d.Length > 0)
		{
			return Enumerable.Count(connected, (string k) => Desc(k) == d) <= 1;
		}
		return false;
		static string Desc(string k)
		{
			int num = k.LastIndexOf('|');
			if (num < 0)
			{
				return k;
			}
			return k.Substring(num + 1);
		}
	}

	private static Plan PlanFor(AppSettings s, MonitorCapability cap, string key, IReadOnlyCollection<string> connected, bool userInitiated)
	{
		AppSettings.PanelSettings panelSettings = s.PanelFor(key, cap.Index, connected);
		bool flag = userInitiated && panelSettings.HasOriginals && !HasWriteRecord(panelSettings) && DescriptionIsUnique(key, connected);
		if (panelSettings.HasOriginals)
		{
			int? brightness = ((cap.SupportsBrightness && panelSettings.OriginalBrightness > cap.Brightness.Min && (flag || Owned(supported: true, cap.Brightness, panelSettings.WroteBrightness))) ? new int?(panelSettings.OriginalBrightness) : ((int?)null));
			int? contrast = (((flag && cap.SupportsContrast) || Owned(cap.SupportsContrast, cap.Contrast, panelSettings.WroteContrast)) ? new int?(panelSettings.OriginalContrast) : ((int?)null));
			int? blue = ((panelSettings.OriginalBlueGain >= 0 && ((flag && cap.SupportsRgbGain) || Owned(cap.SupportsRgbGain, cap.BlueGain, panelSettings.WroteBlueGain))) ? new int?(panelSettings.OriginalBlueGain) : ((int?)null));
			int? red = (((flag && cap.SupportsRedGain) || Owned(cap.SupportsRedGain, cap.RedGain, panelSettings.WroteRedGain)) ? new int?(panelSettings.OriginalRedGain) : ((int?)null));
			int? green = (((flag && cap.SupportsGreenGain) || Owned(cap.SupportsGreenGain, cap.GreenGain, panelSettings.WroteGreenGain)) ? new int?(panelSettings.OriginalGreenGain) : ((int?)null));
			if (brightness.HasValue || contrast.HasValue || blue.HasValue || red.HasValue || green.HasValue)
			{
				return new Plan(panelSettings, Mode.Originals, brightness, contrast, blue, red, green);
			}
		}
		bool flag2 = ((panelSettings.WroteBlueGain < 0 && panelSettings.WroteRedGain < 0 && panelSettings.WroteGreenGain < 0) ? (panelSettings.LowBlue >= 0 || panelSettings.RedGain >= 0 || panelSettings.GreenGain >= 0) : (Owned(cap.SupportsRgbGain, cap.BlueGain, panelSettings.WroteBlueGain) || Owned(cap.SupportsRedGain, cap.RedGain, panelSettings.WroteRedGain) || Owned(cap.SupportsGreenGain, cap.GreenGain, panelSettings.WroteGreenGain)));
		return new Plan(panelSettings, flag2 ? Mode.Defaults : Mode.None, null, null, null, null, null);
	}

	public static PanicResult Run(AppSettings s, SettingsStore store, IMonitorWriter writer, Func<IReadOnlyList<MonitorCapability>> probe, Action gammaLinear, Action driverBaseline, Func<int, string?>? deviceName = null, int? onlyMonitorIndex = null, Action<Action>? runOnUi = null, bool userInitiated = false)
	{
		if (deviceName == null)
		{
			deviceName = MonitorDevices.DeviceNameForIndex;
		}
		Action<Action> action = runOnUi ?? ((Action<Action>)((Action a) =>
		{
			a();
		}));
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		try
		{
			IReadOnlyList<MonitorCapability> readOnlyList;
			try
			{
				readOnlyList = probe() ?? Array.Empty<MonitorCapability>();
			}
			catch
			{
				readOnlyList = Array.Empty<MonitorCapability>();
			}
			IReadOnlyList<string> connected = KeysFor(readOnlyList, deviceName);
			foreach (MonitorCapability cap in readOnlyList)
			{
				if (onlyMonitorIndex.HasValue && cap.Index != onlyMonitorIndex.Value)
				{
					continue;
				}
				try
				{
					Plan plan = null;
					action(() =>
					{
						plan = PlanFor(s, cap, KeyFor(cap, deviceName), connected, userInitiated);
					});
					if (plan == null || plan.Mode == Mode.None)
					{
						continue;
					}
					if (plan.Mode == Mode.Originals)
					{
						(bool Brightness, bool Contrast, bool BlueGain, bool RedGain, bool GreenGain) r = writer.RestoreAll(cap.Index, plan.Brightness, plan.Contrast, plan.Blue, plan.Red, plan.Green);
						bool all = (!plan.Brightness.HasValue || r.Brightness) && (!plan.Contrast.HasValue || r.Contrast) && (!plan.Blue.HasValue || r.BlueGain) && (!plan.Red.HasValue || r.RedGain) && (!plan.Green.HasValue || r.GreenGain);
						action(() =>
						{
							AppSettings.PanelSettings panel = plan.Panel;
							if (plan.Brightness.HasValue && r.Brightness)
							{
								panel.Brightness = -1;
								panel.WroteBrightness = -1;
							}
							if (plan.Contrast.HasValue && r.Contrast)
							{
								panel.Contrast = -1;
								panel.WroteContrast = -1;
							}
							if (plan.Blue.HasValue && r.BlueGain)
							{
								panel.LowBlue = -1;
								panel.WroteBlueGain = -1;
							}
							if (plan.Red.HasValue && r.RedGain)
							{
								panel.RedGain = -1;
								panel.WroteRedGain = -1;
							}
							if (plan.Green.HasValue && r.GreenGain)
							{
								panel.GreenGain = -1;
								panel.WroteGreenGain = -1;
							}
							if (all)
							{
								panel.PanelTouched = false;
							}
						});
						if (all)
						{
							num++;
						}
						else
						{
							num3++;
						}
						continue;
					}
					bool flag = writer.RestoreColourDefaults(cap.Index);
					if (flag)
					{
						action(() =>
						{
							AppSettings.PanelSettings panel = plan.Panel;
							panel.LowBlue = -1;
							panel.RedGain = -1;
							panel.GreenGain = -1;
							panel.WroteBlueGain = -1;
							panel.WroteRedGain = -1;
							panel.WroteGreenGain = -1;
							panel.PanelTouched = false;
						});
					}
					if (flag)
					{
						num2++;
					}
					else
					{
						num3++;
					}
				}
				catch
				{
					num3++;
				}
			}
		}
		catch
		{
		}
		bool gammaLinear2 = false;
		bool driverBack = false;
		try
		{
			action(gammaLinear);
			gammaLinear2 = true;
		}
		catch
		{
		}
		try
		{
			action(driverBaseline);
			driverBack = true;
		}
		catch
		{
		}
		try
		{
			action(() =>
			{
				if (s.MonitorColours != null)
				{
					foreach (MonitorColourSetting monitorColour in s.MonitorColours)
					{
						monitorColour.GammaPercent = 100;
						monitorColour.WarmthPercent = 0;
					}
				}
				store.Save(s);
			});
		}
		catch
		{
		}
		return new PanicResult(num, num2, num3, gammaLinear2, driverBack);
	}
}
