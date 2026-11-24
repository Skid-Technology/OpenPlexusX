using System;

namespace VibranceHud.Remote;

internal static class PhoneFields
{
	private const int ToneMin = -100;

	private const int ToneMax = 100;

	internal static readonly PhoneField[] Sliders = new PhoneField[8]
	{
		new PhoneField("saturation", "Saturation", 0, 300, 100, "%"),
		new PhoneField("vibrance", "Vibrance", 0, 350, 50, "%"),
		new PhoneField("brightness", "Brightness", 50, 170, 100, "%"),
		new PhoneField("gamma", "Gamma", 50, 150, 100, "%"),
		new PhoneField("contrast", "Contrast", 50, 150, 100, "%"),
		new PhoneField("temperature", "Temperature", -100, 100, 0, ""),
		new PhoneField("highlights", "Highlights", -100, 100, 0, ""),
		new PhoneField("shadows", "Shadows", -100, 100, 0, "")
	};

	internal const string Preset = "preset";

	internal const string Compare = "compare";

	internal static bool TryFind(string key, out PhoneField field)
	{
		PhoneField[] sliders = Sliders;
		for (int i = 0; i < sliders.Length; i++)
		{
			PhoneField phoneField = sliders[i];
			if (string.Equals(phoneField.Key, key, StringComparison.Ordinal))
			{
				field = phoneField;
				return true;
			}
		}
		field = default;
		return false;
	}

	internal static bool IsAllowed(string key)
	{
		PhoneField field;
		if (!(key == "preset") && !(key == "compare"))
		{
			return TryFind(key, out field);
		}
		return true;
	}

	internal static int Clamp(string key, int value)
	{
		if (TryFind(key, out var field))
		{
			return Math.Clamp(value, field.Min, field.Max);
		}
		return Math.Clamp(value, 0, 1000);
	}
}
