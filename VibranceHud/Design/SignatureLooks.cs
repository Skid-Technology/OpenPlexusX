using System;
using System.Collections.Generic;

namespace VibranceHud.Design;

public static class SignatureLooks
{
	public static readonly IReadOnlyList<SignatureLook> All = new SignatureLook[2]
	{
		new SignatureLook("Ash", BackdropLook.Off, SurfaceStyle.Glass),
		new SignatureLook("Slate", BackdropLook.Off, SurfaceStyle.Glass)
	};

	public static bool TryGet(string? theme, out SignatureLook look)
	{
		foreach (SignatureLook item in All)
		{
			if (string.Equals(item.Theme, theme, StringComparison.OrdinalIgnoreCase))
			{
				look = item;
				return true;
			}
		}
		look = default;
		return false;
	}
}
