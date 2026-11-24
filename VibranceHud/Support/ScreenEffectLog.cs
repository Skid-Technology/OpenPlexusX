using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace VibranceHud.Support;

public sealed class ScreenEffectLog
{
	internal const int CapBytes = 262144;

	internal const int KeepBytes = 131072;

	internal const string FileName = "screen-effect.log";

	private static readonly Encoding NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private static readonly ScreenEffectLog Shared = new ScreenEffectLog(null, null);

	private readonly object _gate = new object();

	private readonly string? _folder;

	private readonly Func<DateTime> _utcNow;

	private bool? _lastHeld;

	internal string Path => System.IO.Path.Combine(_folder ?? System.IO.Path.Combine(AppFolders.Local, "logs"), "screen-effect.log");

	internal ScreenEffectLog(string? folder, Func<DateTime>? utcNow)
	{
		_folder = folder;
		_utcNow = utcNow ?? ((Func<DateTime>)(() => DateTime.UtcNow));
	}

	public static string Line(DateTime utc, bool held, int sat)
	{
		return utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture) + (held ? " held" : " released") + " sat=" + sat.ToString(CultureInfo.InvariantCulture);
	}

	public static void Note(bool held, int sat)
	{
		Shared.Record(held, sat);
	}

	internal void Record(bool held, int sat)
	{
		try
		{
			lock (_gate)
			{
				if (_lastHeld != held)
				{
					_lastHeld = held;
					Append(Line(_utcNow(), held, sat));
				}
			}
		}
		catch
		{
		}
	}

	private void Append(string line)
	{
		try
		{
			string path = Path;
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
			FileInfo fileInfo = new FileInfo(path);
			if (fileInfo.Exists && fileInfo.Length > 262144)
			{
				CutToTheTail(path);
			}
			File.AppendAllText(path, line + "\n", NoBom);
		}
		catch (Exception)
		{
		}
	}

	private static void CutToTheTail(string path)
	{
		byte[] array;
		using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
		{
			int num = (int)Math.Min(fileStream.Length, 131072L);
			array = new byte[num];
			fileStream.Seek(-num, SeekOrigin.End);
			int i;
			int num2;
			for (i = 0; i < num; i += num2)
			{
				num2 = fileStream.Read(array, i, num - i);
				if (num2 <= 0)
				{
					break;
				}
			}
			if (i < num)
			{
				Array.Resize(ref array, i);
			}
		}
		int num3 = Array.IndexOf(array, (byte)10) + 1;
		if (num3 <= 0 || num3 >= array.Length)
		{
			num3 = 0;
		}
		using FileStream fileStream2 = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
		fileStream2.Write(array, num3, array.Length - num3);
	}
}
