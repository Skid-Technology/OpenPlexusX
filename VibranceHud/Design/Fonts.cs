using System;
using System.Drawing;

namespace VibranceHud.Design;

public static class Fonts
{
	private const string SemiboldFaceSuffix = " Semibold";

	private static readonly Font?[] _cache = new Font[Enum.GetValues<FontRole>().Length];

	private static string? _semiboldFamily;

	private static float _pointScale = 1f;

	private static string? _monoFamily;

	private static TypeStep CaptionStep => new TypeStep(12, 16, 400);

	private static TypeStep CaptionStrongStep => new TypeStep(12, 16, 600);

	private static TypeStep BodyStep => new TypeStep(14, 20, 400);

	private static TypeStep BodyStrongStep => new TypeStep(14, 20, 600);

	private static TypeStep SubtitleStep => new TypeStep(20, 28, 600);

	private static TypeStep TitleStep => new TypeStep(28, 36, 600);

	private static TypeStep ValueStep => new TypeStep(40, 48, 600);

	public static float PointScaleForOffscreenRenders
	{
		get
		{
			return _pointScale;
		}
		set
		{
			if (!(value >= 0.5f) || !(value <= 4f))
			{
				throw new ArgumentOutOfRangeException("value", value, "a render scale outside 0.5 to 4 is a mistake, not a monitor");
			}
			_pointScale = value;
		}
	}

	public static Font Value => Get(FontRole.Value);

	public static Font Display => Get(FontRole.Display);

	public static Font Title => Get(FontRole.Title);

	public static Font Heading => Get(FontRole.Heading);

	public static Font Body => Get(FontRole.Body);

	public static Font Label => Get(FontRole.Label);

	public static Font Caption => Get(FontRole.Caption);

	public static Font Micro => Get(FontRole.Micro);

	public static Font Mono => Get(FontRole.Mono);

	public static Font BodyBold => Get(FontRole.BodyBold);

	public static Font LabelBold => Get(FontRole.LabelBold);

	public static Font CaptionBold => Get(FontRole.CaptionBold);

	public static Font HeadingRegular => Get(FontRole.HeadingRegular);

	public static string MonoFamily => _monoFamily ?? (_monoFamily = ResolveFamilyOrGenericMono(SignalTokens.Root.FontMonoFamilies));

	public static string SemiboldFamily => _semiboldFamily ?? (_semiboldFamily = SemiboldFamilyFor(Theme.FontFamily));

	public static TypeStep Step(FontRole role)
	{
		return role switch
		{
			FontRole.Value => ValueStep, 
			FontRole.Display => TitleStep, 
			FontRole.Title => SubtitleStep, 
			FontRole.Heading => BodyStrongStep, 
			FontRole.HeadingRegular => BodyStep, 
			FontRole.Body => BodyStep, 
			FontRole.BodyBold => BodyStrongStep, 
			FontRole.Label => BodyStep, 
			FontRole.LabelBold => BodyStrongStep, 
			FontRole.Caption => CaptionStep, 
			FontRole.CaptionBold => CaptionStrongStep, 
			FontRole.Mono => BodyStep, 
			_ => CaptionStrongStep, 
		};
	}

	public static float PointsFor(FontRole role)
	{
		return (float)Step(role).SizePx * 72f / 96f * _pointScale;
	}

	public static int LineHeight(FontRole role)
	{
		return Tokens.Scale(Step(role).LinePx);
	}

	public static Font For(FontRole role)
	{
		return Get(role);
	}

	public static Font Of(FontRole role, string family)
	{
		return Make(Step(role), family);
	}

	private static Font Get(FontRole role)
	{
		ref Font reference = ref _cache[(int)role];
		return reference ?? (reference = Make(Step(role), (role == FontRole.Mono) ? MonoFamily : Theme.FontFamily));
	}

	private static Font Make(TypeStep step, string family)
	{
		float emSize = (float)step.SizePx * 72f / 96f * _pointScale;
		if (step.Weight < 600)
		{
			return new Font(family, emSize, FontStyle.Regular);
		}
		string text = SemiboldFamilyFor(family);
		if (!(text == family))
		{
			return new Font(text, emSize, FontStyle.Regular);
		}
		return new Font(family, emSize, FontStyle.Bold);
	}

	private static string ResolveFamilyOrGenericMono(string[] stack)
	{
		for (int num = stack.Length - 1; num >= 0; num--)
		{
			if (IsInstalled(stack[num]))
			{
				return stack[num];
			}
		}
		return FontFamily.GenericMonospace.Name;
	}

	private static string SemiboldFamilyFor(string family)
	{
		string text = family + " Semibold";
		if (!IsInstalled(text))
		{
			return family;
		}
		return text;
	}

	public static string ResolveFamily(string? wanted)
	{
		if (string.IsNullOrWhiteSpace(wanted))
		{
			return Theme.DefaultTypeface;
		}
		if (IsInstalled(wanted))
		{
			return wanted;
		}
		if (IsInstalled(Theme.DefaultTypeface))
		{
			return Theme.DefaultTypeface;
		}
		return FontFamily.GenericSansSerif.Name;
	}

	private static bool IsInstalled(string family)
	{
		try
		{
			using FontFamily fontFamily = new FontFamily(family);
			return string.Equals(fontFamily.Name, family, StringComparison.OrdinalIgnoreCase);
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	public static void Rebuild()
	{
		Array.Clear(_cache);
		_semiboldFamily = null;
		_monoFamily = null;
	}
}
