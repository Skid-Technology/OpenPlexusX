using System;

namespace VibranceHud.Audio;

public static class AudioLimiter
{
	public const float MinVolume = 0.05f;

	public const float DefaultAttack = 0.5f;

	public const float DefaultRelease = 0.3f;

	public static float NextVolume(float peak, float threshold, float currentVolume, float maxVolume, float attack = 0.5f, float release = 0.3f)
	{
		float num = ((peak > 0.0001f) ? MathF.Min(maxVolume, threshold / peak) : maxVolume);
		float num2 = ((num < currentVolume) ? attack : release);
		return Math.Clamp(currentVolume + (num - currentVolume) * num2, 0.05f, maxVolume);
	}
}
