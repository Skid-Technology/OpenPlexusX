namespace VibranceHud.Audio;

public interface IAudioOutput
{
	float Peak { get; }

	float Volume { get; set; }
}
