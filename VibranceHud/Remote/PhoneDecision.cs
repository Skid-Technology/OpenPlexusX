namespace VibranceHud.Remote;

internal readonly record struct PhoneDecision(PhoneVerdict Verdict, string Field, int Value)
{
	public static PhoneDecision Of(PhoneVerdict verdict)
	{
		return new PhoneDecision(verdict, "", 0);
	}
}
