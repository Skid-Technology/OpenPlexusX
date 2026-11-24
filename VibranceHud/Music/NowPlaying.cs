using System;
using System.Drawing;

namespace VibranceHud.Music;

internal sealed record NowPlaying(string Artist, string Song)
{
	internal string Title => Song;

	internal string? AppName { get; init; }

	internal bool IsPlaying { get; init; } = true;

	internal bool CanPrevious { get; init; } = true;

	internal bool CanNext { get; init; } = true;

	internal bool CanPlayPause { get; init; } = true;

	internal Bitmap? Art { get; init; }

	internal string Line
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Artist))
			{
				return Title + " – " + Artist;
			}
			return Title;
		}
	}

	internal static NowPlaying? FromSpotifyTitle(string? title)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			return null;
		}
		string text = title.Trim();
		if (text.StartsWith("Spotify", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		int num = text.IndexOf(" - ", StringComparison.Ordinal);
		if (num <= 0)
		{
			return null;
		}
		string text2 = text.Substring(0, num).Trim();
		string text3 = text.Substring(num + 3).Trim();
		if (text2.Length == 0 || text3.Length == 0)
		{
			return null;
		}
		return new NowPlaying(text2, text3);
	}
}
