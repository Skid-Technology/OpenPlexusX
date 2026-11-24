using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using VibranceHud.Capabilities;
using VibranceHud.Crosshair;
using VibranceHud.Customize;
using VibranceHud.DarkBoost;

namespace VibranceHud;

public sealed class AppSettings
{
	public sealed class PanelSettings
	{
		public int Index { get; set; }

		public int Brightness { get; set; } = -1;

		public int Contrast { get; set; } = -1;

		public int LowBlue { get; set; } = -1;

		public int RedGain { get; set; } = -1;

		public int GreenGain { get; set; } = -1;

		public string Key { get; set; } = "";

		public bool PanelTouched { get; set; }

		public int WroteBrightness { get; set; } = -1;

		public int WroteContrast { get; set; } = -1;

		public int WroteBlueGain { get; set; } = -1;

		public int WroteRedGain { get; set; } = -1;

		public int WroteGreenGain { get; set; } = -1;

		public bool HasOriginals { get; set; }

		public int OriginalBrightness { get; set; }

		public int OriginalContrast { get; set; }

		public int OriginalBlueGain { get; set; }

		public int OriginalRedGain { get; set; }

		public int OriginalGreenGain { get; set; }
	}

	internal const uint ZoomDefaultVirtualKey = 6u;

	public int Level { get; set; } = 100;

	public int? VibrancePercent { get; set; }

	public int? SaturationPercent { get; set; }

	public int ResolvedVibrance => VibrancePercent ?? Math.Min(Level, 100);

	public int ResolvedSaturation
	{
		get
		{
			int? saturationPercent = SaturationPercent;
			if (!saturationPercent.HasValue)
			{
				if (Level <= 100)
				{
					return 100;
				}
				return Level;
			}
			return saturationPercent.GetValueOrDefault();
		}
	}

	public bool VibranceChosenByUser { get; set; }

	public int VibranceOwnershipVersion { get; set; }

	public int? DriverVibranceWeLastWrote { get; set; }

	public bool StartWithWindows { get; set; }

	public bool KeepDesktopComposited { get; set; }

	public bool LowLatencyResetDone { get; set; }

	public bool CompositionKeeperDefaultOnDone { get; set; }

	public bool CompositionKeeperDefaultOffDone { get; set; }

	public int OpacityPercent { get; set; } = 100;

	public bool LightTheme { get; set; }

	public string ThemeName { get; set; } = "";

	public string BackdropLookName { get; set; } = "";

	public string AccessEndingWarnedFor { get; set; } = "";

	public string OfflineNoticeShownFor { get; set; } = "";

	public string ServerNoticeShownId { get; set; } = "";

	public string ServerNoticeShownAt { get; set; } = "";

	public string SignInBrowserPath { get; set; } = "";

	public string SignInBrowserName { get; set; } = "";

	public string SurfaceStyleName { get; set; } = "";

	public string ChromeName { get; set; } = "";

	public int ColourModeLookVersion { get; set; }

	public int Hue { get; set; }

	public int RedGain { get; set; } = 100;

	public int GreenGain { get; set; } = 100;

	public int BlueGain { get; set; } = 100;

	public string TaskbarStyleName { get; set; } = "";

	public uint TaskbarColorArgb { get; set; }

	public bool TaskbarFollowsAccent { get; set; }

	public int BrightnessPercent { get; set; } = 100;

	public int GammaPercent { get; set; } = 100;

	public int ContrastPercent { get; set; } = 100;

	public int? Temperature { get; set; }

	public bool EyeCare { get; set; }

	public int ResolvedTemperature
	{
		get
		{
			int? temperature = Temperature;
			if (!temperature.HasValue)
			{
				if (!EyeCare)
				{
					return 0;
				}
				return 50;
			}
			return temperature.GetValueOrDefault();
		}
	}

	public ColourVisionMode ColourVision { get; set; }

	public bool ColourVisionFromPreset { get; set; }

	public ColourVisionMode UserColourVision { get; set; }

	public List<DisplayPreset> CustomPresets { get; set; } = new List<DisplayPreset>();

	public OverlayMode OverlayMode { get; set; }

	public DxInitFailureKind DxFailure { get; set; }

	public string DxFailureMessage { get; set; } = "";

	public int DxFailureCode { get; set; }

	public bool QualityColour { get; set; }

	public string CustomBackgroundFile { get; set; } = "";

	public int CustomBackgroundDim { get; set; } = 40;

	public int CustomBackgroundBlur { get; set; }

	public int CustomBackgroundMatteArgb { get; set; }

	public int SlateGuiOpacityPercent { get; set; }

	public bool SlateMatteBlur { get; set; } = true;

	public int CustomAccentArgb { get; set; }

	public bool NightVisionOn { get; set; }

	public bool DarkBoostBanRiskAccepted { get; set; }

	public bool NightVisionCodeValidated { get; set; }

	public uint NightVisionHotkeyModifierMask { get; set; }

	public uint NightVisionHotkeyVirtualKey { get; set; }

	public bool NightVisionHotkeyEnabled { get; set; }

	public bool ZoomEnabled { get; set; }

	public float ZoomLevel { get; set; } = 2f;

	public uint ZoomHotkeyModifierMask { get; set; }

	public uint ZoomHotkeyVirtualKey { get; set; }

	public bool ZoomSmooth { get; set; } = true;

	public bool ZoomSmoothEdges { get; set; }

	public bool ZoomKeyDefaulted { get; set; }

	public bool CrosshairEnabled { get; set; }

	public CrosshairConfig ActiveCrosshair { get; set; } = new CrosshairConfig();

	public List<CrosshairConfig> SavedCrosshairs { get; set; } = new List<CrosshairConfig>();

	public List<string> FavouriteCrosshairs { get; set; } = new List<string>();

	public string PendingUpdateInstaller { get; set; } = "";

	public string PendingUpdateSha256 { get; set; } = "";

	public string PendingUpdateSignedManifest { get; set; } = "";

	public string PendingUpdateVersion { get; set; } = "";

	public string UpdateRefusedSha256 { get; set; } = "";

	public string UpdateRefusedAtUtc { get; set; } = "";

	public string UpdateRefusedNoticedSha256 { get; set; } = "";

	public string LastSeenVersion { get; set; } = "";

	public bool OnboardingComplete { get; set; }

	public bool ForcedResignInDone { get; set; }

	public string MovedMachineNoticeShownFor { get; set; } = "";

	public bool PlansShown { get; set; }

	public string FavoriteGame { get; set; } = "";

	public string CurrentGameId { get; set; } = "";

	public string PendingGameOfferId { get; set; } = "";

	public string PendingDisplayRestore { get; set; } = "";

	public GameLookSnapshot? LookBeforeGame { get; set; }

	public List<string> DeclinedGameOffers { get; set; } = new List<string>();

	public List<MonitorRule> MonitorRules { get; set; } = new List<MonitorRule>();

	public int RustResolutionWidth { get; set; }

	public int RustResolutionHeight { get; set; }

	public bool AudioEdgeEnabled { get; set; }

	public int AudioEdgeThresholdPercent { get; set; } = 50;

	public float AudioEdgeRestoreVolume { get; set; } = 1f;

	public bool AudioEdgeRestorePending { get; set; }

	public int NvapiRestoreLevel { get; set; } = -1;

	public Dictionary<string, int> NvapiRestoreLevels { get; set; } = new Dictionary<string, int>();

	public bool NvapiRestorePending { get; set; }

	public bool GammaRestorePending { get; set; }

	public int EyeCareWarmth { get; set; }

	public int EyeCareDim { get; set; }

	public uint HotkeyModifierMask { get; set; } = 3u;

	public uint HotkeyVirtualKey { get; set; } = 86u;

	public bool ScreenshotHotkeyEnabled { get; set; } = true;

	public uint ScreenshotHotkeyModifierMask { get; set; } = 3u;

	public uint ScreenshotHotkeyVirtualKey { get; set; } = 83u;

	public int VibranceScaleVersion { get; set; }

	public bool StreamingMode { get; set; }

	public bool CompetitiveMode { get; set; }

	public bool MainHotkeyEnabled { get; set; }

	public uint MainHotkeyModifierMask { get; set; } = 6u;

	public uint MainHotkeyVirtualKey { get; set; } = 77u;

	public List<PresetKeyBinding> PresetKeyBindings { get; set; } = new List<PresetKeyBinding>();

	public List<MonitorColourSetting> MonitorColours { get; set; } = new List<MonitorColourSetting>();

	public List<string>? SelectedMonitorIds { get; set; }

	public List<string>? UntickedDeviceKeys { get; set; }

	public Dictionary<string, int>? OwnedMonitorVibrance { get; set; }

	public Dictionary<string, int>? OwnedMonitorVibranceLastWritten { get; set; }

	public Dictionary<string, ushort[]>? OwnedMonitorGamma { get; set; }

	[JsonIgnore]
	public bool ManualOverrideActive { get; set; }

	public int PopupX { get; set; } = int.MinValue;

	public int PopupY { get; set; } = int.MinValue;

	public int WindowX { get; set; }

	public int WindowY { get; set; }

	public int WindowWidth { get; set; }

	public int WindowHeight { get; set; }

	public bool WindowMaximized { get; set; }

	public ToneSettings? Tone { get; set; }

	public float[]? Mix { get; set; }

	[JsonIgnore]
	public ChannelMix ResolvedMix => ChannelMix.FromArray(Mix);

	public List<PanelSettings> Panels { get; set; } = new List<PanelSettings>();

	public string ColourPresetGame { get; set; } = "";

	public string ColourPresetName { get; set; } = "";

	public bool ShowOnDiscord { get; set; } = true;

	[JsonIgnore]
	public ToneSettings ResolvedTone
	{
		get
		{
			ToneSettings toneSettings = Tone ?? ToneSettings.Neutral;
			return toneSettings with
			{
				Gamma = GammaPercent
			};
		}
	}

	public CustomizationSettings Customization { get; set; } = new CustomizationSettings();

	public SaturationMetric SaturationFormula { get; set; }

	public int Darken { get; set; }

	public int DarkenHue { get; set; } = 120;

	public int HueTurn { get; set; }

	public int HueTurnAmount { get; set; } = 100;

	public int HueComplement { get; set; }

	[JsonIgnore]
	public ColourDepth Depth
	{
		get
		{
			return new ColourDepth(SaturationFormula, Darken, DarkenHue, HueTurn, HueTurnAmount, HueComplement);
		}
		set
		{
			SaturationFormula = value.Formula;
			Darken = value.Darken;
			DarkenHue = value.DarkenHue;
			HueTurn = value.HueTurn;
			HueTurnAmount = value.HueTurnAmount;
			HueComplement = value.HueComplement;
		}
	}

	public bool MusicHotkeyEnabled { get; set; }

	public uint MusicHotkeyModifierMask { get; set; } = 3u;

	public uint MusicHotkeyVirtualKey { get; set; } = 80u;

	public bool NightVisionHotkeyHold { get; set; }

	public bool HotkeysOnlyInGames { get; set; } = true;

	public List<DarkBoostSetup> DarkBoostSetups { get; set; } = new List<DarkBoostSetup> { VibranceHud.DarkBoost.DarkBoostSetups.Default };

	public int DarkBoostSelected { get; set; }

	public bool DarkBoostFade { get; set; } = true;

	public uint DarkBoostNextKeyModifierMask { get; set; }

	public uint DarkBoostNextKeyVirtualKey { get; set; }

	public int? MusicMiniX { get; set; }

	public int? MusicMiniY { get; set; }

	public bool MusicPlayPauseHotkeyEnabled { get; set; }

	public uint MusicPlayPauseHotkeyModifierMask { get; set; }

	public uint MusicPlayPauseHotkeyVirtualKey { get; set; }

	public bool MusicNextHotkeyEnabled { get; set; }

	public uint MusicNextHotkeyModifierMask { get; set; }

	public uint MusicNextHotkeyVirtualKey { get; set; }

	public bool MusicPreviousHotkeyEnabled { get; set; }

	public uint MusicPreviousHotkeyModifierMask { get; set; }

	public uint MusicPreviousHotkeyVirtualKey { get; set; }

	public GammaProbeMemory? GammaProbeMemory { get; set; }

	public static void MigrateVibranceOwnership(AppSettings settings)
	{
		if (settings.VibranceOwnershipVersion < 1)
		{
			settings.VibranceChosenByUser = settings.VibrancePercent.HasValue || settings.Level != 100;
			settings.VibranceOwnershipVersion = 1;
		}
	}

	internal static bool ApplyZoomKeyDefault(AppSettings settings)
	{
		if (settings.ZoomKeyDefaulted)
		{
			return false;
		}
		if (settings.ZoomHotkeyVirtualKey == 0 && !ADarkBoostKeyIsTheZoomDefault(settings) && !HotkeyConflicts.Owner(settings, HotkeySlot.Zoom, 0u, 6u).HasValue)
		{
			settings.ZoomHotkeyModifierMask = 0u;
			settings.ZoomHotkeyVirtualKey = 6u;
		}
		settings.ZoomKeyDefaulted = true;
		return true;
	}

	private static bool ADarkBoostKeyIsTheZoomDefault(AppSettings s)
	{
		if (s.NightVisionHotkeyVirtualKey != 6 || s.NightVisionHotkeyModifierMask != 0)
		{
			if (s.DarkBoostNextKeyVirtualKey == 6)
			{
				return s.DarkBoostNextKeyModifierMask == 0;
			}
			return false;
		}
		return true;
	}

	internal bool HoldsTheNextSetupKey(uint mask, uint vk)
	{
		if (DarkBoostNextKeyVirtualKey == vk)
		{
			return DarkBoostNextKeyModifierMask == mask;
		}
		return false;
	}

	public PanelSettings PanelFor(int index)
	{
		if (Panels == null)
		{
			List<PanelSettings> list = (Panels = new List<PanelSettings>());
		}
		PanelSettings panelSettings = Panels.FirstOrDefault((PanelSettings p) => p.Index == index);
		if (panelSettings != null)
		{
			return panelSettings;
		}
		panelSettings = new PanelSettings
		{
			Index = index
		};
		Panels.Add(panelSettings);
		return panelSettings;
	}

	public PanelSettings PanelFor(string key, int index, IReadOnlyCollection<string>? connectedKeys = null)
	{
		if (Panels == null)
		{
			List<PanelSettings> list = (Panels = new List<PanelSettings>());
		}
		PanelSettings panelSettings = Panels.FirstOrDefault((PanelSettings p) => p.Key == key);
		if (panelSettings != null)
		{
			panelSettings.Index = index;
			return panelSettings;
		}
		string desc = Desc(key);
		if (desc.Length > 0 && connectedKeys != null && Enumerable.Count(connectedKeys, (string k) => Desc(k) == desc) <= 1)
		{
			List<PanelSettings> list3 = Panels.Where((PanelSettings p) => p.Key.Length > 0 && Desc(p.Key) == desc && !connectedKeys.Contains(p.Key)).ToList();
			if (list3.Count == 1)
			{
				PanelSettings panelSettings2 = list3[0];
				panelSettings2.Key = key;
				panelSettings2.Index = index;
				return panelSettings2;
			}
		}
		PanelSettings panelSettings3 = Panels.FirstOrDefault((PanelSettings p) => p.Key.Length == 0 && p.Index == index);
		if (panelSettings3 != null)
		{
			panelSettings3.Key = key;
			return panelSettings3;
		}
		PanelSettings panelSettings4 = new PanelSettings
		{
			Index = index,
			Key = key
		};
		Panels.Add(panelSettings4);
		return panelSettings4;
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
}
