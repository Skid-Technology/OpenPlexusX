using System;
using System.Threading;

namespace VibranceHud.Audio;

internal static class AudioLoopDiagnostic
{
	public static void RunAndPrint()
	{
		using WindowsAudioOutput windowsAudioOutput = new WindowsAudioOutput();
		float volume = windowsAudioOutput.Volume;
		try
		{
			float[] array = new float[3] { 1f, 0.5f, 0.25f };
			for (int i = 0; i < array.Length; i++)
			{
				float value = (windowsAudioOutput.Volume = array[i]);
				Thread.Sleep(500);
				Console.WriteLine($"Volume {value:P0} -> Peak {windowsAudioOutput.Peak:F3}");
			}
		}
		finally
		{
			windowsAudioOutput.Volume = volume;
		}
	}
}
