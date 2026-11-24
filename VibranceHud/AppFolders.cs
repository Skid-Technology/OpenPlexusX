using System;
using System.IO;

namespace VibranceHud;

public static class AppFolders
{
	public static string Roaming => For(Environment.SpecialFolder.ApplicationData, RootForTest ?? SecurityCandidate.StorageRoot, FlavourPolicy.Current.DataFolderName);

	public static string Local => For(Environment.SpecialFolder.LocalApplicationData, RootForTest ?? SecurityCandidate.StorageRoot, FlavourPolicy.Current.DataFolderName);

	public static string Pictures => For(Environment.SpecialFolder.MyPictures, RootForTest ?? SecurityCandidate.StorageRoot);

	public static string TaskbarPins => PinnedIn(RootForTest ?? SecurityCandidate.StorageRoot);

	internal static string? RootForTest { get; set; }

	internal static string PinnedIn(string? root)
	{
		return Path.Combine((root == null) ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) : Path.Combine(root, Environment.SpecialFolder.ApplicationData.ToString()), "Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar");
	}

	internal static string For(Environment.SpecialFolder folder, string? root)
	{
		return For(folder, root, "PlexusX");
	}

	internal static string For(Environment.SpecialFolder folder, string? root, string folderName)
	{
		return Path.Combine((root == null) ? Environment.GetFolderPath(folder) : Path.Combine(root, folder.ToString()), folderName);
	}
}
