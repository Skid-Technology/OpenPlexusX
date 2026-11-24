using System;
using System.Threading;
using System.Threading.Tasks;

namespace VibranceHud.Audio;

public sealed class AudioEdgeService : IDisposable
{
	public enum MeterState
	{
		Off,
		NoReading,
		Holding,
		Clear
	}

	public const int TickMs = 30;

	public const float ExternalChangeTolerance = 0.001f;

	public const int HoldTicks = 20;

	public const float WriteEpsilon = 0.002f;

	private readonly IAudioOutput _output;

	private CancellationTokenSource? _cts;

	private float _restoreVolume = 1f;

	private float _lastWritten = float.NaN;

	private readonly float[] _recentPeaks = new float[20];

	private int _nextPeak;

	internal int WriteCount { get; private set; }

	public float Threshold { get; set; } = 0.3f;

	public bool IsRunning => _cts != null;

	public float Peak => _output.Peak;

	public float CurrentVolume => _output.Volume;

	public float RestoreVolume => _restoreVolume;

	public MeterState Meter
	{
		get
		{
			if (!IsRunning)
			{
				return MeterState.Off;
			}
			float currentVolume = CurrentVolume;
			float restoreVolume = _restoreVolume;
			if (float.IsNaN(currentVolume) || float.IsNaN(restoreVolume))
			{
				return MeterState.NoReading;
			}
			if (!(currentVolume < restoreVolume - 0.01f))
			{
				return MeterState.Clear;
			}
			return MeterState.Holding;
		}
	}

	public event Action<float>? OnRestorePointCaptured;

	public event Action? OnRestoreCompleted;

	public AudioEdgeService(IAudioOutput output)
	{
		_output = output;
	}

	public void Start()
	{
		Start(runLoop: true);
	}

	internal void StartWithoutLoopForTest()
	{
		Start(runLoop: false);
	}

	private void Start(bool runLoop)
	{
		if (IsRunning)
		{
			return;
		}
		float f = (_restoreVolume = _output.Volume);
		_lastWritten = float.NaN;
		Array.Clear(_recentPeaks);
		_nextPeak = 0;
		if (!float.IsNaN(f))
		{
			OnRestorePointCaptured?.Invoke(_restoreVolume);
		}
		_cts = new CancellationTokenSource();
		CancellationToken token = _cts.Token;
		if (!runLoop)
		{
			return;
		}
		Task.Run(async () =>
		{
			try
			{
				while (!token.IsCancellationRequested)
				{
					Tick();
					await Task.Delay(30, token);
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch
			{
			}
			finally
			{
				if (!float.IsNaN(_restoreVolume))
				{
					_output.Volume = _restoreVolume;
				}
				OnRestoreCompleted?.Invoke();
			}
		}, token);
	}

	public void ForceRestore(float volume)
	{
		_output.Volume = Math.Clamp(volume, 0f, 1f);
	}

	public void Tick()
	{
		float volume = _output.Volume;
		if (float.IsNaN(volume))
		{
			return;
		}
		if (float.IsNaN(_restoreVolume))
		{
			_restoreVolume = volume;
			_lastWritten = float.NaN;
			OnRestorePointCaptured?.Invoke(_restoreVolume);
		}
		if (!float.IsNaN(_lastWritten))
		{
			float num = volume - _lastWritten;
			if (MathF.Abs(num) > 0.001f)
			{
				_restoreVolume = Math.Clamp(_restoreVolume + num, 0.05f, 1f);
				OnRestorePointCaptured?.Invoke(_restoreVolume);
			}
		}
		_recentPeaks[_nextPeak] = _output.Peak;
		_nextPeak = (_nextPeak + 1) % _recentPeaks.Length;
		float num2 = 0f;
		float[] recentPeaks = _recentPeaks;
		foreach (float y in recentPeaks)
		{
			num2 = MathF.Max(num2, y);
		}
		float num3 = AudioLimiter.NextVolume(num2, Threshold, volume, _restoreVolume);
		if (MathF.Abs(num3 - volume) > 0.002f)
		{
			float volume2 = _output.Volume;
			if (!float.IsNaN(volume2))
			{
				if (MathF.Abs(volume2 - volume) > 0.001f)
				{
					_restoreVolume = Math.Clamp(_restoreVolume + (volume2 - volume), 0.05f, 1f);
					_lastWritten = volume2;
					OnRestorePointCaptured?.Invoke(_restoreVolume);
				}
				else
				{
					_output.Volume = num3;
					_lastWritten = _output.Volume;
					WriteCount++;
				}
			}
		}
		else
		{
			_lastWritten = volume;
		}
	}

	public void Stop()
	{
		CancellationTokenSource cts = _cts;
		_cts = null;
		if (cts != null)
		{
			cts.Cancel();
			cts.Dispose();
			if (!float.IsNaN(_restoreVolume))
			{
				_output.Volume = _restoreVolume;
			}
			_lastWritten = float.NaN;
			OnRestoreCompleted?.Invoke();
		}
	}

	public void Dispose()
	{
		Stop();
		(_output as IDisposable)?.Dispose();
	}
}
