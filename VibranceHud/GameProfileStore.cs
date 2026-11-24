using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace VibranceHud;

public static class GameProfileStore
{
	private enum ReadOutcome
	{
		Nothing,
		Loaded,
		Failed
	}

	private sealed class ProfilesDocument
	{
		public int Version { get; set; } = 1;

		public List<GameProfile> Profiles { get; set; } = new List<GameProfile>();
	}

	private const int CurrentSchemaVersion = 1;

	public static string StorePath
	{
		get
		{
			string local = AppFolders.Local;
			try
			{
				Directory.CreateDirectory(local);
			}
			catch (Exception)
			{
			}
			return Path.Combine(local, "profiles.json");
		}
	}

	public static IReadOnlyList<GameProfile> Load()
	{
		return Load(StorePath);
	}

	public static IReadOnlyList<GameProfile> Load(string path)
	{
		return Load(path, File.ReadAllText);
	}

	internal static IReadOnlyList<GameProfile> Load(string path, Func<string, string> readText)
	{
		TryRead(path, readText, out List<GameProfile> profiles);
		return profiles;
	}

	private static ReadOutcome TryRead(string path, Func<string, string> readText, out List<GameProfile> profiles)
	{
		profiles = new List<GameProfile>();
		if (!File.Exists(path))
		{
			return ReadOutcome.Nothing;
		}
		string text;
		try
		{
			text = readText(path);
		}
		catch (Exception)
		{
			return ReadOutcome.Failed;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return ReadOutcome.Nothing;
		}
		try
		{
			profiles = JsonSerializer.Deserialize<ProfilesDocument>(text)?.Profiles ?? new List<GameProfile>();
			return ReadOutcome.Loaded;
		}
		catch (Exception)
		{
			profiles = new List<GameProfile>();
			return ReadOutcome.Failed;
		}
	}

	public static string SerializeAll(IEnumerable<GameProfile> profiles)
	{
		return JsonSerializer.Serialize(new ProfilesDocument
		{
			Version = 1,
			Profiles = new List<GameProfile>(profiles)
		}, new JsonSerializerOptions
		{
			WriteIndented = true
		});
	}

	public static void Set(GameProfile profile)
	{
		TrySet(profile, StorePath, File.ReadAllText);
	}

	public static void Set(GameProfile profile, string path)
	{
		TrySet(profile, path, File.ReadAllText);
	}

	internal static bool TrySet(GameProfile profile, string path, Func<string, string> readText)
	{
		if (TryRead(path, readText, out List<GameProfile> profiles) == ReadOutcome.Failed)
		{
			return false;
		}
		int num = profiles.FindIndex((GameProfile p) => p.GameId == profile.GameId);
		profile.LastUpdated = DateTime.UtcNow;
		if (num >= 0)
		{
			profiles[num] = profile;
		}
		else
		{
			profiles.Add(profile);
		}
		return WriteAtomically(path, SerializeAll(profiles));
	}

	public static void Remove(string gameId)
	{
		TryRemove(gameId, StorePath, File.ReadAllText);
	}

	public static void Remove(string gameId, string path)
	{
		TryRemove(gameId, path, File.ReadAllText);
	}

	internal static bool TryRemove(string gameId, string path, Func<string, string> readText)
	{
		if (TryRead(path, readText, out List<GameProfile> profiles) == ReadOutcome.Failed)
		{
			return false;
		}
		profiles.RemoveAll((GameProfile p) => p.GameId == gameId);
		return WriteAtomically(path, SerializeAll(profiles));
	}

	private static bool WriteAtomically(string path, string json)
	{
		string text = "";
		try
		{
			string directoryName = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			File.WriteAllText(text, json);
			try
			{
				if (File.Exists(path))
				{
					File.Replace(text, path, null, ignoreMetadataErrors: true);
				}
				else
				{
					File.Move(text, path);
				}
			}
			catch (Exception) when (File.Exists(text))
			{
				File.Copy(text, path, overwrite: true);
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
		finally
		{
			try
			{
				if (text.Length > 0 && File.Exists(text))
				{
					File.Delete(text);
				}
			}
			catch
			{
			}
		}
	}

	public static GameProfile? Get(string gameId)
	{
		return Get(gameId, StorePath);
	}

	public static GameProfile? Get(string gameId, string path)
	{
		foreach (GameProfile item in Load(path))
		{
			if (item.GameId == gameId)
			{
				return item;
			}
		}
		return null;
	}
}
