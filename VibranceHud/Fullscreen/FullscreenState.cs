using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Fullscreen;

public sealed class FullscreenState
{
	public static readonly TimeSpan EnterHold = TimeSpan.FromSeconds(2.0);

	private readonly Func<bool> _shellSaysD3DFullscreen;

	private readonly Func<string?> _foregroundProcessName;

	private readonly HashSet<string> _tracked;

	private readonly Func<long> _nowMs;

	public static readonly TimeSpan LeaveHold = TimeSpan.FromMilliseconds(750.0);

	private long? _candidateSinceMs;

	private string? _candidate;

	private long? _leaveSinceMs;

	private readonly bool _anyWindow;

	private const string UnnamedWindow = "(a full-screen window)";

	private static readonly string CouldNotTell = new string("(could not tell)".ToCharArray());

	public bool LeavePending => _leaveSinceMs.HasValue;

	public bool IsExclusiveFullscreen { get; private set; }

	public string? Game
	{
		get
		{
			if (!IsExclusiveFullscreen)
			{
				return null;
			}
			return _candidate;
		}
	}

	public bool CandidatePending
	{
		get
		{
			if (_candidate != null)
			{
				return !IsExclusiveFullscreen;
			}
			return false;
		}
	}

	public string? LatchedGame { get; private set; }

	public FullscreenState(Func<bool> shellSaysD3DFullscreen, Func<string?> foregroundProcessName, IEnumerable<string> trackedProcessNames, Func<long> monotonicMs)
		: this(shellSaysD3DFullscreen, foregroundProcessName, trackedProcessNames, monotonicMs, anyWindow: false)
	{
	}

	private FullscreenState(Func<bool> shellSaysD3DFullscreen, Func<string?> foregroundProcessName, IEnumerable<string> trackedProcessNames, Func<long> monotonicMs, bool anyWindow)
	{
		_anyWindow = anyWindow;
		_shellSaysD3DFullscreen = shellSaysD3DFullscreen ?? throw new ArgumentNullException("shellSaysD3DFullscreen");
		_foregroundProcessName = foregroundProcessName ?? throw new ArgumentNullException("foregroundProcessName");
		_tracked = new HashSet<string>(trackedProcessNames ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		_nowMs = monotonicMs ?? throw new ArgumentNullException("monotonicMs");
	}

	public static FullscreenState ForAnyFullScreenWindow(Func<bool> foregroundFillsTheScreen, Func<string?> foregroundProcessName, Func<long> monotonicMs)
	{
		return new FullscreenState(foregroundFillsTheScreen, foregroundProcessName, Array.Empty<string>(), monotonicMs, anyWindow: true);
	}

	public bool GameClosed(string? processName)
	{
		string text = WithoutExe(processName);
		if (LatchedGame == null || text == null || !string.Equals(text, LatchedGame, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		LatchedGame = null;
		return true;
	}

	public bool Poll()
	{
		string text = CandidateNow();
		if ((object)text == CouldNotTell)
		{
			return IsExclusiveFullscreen;
		}
		if (text == null)
		{
			if (_anyWindow && IsExclusiveFullscreen)
			{
				long valueOrDefault = _leaveSinceMs.GetValueOrDefault();
				if (!_leaveSinceMs.HasValue)
				{
					valueOrDefault = _nowMs();
					_leaveSinceMs = valueOrDefault;
				}
				if (_nowMs() - _leaveSinceMs.Value < (long)LeaveHold.TotalMilliseconds)
				{
					return true;
				}
			}
			_leaveSinceMs = null;
			_candidate = null;
			_candidateSinceMs = null;
			IsExclusiveFullscreen = false;
			return false;
		}
		_leaveSinceMs = null;
		if (IsExclusiveFullscreen && string.Equals(text, _candidate, StringComparison.OrdinalIgnoreCase))
		{
			if (!_anyWindow)
			{
				LatchedGame = _candidate;
			}
			return true;
		}
		if (_anyWindow && IsExclusiveFullscreen)
		{
			_candidate = text;
			return true;
		}
		if (!string.Equals(text, _candidate, StringComparison.OrdinalIgnoreCase))
		{
			_candidate = text;
			_candidateSinceMs = _nowMs();
			return IsExclusiveFullscreen = false;
		}
		if (!_candidateSinceMs.HasValue || _nowMs() - _candidateSinceMs.Value < (long)EnterHold.TotalMilliseconds)
		{
			return IsExclusiveFullscreen = false;
		}
		if (!_anyWindow)
		{
			LatchedGame = _candidate;
		}
		return IsExclusiveFullscreen = true;
	}

	private string? CandidateNow()
	{
		try
		{
			if (!_shellSaysD3DFullscreen())
			{
				if (LatchedGame != null && string.Equals(TrackedForeground(), LatchedGame, StringComparison.OrdinalIgnoreCase))
				{
					LatchedGame = null;
				}
				return null;
			}
			return TrackedForeground();
		}
		catch
		{
			return null;
		}
	}

	private string? TrackedForeground()
	{
		string name;
		try
		{
			name = _foregroundProcessName();
		}
		catch when (!_anyWindow)
		{
			return CouldNotTell;
		}
		string text = WithoutExe(name);
		if (_anyWindow)
		{
			return text ?? "(a full-screen window)";
		}
		if (text == null)
		{
			return CouldNotTell;
		}
		if (!_tracked.Contains(text))
		{
			return null;
		}
		return text;
	}

	private static string? WithoutExe(string? name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
		{
			name = name.Substring(0, name.Length - 4);
		}
		if (name.Length != 0)
		{
			return name;
		}
		return null;
	}
}
