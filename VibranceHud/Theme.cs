using System.Drawing;
using System.Runtime.CompilerServices;
using VibranceHud.Design;

namespace VibranceHud;

public static class Theme
{
	public static readonly string DefaultTypeface;

	[CompilerGenerated]
	private static string FontFamily__BackingField;

	[CompilerGenerated]
	private static string CurrentName__BackingField;

	[CompilerGenerated]
	private static double PanelOpacity__BackingField;

	public static string FontFamily
	{
		[CompilerGenerated]
		get
		{
			return FontFamily__BackingField;
		}
		[CompilerGenerated]
		internal set
		{
			FontFamily__BackingField = value;
		}
	}

	public static bool IsLight { get; private set; }

	public static string CurrentName
	{
		[CompilerGenerated]
		get
		{
			return CurrentName__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			CurrentName__BackingField = value;
		}
	}

	public static Color Background { get; private set; }

	public static Color Surface { get; private set; }

	public static Color SurfaceHover { get; private set; }

	public static Color Border { get; private set; }

	public static Color GlassFill { get; private set; }

	public static Color GlassEdge { get; private set; }

	public static Color Text { get; private set; }

	public static Color TextDim { get; private set; }

	public static Color TextFaint { get; private set; }

	public static Color Accent { get; private set; }

	public static Color AccentDim { get; private set; }

	public static Color OnAccent
	{
		get
		{
			if (!IsLight)
			{
				return Background;
			}
			return Color.White;
		}
	}

	public static Color OnAccentDim
	{
		get
		{
			if (!IsLight)
			{
				return Text;
			}
			return Color.White;
		}
	}

	public static Color Positive
	{
		get
		{
			if (!IsLight)
			{
				return SignalTokens.Ash.Success;
			}
			return SignalTokens.Root.SuccessPaper;
		}
	}

	public static Color Negative
	{
		get
		{
			if (!IsLight)
			{
				return SignalTokens.Ash.Danger;
			}
			return SignalTokens.Root.DangerPaper;
		}
	}

	public static Color PlexusNodeA { get; private set; }

	public static Color PlexusNodeB { get; private set; }

	public static Color PlexusLine { get; private set; }

	public static double PanelOpacity
	{
		[CompilerGenerated]
		get
		{
			return PanelOpacity__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			PanelOpacity__BackingField = value;
		}
	}

	public static bool IsSeeThrough => PanelOpacity < 1.0;

	static Theme()
	{
		DefaultTypeface = SignalTokens.Root.FontUiFamilies[^1];
		FontFamily__BackingField = DefaultTypeface;
		CurrentName__BackingField = "Ash";
		PanelOpacity__BackingField = 1.0;
		Apply(ThemeCatalog.ByName("Ash"));
	}

	public static void Apply(string name)
	{
		Apply(ThemeCatalog.ByName(name));
	}

	public static void Apply(ThemePalette p)
	{
		CurrentName = p.Name;
		IsLight = p.IsLight;
		Background = p.Background;
		Surface = p.Surface;
		SurfaceHover = p.SurfaceHover;
		Border = p.Border;
		GlassFill = p.GlassFill;
		GlassEdge = p.GlassEdge;
		Text = p.Text;
		TextDim = p.TextDim;
		TextFaint = (p.TextFaint.IsEmpty ? p.TextDim : p.TextFaint);
		Accent = p.Accent;
		AccentDim = p.AccentDim;
		PlexusNodeA = p.PlexusNodeA;
		PlexusNodeB = p.PlexusNodeB;
		PlexusLine = p.PlexusLine;
		PanelOpacity = p.PanelOpacity;
		string text = Fonts.ResolveFamily(p.FontFamily);
		if (text != FontFamily)
		{
			FontFamily = text;
			Fonts.Rebuild();
		}
	}
}
