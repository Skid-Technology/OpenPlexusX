using System;

namespace VibranceHud.Presets;

public static class LinkInbox
{
	private static readonly object Gate = new object();

	private static string? _waiting;

	private static Action<string>? _handler;

	public static Action<string>? Handler
	{
		get
		{
			lock (Gate)
			{
				return _handler;
			}
		}
		set
		{
			string waiting;
			lock (Gate)
			{
				_handler = value;
				waiting = _waiting;
				_waiting = null;
			}
			if (value != null && waiting != null)
			{
				try
				{
					value(waiting);
				}
				catch
				{
				}
			}
		}
	}

	internal static string? WaitingForTest
	{
		get
		{
			lock (Gate)
			{
				return _waiting;
			}
		}
	}

	public static void Offer(string? link)
	{
		if (string.IsNullOrWhiteSpace(link))
		{
			return;
		}
		Action<string> handler;
		lock (Gate)
		{
			handler = _handler;
			if (handler == null)
			{
				_waiting = link;
				return;
			}
		}
		try
		{
			handler(link);
		}
		catch
		{
		}
	}

	internal static void ResetForTest()
	{
		lock (Gate)
		{
			_waiting = null;
			_handler = null;
		}
	}
}
