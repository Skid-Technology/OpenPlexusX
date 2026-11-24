namespace VibranceHud;

public interface IDisplayOverlay
{
	OverlayMode ActiveMode { get; }

	DxInitFailureKind LastFailure { get; }

	string LastFailureMessage { get; }

	int LastFailureCode => 0;

	bool EffectIsLive => true;

	bool ScreenEffectHeld => false;
}
