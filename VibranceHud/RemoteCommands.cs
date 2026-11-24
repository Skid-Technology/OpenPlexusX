using System;
using VibranceHud.Presets;

namespace VibranceHud;

public static class RemoteCommands
{
	private static readonly object Gate = new object();

	private static Action? _onShow;

	private static Action? _onQuit;

	private static int _dropped;

	public static Action? OnShow
	{
		get
		{
			lock (Gate)
			{
				return _onShow;
			}
		}
		set
		{
			lock (Gate)
			{
				_onShow = value;
			}
		}
	}

	public static Action? OnQuit
	{
		get
		{
			lock (Gate)
			{
				return _onQuit;
			}
		}
		set
		{
			lock (Gate)
			{
				_onQuit = value;
			}
		}
	}

	internal static int DroppedForTest
	{
		get
		{
			lock (Gate)
			{
				return _dropped;
			}
		}
	}

	public static void Deliver(PipeRequest request)
	{
		Deliver(request, LinkInbox.Offer);
	}

	internal static void Deliver(PipeRequest request, Action<string?> offerLink)
	{
		switch (request.Verb)
		{
		case PipeVerb.Show:
			Show();
			break;
		case PipeVerb.Quit:
			Quit();
			break;
		default:
			offerLink(request.Payload);
			break;
		}
	}

	public static void Show()
	{
		Raise(OnShow);
	}

	public static void Quit()
	{
		Raise(OnQuit);
	}

	private static void Raise(Action? handler)
	{
		if (handler == null)
		{
			lock (Gate)
			{
				_dropped++;
				return;
			}
		}
		try
		{
			handler();
		}
		catch
		{
		}
	}

	internal static void ResetForTest()
	{
		lock (Gate)
		{
			_onShow = null;
			_onQuit = null;
			_dropped = 0;
		}
	}
}
