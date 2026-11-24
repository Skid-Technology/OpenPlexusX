using System;
using System.Collections.Generic;

namespace VibranceHud;

public sealed class AppStatus
{
	public Version? MinimumVersion { get; init; }

	public IReadOnlyList<string> BlockedFlavours { get; init; } = Array.Empty<string>();

	public string Message { get; init; } = "";

	public string MessageId { get; init; } = "";

	public string MessageUrl { get; init; } = "";
}
