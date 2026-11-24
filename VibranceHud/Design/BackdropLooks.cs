using System;

namespace VibranceHud.Design;

public static class BackdropLooks
{
	internal static Action<string>? FailPointForTest { get; set; }

	public static string Name(BackdropLook look)
	{
		return look.ToString();
	}

	public static BackdropLook Parse(string? stored)
	{
		string text = (stored ?? "").Trim();
		if (text.Length == 0 || char.IsDigit(text[0]) || text[0] == '-')
		{
			return BackdropLook.Blossom;
		}
		if (text.Contains(','))
		{
			return BackdropLook.Blossom;
		}
		if (!Enum.TryParse<BackdropLook>(text, ignoreCase: true, out var result) || !Enum.IsDefined(result))
		{
			return BackdropLook.Blossom;
		}
		return result;
	}
}
