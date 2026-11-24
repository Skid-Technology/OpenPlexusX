using System;
using System.Collections.Generic;

namespace VibranceHud;

public interface IVibranceController
{
	int CurrentLevel { get; }

	int DefaultLevel { get; }

	bool IsAvailable { get; }

	VibranceDriverState DriverState
	{
		get
		{
			if (!IsAvailable)
			{
				return VibranceDriverState.NoNvidiaCard;
			}
			return VibranceDriverState.Available;
		}
	}

	bool EveryDisplayIsAt(int level)
	{
		return CurrentLevel == level;
	}

	void SetLevel(int level);

	IReadOnlyList<KeyValuePair<string, int>> ReadEveryDisplayLevel()
	{
		return Array.Empty<KeyValuePair<string, int>>();
	}

	void SetLevelFor(string displayId, int level)
	{
	}
}
