using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace VibranceHud.Games;

public static class SupportedGames
{
	public static readonly SupportedGame Rust = new SupportedGame("rust", "Rust", 252490, "Rust", "RustClient");

	public static readonly SupportedGame Cs2 = new SupportedGame("cs2", "Counter-Strike 2", 730, "Counter-Strike Global Offensive", "cs2");

	public static readonly SupportedGame Apex = new SupportedGame("apex", "Apex Legends", 1172470, "Apex Legends", "r5apex", null, new string[1] { "r5apex_dx12" });

	public static readonly SupportedGame Fortnite = new SupportedGame("fortnite", "Fortnite", 0, "Fortnite", "FortniteClient-Win64-Shipping", "Fortnite");

	public static readonly SupportedGame Valorant = new SupportedGame("valorant", "VALORANT", 0, "VALORANT", "VALORANT-Win64-Shipping");

	public static readonly SupportedGame Siege = new SupportedGame("r6", "Rainbow Six Siege", 359550, "Tom Clancy's Rainbow Six Siege", "RainbowSix", null, new string[2] { "RainbowSix_Vulkan", "RainbowSix_BE" });

	public static readonly IReadOnlyList<SupportedGame> All = new SupportedGame[6] { Rust, Cs2, Apex, Fortnite, Valorant, Siege };

	public static IReadOnlyDictionary<string, string> ProcessNames => All.ToDictionary((SupportedGame g) => g.Id, (SupportedGame g) => g.ProcessName, StringComparer.OrdinalIgnoreCase);

	public static IEnumerable<string> AllProcessNames => All.SelectMany((SupportedGame g) => g.AllProcessNames);

	public static SupportedGame? ById(string? id)
	{
		if (id != null)
		{
			return All.FirstOrDefault((SupportedGame g) => g.Id == id);
		}
		return null;
	}

	public static SupportedGame? CurrentlyRunning()
	{
		return All.FirstOrDefault((SupportedGame g) => g.AllProcessNames.Any((string n) => Process.GetProcessesByName(n).Length != 0));
	}
}
