using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using VibranceHud.Crosshair;

namespace VibranceHud.Fullscreen;

public static class ExclusiveFullscreenEvidence
{
	public const string FullscreenOptimisationsOff = "DISABLEDXMAXIMIZEDWINDOWEDMODE";

	private const string LayersKey = "Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers";

	public static bool MatrixCannotArrive(bool shellSaysD3DFullscreen, string? foregroundProcessName, IEnumerable<KeyValuePair<string, string>> appCompatLayers)
	{
		if (shellSaysD3DFullscreen)
		{
			return FullscreenOptimisationsAreOffFor(foregroundProcessName, appCompatLayers);
		}
		return false;
	}

	internal static bool FullscreenOptimisationsAreOffFor(string? foregroundProcessName, IEnumerable<KeyValuePair<string, string>> appCompatLayers)
	{
		string text = WithExe(foregroundProcessName);
		if (text == null || appCompatLayers == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, string> appCompatLayer in appCompatLayers)
		{
			if (!string.IsNullOrEmpty(appCompatLayer.Key) && !string.IsNullOrEmpty(appCompatLayer.Value))
			{
				string fileName;
				try
				{
					fileName = Path.GetFileName(appCompatLayer.Key);
				}
				catch
				{
					continue;
				}
				if (string.Equals(fileName, text, StringComparison.OrdinalIgnoreCase) && NamesTheToken(appCompatLayer.Value))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool NamesTheToken(string layers)
	{
		string[] array = layers.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			if (string.Equals(array[i], "DISABLEDXMAXIMIZEDWINDOWEDMODE", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static string? WithExe(string? name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			return null;
		}
		name = name.Trim();
		if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			return name + ".exe";
		}
		return name;
	}

	public static bool ForThisMachine()
	{
		try
		{
			if (!CrosshairService.IsExclusiveFullscreen())
			{
				return false;
			}
			string text = ForegroundWindowProcess.Name();
			if (text == null)
			{
				return false;
			}
			return FullscreenOptimisationsAreOffFor(text, ReadLayers());
		}
		catch
		{
			return false;
		}
	}

	private static IEnumerable<KeyValuePair<string, string>> ReadLayers()
	{
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		Collect(Registry.CurrentUser, list);
		Collect(Registry.LocalMachine, list);
		return list;
	}

	private static void Collect(RegistryKey hive, List<KeyValuePair<string, string>> into)
	{
		try
		{
			using RegistryKey registryKey = hive.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers", writable: false);
			if (registryKey == null)
			{
				return;
			}
			string[] valueNames = registryKey.GetValueNames();
			foreach (string text in valueNames)
			{
				if (!string.IsNullOrEmpty(text) && registryKey.GetValue(text) is string { Length: >0 } text2)
				{
					into.Add(new KeyValuePair<string, string>(text, text2));
				}
			}
		}
		catch
		{
		}
	}
}
