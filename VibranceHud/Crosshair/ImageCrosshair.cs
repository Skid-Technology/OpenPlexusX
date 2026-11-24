using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;

namespace VibranceHud.Crosshair;

public static class ImageCrosshair
{
	public const int MaxSidePx = 512;

	private static readonly ConcurrentDictionary<string, Bitmap?> _cache = new ConcurrentDictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

	public static Bitmap? Load(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}
		if (_cache.TryGetValue(path, out Bitmap value))
		{
			return value;
		}
		Bitmap bitmap = Decode(path);
		if (bitmap != null)
		{
			_cache.TryAdd(path, bitmap);
		}
		return bitmap;
	}

	private static Bitmap? Decode(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			using MemoryStream stream = new MemoryStream(File.ReadAllBytes(path));
			using Bitmap bitmap = new Bitmap(stream);
			if (bitmap.Width > 512 || bitmap.Height > 512)
			{
				return null;
			}
			return new Bitmap(bitmap);
		}
		catch
		{
			return null;
		}
	}

	public static void ClearCache()
	{
		_cache.Clear();
	}

	public static bool TryImport(string? sourcePath, string storeDir, out string stored, out string error)
	{
		stored = "";
		if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
		{
			error = "That file could not be found.";
			return false;
		}
		Bitmap bitmap;
		try
		{
			using MemoryStream stream = new MemoryStream(File.ReadAllBytes(sourcePath));
			bitmap = new Bitmap(stream);
		}
		catch
		{
			error = "That file is not an image PlexusX can read. PNG works best, because it can have a transparent background.";
			return false;
		}
		Bitmap value = bitmap;
		try
		{
			if (bitmap.Width > 512 || bitmap.Height > 512)
			{
				error = $"That image is {bitmap.Width}×{bitmap.Height}. The largest a crosshair may be is {512}×{512} - anything bigger costs frames " + "in every game, which is the opposite of the point.";
				return false;
			}
		}
		finally
		{
			((IDisposable)value)?.Dispose();
		}
		try
		{
			Directory.CreateDirectory(storeDir);
			string path = "crosshair-" + ShortHash(File.ReadAllBytes(sourcePath)) + Path.GetExtension(sourcePath);
			string text = Path.Combine(storeDir, path);
			File.Copy(sourcePath, text, overwrite: true);
			_cache.TryRemove(text, out value);
			stored = text;
			error = "";
			return true;
		}
		catch (Exception ex)
		{
			error = "The image is fine, but it could not be saved: " + ex.Message;
			return false;
		}
	}

	private static string ShortHash(byte[] bytes)
	{
		using SHA256 sHA = SHA256.Create();
		return Convert.ToHexString(sHA.ComputeHash(bytes), 0, 6).ToLowerInvariant();
	}
}
