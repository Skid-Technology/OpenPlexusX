namespace VibranceHud.DarkBoost;

public static class DarkBoostTints
{
	public static readonly DarkBoostTint[] All = new DarkBoostTint[5]
	{
		DarkBoostTint.Green,
		DarkBoostTint.Clean,
		DarkBoostTint.Amber,
		DarkBoostTint.Cyan,
		DarkBoostTint.White
	};

	public static string Name(DarkBoostTint t)
	{
		return t.ToString();
	}

	public static string Fact(DarkBoostTint t)
	{
		return t switch
		{
			DarkBoostTint.Green => "Green adds all colours together, so it pulls out the most detail.", 
			DarkBoostTint.Clean => "Clean keeps the game’s own colours. A little less detail than Green.", 
			DarkBoostTint.Amber => "Amber adds all colours together and makes the picture warm orange.", 
			DarkBoostTint.Cyan => "Cyan adds all colours together and makes the picture blue-green.", 
			_ => "White adds all colours together and makes the picture grey.", 
		};
	}

	public static double[]? PreviewWeights(DarkBoostTint t)
	{
		return t switch
		{
			DarkBoostTint.Green => new double[3] { 0.08, 1.0, 0.25 }, 
			DarkBoostTint.Amber => new double[3] { 1.0, 0.62, 0.12 }, 
			DarkBoostTint.Cyan => new double[3] { 0.12, 0.85, 1.0 }, 
			DarkBoostTint.White => new double[3] { 0.9, 0.9, 0.9 }, 
			_ => null, 
		};
	}
}
