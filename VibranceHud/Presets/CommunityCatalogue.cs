using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using VibranceHud.Crosshair;
using VibranceHud.Display;
using VibranceHud.Licensing;

namespace VibranceHud.Presets;

public sealed record CommunityCatalogue(IReadOnlyList<LibraryPreset> Looks, IReadOnlyList<CrosshairLibraryEntry> Crosshairs, IReadOnlyList<DisplayLibraryEntry> Displays, IReadOnlyList<ConfigLibraryEntry> Configs)
{
	public IReadOnlyList<LockedLibraryCard> Locked { get; init; } = Array.Empty<LockedLibraryCard>();

	internal string? Source { get; private init; }

	internal string ReadWith { get; private init; } = "";

	public static readonly CommunityCatalogue Empty = new CommunityCatalogue(Array.Empty<LibraryPreset>(), Array.Empty<CrosshairLibraryEntry>(), Array.Empty<DisplayLibraryEntry>(), Array.Empty<ConfigLibraryEntry>());

	public const int MaxNameLength = 48;

	public const int MaxBlurbLength = 60;

	private const string FallbackGame = "Other";

	private const string CommunityGlyph = "custom";

	private const string LookKind = "look";

	private const string CrosshairKind = "crosshair";

	private const string DisplayKind = "display";

	private const string ConfigKind = "config";

	private const string LookPrefix = "PX-";

	private const string CrosshairPrefix = "PXC-";

	private const string DisplayPrefix = "PXM-";

	private const string ConfigPrefix = "PXF-";

	internal CommunityCatalogue ReadAgainIfTheCodesMoved()
	{
		if (Source != null && !(CodesKey(ServedCodes.Current) == ReadWith))
		{
			return Parse(Source);
		}
		return this;
	}

	private static string CodesKey(ShareCodeFormat? format)
	{
		if (format == null || format.Withheld)
		{
			return "";
		}
		int[] source = new int[16]
		{
			format.ChecksumModulus, format.ChecksumWeightStep, format.ChecksumWeightOffset, format.ChecksumIndexOffset, format.MaxVibrance, format.MaxSaturation, format.MinBrightness, format.MaxBrightness, format.MinGamma, format.MaxGamma,
			format.MinContrast, format.MaxContrast, format.MinTemperature, format.MaxTemperature, format.MinGrade, format.MaxGrade
		};
		IEnumerable<string> values = from l in format.BodyLengths
			orderby l.Key
			select l.Key.ToString(CultureInfo.InvariantCulture) + ":" + l.Value.ToString(CultureInfo.InvariantCulture);
		return format.Alphabet + "|" + format.Prefix + "|" + string.Join(",", values) + "|" + string.Join(",", source.Select((int n) => n.ToString(CultureInfo.InvariantCulture)));
	}

	public static CommunityCatalogue Parse(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return Empty;
		}
		string readWith = CodesKey(ServedCodes.Current);
		List<LibraryPreset> looks = new List<LibraryPreset>();
		List<CrosshairLibraryEntry> crosshairs = new List<CrosshairLibraryEntry>();
		List<DisplayLibraryEntry> displays = new List<DisplayLibraryEntry>();
		List<ConfigLibraryEntry> configs = new List<ConfigLibraryEntry>();
		List<LockedLibraryCard> locked = new List<LockedLibraryCard>();
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
			{
				return Empty;
			}
			if (!jsonDocument.RootElement.TryGetProperty("presets", out var value))
			{
				return Empty;
			}
			if (value.ValueKind != JsonValueKind.Array)
			{
				return Empty;
			}
			foreach (JsonElement item in value.EnumerateArray())
			{
				try
				{
					Read(item, looks, crosshairs, displays, configs, locked);
				}
				catch
				{
				}
			}
		}
		catch
		{
			return Empty;
		}
		return new CommunityCatalogue(looks, crosshairs, displays, configs)
		{
			Source = json,
			ReadWith = readWith,
			Locked = locked
		};
	}

	private static void Read(JsonElement entry, List<LibraryPreset> looks, List<CrosshairLibraryEntry> crosshairs, List<DisplayLibraryEntry> displays, List<ConfigLibraryEntry> configs, List<LockedLibraryCard> locked)
	{
		if (entry.ValueKind != JsonValueKind.Object)
		{
			return;
		}
		string text = Text(entry, "kind");
		string text2 = Text(entry, "code");
		string name = Clip(Text(entry, "name"), 48, "Untitled");
		string author = Clip(Text(entry, "author"), 48, "Anonymous");
		string blurb = Clip(Text(entry, "blurb"), 60, "");
		string game = Game(Text(entry, "game"));
		string communityId = Text(entry, "id");
		ConfigCode.Parts parts;
		if (text2.Length == 0)
		{
			string text3 = KnownKind(text);
			if (text3 != null)
			{
				locked.Add(new LockedLibraryCard(text3, name, author, game, blurb)
				{
					CommunityId = communityId
				});
			}
		}
		else if (string.Equals(text, "look", StringComparison.OrdinalIgnoreCase))
		{
			if (text2.StartsWith("PX-", StringComparison.Ordinal) && !text2.StartsWith("PXC-", StringComparison.Ordinal) && ProfileCode.TryDecode(text2, out var profile))
			{
				looks.Add(new LibraryPreset(name, author, game, blurb, LookFrom(profile, name, blurb))
				{
					CommunityId = communityId
				});
			}
		}
		else if (string.Equals(text, "crosshair", StringComparison.OrdinalIgnoreCase))
		{
			if (text2.StartsWith("PXC-", StringComparison.Ordinal) && CrosshairCode.TryDecode(text2, out CrosshairConfig crosshair))
			{
				crosshairs.Add(new CrosshairLibraryEntry(name, author, game, blurb, crosshair)
				{
					CommunityId = communityId
				});
			}
		}
		else if (string.Equals(text, "display", StringComparison.OrdinalIgnoreCase))
		{
			if (text2.StartsWith("PXM-", StringComparison.Ordinal) && MonitorCode.TryDecode(text2, out var mode))
			{
				displays.Add(new DisplayLibraryEntry(name, author, game, blurb, mode)
				{
					CommunityId = communityId
				});
			}
		}
		else if (string.Equals(text, "config", StringComparison.OrdinalIgnoreCase) && text2.StartsWith("PXF-", StringComparison.Ordinal) && ConfigCode.TryDecode(text2, out parts))
		{
			configs.Add(new ConfigLibraryEntry(name, author, game, blurb, parts)
			{
				CommunityId = communityId
			});
		}
	}

	public static DisplayPreset LookFrom(ProfileCode profile, string name, string blurb)
	{
		return new DisplayPreset(name, blurb, profile.Saturation, profile.Vibrance, profile.Brightness, profile.Gamma, profile.Contrast, profile.Temperature, "custom", "", profile.Highlights, profile.Shadows, NightVision: false, profile.ColourVision, 0, 0, 0, 0, null, profile.Hue, profile.RedGain, profile.GreenGain, profile.BlueGain, profile.Formula, profile.Darken, profile.DarkenHue, profile.HueTurn, profile.HueTurnAmount, profile.HueComplement);
	}

	private static string? KnownKind(string kind)
	{
		return new string[4] { "look", "crosshair", "display", "config" }.FirstOrDefault((string k) => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase));
	}

	private static string Text(JsonElement entry, string field)
	{
		if (!entry.TryGetProperty(field, out var value) || value.ValueKind != JsonValueKind.String)
		{
			return "";
		}
		return value.GetString() ?? "";
	}

	private static string Clip(string value, int max, string fallback)
	{
		value = value.Trim();
		if (value.Length == 0)
		{
			return fallback;
		}
		if (value.Length > max)
		{
			return value.Substring(0, max);
		}
		return value;
	}

	private static string Game(string game)
	{
		game = Clip(game, 48, "Other");
		if (!string.Equals(game, "All", StringComparison.OrdinalIgnoreCase))
		{
			return game;
		}
		return "Other";
	}
}
