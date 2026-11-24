using System;

namespace VibranceHud.DarkBoost;

public sealed record DarkBoostSetup(string Name, DarkBoostTint Tint, int Strength, int BlackLevel)
{
	public DarkBoostSetup Clamped()
	{
		return this with
		{
			Name = (string.IsNullOrWhiteSpace(Name) ? "Setup" : Name.Trim()),
			Strength = Math.Clamp(Strength, 0, 100),
			BlackLevel = Math.Clamp(BlackLevel, 0, 100)
		};
	}
}
