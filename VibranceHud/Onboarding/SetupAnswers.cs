using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Onboarding;

public sealed class SetupAnswers
{
	public const string OtherGame = "Something else";

	private static readonly string[] TunedGames = new string[6] { "Rust", "CS2", "Fortnite", "PUBG", "Valorant", "Rainbow Six Siege" };

	private const int CalmSaturation = 100;

	public static IReadOnlyList<string> Games => TunedGames.Concat(new string[1] { "Something else" }).ToList();

	public string Game { get; set; } = "";

	public Complaint Complaint { get; set; }

	public bool WantsCrosshair { get; set; }

	public int SeededSaturation
	{
		get
		{
			if (Complaint != Complaint.EyeStrain)
			{
				return SaturationFor(Game);
			}
			return 100;
		}
	}

	public static int SaturationFor(string? game)
	{
		if (string.Equals(game, "Valorant", StringComparison.OrdinalIgnoreCase))
		{
			return 145;
		}
		if (string.Equals(game, "Rainbow Six Siege", StringComparison.OrdinalIgnoreCase))
		{
			return 160;
		}
		if (string.Equals(game, "Fortnite", StringComparison.OrdinalIgnoreCase))
		{
			return 175;
		}
		if (string.Equals(game, "Rust", StringComparison.OrdinalIgnoreCase))
		{
			return 190;
		}
		if (string.Equals(game, "CS2", StringComparison.OrdinalIgnoreCase))
		{
			return 220;
		}
		if (string.Equals(game, "PUBG", StringComparison.OrdinalIgnoreCase))
		{
			return 235;
		}
		return DemoGrades.Opening.Saturation;
	}

	public void ApplyTo(AppSettings settings)
	{
		if (settings == null)
		{
			throw new ArgumentNullException("settings");
		}
		settings.FavoriteGame = Game ?? "";
		settings.CrosshairEnabled = WantsCrosshair;
		settings.EyeCare = Complaint == Complaint.EyeStrain;
		settings.StreamingMode = Complaint == Complaint.FlatRecordings;
		settings.SaturationPercent = SeededSaturation;
	}

	public string Headline()
	{
		if (!string.IsNullOrWhiteSpace(Game) && !(Game == "Something else"))
		{
			return "Your " + Game + " setup is ready";
		}
		return "Your setup is ready";
	}

	public IEnumerable<string> WhatWasSetUp()
	{
		if (!string.IsNullOrWhiteSpace(Game) && Game != "Something else")
		{
			yield return Game + " presets";
		}
		if (Complaint == Complaint.EyeStrain)
		{
			yield return "Eye care, to take the edge off long sessions";
		}
		if (WantsCrosshair)
		{
			yield return "Crosshair overlay";
		}
	}
}
