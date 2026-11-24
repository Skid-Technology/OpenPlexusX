using System;
using A;
using VibranceHud.Licensing;

namespace VibranceHud;

internal sealed class DarkBoostFollower
{
	private int _generation = ServedNightVision.Generation;

	internal bool Offered { get; private set; } = SafeAvailable();

	internal bool Follow(VibranceEngine engine, bool wanted, Action unbindHotkey, Action bindHotkeyIfOffered)
	{
		int generation = ServedNightVision.Generation;
		bool flag = SafeAvailable();
		if (generation == _generation && flag == Offered)
		{
			return false;
		}
		_generation = generation;
		Offered = flag;
		engine.NightVision = flag & wanted;
		engine.NightVisionSourceChanged();
		unbindHotkey();
		if (flag)
		{
			bindHotkeyIfOffered();
		}
		return true;
	}

	internal void Settle()
	{
		_generation = ServedNightVision.Generation;
		Offered = SafeAvailable();
	}

	private static bool SafeAvailable()
	{
		try
		{
			return global::A.A.Available;
		}
		catch
		{
			return false;
		}
	}
}
