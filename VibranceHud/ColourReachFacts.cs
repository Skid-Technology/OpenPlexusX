namespace VibranceHud;

public readonly record struct ColourReachFacts(bool ExclusiveFullscreen, bool OverlayLive, DxInitFailureKind DxFailure, bool DxTried, bool DisplayIsCarryingTheLook = false, string? WhatTheDisplayCannotHold = null, GammaDelivery RampOutcome = default(GammaDelivery), bool CompetitiveOn = false);
