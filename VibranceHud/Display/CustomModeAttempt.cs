namespace VibranceHud.Display;

public readonly record struct CustomModeAttempt(CustomModeOutcome Outcome, CustomModeVerdict Verdict, int Hz, bool Stretched);
