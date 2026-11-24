using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using VibranceHud.Licensing;

namespace VibranceHud;

public static class CrashLog
{
	public const int RetentionDays = 30;

	public const int MaxRetainedLogs = 50;

	private static readonly string CrashFileNamePrefix = "crash-";

	private static readonly Regex[] RedactionPatterns = new Regex[5]
	{
		new Regex("[A-Z]:[\\\\/]Users[\\\\/][^\\\\\\/\\r\\n\\\"<>|*?]*[^\\\\\\/\\r\\n\\\"<>|*?\\s]", RegexOptions.Compiled),
		new Regex("\\b[A-Z]{4}-[A-Z]-[FTP]-[A-Z2-9]{8}-[A-Z2-9]{8}\\b", RegexOptions.Compiled),
		new Regex("\\b[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}\\b", RegexOptions.Compiled),
		new Regex("\\b[A-Z2-9]{16}\\b", RegexOptions.Compiled),
		new Regex("[A-Za-z0-9._%+\\-]+@[A-Za-z0-9\\-]+(?:\\.[A-Za-z0-9\\-]+)+", RegexOptions.Compiled)
	};

	private static readonly string[] RedactionReplacements = new string[5] { "<user>", "<serial>", "<hw>", "<hw>", "<email>" };

	internal static readonly string[] SensitiveFolderNames = new string[3] { "Steam\\steamapps", "Steam\\userdata", "Epic Games" };

	internal static readonly string[] SensitiveFolderReplacements = new string[3] { "<steam-path>", "<steam-path>", "<epic-path>" };

	public static string CrashDirectory
	{
		get
		{
			string text = DirectoryForTest ?? Path.Combine(AppFolders.Local, "crashes");
			try
			{
				Directory.CreateDirectory(text);
			}
			catch
			{
			}
			return text;
		}
	}

	internal static string? DirectoryForTest { get; set; }

	public static string Write(Exception ex)
	{
		try
		{
			if (ex == null)
			{
				return "";
			}
			string crashDirectory = CrashDirectory;
			string text = CrashFileNamePrefix + DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss-fff");
			string value = BuildReport(ex);
			for (int i = 0; i <= 10; i++)
			{
				string path = text + ((i == 0) ? "" : ("-" + i)) + ".txt";
				string text2 = Path.Combine(crashDirectory, path);
				FileStream fileStream;
				try
				{
					fileStream = new FileStream(text2, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
				}
				catch (IOException) when (File.Exists(text2))
				{
					continue;
				}
				using (fileStream)
				{
					using StreamWriter streamWriter = new StreamWriter(fileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
					streamWriter.Write(value);
				}
				return text2;
			}
			return "";
		}
		catch
		{
			return "";
		}
	}

	public static void Cleanup()
	{
		try
		{
			string crashDirectory = CrashDirectory;
			if (!Directory.Exists(crashDirectory))
			{
				return;
			}
			DateTime dateTime = DateTime.UtcNow.AddDays(-30.0);
			List<(string, DateTime)> list = (from x in Directory.EnumerateFiles(crashDirectory, CrashFileNamePrefix + "*.txt").Select((string p) =>
				{
					try
					{
						return (Path: p, Time: File.GetLastWriteTimeUtc(p));
					}
					catch
					{
						return (Path: p, Time: DateTime.MinValue);
					}
				})
				orderby x.Time
				select x).ToList();
			foreach (var item in list)
			{
				if (item.Item2 < dateTime)
				{
					try
					{
						File.Delete(item.Item1);
					}
					catch
					{
					}
				}
			}
			List<(string, DateTime)> list2 = list.Where(((string Path, DateTime Time) f) => File.Exists(f.Path)).OrderByDescending(((string Path, DateTime Time) f) => f.Time).ToList();
			while (list2.Count > 50)
			{
				(string, DateTime) tuple = list2[list2.Count - 1];
				try
				{
					File.Delete(tuple.Item1);
				}
				catch
				{
				}
				list2.RemoveAt(list2.Count - 1);
			}
		}
		catch
		{
		}
	}

	private static string BuildReport(Exception ex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("PlexusX crash report");
		stringBuilder.AppendLine("Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
		stringBuilder.AppendLine("Version: " + SafeVersion());
		stringBuilder.AppendLine("OS: " + SafeOs());
		stringBuilder.AppendLine(".NET: " + SafeDotnet());
		stringBuilder.AppendLine("Build: " + SafeBuild());
		stringBuilder.AppendLine("Resources: " + SafeResources());
		stringBuilder.AppendLine();
		AppendException(stringBuilder, ex);
		return Redact(stringBuilder.ToString());
	}

	private static void AppendException(StringBuilder sb, Exception ex, int depth = 0)
	{
		if (ex == null || depth > 5)
		{
			return;
		}
		string text = new string(' ', depth * 2);
		sb.AppendLine(text + "Exception type: " + ex.GetType().FullName);
		sb.AppendLine(text + "Message: " + ex.Message);
		sb.AppendLine(text + "Stack trace:");
		sb.AppendLine(text + ex.StackTrace);
		if (ex is AggregateException ex2)
		{
			{
				foreach (Exception item in ex2.InnerExceptions.Take(5))
				{
					sb.AppendLine(text + "-- Inner exception --");
					AppendException(sb, item, depth + 1);
				}
				return;
			}
		}
		if (ex.InnerException != null)
		{
			sb.AppendLine(text + "-- Inner exception --");
			AppendException(sb, ex.InnerException, depth + 1);
		}
	}

	private static string SafeVersion()
	{
		try
		{
			return UpdateService.CurrentVersion.ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeOs()
	{
		try
		{
			return Environment.OSVersion.VersionString;
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeDotnet()
	{
		try
		{
			return Environment.Version.ToString();
		}
		catch
		{
			return "unknown";
		}
	}

	private static string SafeBuild()
	{
		try
		{
			return LoadedBuild.Sha256() ?? "unknown";
		}
		catch
		{
			return "unknown";
		}
	}

	[DllImport("user32.dll")]
	private static extern uint GetGuiResources(nint hProcess, uint uiFlags);

	private static string SafeResources()
	{
		try
		{
			using Process process = Process.GetCurrentProcess();
			return string.Format(CultureInfo.InvariantCulture, "gdi={0} user={1} handles={2} private_mb={3} uptime_min={4:0}", GetGuiResources(process.Handle, 0u), GetGuiResources(process.Handle, 1u), process.HandleCount, process.PrivateMemorySize64 / 1048576, (DateTime.Now - process.StartTime).TotalMinutes);
		}
		catch
		{
			return "unknown";
		}
	}

	internal static string Redact(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input ?? "";
		}
		string input2 = input;
		for (int i = 0; i < RedactionPatterns.Length; i++)
		{
			input2 = RedactionPatterns[i].Replace(input2, RedactionReplacements[i]);
		}
		return RedactBySubstring(input2);
	}

	internal static string RedactBySubstring(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			return input ?? "";
		}
		string text = input;
		for (int i = 0; i < SensitiveFolderNames.Length; i++)
		{
			text = text.Replace(SensitiveFolderNames[i], SensitiveFolderReplacements[i]);
		}
		return text;
	}
}
