using System;
using System.Threading;

namespace VibranceHud.Music;

internal sealed class WindowsSessionSource : INowPlayingSource
{
	private readonly INowPlayingSource _windows;

	private readonly INowPlayingSource _spotify;

	public bool NeedsWorker => true;

	internal WindowsSessionSource(INowPlayingSource? windows = null, INowPlayingSource? spotify = null)
	{
		_windows = windows ?? new GsmtcSource();
		_spotify = spotify ?? new SpotifyWindowTitleSource();
	}

	public NowPlaying? Read()
	{
		return Read(CancellationToken.None);
	}

	public NowPlaying? Read(CancellationToken cancellation)
	{
		try
		{
			return _windows.Read(cancellation);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch
		{
			try
			{
				return _spotify.Read(cancellation);
			}
			catch
			{
				return null;
			}
		}
	}

	public bool TryCommand(MediaKey key)
	{
		try
		{
			return _windows.TryCommand(key);
		}
		catch
		{
			return false;
		}
	}
}
