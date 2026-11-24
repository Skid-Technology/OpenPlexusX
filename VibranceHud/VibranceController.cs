using System;
using System.Collections.Generic;

namespace VibranceHud;

public sealed class VibranceController : IVibranceController
{
	private readonly INvapiDisplayAccess _nvapi;

	private readonly Action<Exception> _report;

	private int _displayCount;

	private bool _nvidiaLost;

	private bool _mayReResolve = true;

	private int _lastGoodCurrent = 50;

	private int _lastGoodDefault = 50;

	public int CurrentLevel => _lastGoodCurrent = Read((INvapiDisplayAccess nvapi) => nvapi.ReadCurrentLevel(0), _lastGoodCurrent);

	public int DefaultLevel => _lastGoodDefault = Read((INvapiDisplayAccess nvapi) => nvapi.ReadDefaultLevel(0), _lastGoodDefault);

	public bool IsAvailable => !_nvidiaLost;

	public VibranceDriverState DriverState
	{
		get
		{
			if (!_nvidiaLost)
			{
				return VibranceDriverState.Available;
			}
			return VibranceDriverState.DisplayNotOnNvidia;
		}
	}

	public int DisplaysThatRefusedTheLastWrite { get; private set; }

	public VibranceController()
		: this(new NvapiDisplayAccess(), NvapiFallbackLog.Note)
	{
	}

	internal VibranceController(INvapiDisplayAccess nvapi, Action<Exception>? report = null)
	{
		_nvapi = nvapi;
		_report = report ?? ((Action<Exception>)((Exception _) =>
		{
		}));
		_displayCount = _nvapi.Resolve();
		if (_displayCount == 0)
		{
			throw new NoNvidiaDisplaysException();
		}
	}

	public bool EveryDisplayIsAt(int level)
	{
		if (_nvidiaLost)
		{
			return true;
		}
		if (_displayCount == 0)
		{
			return true;
		}
		for (int i = 0; i < _displayCount; i++)
		{
			int index = i;
			if (Read((INvapiDisplayAccess nvapi) => nvapi.ReadCurrentLevel(index), int.MinValue) != level)
			{
				return false;
			}
		}
		return true;
	}

	internal bool Redetect()
	{
		if (_nvidiaLost)
		{
			return false;
		}
		try
		{
			string text = IdsKey();
			int num = _nvapi.Resolve();
			if (num <= 0)
			{
				return false;
			}
			bool flag = num != _displayCount;
			_displayCount = num;
			_mayReResolve = true;
			return flag || IdsKey() != text;
		}
		catch
		{
			return false;
		}
	}

	private string IdsKey()
	{
		try
		{
			string[] array = new string[_displayCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _nvapi.DisplayId(i);
			}
			return string.Join("|", array);
		}
		catch
		{
			return "";
		}
	}

	public IReadOnlyList<KeyValuePair<string, int>> ReadEveryDisplayLevel()
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		if (_nvidiaLost)
		{
			return list;
		}
		for (int i = 0; i < _displayCount; i++)
		{
			int index = i;
			int num = Read((INvapiDisplayAccess nvapi) => nvapi.ReadCurrentLevel(index), int.MinValue);
			if (num != int.MinValue)
			{
				try
				{
					list.Add(new KeyValuePair<string, int>(_nvapi.DisplayId(index), num));
				}
				catch
				{
				}
			}
		}
		return list;
	}

	public void SetLevelFor(string displayId, int level)
	{
		if (_nvidiaLost)
		{
			return;
		}
		level = Math.Clamp(level, 0, 100);
		if (WriteOne(displayId, level, out Exception failure, out bool found))
		{
			_mayReResolve = true;
		}
		else if (failure != null && TryReResolve(failure))
		{
			if (WriteOne(displayId, level, out Exception failure2, out found))
			{
				_mayReResolve = true;
			}
			else if (failure2 != null)
			{
				GiveUp(failure2);
			}
		}
	}

	private bool WriteOne(string displayId, int level, out Exception? failure, out bool found)
	{
		failure = null;
		found = false;
		try
		{
			for (int i = 0; i < _displayCount; i++)
			{
				if (!(_nvapi.DisplayId(i) != displayId))
				{
					found = true;
					_nvapi.WriteLevel(i, level);
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			failure = ex;
		}
		return false;
	}

	public void SetLevel(int level)
	{
		if (_nvidiaLost)
		{
			return;
		}
		level = Math.Clamp(level, 0, 100);
		if (WriteToEveryDisplay(level, out Exception failure))
		{
			_mayReResolve = true;
		}
		else if (failure != null && TryReResolve(failure))
		{
			if (WriteToEveryDisplay(level, out Exception failure2))
			{
				_mayReResolve = true;
			}
			else
			{
				GiveUp(failure2 ?? failure);
			}
		}
	}

	private int Read(Func<INvapiDisplayAccess, int> read, int lastGood)
	{
		if (_nvidiaLost)
		{
			return lastGood;
		}
		try
		{
			int result = read(_nvapi);
			_mayReResolve = true;
			return result;
		}
		catch (Exception cause)
		{
			if (!TryReResolve(cause))
			{
				return lastGood;
			}
			try
			{
				int result2 = read(_nvapi);
				_mayReResolve = true;
				return result2;
			}
			catch (Exception cause2)
			{
				GiveUp(cause2);
				return lastGood;
			}
		}
	}

	private bool WriteToEveryDisplay(int level, out Exception? failure)
	{
		Exception first = null;
		int refused = 0;
		List<int> list = TolerantOutputBuilder.Build(_displayCount, (int i) =>
		{
			_nvapi.WriteLevel(i, level);
		}, (int i, Exception ex) =>
		{
			refused++;
			if (first == null)
			{
				first = ex;
			}
			ColourTrace.Refused("DRIVER", "NVIDIA would not set Digital Vibrance on display " + i);
		});
		DisplaysThatRefusedTheLastWrite = refused;
		failure = first;
		return list.Count > 0;
	}

	private bool TryReResolve(Exception cause)
	{
		if (!_mayReResolve)
		{
			GiveUp(cause);
			return false;
		}
		_mayReResolve = false;
		try
		{
			int num = _nvapi.Resolve();
			if (num > 0)
			{
				_displayCount = num;
				return true;
			}
		}
		catch (Exception cause2)
		{
			GiveUp(cause2);
			return false;
		}
		GiveUp(cause);
		return false;
	}

	private void GiveUp(Exception cause)
	{
		if (_nvidiaLost)
		{
			return;
		}
		_nvidiaLost = true;
		try
		{
			_report(cause);
		}
		catch
		{
		}
	}
}
