using System;
using System.IO;
using System.Text.Json;

namespace VibranceHud;

public sealed class SettingsStore
{
	private readonly string _directory;

	private static readonly object _gate = new object();

	public static string DefaultDirectory => AppFolders.Roaming;

	private string FilePath => Path.Combine(_directory, "settings.json");

	private string BackupPath => Path.Combine(_directory, "settings.bak");

	public SettingsStore(string directory)
	{
		_directory = directory;
	}

	public static SettingsStore Default()
	{
		return new SettingsStore(DefaultDirectory);
	}

	public AppSettings Load()
	{
		AppSettings? obj = TryRead(FilePath) ?? TryRead(BackupPath) ?? new AppSettings();
		LookCorrection.NormaliseOnLoad(obj);
		return obj;
	}

	private static AppSettings? TryRead(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			string text = File.ReadAllText(path);
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			return JsonSerializer.Deserialize<AppSettings>(text);
		}
		catch
		{
			return null;
		}
	}

	public bool Save(AppSettings settings)
	{
		string text = "";
		bool result;
		try
		{
			lock (_gate)
			{
				Directory.CreateDirectory(_directory);
				string contents = JsonSerializer.Serialize(settings, new JsonSerializerOptions
				{
					WriteIndented = true
				});
				text = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
				File.WriteAllText(text, contents);
				try
				{
					if (File.Exists(FilePath))
					{
						File.Replace(text, FilePath, BackupPath, ignoreMetadataErrors: true);
					}
					else
					{
						File.Move(text, FilePath);
					}
				}
				catch (Exception) when (File.Exists(text))
				{
					File.Copy(text, FilePath, overwrite: true);
				}
			}
			result = true;
		}
		catch (Exception)
		{
			result = false;
		}
		finally
		{
			if (text.Length > 0)
			{
				try
				{
					if (File.Exists(text))
					{
						File.Delete(text);
					}
				}
				catch
				{
				}
			}
		}
		return result;
	}
}
