using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Licensing;

public static class ServedLooks
{
	private static readonly ScenePresetSet NoSet = new ScenePresetSet("", Array.Empty<DisplayPreset>());

	private static readonly GradeCurve DefaultCurve = new GradeCurve(
		RegionRange: 0.25,
		RegionSharpness: 2,
		PointRange: 0.15,
		PointSharpness: 3,
		TintRange: 0.08);

	private static readonly IReadOnlyList<ScenePresetSet> BuiltInSets = new ScenePresetSet[]
	{
		new ScenePresetSet("Rust", new DisplayPreset[]
		{
			new DisplayPreset("Balanced", "Clean contrast and natural colour lift.", 115, 70, 102, 104, 106, 0, "balance", "rust-balanced", Highlights: -10, Shadows: 20),
			new DisplayPreset("Vivid", "Stronger foliage and player separation.", 135, 85, 104, 106, 110, 4, "vivid", "rust-vivid", Highlights: -15, Shadows: 25),
			new DisplayPreset("Forest", "Lifts dark tree lines and shadows.", 122, 78, 104, 108, 108, 6, "forest", "rust-forest", Highlights: -12, Shadows: 35),
			new DisplayPreset("Desert", "Cuts sand glare while keeping players sharp.", 118, 72, 98, 102, 112, -8, "desert", "rust-desert", Highlights: -25, Shadows: 15),
			new DisplayPreset("Snow", "Tames snow bloom and bright horizons.", 112, 68, 95, 100, 114, -6, "snow", "rust-snow", Highlights: -35, Shadows: 18),
			new DisplayPreset("Black Holo", "Turns bright holo reticles dark against glare.", 116, 72, 102, 104, 108, 0, "holo", "rust-blackholo", Highlights: -15, Shadows: 20, ColourVision: ColourVisionMode.HoloBlack)
		}),
		new ScenePresetSet("Counter-Strike 2", new DisplayPreset[]
		{
			new DisplayPreset("Balanced", "Crisp visibility without blowing out walls.", 118, 75, 102, 104, 106, 0, "balance", "cs2-balanced", Highlights: -10, Shadows: 18),
			new DisplayPreset("Vivid", "Punched-up player models and utility.", 140, 90, 103, 105, 110, 2, "vivid", "cs2-vivid", Highlights: -12, Shadows: 22),
			new DisplayPreset("Dust", "Cuts warm sandstone haze on Mirage and Dust 2.", 124, 80, 99, 103, 110, -12, "desert", "cs2-dust", Highlights: -22, Shadows: 20),
			new DisplayPreset("Concrete", "Adds pop on grey maps like Nuke and Ancient.", 128, 82, 102, 105, 112, 6, "concrete", "cs2-concrete", Highlights: -10, Shadows: 24),
			new DisplayPreset("Shadows", "Opens up dark corners and tunnels.", 120, 76, 105, 110, 106, 0, "shadows", "cs2-shadows", Highlights: -15, Shadows: 40)
		}),
		new ScenePresetSet("Valorant", new DisplayPreset[]
		{
			new DisplayPreset("Balanced", "Clean contrast for abilities and outlines.", 116, 74, 101, 103, 106, 0, "balance", "valorant-balanced", Highlights: -10, Shadows: 15),
			new DisplayPreset("Vivid", "High-energy colour separation.", 138, 88, 102, 104, 110, 0, "vivid", "valorant-vivid", Highlights: -12, Shadows: 18),
			new DisplayPreset("Outlines", "Makes yellow and purple outlines stand out.", 132, 86, 100, 104, 114, -4, "outlines", "valorant-outlines", Highlights: -18, Shadows: 20),
			new DisplayPreset("Bright Maps", "Reduces glare on Breeze and Sunset.", 118, 74, 96, 100, 112, -10, "daylight", "valorant-bright-maps", Highlights: -30, Shadows: 14),
			new DisplayPreset("Shade", "Lifts shaded angles and mid sites.", 120, 78, 104, 108, 108, 4, "shadows", "valorant-shade", Highlights: -12, Shadows: 32)
		}),
		new ScenePresetSet("Fortnite", new DisplayPreset[]
		{
			new DisplayPreset("Balanced", "Even vibrance and clear builds.", 116, 72, 102, 104, 106, 0, "balance", "fortnite-balanced", Highlights: -10, Shadows: 18),
			new DisplayPreset("Vivid", "Saturated colours and snappy contrast.", 136, 86, 103, 105, 110, 2, "vivid", "fortnite-vivid", Highlights: -14, Shadows: 20),
			new DisplayPreset("Daylight", "Tames bright sky and storm glare.", 118, 74, 97, 101, 110, -8, "daylight", "fortnite-daylight", Highlights: -28, Shadows: 16),
			new DisplayPreset("Town", "Sharpens indoor fights and cluttered POIs.", 122, 78, 103, 106, 110, 4, "urban", "fortnite-town", Highlights: -14, Shadows: 28),
			new DisplayPreset("Night", "Lifts dark zones during night cycles.", 120, 76, 106, 112, 106, 0, "night", "fortnite-night", Highlights: -10, Shadows: 42)
		}),
		new ScenePresetSet("PUBG", new DisplayPreset[]
		{
			new DisplayPreset("Balanced", "Clear spotting across open terrain.", 118, 74, 102, 104, 108, 0, "balance", "pubg-balanced", Highlights: -12, Shadows: 20),
			new DisplayPreset("Vivid", "Stronger colour contrast for spotting movement.", 136, 88, 103, 106, 112, 2, "vivid", "pubg-vivid", Highlights: -14, Shadows: 24),
			new DisplayPreset("Desert", "Cools Miramar glare and sharpens ridges.", 120, 76, 98, 102, 112, -12, "desert", "pubg-desert", Highlights: -26, Shadows: 18),
			new DisplayPreset("Jungle", "Separates players from dense Sanhok foliage.", 126, 82, 104, 108, 110, 6, "forest", "pubg-jungle", Highlights: -14, Shadows: 34),
			new DisplayPreset("Urban", "Clarity in windows, compounds, and shadows.", 122, 78, 103, 106, 110, 2, "urban", "pubg-urban", Highlights: -14, Shadows: 30)
		}),
		new ScenePresetSet("Rainbow Six Siege", new DisplayPreset[]
		{
			new DisplayPreset("Balanced", "Balanced interior visibility.", 116, 74, 102, 105, 108, 0, "balance", "siege-balanced", Highlights: -12, Shadows: 22),
			new DisplayPreset("Vivid", "Stronger operator and gadget contrast.", 134, 86, 103, 106, 112, 2, "vivid", "siege-vivid", Highlights: -14, Shadows: 24),
			new DisplayPreset("Corners", "Brightens dark rooms and anchor spots.", 118, 76, 105, 110, 108, 0, "shadows", "siege-corners", Highlights: -12, Shadows: 42),
			new DisplayPreset("Doorways", "Balances bright exteriors against dark interiors.", 120, 78, 102, 108, 110, -4, "doorways", "siege-doorways", Highlights: -26, Shadows: 36),
			new DisplayPreset("Daylight", "Cuts exterior window glare.", 116, 72, 97, 102, 112, -8, "daylight", "siege-daylight", Highlights: -30, Shadows: 18)
		})
	};

	public static GradeCurve CurrentCurve => DefaultCurve;

	public static ServedSource Source => ServedSource.Served;

	public static int Generation => 1;

	public static bool OnTheirWay => false;

	public static IReadOnlyList<ScenePresetSet> ByGame => BuiltInSets;

	public static IReadOnlyList<string> Games => BuiltInSets.Select((ScenePresetSet s) => s.Game).ToList();

	public static IReadOnlyList<DisplayPreset> All => BuiltInSets.SelectMany((ScenePresetSet s) => s.Presets).ToList();

	public static ScenePresetSet ForGame(string? game)
	{
		if (BuiltInSets.Count == 0)
		{
			return NoSet;
		}
		return BuiltInSets.FirstOrDefault((ScenePresetSet s) => string.Equals(s.Game, game, StringComparison.OrdinalIgnoreCase)) ?? BuiltInSets[0];
	}

	internal static void Install()
	{
	}
}
