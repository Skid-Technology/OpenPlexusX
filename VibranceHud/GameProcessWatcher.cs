using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VibranceHud.Games;

namespace VibranceHud;

public sealed class GameProcessWatcher
{
	public delegate void GameEventHandler(string gameId);

	internal interface IProcessSource
	{
		IReadOnlyCollection<(int Pid, string Name)> Snapshot();

		IReadOnlyCollection<int> RunningIds();
	}

	private sealed class NamesOnly : IProcessSource
	{
		private readonly Func<IReadOnlyCollection<string>> _names;

		public NamesOnly(Func<IReadOnlyCollection<string>> names)
		{
			_names = names;
		}

		public IReadOnlyCollection<(int Pid, string Name)> Snapshot()
		{
			return (from n in _names()
				select (-1, n: n)).ToList();
		}

		public IReadOnlyCollection<int> RunningIds()
		{
			throw new NotSupportedException("a names-only probe has no ids");
		}
	}

	internal sealed class SystemProcessSource : IProcessSource
	{
		public static readonly SystemProcessSource Instance = new SystemProcessSource();

		public IReadOnlyCollection<(int Pid, string Name)> Snapshot()
		{
			List<(int, string)> list = new List<(int, string)>();
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				try
				{
					list.Add((process.Id, process.ProcessName));
				}
				catch (InvalidOperationException)
				{
				}
				finally
				{
					process.Dispose();
				}
			}
			return list;
		}

		[DllImport("psapi.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool EnumProcesses([Out] uint[] processIds, uint arraySizeBytes, out uint bytesReturned);

		public IReadOnlyCollection<int> RunningIds()
		{
			for (int num = 1024; num <= 1048576; num *= 2)
			{
				uint[] array = new uint[num];
				if (!EnumProcesses(array, (uint)(array.Length * 4), out var bytesReturned))
				{
					throw new Win32Exception();
				}
				int num2 = (int)(bytesReturned / 4);
				if (num2 < array.Length)
				{
					HashSet<int> hashSet = new HashSet<int>();
					for (int i = 0; i < num2; i++)
					{
						hashSet.Add((int)array[i]);
					}
					return hashSet;
				}
			}
			throw new InvalidOperationException("more than a million process ids");
		}
	}

	public static readonly TimeSpan IdlePollInterval = TimeSpan.FromSeconds(10.0);

	public static readonly TimeSpan InGamePollInterval = TimeSpan.FromSeconds(30.0);

	public static readonly TimeSpan NameConfirmInterval = TimeSpan.FromMinutes(15.0);

	private static readonly int ChecksPerNameConfirm = Math.Max(1, (int)(NameConfirmInterval.TotalSeconds / InGamePollInterval.TotalSeconds));

	private readonly IReadOnlyDictionary<string, string> _gameIdToExe;

	private readonly TimeSpan? _fixedInterval;

	private readonly HashSet<string> _knownRunning = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private CancellationTokenSource? _cts;

	private readonly IProcessSource _source;

	private readonly HashSet<int> _knownPids = new HashSet<int>();

	private int _checksSinceScan;

	private bool _confirmPending;

	public bool IsRunning
	{
		get
		{
			if (_cts != null)
			{
				return !_cts.IsCancellationRequested;
			}
			return false;
		}
	}

	internal TimeSpan NextPollInterval
	{
		get
		{
			TimeSpan? fixedInterval = _fixedInterval;
			if (!fixedInterval.HasValue)
			{
				if (_knownRunning.Count <= 0)
				{
					return IdlePollInterval;
				}
				return InGamePollInterval;
			}
			return fixedInterval.GetValueOrDefault();
		}
	}

	public event GameEventHandler? OnGameLaunched;

	public event GameEventHandler? OnGameClosed;

	public GameProcessWatcher(IReadOnlyDictionary<string, string> gameIdToExe, TimeSpan? pollInterval = null, Func<IReadOnlyCollection<string>>? runningProcessNames = null)
		: this(gameIdToExe, (runningProcessNames == null) ? ((IProcessSource)SystemProcessSource.Instance) : ((IProcessSource)new NamesOnly(runningProcessNames)), pollInterval)
	{
	}

	internal GameProcessWatcher(IReadOnlyDictionary<string, string> gameIdToExe, IProcessSource source, TimeSpan? pollInterval = null)
	{
		_gameIdToExe = gameIdToExe ?? throw new ArgumentNullException("gameIdToExe");
		_fixedInterval = pollInterval;
		_source = source ?? throw new ArgumentNullException("source");
	}

	internal void RaiseLaunchedForTest(string gameId)
	{
		OnGameLaunched?.Invoke(gameId);
	}

	internal void RaiseClosedForTest(string gameId)
	{
		OnGameClosed?.Invoke(gameId);
	}

	public void Start()
	{
		if (!IsRunning)
		{
			_cts = new CancellationTokenSource();
			Task.Run(() => PollLoop(_cts.Token));
		}
	}

	public void Stop()
	{
		try
		{
			_cts?.Cancel();
		}
		catch (ObjectDisposedException)
		{
		}
		_cts?.Dispose();
		_cts = null;
	}

	internal void PollOnce()
	{
		if (_knownRunning.Count > 0 && _knownPids.Count > 0 && !_confirmPending && _checksSinceScan < ChecksPerNameConfirm)
		{
			IReadOnlyCollection<int> readOnlyCollection;
			try
			{
				readOnlyCollection = _source.RunningIds();
			}
			catch (Exception)
			{
				return;
			}
			_checksSinceScan++;
			ISet<int> set = (readOnlyCollection as ISet<int>) ?? new HashSet<int>(readOnlyCollection);
			if (_knownPids.All(set.Contains))
			{
				return;
			}
			_confirmPending = true;
		}
		FullScan();
	}

	private void FullScan()
	{
		IReadOnlyCollection<(int, string)> readOnlyCollection;
		try
		{
			readOnlyCollection = _source.Snapshot();
		}
		catch (Exception)
		{
			return;
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (text3, text4) in _gameIdToExe)
		{
			dictionary[text4] = text3;
			SupportedGame supportedGame = SupportedGames.ById(text3);
			if ((object)supportedGame == null || !string.Equals(supportedGame.ProcessName, text4, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			foreach (string allProcessName in supportedGame.AllProcessNames)
			{
				dictionary[allProcessName] = text3;
			}
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<int> hashSet2 = new HashSet<int>();
		foreach (var (num, text5) in readOnlyCollection)
		{
			if (text5 != null && dictionary.TryGetValue(text5, out var value))
			{
				hashSet.Add(value);
				if (num > 0)
				{
					hashSet2.Add(num);
				}
			}
		}
		foreach (string item in hashSet.Except(_knownRunning).ToList())
		{
			OnGameLaunched?.Invoke(item);
		}
		foreach (string item2 in _knownRunning.Except(hashSet).ToList())
		{
			OnGameClosed?.Invoke(item2);
		}
		_knownRunning.Clear();
		foreach (string item3 in hashSet)
		{
			_knownRunning.Add(item3);
		}
		_knownPids.Clear();
		foreach (int item4 in hashSet2)
		{
			_knownPids.Add(item4);
		}
		_checksSinceScan = 0;
		_confirmPending = false;
	}

	private async Task PollLoop(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				PollOnce();
			}
			catch (Exception)
			{
			}
			try
			{
				await Task.Delay(NextPollInterval, ct).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (TaskCanceledException)
			{
				break;
			}
		}
	}
}
