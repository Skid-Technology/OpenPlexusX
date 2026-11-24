namespace VibranceHud.Support;

public static class RuntimeFacts
{
	private static OverlayMode _mode = OverlayMode.Mag;

	private static bool _effectIsLive;

	public static string Overlay => DescribeOverlay(_mode, _effectIsLive);

	public static bool MagInitialized
	{
		get
		{
			if (_mode == OverlayMode.Mag)
			{
				return _effectIsLive;
			}
			return false;
		}
	}

	public static bool ExclusiveFullscreenSeen { get; private set; }

	public static bool ScreenEffectHeld { get; internal set; }

	public static int Saturation { get; internal set; } = 100;

	public static bool DisplayIsCarryingTheLook { get; private set; }

	public static string? WhatTheDisplayCannotHold { get; private set; }

	public static GammaDelivery RampDelivery { get; private set; } = GammaDelivery.Full;

	public static string DescribeOverlay(OverlayMode mode, bool effectIsLive)
	{
		if (!effectIsLive)
		{
			return "None";
		}
		if (mode != OverlayMode.Dx)
		{
			return "Mag";
		}
		return "Dx";
	}

	public static void NoteOverlay(OverlayMode mode, bool effectIsLive)
	{
		_mode = mode;
		_effectIsLive = effectIsLive;
	}

	public static void NoteRampDelivery(GammaDelivery delivery)
	{
		RampDelivery = delivery;
	}

	public static void NoteDisplayOnlyLook(bool carrying, string? cannotHold)
	{
		DisplayIsCarryingTheLook = carrying;
		WhatTheDisplayCannotHold = (carrying ? cannotHold : null);
	}

	public static void NoteFullscreen(bool seenNow)
	{
		if (seenNow)
		{
			ExclusiveFullscreenSeen = true;
		}
	}

	internal static void ForgetTheDisplayCarryForTest()
	{
		RampDelivery = GammaDelivery.Full;
		DisplayIsCarryingTheLook = false;
		WhatTheDisplayCannotHold = null;
	}

	internal static void ResetForTest()
	{
		_mode = OverlayMode.Mag;
		_effectIsLive = false;
		ExclusiveFullscreenSeen = false;
		ScreenEffectHeld = false;
		Saturation = 100;
		RampDelivery = GammaDelivery.Full;
		DisplayIsCarryingTheLook = false;
		WhatTheDisplayCannotHold = null;
	}
}
