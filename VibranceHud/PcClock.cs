using System;
using VibranceHud.Licensing;

namespace VibranceHud;

public static class PcClock
{
	public const string CheckYourDateAndTime = "Check your PC's date and time.";

	public static readonly TimeSpan Tolerance = TimeSpan.FromHours(24.0);

	public static bool LooksWrong(DateTime? signedUtc, DateTime? pcUtc)
	{
		if (signedUtc.HasValue)
		{
			DateTime valueOrDefault = signedUtc.GetValueOrDefault();
			if (pcUtc.HasValue)
			{
				return LooksWrong(pcUtc.GetValueOrDefault().ToUniversalTime() - valueOrDefault.ToUniversalTime());
			}
		}
		return false;
	}

	public static bool LooksWrong(TimeSpan? pcMinusSigned)
	{
		if (pcMinusSigned.HasValue)
		{
			return pcMinusSigned.GetValueOrDefault().Duration() > Tolerance;
		}
		return false;
	}

	public static bool LooksWrongNow()
	{
		return false;
	}

	public static string WaitingWords()
	{
		return WaitingWords("Your looks are on the way. They load when PlexusX reaches our server.");
	}

	public static string WaitingWords(string usual)
	{
		if (!LooksWrongNow())
		{
			return usual;
		}
		return "Check your PC's date and time.";
	}
}
