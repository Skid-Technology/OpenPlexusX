using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VibranceHud;

public static class StartupManager
{
	internal interface IRunKey : IDisposable
	{
		object? GetValue(string name);

		void SetValue(string name, string value);

		void DeleteValue(string name);
	}

	private sealed class RegistryRunKey : IRunKey, IDisposable
	{
		private readonly RegistryKey _key;

		public RegistryRunKey(RegistryKey key)
		{
			_key = key;
		}

		public object? GetValue(string name)
		{
			return _key.GetValue(name);
		}

		public void SetValue(string name, string value)
		{
			_key.SetValue(name, value);
		}

		public void DeleteValue(string name)
		{
			_key.DeleteValue(name, throwOnMissingValue: false);
		}

		public void Dispose()
		{
			_key.Dispose();
		}
	}

	internal interface IApprovedKey : IDisposable
	{
		byte[]? Get();

		void Set(byte[] data);
	}

	private sealed class RegistryApprovedKey : IApprovedKey, IDisposable
	{
		private readonly RegistryKey _key;

		public RegistryApprovedKey(RegistryKey key)
		{
			_key = key;
		}

		public byte[]? Get()
		{
			return _key.GetValue("PlexusX") as byte[];
		}

		public void Set(byte[] data)
		{
			_key.SetValue("PlexusX", data, RegistryValueKind.Binary);
		}

		public void Dispose()
		{
			_key.Dispose();
		}
	}

	private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string ValueName = "PlexusX";

	private const string LegacyValueName = "VibranceHud";

	private const string LegacyExeName = "VibranceHud.exe";

	public const string TrustedExeName = "PlexusX.exe";

	internal const string StartupApprovedPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run";

	internal static readonly byte[] ApprovedEnabledBytes = new byte[12]
	{
		2, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0
	};

	internal static bool TaskManagerHasItOff(byte[]? approved)
	{
		if (approved != null && approved.Length > 0)
		{
			return (approved[0] & 1) == 1;
		}
		return false;
	}

	public static bool IsEnabled()
	{
		return IsEnabled(() => Wrap(Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")), () => WrapApproved(Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run")));
	}

	internal static bool IsEnabled(Func<IRunKey?> openRunKey, Func<IApprovedKey?> openApproved)
	{
		if (SecurityCandidate.IsEnabled)
		{
			return false;
		}
		if (!RunEntryExists(openRunKey))
		{
			return false;
		}
		try
		{
			using IApprovedKey approvedKey = openApproved();
			return !TaskManagerHasItOff(approvedKey?.Get());
		}
		catch
		{
			return true;
		}
	}

	public static bool RunEntryExists()
	{
		return RunEntryExists(() => Wrap(Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")));
	}

	internal static bool RunEntryExists(Func<IRunKey?> openRunKey)
	{
		if (SecurityCandidate.IsEnabled)
		{
			return false;
		}
		try
		{
			using IRunKey runKey = openRunKey();
			return runKey?.GetValue("PlexusX") != null;
		}
		catch
		{
			return false;
		}
	}

	public static bool SetEnabled(bool enabled)
	{
		return SetEnabled(enabled, Application.ExecutablePath, () => Wrap(Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")), File.Exists, () => WrapApproved(Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run")));
	}

	internal static bool SetEnabled(bool enabled, string? runningExe, Func<IRunKey?> openRunKey, Func<string, bool> fileExists, Func<IApprovedKey?>? openApproved = null)
	{
		if (SecurityCandidate.IsEnabled)
		{
			return false;
		}
		if (enabled && !IsTrustedExecutable(runningExe))
		{
			return false;
		}
		try
		{
			using IRunKey runKey = openRunKey();
			if (runKey == null)
			{
				return false;
			}
			bool flag = BelongsToAnotherCopy(runKey.GetValue("PlexusX"), runningExe, fileExists);
			if (!enabled)
			{
				if (!flag)
				{
					runKey.DeleteValue("PlexusX");
				}
				runKey.DeleteValue("VibranceHud");
				return !flag;
			}
			if (flag)
			{
				return false;
			}
			runKey.SetValue("PlexusX", CommandLineFor(runningExe));
			runKey.DeleteValue("VibranceHud");
			TurnTaskManagersSwitchOn(openApproved);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void TurnTaskManagersSwitchOn(Func<IApprovedKey?>? openApproved)
	{
		if (openApproved == null)
		{
			return;
		}
		try
		{
			using IApprovedKey approvedKey = openApproved();
			if (approvedKey != null && TaskManagerHasItOff(approvedKey.Get()))
			{
				approvedKey.Set((byte[])ApprovedEnabledBytes.Clone());
			}
		}
		catch
		{
		}
	}

	public static void EnsureCurrentPath()
	{
		if (SecurityCandidate.IsEnabled)
		{
			return;
		}
		try
		{
			string executablePath = Application.ExecutablePath;
			if (IsTrustedExecutable(executablePath))
			{
				EnsureCurrentPath(executablePath, () => Wrap(Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true)), File.Exists);
			}
		}
		catch
		{
		}
	}

	internal static void EnsureCurrentPath(string? runningExe, Func<IRunKey?> openRunKey, Func<string, bool> fileExists)
	{
		try
		{
			if (!IsTrustedExecutable(runningExe))
			{
				return;
			}
			using IRunKey runKey = openRunKey();
			if (runKey != null && runKey.GetValue("PlexusX") is string text && !BelongsToAnotherCopy(text, runningExe, fileExists))
			{
				string text2 = CommandLineFor(runningExe);
				if (!string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
				{
					runKey.SetValue("PlexusX", text2);
				}
				runKey.DeleteValue("VibranceHud");
			}
		}
		catch
		{
		}
	}

	private static bool BelongsToAnotherCopy(object? value, string? runningExe, Func<string, bool> fileExists)
	{
		if (!(value is string command))
		{
			return false;
		}
		string text = ExecutableIn(command);
		if (text == null || !IsTrustedExecutable(text))
		{
			return false;
		}
		if (SamePath(text, runningExe))
		{
			return false;
		}
		return fileExists(text);
	}

	private static bool SamePath(string a, string? b)
	{
		if (string.IsNullOrWhiteSpace(b))
		{
			return false;
		}
		return string.Equals(Full(a), Full(b), StringComparison.OrdinalIgnoreCase);
		static string Full(string p)
		{
			try
			{
				return Path.GetFullPath(p);
			}
			catch
			{
				return p;
			}
		}
	}

	private static IRunKey? Wrap(RegistryKey? key)
	{
		if (key != null)
		{
			return new RegistryRunKey(key);
		}
		return null;
	}

	private static IApprovedKey? WrapApproved(RegistryKey? key)
	{
		if (key != null)
		{
			return new RegistryApprovedKey(key);
		}
		return null;
	}

	internal static IApprovedKey? ApprovedKeyAt(RegistryKey? key)
	{
		return WrapApproved(key);
	}

	public static void RemoveLegacyEntry()
	{
		RemoveLegacyEntry(Application.ExecutablePath, () => Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true), File.Exists);
	}

	internal static bool RemoveLegacyEntry(string? runningExe, Func<RegistryKey?> openRunKey, Func<string, bool> fileExists)
	{
		if (SecurityCandidate.IsEnabled)
		{
			return false;
		}
		try
		{
			if (!IsTrustedExecutable(runningExe))
			{
				return false;
			}
			using RegistryKey registryKey = openRunKey();
			if (registryKey == null)
			{
				return false;
			}
			if (!(registryKey.GetValue("VibranceHud") is string command))
			{
				return false;
			}
			string text = ExecutableIn(command);
			if (text == null)
			{
				return false;
			}
			string fileName = Path.GetFileName(text);
			if ((!string.Equals(fileName, "PlexusX.exe", StringComparison.OrdinalIgnoreCase) && !string.Equals(fileName, "VibranceHud.exe", StringComparison.OrdinalIgnoreCase)) || fileExists(text))
			{
				return false;
			}
			registryKey.DeleteValue("VibranceHud", throwOnMissingValue: false);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string? ExecutableIn(string command)
	{
		string text = command.Trim();
		string text2;
		if (text.StartsWith('"'))
		{
			int num = text.IndexOf('"', 1);
			if (num < 0)
			{
				return null;
			}
			text2 = text.Substring(1, num - 1).Trim();
		}
		else
		{
			int num2 = text.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
			if (num2 < 0)
			{
				return null;
			}
			int num3 = num2 + ".exe".Length;
			if (num3 < text.Length && !char.IsWhiteSpace(text[num3]))
			{
				return null;
			}
			text2 = text.Substring(0, num3);
		}
		if (text2.Length < 3 || !char.IsAsciiLetter(text2[0]) || text2[1] != ':' || (text2[2] != '\\' && text2[2] != '/'))
		{
			return null;
		}
		return text2;
	}

	public static bool IsTrustedExecutable(string? path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return string.Equals(Path.GetFileName(path), "PlexusX.exe", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public static string CommandLineFor(string exePath)
	{
		return "\"" + exePath + "\" --tray";
	}
}
