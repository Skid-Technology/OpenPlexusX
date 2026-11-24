namespace VibranceHud;

public enum DxInitFailureKind
{
	None,
	NoCompatibleAdapter,
	DeviceCreationFailed,
	NoOutputs,
	DriverIssue,
	SdkIssue,
	OutOfMemory,
	CaptureInUse,
	Unknown
}
