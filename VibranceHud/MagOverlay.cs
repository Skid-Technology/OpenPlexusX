using System;
using System.Runtime.InteropServices;
using VibranceHud.Zoom;

namespace VibranceHud;

public sealed class MagOverlay : ISaturationOverlay, IDisplayOverlay, IDisposable
{
	private struct MAGCOLOREFFECT
	{
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
		public float[] transform;
	}

	private static readonly float[] Identity = new float[25]
	{
		1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f,
		0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f,
		0f, 0f, 0f, 0f, 1f
	};

	private readonly bool _available;

	private bool _initialized;

	private readonly LastSentEffect _lastSent = new LastSentEffect();

	private readonly Func<float[], bool> _send = RealSend;

	private readonly Func<bool> _initialize = TakeTheRuntime;

	private readonly Func<bool> _uninitialize = GiveTheRuntime;

	internal const string NotStarted = "Windows would not start the Magnification screen effect";

	private bool _lastSendSucceeded = true;

	public OverlayMode ActiveMode => OverlayMode.Mag;

	public bool IsAvailable => _available;

	public DxInitFailureKind LastFailure => DxInitFailureKind.None;

	public string LastFailureMessage => "";

	public bool EffectIsLive => _available;

	public bool ScreenEffectHeld => _initialized;

	public string WhyNothingIsDrawn
	{
		get
		{
			if (!_available)
			{
				return "Windows would not start the Magnification screen effect";
			}
			return "";
		}
	}

	public bool LastApplySucceeded => _lastSendSucceeded;

	private static bool TakeTheRuntime()
	{
		return MagRuntime.Shared.TakeForColour();
	}

	private static bool GiveTheRuntime()
	{
		MagRuntime.Shared.Give();
		return true;
	}

	[DllImport("Magnification.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool MagSetFullscreenColorEffect(ref MAGCOLOREFFECT pEffect);

	public MagOverlay()
		: this(TakeTheRuntime, GiveTheRuntime, RealSend)
	{
	}

	private static bool TryStart(Func<bool> initialize)
	{
		try
		{
			return initialize();
		}
		catch (DllNotFoundException)
		{
			return false;
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
	}

	internal MagOverlay(Func<bool> initialize, Func<bool> uninitialize, Func<float[], bool> send)
	{
		_initialize = initialize;
		_uninitialize = uninitialize;
		_send = send;
		_available = TryStart(initialize);
		if (_available)
		{
			uninitialize();
		}
	}

	internal MagOverlay(Func<float[], bool> send, bool initialized)
	{
		_send = send;
		_initialize = () => true;
		_uninitialize = () => true;
		_available = initialized;
		_initialized = initialized;
	}

	public MagOverlay HeldForTheGoodbye()
	{
		if (_available && !_initialized)
		{
			_initialized = TryStart(_initialize);
		}
		return this;
	}

	public void Apply(float[] matrix)
	{
		if (!_available)
		{
			SayNothingIsDrawn();
			return;
		}
		if (IsIdentity(matrix))
		{
			Clear();
			return;
		}
		if (!_initialized)
		{
			_initialized = TryStart(_initialize);
			if (!_initialized)
			{
				_lastSendSucceeded = false;
				_lastSent.Forget();
				return;
			}
		}
		Send(matrix);
	}

	internal static bool IsIdentity(float[] matrix)
	{
		if (matrix == null || matrix.Length != Identity.Length)
		{
			return false;
		}
		for (int i = 0; i < Identity.Length; i++)
		{
			if (matrix[i] != Identity[i])
			{
				return false;
			}
		}
		return true;
	}

	private void SayNothingIsDrawn()
	{
		ColourTrace.Refused("SCREEN EFFECT", "Windows would not start the Magnification screen effect");
	}

	public void Clear()
	{
		if (!_available)
		{
			SayNothingIsDrawn();
			return;
		}
		if (!_initialized)
		{
			_lastSendSucceeded = true;
			return;
		}
		Send(Identity);
		if (_lastSendSucceeded)
		{
			HandBack();
		}
	}

	private void HandBack()
	{
		_uninitialize();
		_initialized = false;
		_lastSent.Forget();
	}

	public void Forget()
	{
		_lastSent.Forget();
	}

	private void Send(float[] transform)
	{
		if (_lastSent.AlreadySent(transform))
		{
			_lastSendSucceeded = true;
			return;
		}
		_lastSendSucceeded = _send(transform);
		if (_lastSendSucceeded)
		{
			_lastSent.Remember(transform);
		}
		else
		{
			_lastSent.Forget();
		}
	}

	private static bool RealSend(float[] transform)
	{
		MAGCOLOREFFECT pEffect = new MAGCOLOREFFECT
		{
			transform = transform
		};
		return MagSetFullscreenColorEffect(ref pEffect);
	}

	public void Dispose()
	{
		if (_initialized)
		{
			_lastSent.Forget();
			Send(Identity);
			HandBack();
		}
	}
}
