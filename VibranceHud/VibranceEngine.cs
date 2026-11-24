using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using A;
using VibranceHud.DarkBoost;
using VibranceHud.Fullscreen;
using VibranceHud.Licensing;
using VibranceHud.Monitors;
using VibranceHud.Support;

namespace VibranceHud;

public sealed class VibranceEngine : IVibranceEngine
{
	public const int MaxVibrance = 350;

	public const int MaxSaturation = 300;

	public const int DriverVibranceCeiling = 100;

	public const int MinBrightness = 50;

	public const int MaxBrightness = 170;

	public const int MinGamma = 50;

	public const int MaxGamma = 150;

	public const int MinContrast = 50;

	public const int MaxContrast = 150;

	public const int MinTemperature = -100;

	public const int MaxTemperature = 100;

	public const float EyeCareWarmth = 0.5f;

	private readonly IVibranceController _controller;

	private readonly ISaturationOverlay _overlay;

	private readonly IGammaRamp _gammaRamp;

	private int _vibrance;

	private int _saturation = 100;

	private int _brightness = 100;

	private int _gamma = 100;

	private int _contrast = 100;

	private int _temperature;

	private bool _dragging;

	private bool _overlaySuspended;

	private bool _colourReachesScreen = true;

	private readonly LastSentEffect _lastOverlay = new LastSentEffect();

	private bool _driverDirty;

	private bool _gammaDirty;

	private bool _theDriverMayHaveBeenReset;

	private bool _matrixCannotArrive;

	private bool _competitiveHold;

	private bool _streamingMode;

	private ToneSettings _tone = ToneSettings.Neutral;

	private ColourVisionMode _colourVision;

	private ColourVisionMode _setAsideCorrection;

	private ChannelMix _mix = ChannelMix.Identity;

	private bool _nightVision;

	private bool _darkBoostRampOn;

	private bool _darkBoostRampRefused;

	private double _darkBoostStrength = 1.0;

	private DarkBoostTint _darkBoostTint;

	private int _darkBoostBlackLevel;

	private bool _darkBoostFade;

	private double _darkBoostFadeAmount;

	private double _darkBoostFadeFrom;

	private double _darkBoostFadeTo;

	private DateTime _darkBoostFadeStart;

	private System.Windows.Forms.Timer? _darkBoostFadeTimer;

	private int _eyeCareWarmth;

	private int _eyeCareDim;

	public const int MinHue = -180;

	public const int MaxHue = 180;

	public const int MinChannelGain = 50;

	public const int MaxChannelGain = 150;

	private int _hue;

	private int _redGain = 100;

	private int _greenGain = 100;

	private int _blueGain = 100;

	private ColourDepth _depth = ColourDepth.Neutral;

	public const int EyeCareTemperature = 50;

	public const int SoftwareNeutral = 50;

	private const int SoftwareSlope = 150;

	private DisplayOnlyLook.Knob _driverKnobFollows;

	private bool _stoodDown;

	private bool _previewNeutral;

	private bool _paidControlsUnlocked = true;

	private const int LiveDragIntervalMs = 45;

	private readonly Stopwatch _dragClock = new Stopwatch();

	private bool _dragSampleOwed;

	private System.Windows.Forms.Timer? _trailingWrite;

	private bool _screenEffectHeld;

	public MonitorSelection? MonitorSelection { get; set; }

	public ColourWriteMeter Writes { get; } = new ColourWriteMeter();

	public bool VibranceChosenByUser { get; private set; }

	public int DriverBaseline { get; set; } = -1;

	public bool HasWrittenTheDriver { get; private set; }

	public Action? BeforeFirstDriverWrite { get; set; }

	public int? DriverLevelWeLastWrote { get; set; }

	public Action<int>? AfterDriverWrite { get; set; }

	public Action<int>? WhenTheDriverIsTakenBack { get; set; }

	public bool IsDragging => _dragging;

	public bool HandedBack { get; private set; }

	public bool ColourReachesScreen
	{
		get
		{
			return _colourReachesScreen;
		}
		set
		{
			if (_colourReachesScreen != value)
			{
				bool carryingTheLookOnTheDisplay = CarryingTheLookOnTheDisplay;
				_colourReachesScreen = value;
				_matrixCannotArrive = !value && AskWhetherTheMatrixIsBlocked();
				if (CarryingTheLookOnTheDisplay != carryingTheLookOnTheDisplay)
				{
					ApplyDriverVibrance();
					ApplyGammaRamp();
				}
				if (!_overlaySuspended)
				{
					ApplyOverlay();
				}
			}
		}
	}

	public Func<bool>? MatrixCannotArriveProbe { get; set; } = ExclusiveFullscreenEvidence.ForThisMachine;

	public bool CompetitiveHold
	{
		get
		{
			return _competitiveHold;
		}
		set
		{
			if (_competitiveHold != value)
			{
				bool carryingTheLookOnTheDisplay = CarryingTheLookOnTheDisplay;
				_competitiveHold = value;
				if (CarryingTheLookOnTheDisplay != carryingTheLookOnTheDisplay)
				{
					ApplyDriverVibrance();
					ApplyGammaRamp();
				}
				if (!_overlaySuspended)
				{
					ApplyOverlay();
				}
			}
		}
	}

	private bool CompetitiveCarryAllowed
	{
		get
		{
			if (_competitiveHold && _controller.IsAvailable)
			{
				return !_streamingMode;
			}
			return false;
		}
	}

	public bool CarryingTheLookOnTheDisplay
	{
		get
		{
			if (_paidControlsUnlocked)
			{
				if (_colourReachesScreen || !_matrixCannotArrive)
				{
					return CompetitiveCarryAllowed;
				}
				return true;
			}
			return false;
		}
	}

	private bool ThePickerPutsBrightnessOnTheRamp
	{
		get
		{
			MonitorSelection monitorSelection = MonitorSelection;
			if (monitorSelection != null)
			{
				return !monitorSelection.AllowsMatrix;
			}
			return false;
		}
	}

	public string? WhatTheDisplayCannotHold
	{
		get
		{
			if (!CarryingTheLookOnTheDisplay)
			{
				return null;
			}
			return DisplayOnlyLook.WhatTheDisplayCannotHold(_mix, _colourVision, _hue);
		}
	}

	public int OverlayWritesRefused { get; private set; }

	public bool ScreenEffectIsLive => (_overlay as IDisplayOverlay)?.EffectIsLive ?? true;

	public int Vibrance
	{
		get
		{
			return _vibrance;
		}
		set
		{
			VibranceChosenByUser = true;
			_vibrance = Math.Clamp(value, 0, 350);
			if (_dragging)
			{
				_driverDirty = true;
			}
			else
			{
				ApplyDriverVibrance(theUserJustAskedForIt: true);
			}
			ScheduleOverlayApply();
		}
	}

	public int Saturation
	{
		get
		{
			return _saturation;
		}
		set
		{
			int saturation = _saturation;
			_saturation = Math.Clamp(value, 0, 300);
			RuntimeFacts.Saturation = _saturation;
			ColourTrace.Value("SATURATION", saturation, _saturation);
			AlsoCarryOnTheDisplay(driver: true, gamma: false);
			ScheduleOverlayApply();
		}
	}

	public int DefaultLevel => _controller.DefaultLevel;

	public bool DriverAvailable => _controller.IsAvailable;

	public VibranceDriverState DriverState => _controller.DriverState;

	public bool StreamingMode
	{
		get
		{
			return _streamingMode;
		}
		set
		{
			if (_streamingMode != value)
			{
				bool carryingTheLookOnTheDisplay = CarryingTheLookOnTheDisplay;
				_streamingMode = value;
				ApplyDriverVibrance();
				if (CarryingTheLookOnTheDisplay != carryingTheLookOnTheDisplay)
				{
					ApplyGammaRamp();
				}
				ScheduleOverlayApply();
			}
		}
	}

	public int Brightness
	{
		get
		{
			return _brightness;
		}
		set
		{
			_brightness = Math.Clamp(value, 50, 170);
			AlsoCarryOnTheDisplay(driver: false, gamma: true);
			ScheduleOverlayApply();
		}
	}

	public int Gamma
	{
		get
		{
			return _gamma;
		}
		set
		{
			_gamma = Math.Clamp(value, 50, 150);
			if (_dragging)
			{
				_gammaDirty = true;
				ScheduleOverlayApply();
			}
			else
			{
				ApplyGammaRamp();
			}
		}
	}

	public ColourVisionMode ColourVision
	{
		get
		{
			return _colourVision;
		}
		set
		{
			if (_colourVision != value)
			{
				_colourVision = value;
				ApplyOverlay();
			}
		}
	}

	public ColourVisionMode SetAsideCorrection
	{
		get
		{
			return _setAsideCorrection;
		}
		set
		{
			if (_setAsideCorrection != value)
			{
				_setAsideCorrection = value;
				if (_colourVision == ColourVisionMode.HoloBlack)
				{
					ApplyOverlay();
				}
			}
		}
	}

	public ToneSettings Tone
	{
		get
		{
			return _tone with
			{
				Gamma = _gamma
			};
		}
		set
		{
			ToneSettings normalized = value.Normalized;
			if (!(_tone == normalized) || _gamma != normalized.Gamma)
			{
				_tone = normalized;
				_gamma = Math.Clamp(normalized.Gamma, 50, 150);
				if (_dragging)
				{
					_gammaDirty = true;
					ScheduleOverlayApply();
				}
				else
				{
					ApplyGammaRamp();
				}
			}
		}
	}

	public ChannelMix Mix
	{
		get
		{
			return _mix;
		}
		set
		{
			ChannelMix channelMix = value ?? ChannelMix.Identity;
			if (!_mix.Near(channelMix))
			{
				_mix = channelMix;
				ScheduleOverlayApply();
			}
		}
	}

	public bool NightVision
	{
		get
		{
			return _nightVision;
		}
		set
		{
			if (_nightVision != value)
			{
				_nightVision = value;
				_darkBoostRampRefused = false;
				StartDarkBoostFade(value ? 1 : 0);
				ScheduleOverlayApply();
				RampInStepWithDarkBoost();
			}
		}
	}

	internal double DarkBoostStrength
	{
		get
		{
			return _darkBoostStrength;
		}
		set
		{
			double num = (double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 1.0);
			if (num != _darkBoostStrength)
			{
				_darkBoostStrength = num;
				if (DarkBoostDrawing)
				{
					ScheduleOverlayApply();
					RampInStepWithDarkBoost();
				}
			}
		}
	}

	internal DarkBoostTint DarkBoostTint => _darkBoostTint;

	internal int DarkBoostBlackLevel => _darkBoostBlackLevel;

	internal bool DarkBoostFadeOn => _darkBoostFade;

	internal Func<DateTime> DarkBoostClockForTest { get; set; } = () => DateTime.UtcNow;

	internal bool DarkBoostFadeByHandForTest { get; set; }

	internal double DarkBoostFadeAmountForTest => _darkBoostFadeAmount;

	private bool DarkBoostDrawing
	{
		get
		{
			if (!_nightVision)
			{
				return _darkBoostFadeAmount > 0.0;
			}
			return true;
		}
	}

	private double EffectiveDarkBoostStrength => _darkBoostStrength * _darkBoostFadeAmount;

	public int Hue
	{
		get
		{
			return _hue;
		}
		set
		{
			_hue = Math.Clamp(value, -180, 180);
			ScheduleOverlayApply();
		}
	}

	public ColourDepth Depth
	{
		get
		{
			return _depth;
		}
		set
		{
			_depth = value.Clamped();
			ScheduleOverlayApply();
		}
	}

	public int RedGain
	{
		get
		{
			return _redGain;
		}
		set
		{
			_redGain = Math.Clamp(value, 50, 150);
			GainMoved();
		}
	}

	public int GreenGain
	{
		get
		{
			return _greenGain;
		}
		set
		{
			_greenGain = Math.Clamp(value, 50, 150);
			GainMoved();
		}
	}

	public int BlueGain
	{
		get
		{
			return _blueGain;
		}
		set
		{
			_blueGain = Math.Clamp(value, 50, 150);
			GainMoved();
		}
	}

	public int EyeCareWarmthPercent
	{
		get
		{
			return _eyeCareWarmth;
		}
		set
		{
			_eyeCareWarmth = Math.Clamp(value, 0, 100);
			ScheduleOverlayApply();
		}
	}

	public int EyeCareDim
	{
		get
		{
			return _eyeCareDim;
		}
		set
		{
			_eyeCareDim = Math.Clamp(value, 0, 80);
			ScheduleOverlayApply();
		}
	}

	public bool EyeCareActive
	{
		get
		{
			if (_eyeCareWarmth <= 0)
			{
				return _eyeCareDim > 0;
			}
			return true;
		}
	}

	public int Contrast
	{
		get
		{
			return _contrast;
		}
		set
		{
			_contrast = Math.Clamp(value, 50, 150);
			AlsoCarryOnTheDisplay(driver: false, gamma: true);
			ScheduleOverlayApply();
		}
	}

	public int Temperature
	{
		get
		{
			return _temperature;
		}
		set
		{
			_temperature = Math.Clamp(value, -100, 100);
			AlsoCarryOnTheDisplay(driver: false, gamma: true);
			ScheduleOverlayApply();
		}
	}

	public bool EyeCare
	{
		get
		{
			return _temperature >= 50;
		}
		set
		{
			Temperature = (value ? 50 : 0);
		}
	}

	public DisplayOnlyLook.Knob DriverKnobFollows
	{
		get
		{
			return _driverKnobFollows;
		}
		set
		{
			if (_driverKnobFollows != value)
			{
				_driverKnobFollows = value;
				AlsoCarryOnTheDisplay(driver: true, gamma: false);
			}
		}
	}

	private bool TheDisplaysChannelsAreNeutral
	{
		get
		{
			if (DisplayOnlyLook.IsNeutral(_brightness, _contrast, _temperature))
			{
				return ColourBalance.GainsNeutral(_redGain, _greenGain, _blueGain);
			}
			return false;
		}
	}

	public bool IsPreviewingNeutral => _previewNeutral;

	public bool PaidControlsUnlocked
	{
		get
		{
			return _paidControlsUnlocked;
		}
		set
		{
			if (_paidControlsUnlocked != value)
			{
				_paidControlsUnlocked = value;
				ApplyAll();
				ApplyGammaRamp();
			}
		}
	}

	public bool DisplayStateIsNeutral
	{
		get
		{
			if (CurrentMatrix() == null && CurrentRamp() == null)
			{
				if (_controller.IsAvailable && VibranceChosenByUser && !_streamingMode && !_previewNeutral)
				{
					return Math.Min(_vibrance, 100) == _controller.DefaultLevel;
				}
				return true;
			}
			return false;
		}
	}

	public ReassertMeter Meter { get; internal set; } = new ReassertMeter();

	internal int StaleDragMs { get; set; } = 2000;

	private bool ADragIsActuallyRunning
	{
		get
		{
			if (_dragging && _dragClock.IsRunning)
			{
				return _dragClock.ElapsedMilliseconds < StaleDragMs;
			}
			return false;
		}
	}

	public void HandBack()
	{
		HandedBack = true;
		_screenEffectHeld = false;
		RuntimeFacts.ScreenEffectHeld = false;
	}

	public VibranceEngine(IVibranceController controller, ISaturationOverlay overlay, IGammaRamp gammaRamp)
	{
		_controller = controller;
		_overlay = overlay;
		_gammaRamp = gammaRamp;
		_vibrance = Math.Clamp(controller.CurrentLevel, 0, 350);
	}

	public void BeginDrag()
	{
		_dragging = true;
		_dragClock.Reset();
	}

	public void EndDrag()
	{
		_dragging = false;
		_dragClock.Stop();
		_trailingWrite?.Stop();
		_dragSampleOwed = false;
		if (_driverDirty)
		{
			_driverDirty = false;
			ApplyDriverVibrance(theUserJustAskedForIt: true);
		}
		if (_gammaDirty)
		{
			_gammaDirty = false;
			ApplyGammaRamp();
		}
		ApplyOverlay();
	}

	public void SuspendOverlay()
	{
		if (!_overlaySuspended)
		{
			_overlaySuspended = true;
			_overlay.Clear();
			NoteTheScreenEffect(null);
			_lastOverlay.Remember(null);
		}
	}

	public void ForgetAppliedState()
	{
		_lastOverlay.Forget();
		_overlay.Forget();
		_theDriverMayHaveBeenReset = true;
	}

	public void ResumeOverlay()
	{
		if (_overlaySuspended)
		{
			_overlaySuspended = false;
			ApplyOverlay();
		}
	}

	public static bool CompetitiveModeCanWork(IVibranceEngine? engine)
	{
		if (engine != null && engine.DriverAvailable)
		{
			return engine.PaidControlsUnlocked;
		}
		return false;
	}

	internal bool CompetitiveMonitorCanCarry(string? deviceName)
	{
		if (!_controller.IsAvailable || string.IsNullOrWhiteSpace(deviceName))
		{
			return false;
		}
		try
		{
			return _controller.ReadEveryDisplayLevel().Any((KeyValuePair<string, int> display) => string.Equals(display.Key, deviceName, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	private bool AskWhetherTheMatrixIsBlocked()
	{
		Func<bool> matrixCannotArriveProbe = MatrixCannotArriveProbe;
		if (matrixCannotArriveProbe == null)
		{
			return false;
		}
		try
		{
			return matrixCannotArriveProbe();
		}
		catch
		{
			return false;
		}
	}

	private void AlsoCarryOnTheDisplay(bool driver, bool gamma)
	{
		if (!CarryingTheLookOnTheDisplay)
		{
			if (!ThePickerPutsBrightnessOnTheRamp)
			{
				return;
			}
			driver = false;
		}
		if (_dragging)
		{
			if (driver)
			{
				_driverDirty = true;
			}
			if (gamma)
			{
				_gammaDirty = true;
			}
		}
		else
		{
			if (driver)
			{
				ApplyDriverVibrance();
			}
			if (gamma)
			{
				ApplyGammaRamp();
			}
		}
	}

	public void SeedVibrance(int value)
	{
		_vibrance = Math.Clamp(value, 0, 350);
		ScheduleOverlayApply();
	}

	internal void NightVisionSourceChanged()
	{
		_darkBoostRampRefused = false;
		ScheduleOverlayApply();
		RampInStepWithDarkBoost();
	}

	internal void SetDarkBoostLook(DarkBoostTint tint, int strength, int blackLevel, bool fade)
	{
		int num = Math.Clamp(blackLevel, 0, 100);
		bool num2 = tint != _darkBoostTint || num != _darkBoostBlackLevel;
		_darkBoostTint = tint;
		_darkBoostBlackLevel = num;
		_darkBoostFade = fade;
		DarkBoostStrength = (double)Math.Clamp(strength, 0, 100) / 100.0;
		if (num2 && DarkBoostDrawing)
		{
			ScheduleOverlayApply();
		}
	}

	private void StartDarkBoostFade(double to)
	{
		bool flag = SynchronizationContext.Current is WindowsFormsSynchronizationContext;
		if (!_darkBoostFade || _darkBoostFadeAmount == to || HandedBack || (!flag && !DarkBoostFadeByHandForTest))
		{
			_darkBoostFadeTimer?.Stop();
			_darkBoostFadeAmount = to;
			return;
		}
		_darkBoostFadeFrom = _darkBoostFadeAmount;
		_darkBoostFadeTo = to;
		_darkBoostFadeStart = DarkBoostClockForTest();
		if (!flag)
		{
			return;
		}
		if (_darkBoostFadeTimer == null)
		{
			_darkBoostFadeTimer = new System.Windows.Forms.Timer
			{
				Interval = 16
			};
			_darkBoostFadeTimer.Tick += (object? _, EventArgs _) =>
			{
				DarkBoostFadeTick();
			};
		}
		_darkBoostFadeTimer.Start();
	}

	internal void DarkBoostFadeTickForTest()
	{
		DarkBoostFadeTick();
	}

	private void DarkBoostFadeTick()
	{
		TimeSpan timeSpan = DarkBoostClockForTest() - _darkBoostFadeStart;
		_darkBoostFadeAmount = DarkBoostFade.Amount(_darkBoostFadeFrom, _darkBoostFadeTo, timeSpan, fade: true);
		if (timeSpan >= DarkBoostFade.Duration || HandedBack)
		{
			_darkBoostFadeAmount = _darkBoostFadeTo;
			_darkBoostFadeTimer?.Stop();
		}
		ScheduleOverlayApply();
		RampInStepWithDarkBoost();
	}

	private void RampInStepWithDarkBoost()
	{
		if (_darkBoostRampOn || DarkBoostRampWanted() != null)
		{
			ApplyGammaRamp();
		}
	}

	private static DarkBoostTable? ServedDarkBoost()
	{
		try
		{
			return ServedNightVision.DarkBoost;
		}
		catch
		{
			return null;
		}
	}

	private static bool DarkBoostOffered()
	{
		try
		{
			return global::A.A.Available;
		}
		catch
		{
			return false;
		}
	}

	private ushort[]? DarkBoostRampWanted()
	{
		if (!DarkBoostDrawing || _darkBoostRampRefused)
		{
			return null;
		}
		DarkBoostTable darkBoostTable = ServedDarkBoost();
		if (darkBoostTable == null || darkBoostTable.RampIsStraight || !DarkBoostOffered())
		{
			return null;
		}
		ushort[] array = DarkBoostPicture.Ramp(darkBoostTable.Ramp, EffectiveDarkBoostStrength);
		if (!DarkBoostPicture.IsStraight(array))
		{
			return array;
		}
		return null;
	}

	private float[]? NightVisionBasis()
	{
		DarkBoostTable darkBoostTable = ServedDarkBoost();
		if (darkBoostTable != null && !_darkBoostRampRefused)
		{
			return darkBoostTable.Matrix;
		}
		return ServedNightVision.Matrix;
	}

	internal (float[]? Basis, ushort[]? Ramp) DarkBoostPreviewBasis()
	{
		if (!DarkBoostOffered())
		{
			return (Basis: null, Ramp: null);
		}
		DarkBoostTable darkBoostTable = ServedDarkBoost();
		ushort[] item = ((darkBoostTable != null && !darkBoostTable.RampIsStraight && !_darkBoostRampRefused) ? darkBoostTable.Ramp : null);
		return (Basis: NightVisionBasis(), Ramp: item);
	}

	private void GainMoved()
	{
		AlsoCarryOnTheDisplay(driver: false, gamma: true);
		ScheduleOverlayApply();
	}

	public CaptureProbe RunCaptureProbe()
	{
		SuspendOverlay();
		try
		{
			return CaptureDiagnostic.Probe(_overlay);
		}
		finally
		{
			ResumeOverlay();
		}
	}

	public void Reset()
	{
		_brightness = 100;
		_temperature = 0;
		_contrast = 100;
		_gamma = 100;
		_tone = ToneSettings.Neutral;
		_mix = ChannelMix.Identity;
		_hue = 0;
		_redGain = (_greenGain = (_blueGain = 100));
		CountedGammaWrite(_gammaRamp.Reset);
		_darkBoostRampOn = false;
		_saturation = 100;
		bool vibranceChosenByUser = VibranceChosenByUser;
		VibranceChosenByUser = false;
		SeedVibrance(DefaultLevel);
		if (vibranceChosenByUser)
		{
			StandTheDriverDownToTheBaseline();
		}
	}

	private void StandTheDriverDownToTheBaseline()
	{
		if (SomeoneElseMovedTheDriver())
		{
			LetTheDriverGo();
			return;
		}
		if (_controller is SelectedVibranceController selectedVibranceController)
		{
			selectedVibranceController.RestoreOwned();
			DriverLevelWeLastWrote = null;
			return;
		}
		int? num = DriverVibranceIntent.For(userChoseAVibrance: true, _vibrance, DriverBaseline, effectSuspended: true);
		if (num.HasValue && !_controller.EveryDisplayIsAt(num.Value))
		{
			WriteTheDriver(num.Value);
		}
	}

	private void WriteTheDriver(int level)
	{
		if (HandedBack)
		{
			return;
		}
		if (!HasWrittenTheDriver)
		{
			HasWrittenTheDriver = true;
			try
			{
				BeforeFirstDriverWrite?.Invoke();
			}
			catch
			{
			}
		}
		Writes.CountDriver();
		_controller.SetLevel(level);
		DriverLevelWeLastWrote = level;
		try
		{
			AfterDriverWrite?.Invoke(level);
		}
		catch
		{
		}
	}

	private bool SomeoneElseMovedTheDriver()
	{
		int? driverLevelWeLastWrote = DriverLevelWeLastWrote;
		if (driverLevelWeLastWrote.HasValue)
		{
			int valueOrDefault = driverLevelWeLastWrote.GetValueOrDefault();
			if (_controller.IsAvailable)
			{
				if (!OutsideDriverChange.HoldsALevelOtherThan(_controller, valueOrDefault))
				{
					return false;
				}
				if (_theDriverMayHaveBeenReset && _controller.EveryDisplayIsAt(_controller.DefaultLevel))
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}

	private void LetTheDriverGo()
	{
		int was = DriverLevelWeLastWrote ?? (-1);
		VibranceChosenByUser = false;
		DriverLevelWeLastWrote = null;
		_stoodDown = false;
		HasWrittenTheDriver = false;
		int num = DriverBaseline;
		try
		{
			num = _controller.CurrentLevel;
		}
		catch
		{
		}
		DriverBaseline = num;
		if (_controller is SelectedVibranceController selectedVibranceController)
		{
			selectedVibranceController.RestoreOwned();
		}
		ColourTrace.Value("DRIVER TAKEN BACK", was, num);
		try
		{
			WhenTheDriverIsTakenBack?.Invoke(num);
		}
		catch
		{
		}
	}

	public static float SoftwareVibranceFactor(int vibrance, bool driverAvailable, bool streaming = false)
	{
		if (driverAvailable && !streaming)
		{
			if (vibrance > 100)
			{
				return (float)vibrance / 100f;
			}
			return 1f;
		}
		if (vibrance <= 50)
		{
			return (float)vibrance / 50f;
		}
		return 1f + (float)(vibrance - 50) / 150f;
	}

	public static int MigrateSoftwareVibrance(int savedValue)
	{
		float num = (float)savedValue / 100f;
		return Math.Clamp((num <= 1f) ? ((int)Math.Round(num * 50f)) : (50 + (int)Math.Round((num - 1f) * 150f)), 0, 350);
	}

	private void ApplyDriverVibrance(bool theUserJustAskedForIt = false)
	{
		if (!theUserJustAskedForIt && VibranceChosenByUser && SomeoneElseMovedTheDriver())
		{
			LetTheDriverGo();
			return;
		}
		bool flag = _streamingMode || _previewNeutral || !_paidControlsUnlocked;
		if (flag && _stoodDown)
		{
			return;
		}
		if (!flag)
		{
			_stoodDown = false;
		}
		if (flag && _controller is SelectedVibranceController selectedVibranceController)
		{
			selectedVibranceController.RestoreOwned();
			DriverLevelWeLastWrote = null;
			_stoodDown = true;
			return;
		}
		int? num = DriverVibranceIntent.For(VibranceChosenByUser, DriverVibranceNow(), DriverBaseline, flag);
		if (num.HasValue)
		{
			if (flag)
			{
				_stoodDown = true;
			}
			if (!_controller.EveryDisplayIsAt(num.Value))
			{
				WriteTheDriver(num.Value);
			}
		}
	}

	private int DriverVibranceNow()
	{
		if (!CarryingTheLookOnTheDisplay)
		{
			return e.A(_vibrance, true, false).Driver;
		}
		return DisplayOnlyLook.DriverLevelFor(e.A(_vibrance, true, false).Driver, _saturation, _driverKnobFollows);
	}

	private void ApplyGammaRamp()
	{
		if (_previewNeutral || !_paidControlsUnlocked)
		{
			CountedGammaWrite(_gammaRamp.Reset);
			_darkBoostRampOn = false;
			return;
		}
		ToneSettings t = _tone with
		{
			Gamma = _gamma
		};
		ushort[] array;
		if (CarryingTheLookOnTheDisplay && !TheDisplaysChannelsAreNeutral)
		{
			array = DisplayOnlyLook.Compose(ToneCurve.Build(t), _brightness, _contrast, _temperature, _redGain, _greenGain, _blueGain);
		}
		else if (!ThePickerPutsBrightnessOnTheRamp || TheDisplaysChannelsAreNeutral)
		{
			array = (t.IsNeutral ? null : ToneCurve.Build(t));
		}
		else
		{
			array = DisplayOnlyLook.Compose(ToneCurve.Build(t), _brightness, _contrast, _temperature, _redGain, _greenGain, _blueGain);
		}
		ushort[] array2 = DarkBoostRampWanted();
		if (array2 != null)
		{
			ushort[] array3 = DarkBoostPicture.ComposeAfter(array ?? DarkBoostPicture.Straight(), array2);
			ushort[] ramp = (DarkBoostPicture.Safe(array3) ? array3 : array2);
			string refusal = null;
			CountedGammaWrite(() =>
			{
				refusal = _gammaRamp.ApplyAllOrNothing(ramp);
			});
			if (HandedBack)
			{
				_darkBoostRampOn = false;
				return;
			}
			if (refusal == null)
			{
				_darkBoostRampOn = true;
				return;
			}
			_darkBoostRampRefused = true;
			ColourTrace.Refused("DARK BOOST", refusal + "; the old key on every display");
			ScheduleOverlayApply();
		}
		_darkBoostRampOn = false;
		if (array == null)
		{
			CountedGammaWrite(_gammaRamp.Reset);
			return;
		}
		ushort[] own = array;
		CountedGammaWrite(() =>
		{
			_gammaRamp.Apply(own);
		});
	}

	private void CountedGammaWrite(Action write)
	{
		if (HandedBack)
		{
			return;
		}
		int deviceWrites = _gammaRamp.DeviceWrites;
		try
		{
			write();
		}
		finally
		{
			Writes.AddGamma(_gammaRamp.DeviceWrites - deviceWrites);
			RuntimeFacts.NoteRampDelivery(_gammaRamp.LastDelivery);
		}
	}

	public void PreviewNeutral(bool on)
	{
		if (_previewNeutral != on)
		{
			_previewNeutral = on;
			ApplyDriverVibrance();
			ApplyOverlay();
			ApplyGammaRamp();
		}
	}

	private void ApplyAll()
	{
		ApplyDriverVibrance();
		ScheduleOverlayApply();
	}

	public void SelectionMoved()
	{
		ForgetAppliedState();
		if (ThePickerPutsBrightnessOnTheRamp && !_overlaySuspended)
		{
			ApplyOverlay();
		}
		ReassertDisplayState();
	}

	public void ReassertDisplayState()
	{
		if (!ADragIsActuallyRunning)
		{
			long start = Meter.Now();
			long start2 = Meter.Now();
			ApplyDriverVibrance();
			_theDriverMayHaveBeenReset = false;
			Meter.Record("driver", Meter.MsSince(start2));
			start2 = Meter.Now();
			ApplyGammaRamp();
			Meter.Record("gamma", Meter.MsSince(start2));
			if (!_overlaySuspended)
			{
				start2 = Meter.Now();
				ApplyOverlay();
				Meter.Record("overlay", Meter.MsSince(start2));
			}
			Meter.Completed(Meter.MsSince(start));
			ColourTrace.Meter(Meter);
		}
	}

	private void WriteTheDragSample()
	{
		_dragSampleOwed = false;
		if (_driverDirty)
		{
			_driverDirty = false;
			ApplyDriverVibrance(theUserJustAskedForIt: true);
		}
		if (_gammaDirty)
		{
			_gammaDirty = false;
			ApplyGammaRamp();
		}
		ApplyOverlay();
	}

	private void ArmTheTrailingWrite(int dueMs)
	{
		if (!(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
		{
			return;
		}
		if (_trailingWrite == null)
		{
			_trailingWrite = new System.Windows.Forms.Timer();
			_trailingWrite.Tick += (object? _, EventArgs _) =>
			{
				_trailingWrite.Stop();
				if (_dragging && !_overlaySuspended && _dragSampleOwed)
				{
					_dragClock.Restart();
					WriteTheDragSample();
				}
			};
		}
		if (!_trailingWrite.Enabled)
		{
			_trailingWrite.Interval = Math.Max(1, dueMs);
			_trailingWrite.Start();
		}
	}

	private void ScheduleOverlayApply()
	{
		if (_dragging)
		{
			if (!_overlaySuspended)
			{
				if (_dragClock.IsRunning && _dragClock.ElapsedMilliseconds < 45)
				{
					_dragSampleOwed = true;
					ArmTheTrailingWrite(45 - (int)_dragClock.ElapsedMilliseconds);
				}
				else
				{
					_dragClock.Restart();
					WriteTheDragSample();
				}
			}
		}
		else if (!_overlaySuspended)
		{
			ApplyOverlay();
		}
	}

	private void ApplyOverlay()
	{
		if (HandedBack)
		{
			return;
		}
		RuntimeFacts.NoteDisplayOnlyLook(CarryingTheLookOnTheDisplay, WhatTheDisplayCannotHold);
		if (_overlay is IDisplayOverlay { EffectIsLive: false })
		{
			return;
		}
		float[] array = CurrentMatrix();
		if (_lastOverlay.AlreadySent(array))
		{
			return;
		}
		if (!_colourReachesScreen && !CarryingTheLookOnTheDisplay)
		{
			OverlayWritesRefused++;
			return;
		}
		Writes.CountOverlay();
		ColourTrace.Screen(array == null, _saturation);
		if (array == null)
		{
			_overlay.Clear();
		}
		else
		{
			_overlay.Apply(array);
		}
		NoteTheScreenEffect(array);
		if (_overlay.LastApplySucceeded)
		{
			_lastOverlay.Remember(array);
		}
		else
		{
			_lastOverlay.Forget();
		}
	}

	private void NoteTheScreenEffect(float[]? written)
	{
		bool held = (RuntimeFacts.ScreenEffectHeld = (_screenEffectHeld = _overlay is IDisplayOverlay displayOverlay && (displayOverlay.ScreenEffectHeld || (displayOverlay.ActiveMode == OverlayMode.Mag && displayOverlay.EffectIsLive && (_overlay.LastApplySucceeded ? (written != null && !MagOverlay.IsIdentity(written)) : _screenEffectHeld)))));
		ScreenEffectLog.Note(held, _saturation);
	}

	public float[]? CurrentMatrix()
	{
		if (CompetitiveCarryAllowed && CarryingTheLookOnTheDisplay && (!DarkBoostDrawing || !DarkBoostOffered()))
		{
			return null;
		}
		MonitorSelection monitorSelection = MonitorSelection;
		if (monitorSelection != null && !monitorSelection.AllowsMatrix)
		{
			return null;
		}
		return CurrentMatrixBeforeNative();
	}

	private float[]? CurrentMatrixBeforeNative()
	{
		if (_previewNeutral)
		{
			return null;
		}
		float vibrance = SoftwareVibranceFactor(_vibrance, _controller.IsAvailable, _streamingMode);
		float saturation = (float)_saturation / 100f;
		float contrast = (float)_contrast / 100f;
		float num = (float)_brightness / 100f;
		float num2 = (float)_temperature / 100f;
		int hue = _hue;
		int red = _redGain;
		int green = _greenGain;
		int blue = _blueGain;
		if (CarryingTheLookOnTheDisplay)
		{
			vibrance = 1f;
			saturation = 1f;
			contrast = 1f;
			num = 1f;
			num2 = 0f;
			red = (green = (blue = 100));
		}
		ChannelMix mix = _mix;
		ColourVisionMode colourVisionMode = ColourVision;
		ColourDepth depth = _depth;
		if (!_paidControlsUnlocked)
		{
			vibrance = SoftwareVibranceFactor(50, _controller.IsAvailable, _streamingMode);
			contrast = 1f;
			num = 1f;
			num2 = 0f;
			mix = ChannelMix.Identity;
			hue = 0;
			red = (green = (blue = 100));
			depth = ColourDepth.Neutral;
			if (colourVisionMode == ColourVisionMode.HoloBlack)
			{
				colourVisionMode = ((!LookCorrection.IsALook(_setAsideCorrection)) ? _setAsideCorrection : ColourVisionMode.Off);
			}
		}
		if (_eyeCareWarmth > 0)
		{
			num2 = Math.Max(num2, (float)_eyeCareWarmth / 100f);
		}
		if (_eyeCareDim > 0)
		{
			num *= 1f - (float)_eyeCareDim / 100f;
		}
		if (DarkBoostDrawing && DarkBoostOffered())
		{
			float[] array = NightVisionBasis();
			if (array != null)
			{
				float[] array2 = DarkBoostLook.Base(array, _darkBoostTint, _darkBoostBlackLevel);
				if (colourVisionMode != ColourVisionMode.Off)
				{
					array2 = VibranceHud.ColourVision.Compose(array2, VibranceHud.ColourVision.Matrix(colourVisionMode));
				}
				return DarkBoostLook.Lerp(GradeMatrix(vibrance, saturation, contrast, num, num2, hue, red, green, blue, mix, colourVisionMode, depth) ?? DarkBoostLook.Identity(), array2, EffectiveDarkBoostStrength);
			}
		}
		return GradeMatrix(vibrance, saturation, contrast, num, num2, hue, red, green, blue, mix, colourVisionMode, depth);
	}

	private float[]? GradeMatrix(float vibrance, float saturation, float contrast, float brightness, float warmth, int hue, int red, int green, int blue, ChannelMix mix, ColourVisionMode correction, ColourDepth depth)
	{
		if (ColorAdjust.IsIdentity(saturation, vibrance, contrast, brightness, warmth) && mix.IsIdentity && ColourBalance.IsNeutral(hue, red, green, blue) && depth.IsNeutral && correction == ColourVisionMode.Off)
		{
			return null;
		}
		ServedVibrancePoint servedVibrancePoint = e.A(_vibrance, _controller.IsAvailable, _streamingMode);
		if (vibrance == 1f)
		{
			servedVibrancePoint = ServedVibrancePoint.Neutral;
		}
		float[] matrix = ColorAdjust.BuildWithVibranceNumbers(saturation, servedVibrancePoint.Red, servedVibrancePoint.Chroma, contrast, brightness, warmth, depth);
		matrix = ColourBalance.ComposeInto(matrix, hue, red, green, blue);
		matrix = mix.ComposeInto(matrix);
		if (correction != ColourVisionMode.Off)
		{
			matrix = VibranceHud.ColourVision.Compose(matrix, VibranceHud.ColourVision.Matrix(correction));
		}
		return matrix;
	}

	public ushort[]? CurrentRamp()
	{
		if (_previewNeutral || !_paidControlsUnlocked)
		{
			return null;
		}
		ToneSettings t = _tone with
		{
			Gamma = _gamma
		};
		if (CarryingTheLookOnTheDisplay && !TheDisplaysChannelsAreNeutral)
		{
			return DisplayOnlyLook.Compose(ToneCurve.Build(t), _brightness, _contrast, _temperature, _redGain, _greenGain, _blueGain);
		}
		if (!t.IsNeutral)
		{
			return ToneCurve.Build(t);
		}
		return null;
	}
}
