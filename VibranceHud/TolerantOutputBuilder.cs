using System;
using System.Collections.Generic;

namespace VibranceHud;

public static class TolerantOutputBuilder
{
	public static List<int> Build(int outputCount, Action<int> build, Action<int, Exception>? onError = null)
	{
		if (build == null)
		{
			throw new ArgumentNullException("build");
		}
		List<int> list = new List<int>();
		for (int i = 0; i < outputCount; i++)
		{
			try
			{
				build(i);
				list.Add(i);
			}
			catch (Exception arg)
			{
				try
				{
					onError?.Invoke(i, arg);
				}
				catch
				{
				}
			}
		}
		return list;
	}
}
