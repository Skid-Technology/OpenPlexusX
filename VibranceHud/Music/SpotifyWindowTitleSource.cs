using System.Diagnostics;

namespace VibranceHud.Music;

internal sealed class SpotifyWindowTitleSource : INowPlayingSource
{
	public string? SpotifyWindowTitle()
	{
		Process[] processesByName;
		try
		{
			processesByName = Process.GetProcessesByName("Spotify");
		}
		catch
		{
			return null;
		}
		string text = null;
		Process[] array = processesByName;
		foreach (Process process in array)
		{
			try
			{
				if (text == null && !string.IsNullOrWhiteSpace(process.MainWindowTitle))
				{
					text = process.MainWindowTitle;
				}
			}
			catch
			{
			}
			finally
			{
				process.Dispose();
			}
		}
		return text;
	}
}
