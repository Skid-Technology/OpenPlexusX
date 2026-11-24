using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace VibranceHud.Theming;

public sealed class CustomThemeService
{
	private const string DefaultFileName = "background.png";

	private static readonly string[] Keepable = new string[6] { ".png", ".gif", ".jpg", ".jpeg", ".bmp", ".webp" };

	private readonly string _dataDir;

	private readonly AppSettings _settings;

	public string ImagePath => Path.Combine(_dataDir, StoredName);

	private string StoredName
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_settings.CustomBackgroundFile))
			{
				return _settings.CustomBackgroundFile;
			}
			return "background.png";
		}
	}

	public bool HasImage
	{
		get
		{
			if (!string.IsNullOrEmpty(_settings.CustomBackgroundFile))
			{
				return File.Exists(ImagePath);
			}
			return false;
		}
	}

	public Color SavedMatte => BackgroundMatte.Default;

	public bool NeedsMatte => AppBackground.NeedsMatte;

	public CustomThemeService(string dataDir, AppSettings settings)
	{
		_dataDir = dataDir;
		_settings = settings;
	}

	public bool SetImage(string sourcePath)
	{
		string text2;
		string text3;
		try
		{
			Directory.CreateDirectory(_dataDir);
			using (FileStream stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
			{
				using Image image = Image.FromStream(stream);
				if (image.Width <= 0 || image.Height <= 0)
				{
					return false;
				}
			}
			string text = Path.GetExtension(sourcePath).ToLowerInvariant();
			if (!Enumerable.Contains(Keepable, text))
			{
				text = ".png";
			}
			text2 = "background" + text;
			text3 = Path.Combine(_dataDir, text2);
			if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(text3), StringComparison.OrdinalIgnoreCase))
			{
				if (Enumerable.Contains(Keepable, Path.GetExtension(sourcePath).ToLowerInvariant()))
				{
					File.Copy(sourcePath, text3, overwrite: true);
				}
				else
				{
					using FileStream stream2 = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
					using Image original = Image.FromStream(stream2);
					using Bitmap bitmap = new Bitmap(original);
					bitmap.Save(text3, ImageFormat.Png);
				}
			}
		}
		catch
		{
			return false;
		}
		if (!AppBackground.Load(text3, _settings.CustomBackgroundDim, _settings.CustomBackgroundBlur))
		{
			return false;
		}
		ImageTheme imageTheme = ImagePalette.Extract(AppBackground.SamplePixels(), ThemeCatalog.ByName("Ash").Accent);
		RemoveStoredFilesExcept(text2);
		_settings.CustomBackgroundFile = text2;
		_settings.CustomBackgroundDim = imageTheme.SuggestedDim;
		_settings.CustomAccentArgb = imageTheme.Accent.ToArgb();
		AppBackground.SetDim(imageTheme.SuggestedDim);
		Install(imageTheme);
		return true;
	}

	public bool Restore()
	{
		if (!HasImage)
		{
			return false;
		}
		if (!AppBackground.Load(ImagePath, _settings.CustomBackgroundDim, _settings.CustomBackgroundBlur))
		{
			return false;
		}
		AppBackground.SetMatte(SavedMatte);
		Install(ImagePalette.Derive((_settings.CustomAccentArgb != 0) ? Color.FromArgb(_settings.CustomAccentArgb) : ImagePalette.Extract(AppBackground.SamplePixels(), ThemeCatalog.ByName("Ash").Accent).Accent, _settings.CustomBackgroundDim));
		return true;
	}

	public void SetDim(int dim)
	{
		_settings.CustomBackgroundDim = dim;
		AppBackground.SetDim(dim);
	}

	public void SetBlur(int blur)
	{
		_settings.CustomBackgroundBlur = blur;
		AppBackground.SetBlur(blur);
	}

	public void SetMatte(Color colour)
	{
		AppBackground.SetMatte(colour);
	}

	public void Remove()
	{
		AppBackground.Clear();
		ThemeCatalog.ClearCustom();
		_settings.CustomBackgroundFile = "";
		_settings.CustomAccentArgb = 0;
		RemoveStoredFilesExcept(null);
	}

	private void RemoveStoredFilesExcept(string? keep)
	{
		try
		{
			if (!Directory.Exists(_dataDir))
			{
				return;
			}
			string[] files = Directory.GetFiles(_dataDir, "background.*");
			foreach (string path in files)
			{
				if (keep == null || !string.Equals(Path.GetFileName(path), keep, StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						File.Delete(path);
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void Install(ImageTheme t)
	{
		ThemeCatalog.SetCustom(t);
	}
}
