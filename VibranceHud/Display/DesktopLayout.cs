using System;
using System.Collections.Generic;

namespace VibranceHud.Display;

public static class DesktopLayout
{
	private sealed class NoDesktopLayout : IDesktopLayout
	{
		public IReadOnlyList<MonitorPlacement> Read()
		{
			return Array.Empty<MonitorPlacement>();
		}

		public bool Place(IReadOnlyList<MonitorPlacement> placements)
		{
			return false;
		}
	}

	internal sealed class RealDesktopLayout : IDesktopLayout
	{
		public IReadOnlyList<MonitorPlacement> Read()
		{
			return DisplayController.Placements();
		}

		public bool Place(IReadOnlyList<MonitorPlacement> placements)
		{
			return DisplayController.PlaceMonitors(placements);
		}
	}

	public static IDesktopLayout None { get; } = new NoDesktopLayout();

	public static IDesktopLayout Real()
	{
		return new RealDesktopLayout();
	}

	public static IReadOnlyList<MonitorPlacement> WhatMoved(IReadOnlyList<MonitorPlacement> before, IReadOnlyList<MonitorPlacement> now)
	{
		List<MonitorPlacement> list = new List<MonitorPlacement>();
		foreach (MonitorPlacement item in before)
		{
			foreach (MonitorPlacement item2 in now)
			{
				if (string.Equals(item.DeviceName, item2.DeviceName, StringComparison.OrdinalIgnoreCase))
				{
					if (item.X != item2.X || item.Y != item2.Y)
					{
						list.Add(item);
					}
					break;
				}
			}
		}
		return list;
	}
}
