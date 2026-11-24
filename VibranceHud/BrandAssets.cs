using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace VibranceHud;

public static class BrandAssets
{
	private static readonly Dictionary<string, Image?> _cache = new Dictionary<string, Image>();

	private static readonly object _gate = new object();

	public static readonly object DrawGate = new object();

	public static string LogoResourceName(bool light)
	{
		if (!light)
		{
			return "logo-horizontal-white.png";
		}
		return "logo-horizontal-black.png";
	}

	public static Image? HorizontalLogo(bool light)
	{
		return Load(LogoResourceName(light));
	}

	public static Image? PresetChip(string art)
	{
		return Load("preset-" + art + ".png");
	}

	private static Image? Load(string resourceName)
	{
		lock (_gate)
		{
			if (_cache.TryGetValue(resourceName, out Image value))
			{
				return value;
			}
			Image image = null;
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			{
				if (stream != null)
				{
					using Image original = Image.FromStream(stream);
					image = new Bitmap(original);
				}
			}
			if (image != null)
			{
				_cache[resourceName] = image;
			}
			return image;
		}
	}
}
