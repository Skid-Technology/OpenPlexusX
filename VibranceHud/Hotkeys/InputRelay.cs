using System;
using System.Threading;

namespace VibranceHud.Hotkeys;

public sealed class InputRelay
{
	public const int Capacity = 256;

	private const int Mask = 255;

	private readonly InputEvent[] _ring = new InputEvent[256];

	private readonly Func<int, bool> _wanted;

	private readonly Action _wake;

	private int _head;

	private int _tail;

	private int _overflow;

	public int Pending => Volatile.Read(in _head) - Volatile.Read(in _tail);

	public InputRelay(Func<int, bool> wanted, Action wake)
	{
		_wanted = wanted;
		_wake = wake;
	}

	public void OnRawEvent(int vk, bool down)
	{
		try
		{
			if (!_wanted(vk))
			{
				return;
			}
			int head = _head;
			int num = Volatile.Read(in _tail);
			if (head - num >= 256)
			{
				Volatile.Write(ref _overflow, 1);
				return;
			}
			_ring[head & 0xFF] = new InputEvent(vk, down);
			Volatile.Write(ref _head, head + 1);
			if (head == num)
			{
				_wake();
			}
		}
		catch
		{
		}
	}

	public void Drain(KeyBinder binder)
	{
		int num = _tail;
		while (num != Volatile.Read(in _head))
		{
			InputEvent e = _ring[num & 0xFF];
			_ring[num & 0xFF] = default;
			num++;
			Volatile.Write(ref _tail, num);
			binder.Handle(e);
		}
		if (Interlocked.Exchange(ref _overflow, 0) != 0)
		{
			binder.ReleaseAll();
		}
	}
}
