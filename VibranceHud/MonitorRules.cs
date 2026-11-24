using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud;

public static class MonitorRules
{
	public static MonitorRule? For(IEnumerable<MonitorRule> rules, string? gameId)
	{
		if (string.IsNullOrWhiteSpace(gameId))
		{
			return null;
		}
		MonitorRule monitorRule = rules?.FirstOrDefault((MonitorRule r) => string.Equals(r.GameId, gameId, StringComparison.OrdinalIgnoreCase));
		if (monitorRule == null || !monitorRule.IsSet)
		{
			return null;
		}
		return monitorRule;
	}

	public static List<MonitorRule> Set(IEnumerable<MonitorRule> rules, string gameId, int width, int height, string device = "")
	{
		List<MonitorRule> list = rules?.ToList() ?? new List<MonitorRule>();
		list.RemoveAll((MonitorRule r) => string.Equals(r.GameId, gameId, StringComparison.OrdinalIgnoreCase));
		if (width > 0 && height > 0)
		{
			list.Add(new MonitorRule
			{
				GameId = gameId,
				Width = width,
				Height = height,
				Device = (device ?? "")
			});
		}
		return list;
	}

	public static List<MonitorRule> Clear(IEnumerable<MonitorRule> rules, string gameId)
	{
		return Set(rules, gameId, 0, 0);
	}
}
