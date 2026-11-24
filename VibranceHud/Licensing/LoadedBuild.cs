using System;
using System.IO;
using System.Security.Cryptography;

namespace VibranceHud.Licensing;

public static class LoadedBuild
{
	private static readonly object Gate = new object();

	private static string? _cached;

	private static bool _computed;

	public static string? Sha256()
	{
		lock (Gate)
		{
			if (_computed)
			{
				return _cached;
			}
			_computed = true;
			_cached = HashOf(typeof(LoadedBuild).Assembly.Location);
			return _cached;
		}
	}

	internal static string? HashOf(string? path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		try
		{
			using FileStream inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			using SHA256 sHA = SHA256.Create();
			return Convert.ToHexString(sHA.ComputeHash(inputStream)).ToLowerInvariant();
		}
		catch
		{
			return null;
		}
	}

	internal static void ForgetForTest()
	{
		lock (Gate)
		{
			_computed = false;
			_cached = null;
		}
	}
}
