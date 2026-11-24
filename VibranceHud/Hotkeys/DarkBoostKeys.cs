using System;

namespace VibranceHud.Hotkeys;

public sealed class DarkBoostKeys : IDisposable
{
	internal const int BindId = 4;

	internal const int NextBindId = 5;

	private readonly KeyBinder _binder;

	private readonly InputRelay _relay;

	private readonly IRawInputSource _source;

	private readonly Func<bool> _offered;

	private readonly Func<bool> _gameRunning;

	private int _vk;

	private uint _modifiers;

	private bool _gameOnly;

	private int _nextVk;

	private uint _nextModifiers;

	private bool _nextGameOnly;

	private bool _disposed;

	internal const int ZoomBindId = 6;

	private int _zoomVk;

	private uint _zoomModifiers;

	private bool _zoomGameOnly = true;

	public Func<bool> ZoomOffered { get; set; } = () => true;

	public bool IsZoomBound => _binder.IsBound(6);

	public bool Keyboard { get; private set; }

	public bool Mouse { get; private set; }

	public bool IsBound => _binder.IsBound(4);

	public bool IsNextBound => _binder.IsBound(5);

	public event Action<bool>? Zoom;

	public event Action<string>? ZoomIgnored;

	public event Action<bool>? Want;

	public event Action? Next;

	public DarkBoostKeys(Func<bool> offered, Func<bool> gameRunning, Action wake, Func<InputRelay, IRawInputSource> source)
	{
		_offered = offered;
		_gameRunning = gameRunning;
		_binder = new KeyBinder(gameRunning);
		_relay = new InputRelay(_binder.IsInteresting, wake);
		_source = source(_relay);
		_binder.Fired += (int id, bool on) =>
		{
			if (id == 4)
			{
				Want?.Invoke(on);
			}
			else if ((id == 5) & on)
			{
				Next?.Invoke();
			}
			else if (id == 6)
			{
				Zoom?.Invoke(on);
			}
		};
		_binder.Ignored += (int id, string why) =>
		{
			if (id == 6)
			{
				ZoomIgnored?.Invoke(why);
			}
		};
	}

	public bool BindZoom(uint modifiers, int vk, bool gameOnly = true)
	{
		bool flag = _binder.Register(6, modifiers, vk, KeyMode.Hold, gameOnly, () => ZoomOffered());
		if (flag)
		{
			_zoomVk = vk;
			_zoomModifiers = modifiers;
			_zoomGameOnly = gameOnly;
		}
		Settle();
		return flag;
	}

	public void UnbindZoom()
	{
		_binder.Unregister(6);
		Settle();
	}

	public bool Bind(uint modifiers, int vk, bool hold, bool gameOnly)
	{
		bool flag = _binder.Register(4, modifiers, vk, hold ? KeyMode.Hold : KeyMode.Press, gameOnly, _offered);
		if (flag)
		{
			_vk = vk;
			_modifiers = modifiers;
			_gameOnly = gameOnly;
		}
		Settle();
		return flag;
	}

	public void Unbind()
	{
		_binder.Unregister(4);
		Settle();
	}

	public bool BindNext(uint modifiers, int vk, bool gameOnly)
	{
		bool flag = _binder.Register(5, modifiers, vk, KeyMode.Press, gameOnly, _offered);
		if (flag)
		{
			_nextVk = vk;
			_nextModifiers = modifiers;
			_nextGameOnly = gameOnly;
		}
		Settle();
		return flag;
	}

	public void UnbindNext()
	{
		_binder.Unregister(5);
		Settle();
	}

	public void Settle()
	{
		if (!_disposed)
		{
			bool offered = _offered();
			bool gameRunning = _gameRunning();
			(bool, bool) tuple = RawInputPolicy.Wanted(offered, IsBound, _vk, _modifiers, _gameOnly, gameRunning);
			(bool, bool) tuple2 = RawInputPolicy.Wanted(offered, IsNextBound, _nextVk, _nextModifiers, _nextGameOnly, gameRunning);
			(bool, bool) tuple3 = RawInputPolicy.Wanted(ZoomOffered(), IsZoomBound, _zoomVk, _zoomModifiers, _zoomGameOnly, gameRunning);
			bool flag = tuple.Item1 || tuple2.Item1 || tuple3.Item1;
			bool flag2 = tuple.Item2 || tuple2.Item2 || tuple3.Item2;
			if (!flag && !flag2)
			{
				_binder.ReleaseAll();
			}
			if (flag != Keyboard || flag2 != Mouse)
			{
				_source.Apply(flag, flag2);
				Keyboard = flag;
				Mouse = flag2;
			}
		}
	}

	public void Drain()
	{
		_relay.Drain(_binder);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_binder.ReleaseAll();
			_source.Apply(keyboard: false, mouse: false);
			bool keyboard = (Mouse = false);
			Keyboard = keyboard;
			_source.Dispose();
		}
	}
}
