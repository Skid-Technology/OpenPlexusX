namespace VibranceHud;

internal static class DragTracker
{
	private static int _count;

	private static int _scrollBars;

	public static bool AnyDragging => _count > 0;

	public static bool AnyScrollBarDragging => _scrollBars > 0;

	public static void Begin()
	{
		_count++;
	}

	public static void End()
	{
		if (_count > 0)
		{
			_count--;
		}
	}

	public static void BeginScrollBar()
	{
		_scrollBars++;
	}

	public static void EndScrollBar()
	{
		if (_scrollBars > 0)
		{
			_scrollBars--;
		}
	}

	internal static void ResetForTest()
	{
		_count = 0;
		_scrollBars = 0;
	}
}
