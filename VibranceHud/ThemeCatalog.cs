using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VibranceHud.Design;
using VibranceHud.Theming;

namespace VibranceHud;

public static class ThemeCatalog
{
	public const string DefaultName = "Ash";

	public const string SlateName = "Slate";

	private static readonly ThemePalette Ash = new ThemePalette("Ash", IsLight: false, SignalTokens.Ash.Bg, SignalTokens.Ash.Surface1, SignalTokens.Ash.Hover, SignalTokens.Ash.Border, SignalTokens.Ash.Surface2, SignalTokens.Ash.BorderStrong, SignalTokens.Ash.Text1, SignalTokens.Ash.Text2, SignalTokens.Ash.Accent, SignalTokens.Ash.Track, SignalTokens.Ash.Text3, SignalTokens.Ash.BorderStrong, SignalTokens.Ash.Border, null, 1.0, SignalTokens.Ash.Text3);

	private static readonly ThemePalette Slate = new ThemePalette("Slate", IsLight: false, SignalTokens.Slate.SlateBase, Over(SignalTokens.Slate.Surface1Default, SignalTokens.Slate.SlateBase), Over(SignalTokens.Slate.Hover, SignalTokens.Slate.SlateBase), Over(SignalTokens.Slate.Border, SignalTokens.Slate.SlateBase), Over(SignalTokens.Slate.Surface2, SignalTokens.Slate.SlateBase), Over(SignalTokens.Slate.BorderStrong, SignalTokens.Slate.SlateBase), SignalTokens.Slate.Text1, SignalTokens.Slate.Text2, SignalTokens.Slate.Accent, Over(SignalTokens.Slate.Track, SignalTokens.Slate.SlateBase), SignalTokens.Slate.Text3, Over(SignalTokens.Slate.BorderStrong, SignalTokens.Slate.SlateBase), Over(SignalTokens.Slate.Border, SignalTokens.Slate.SlateBase), null, 0.66, SignalTokens.Slate.Text3);

	public const string CustomName = "Custom";

	private static ThemePalette? _custom;

	private static IReadOnlyList<ThemePalette>? _allCache;

	private static readonly ThemePalette[] BuiltIn = new ThemePalette[2] { Ash, Slate };

	public static bool HasCustom => _custom != null;

	public static IReadOnlyList<ThemePalette> All => _allCache ?? (_allCache = (((object)_custom == null) ? BuiltIn : BuiltIn.Concat(new ThemePalette[1] { _custom }).ToArray()));

	private static Color Over(Color overlay, Color ground)
	{
		return Color.FromArgb(255, ground.R + overlay.A * (overlay.R - ground.R) / 255, ground.G + overlay.A * (overlay.G - ground.G) / 255, ground.B + overlay.A * (overlay.B - ground.B) / 255);
	}

	public static void SetCustom(ImageTheme t)
	{
		_custom = new ThemePalette("Custom", IsLight: false, t.Background, t.Surface, t.SurfaceHover, t.Border, t.GlassFill, t.GlassEdge, SignalTokens.Ash.Text1, SignalTokens.Ash.Text3, t.Accent, t.AccentDim, t.NodeA, t.NodeB, t.Line, null, 1.0, SignalTokens.Ash.Text3);
		_allCache = null;
	}

	public static void ClearCustom()
	{
		_custom = null;
		_allCache = null;
	}

	public static bool IsIdentity(ThemePalette p)
	{
		SignatureLook look;
		return SignatureLooks.TryGet(p.Name, out look);
	}

	public static ThemePalette ByName(string? name)
	{
		return All.FirstOrDefault((ThemePalette p) => p.Name == name) ?? All.First((ThemePalette p) => p.Name == "Ash");
	}

	public static ThemePalette Resolve(string? savedName, bool legacyLight)
	{
		if (!string.IsNullOrWhiteSpace(savedName) && All.Any((ThemePalette p) => p.Name == savedName))
		{
			return ByName(savedName);
		}
		return ByName("Ash");
	}
}
