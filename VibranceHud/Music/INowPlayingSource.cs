using System.Threading;

namespace VibranceHud.Music;

internal interface INowPlayingSource
{
	bool NeedsWorker => false;

	NowPlaying? Read()
	{
		return NowPlaying.FromSpotifyTitle(SpotifyWindowTitle());
	}

	NowPlaying? Read(CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		return Read();
	}

	bool TryCommand(MediaKey key)
	{
		return false;
	}

	string? SpotifyWindowTitle()
	{
		return null;
	}
}
