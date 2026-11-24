using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using VibranceHud.Startup;

namespace VibranceHud;

public static class TaskbarPins
{
	internal readonly record struct Pin(string LinkPath, string Target);

	internal enum OnDisk
	{
		There,
		Gone,
		CannotTell
	}

	internal interface IPinFolder
	{
		IReadOnlyList<Pin> Read();

		void PointAt(Pin pin, string exe);
	}

	private sealed class ShellLinkFolder : IPinFolder
	{
		private readonly string _directory;

		private readonly Action<string, Exception>? _writeDown;

		public ShellLinkFolder(string directory, Action<string, Exception>? writeDown)
		{
			_directory = directory;
			_writeDown = writeDown;
		}

		public IReadOnlyList<Pin> Read()
		{
			List<Pin> list = new List<Pin>();
			if (!Directory.Exists(_directory))
			{
				return list;
			}
			foreach (string item in Directory.EnumerateFiles(_directory, "*.lnk"))
			{
				try
				{
					IShellLinkW shellLinkW = Load(item, writable: false);
					try
					{
						list.Add(new Pin(item, TargetOf(shellLinkW)));
					}
					finally
					{
						Marshal.ReleaseComObject(shellLinkW);
					}
				}
				catch (Exception ex)
				{
					Note(_writeDown, "reading " + Path.GetFileName(item), ex);
				}
			}
			return list;
		}

		public void PointAt(Pin pin, string exe)
		{
			IShellLinkW shellLinkW = Load(pin.LinkPath, writable: true);
			try
			{
				Check(shellLinkW.SetPath(exe));
				Check(shellLinkW.SetWorkingDirectory(Path.GetDirectoryName(exe) ?? ""));
				StringBuilder stringBuilder = new StringBuilder(1040);
				shellLinkW.GetIconLocation(stringBuilder, stringBuilder.Capacity, out var _);
				string text = stringBuilder.ToString();
				if (text.Length == 0 || IsOurExeName(IconFileName(text)))
				{
					Check(shellLinkW.SetIconLocation(exe, 0));
				}
				((IPersistFile)shellLinkW).Save(pin.LinkPath, fRemember: true);
			}
			finally
			{
				Marshal.ReleaseComObject(shellLinkW);
			}
		}

		private static IShellLinkW Load(string link, bool writable)
		{
			IShellLinkW shellLinkW = (IShellLinkW)new ShellLink();
			try
			{
				((IPersistFile)shellLinkW).Load(link, writable ? 2 : 0);
				return shellLinkW;
			}
			catch
			{
				Marshal.ReleaseComObject(shellLinkW);
				throw;
			}
		}

		private static string TargetOf(IShellLinkW shell)
		{
			StringBuilder stringBuilder = new StringBuilder(1040);
			if (shell.GetPath(stringBuilder, stringBuilder.Capacity, IntPtr.Zero, 0) != 0)
			{
				return "";
			}
			return stringBuilder.ToString();
		}

		private static void Check(int hr)
		{
			if (hr < 0)
			{
				Marshal.ThrowExceptionForHR(hr);
			}
		}
	}

	[ComImport]
	[Guid("00021401-0000-0000-C000-000000000046")]
	private class ShellLink
	{
	}

	[ComImport]
	[Guid("000214F9-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellLinkW
	{
		[PreserveSig]
		int GetPath([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder file, int cch, nint findData, int flags);

		[PreserveSig]
		int GetIDList(out nint pidl);

		[PreserveSig]
		int SetIDList(nint pidl);

		[PreserveSig]
		int GetDescription([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int cch);

		[PreserveSig]
		int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);

		[PreserveSig]
		int GetWorkingDirectory([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder dir, int cch);

		[PreserveSig]
		int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string dir);

		[PreserveSig]
		int GetArguments([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder args, int cch);

		[PreserveSig]
		int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);

		[PreserveSig]
		int GetHotkey(out short hotkey);

		[PreserveSig]
		int SetHotkey(short hotkey);

		[PreserveSig]
		int GetShowCmd(out int showCmd);

		[PreserveSig]
		int SetShowCmd(int showCmd);

		[PreserveSig]
		int GetIconLocation([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int cch, out int index);

		[PreserveSig]
		int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);

		[PreserveSig]
		int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string rel, int reserved);

		[PreserveSig]
		int Resolve(nint hwnd, int flags);

		[PreserveSig]
		int SetPath([MarshalAs(UnmanagedType.LPWStr)] string file);
	}

	internal const string UninstallKeyName = "{8F3A1C2B-4D5E-4F6A-9B7C-1234567890AB}}_is1";

	private const string UninstallRoot = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall";

	private const string ProductNamePrefix = "PlexusX";

	private static readonly string[] OurExeNames = new string[2] { "PlexusX.exe", "VibranceHud.exe" };

	private const int MaxPath = 1040;

	private const int StgmRead = 0;

	private const int StgmReadWrite = 2;

	internal static string PinnedFolder => AppFolders.TaskbarPins;

	public static int PointAtThisCopy()
	{
		string executablePath = Application.ExecutablePath;
		if (!StartupManager.IsTrustedExecutable(executablePath))
		{
			return 0;
		}
		string text = InstalledExe();
		if (text == null || !SamePath(executablePath, text))
		{
			return 0;
		}
		string pinnedFolder = PinnedFolder;
		if (!Path.IsPathRooted(pinnedFolder))
		{
			return 0;
		}
		return PointAtThisCopy(executablePath, UpdateService.CurrentVersion, text, () => OtherProductFolders(WriteDown), FolderAt(pinnedFolder, WriteDown), Look, VersionOf, WriteDown);
	}

	internal static void WriteDown(string what, Exception ex)
	{
		StartupStep.Record("pointing your taskbar pin at this PlexusX - " + what, ex);
	}

	internal static int PointAtThisCopy(string? runningExe, Version runningVersion, string? installedExe, IReadOnlyList<string>? otherProductFolders, IPinFolder folder, Func<string, OnDisk> look, Func<string, Version?> versionOf, Action<string, Exception>? writeDown = null)
	{
		return PointAtThisCopy(runningExe, runningVersion, installedExe, () => otherProductFolders, folder, look, versionOf, writeDown);
	}

	private static int PointAtThisCopy(string? runningExe, Version runningVersion, string? installedExe, Func<IReadOnlyList<string>?> otherProductFolders, IPinFolder folder, Func<string, OnDisk> look, Func<string, Version?> versionOf, Action<string, Exception>? writeDown)
	{
		if (!StartupManager.IsTrustedExecutable(runningExe))
		{
			return 0;
		}
		if (installedExe == null || !SamePath(runningExe, installedExe))
		{
			return 0;
		}
		string installFolder = Path.GetDirectoryName(installedExe) ?? "";
		IReadOnlyList<string> readOnlyList = null;
		bool flag = false;
		int num = 0;
		foreach (Pin item in folder.Read())
		{
			try
			{
				if (CouldBeOurs(item.Target, runningExe))
				{
					if (!flag)
					{
						readOnlyList = otherProductFolders();
						flag = true;
					}
					if (readOnlyList == null)
					{
						return num;
					}
					if (IsAStaleCopyOfThisPlexusX(item.Target, runningExe, runningVersion, installFolder, readOnlyList, look, versionOf))
					{
						folder.PointAt(item, runningExe);
						num++;
					}
				}
			}
			catch (Exception ex)
			{
				Note(writeDown, "one pin (" + Path.GetFileName(item.LinkPath) + ")", ex);
			}
		}
		return num;
	}

	private static bool CouldBeOurs(string target, string runningExe)
	{
		if (!string.IsNullOrWhiteSpace(target) && IsLocalPath(target) && IsOurExeName(Path.GetFileName(target)))
		{
			return !SamePath(target, runningExe);
		}
		return false;
	}

	internal static bool IsAStaleCopyOfThisPlexusX(string target, string runningExe, Version runningVersion, string installFolder, IReadOnlyList<string> otherProductFolders, Func<string, OnDisk> look, Func<string, Version?> versionOf)
	{
		if (!CouldBeOurs(target, runningExe))
		{
			return false;
		}
		foreach (string otherProductFolder in otherProductFolders)
		{
			if (IsInside(target, otherProductFolder))
			{
				return false;
			}
		}
		switch (look(target))
		{
		case OnDisk.Gone:
			return true;
		case OnDisk.CannotTell:
			return false;
		default:
		{
			if (!IsInside(target, installFolder))
			{
				return false;
			}
			Version version = versionOf(target);
			if (version != null)
			{
				return version < runningVersion;
			}
			return false;
		}
		}
	}

	internal static OnDisk Look(string path)
	{
		return Look(path, File.Exists, IsAFixedDriveThatIsReady);
	}

	internal static OnDisk Look(string path, Func<string, bool> fileExists, Func<string, bool> isAFixedDriveThatIsReady)
	{
		try
		{
			string pathRoot = Path.GetPathRoot(path);
			if (string.IsNullOrEmpty(pathRoot) || !isAFixedDriveThatIsReady(pathRoot))
			{
				return OnDisk.CannotTell;
			}
			if (fileExists(path))
			{
				return OnDisk.There;
			}
			string directoryName = Path.GetDirectoryName(path);
			string fileName = Path.GetFileName(path);
			while (directoryName != null && !Directory.Exists(directoryName))
			{
				fileName = Path.GetFileName(directoryName);
				directoryName = Path.GetDirectoryName(directoryName);
			}
			if (directoryName == null || fileName.Length == 0)
			{
				return OnDisk.CannotTell;
			}
			using IEnumerator<string> enumerator = Directory.EnumerateFileSystemEntries(directoryName, fileName).GetEnumerator();
			return (!enumerator.MoveNext()) ? OnDisk.Gone : OnDisk.CannotTell;
		}
		catch
		{
			return OnDisk.CannotTell;
		}
	}

	private static bool IsAFixedDriveThatIsReady(string root)
	{
		DriveInfo driveInfo = new DriveInfo(root);
		if (driveInfo.DriveType == DriveType.Fixed)
		{
			return driveInfo.IsReady;
		}
		return false;
	}

	internal static IReadOnlyList<string>? OtherProductFolders(Action<string, Exception>? writeDown = null)
	{
		try
		{
			List<(string, string, string)> list = new List<(string, string, string)>();
			ReadUninstallEntries(Registry.CurrentUser, list);
			using (RegistryKey hive = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
			{
				ReadUninstallEntries(hive, list);
			}
			using (RegistryKey hive2 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
			{
				ReadUninstallEntries(hive2, list);
			}
			return OtherProductFolders(list);
		}
		catch (Exception ex)
		{
			Note(writeDown, "reading the other PlexusX installs", ex);
			return null;
		}
	}

	private static void ReadUninstallEntries(RegistryKey hive, List<(string, string?, string?)> into)
	{
		using RegistryKey registryKey = hive.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall");
		if (registryKey == null)
		{
			return;
		}
		string[] subKeyNames = registryKey.GetSubKeyNames();
		foreach (string text in subKeyNames)
		{
			using RegistryKey registryKey2 = registryKey.OpenSubKey(text);
			if (registryKey2 != null)
			{
				into.Add((text, registryKey2.GetValue("DisplayName") as string, registryKey2.GetValue("InstallLocation") as string));
			}
		}
	}

	internal static IReadOnlyList<string> OtherProductFolders(IEnumerable<(string KeyName, string? DisplayName, string? InstallLocation)> entries)
	{
		List<string> list = new List<string>();
		foreach (var (a, text, text2) in entries)
		{
			if (!string.Equals(a, "{8F3A1C2B-4D5E-4F6A-9B7C-1234567890AB}}_is1", StringComparison.OrdinalIgnoreCase) && text != null && text.TrimStart().StartsWith("PlexusX", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2.Trim());
			}
		}
		return list;
	}

	internal static string? InstalledExe()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{8F3A1C2B-4D5E-4F6A-9B7C-1234567890AB}}_is1");
			if (!(registryKey?.GetValue("InstallLocation") is string text) || string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			return Path.Combine(text.Trim(), "PlexusX.exe");
		}
		catch
		{
			return null;
		}
	}

	internal static Version? VersionOf(string path)
	{
		try
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
			if (versionInfo.FileMajorPart == 0 && versionInfo.FileMinorPart == 0 && versionInfo.FileBuildPart == 0)
			{
				return null;
			}
			return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, Math.Max(versionInfo.FileBuildPart, 0));
		}
		catch
		{
			return null;
		}
	}

	private static void Note(Action<string, Exception>? writeDown, string what, Exception ex)
	{
		try
		{
			writeDown?.Invoke(what, ex);
		}
		catch
		{
		}
	}

	private static bool IsOurExeName(string name)
	{
		return Array.FindIndex(OurExeNames, (string n) => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) >= 0;
	}

	private static string IconFileName(string iconPath)
	{
		string text = iconPath.Trim().Trim('"');
		int num = text.LastIndexOf(',');
		if (num > text.LastIndexOf('\\'))
		{
			text = text.Substring(0, num).Trim().Trim('"');
		}
		return Path.GetFileName(text);
	}

	private static bool IsLocalPath(string path)
	{
		if (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':')
		{
			if (path[2] != '\\')
			{
				return path[2] == '/';
			}
			return true;
		}
		return false;
	}

	private static string Full(string p)
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

	private static bool SamePath(string a, string b)
	{
		return string.Equals(Full(a), Full(b), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsInside(string path, string folder)
	{
		if (string.IsNullOrWhiteSpace(folder))
		{
			return false;
		}
		string value = Full(folder.Trim()).TrimEnd('\\', '/') + "\\";
		return Full(path).StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}

	internal static IPinFolder FolderAt(string directory, Action<string, Exception>? writeDown = null)
	{
		return new ShellLinkFolder(directory, writeDown);
	}
}
