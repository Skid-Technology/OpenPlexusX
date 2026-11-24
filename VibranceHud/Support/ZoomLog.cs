using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace VibranceHud.Support;

public sealed class ZoomLog
{
	internal const string FileName = "zoom.log";

	internal const int CapBytes = 131072;

	internal const int KeepBytes = 65536;

	private static readonly Encoding NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private static readonly ZoomLog Shared = new ZoomLog(null, null);

	private readonly object _gate = new object();

	private readonly string? _folder;

	private readonly Func<DateTime> _utcNow;

	internal string Path => System.IO.Path.Combine(_folder ?? System.IO.Path.Combine(AppFolders.Local, "logs"), "zoom.log");

	internal ZoomLog(string? folder, Func<DateTime>? utcNow)
	{
		_folder = folder;
		_utcNow = utcNow ?? ((Func<DateTime>)(() => DateTime.UtcNow));
	}

	public static void Note(string what)
	{
		Shared.Write(what);
	}

	internal void Write(string what)
	{
		try
		{
			lock (_gate)
			{
				string text = _utcNow().ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
				string path = Path;
				Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
				FileInfo fileInfo = new FileInfo(path);
				if (fileInfo.Exists && fileInfo.Length > 131072)
				{
					CutToTheTail(path);
				}
				File.AppendAllText(path, text + " " + what + "\n", NoBom);
			}
		}
		catch
		{
		}
	}

	private static void CutToTheTail(string path)
	{
		byte[] array = File.ReadAllBytes(path);
		int num = Math.Min(array.Length, 65536);
		int num2 = array.Length - num;
		int num3 = Array.IndexOf(array, (byte)10, num2);
		if (num3 >= 0 && num3 + 1 < array.Length)
		{
			num2 = num3 + 1;
		}
		using FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
		fileStream.Write(array, num2, array.Length - num2);
	}
}
