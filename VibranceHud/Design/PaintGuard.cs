using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VibranceHud.Design;

internal static class PaintGuard
{
	private static readonly ConditionalWeakTable<object, object> _logged = new ConditionalWeakTable<object, object>();

	private static readonly object _mark = new object();

	private static readonly object _gate = new object();

	internal static int LoggedForTest { get; private set; }

	public static bool IsGdiFailure(Exception ex)
	{
		if (!(ex is OutOfMemoryException) && !(ex is ExternalException) && !(ex is ArgumentException))
		{
			return ex is InvalidOperationException;
		}
		return true;
	}

	public static void Try(object surfaceKey, Action paint)
	{
		try
		{
			paint();
		}
		catch (Exception ex) when (IsGdiFailure(ex))
		{
			LogOnce(surfaceKey, ex);
		}
	}

	public static void LogOnce(object surfaceKey, Exception ex)
	{
		lock (_gate)
		{
			if (!_logged.TryAdd(surfaceKey, _mark))
			{
				return;
			}
			LoggedForTest++;
		}
		CrashLog.Write(ex);
	}
}
