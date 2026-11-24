using System;
using System.Collections.Generic;
using System.Numerics;

namespace VibranceHud.Hotkeys;

public sealed class KeyBinder
{
	private sealed class Binding
	{
		public int Id;

		public uint Modifiers;

		public int Vk;

		public KeyMode Mode;

		public bool GameOnly;

		public Func<bool>? Allowed;

		public bool Held;

		public bool Engaged;
	}

	public const int VK_LBUTTON = 1;

	public const int VK_RBUTTON = 2;

	public const int VK_MBUTTON = 4;

	public const int VK_XBUTTON1 = 5;

	public const int VK_XBUTTON2 = 6;

	private readonly List<Binding> _bindings = new List<Binding>();

	private readonly Func<bool> _gameRunning;

	private int _modifierSides;

	private volatile bool[] _interesting = Rebuilt(Array.Empty<Binding>());

	public const string NoGameRunning = "no game running";

	public const string NotAllowed = "not allowed";

	public int Count => _bindings.Count;

	public event Action<int, bool>? Fired;

	public event Action<int, string>? Ignored;

	public KeyBinder(Func<bool> gameRunning)
	{
		_gameRunning = gameRunning;
	}

	public static bool IsBindable(int vk)
	{
		if (vk > 0 && vk < 256 && vk != 1 && vk != 2)
		{
			return ModifierSide(vk) < 0;
		}
		return false;
	}

	public bool Register(int id, uint modifiers, int vk, KeyMode mode = KeyMode.Press, bool gameOnly = false, Func<bool>? allowed = null)
	{
		if (!IsBindable(vk))
		{
			return false;
		}
		modifiers &= 0xF;
		foreach (Binding binding in _bindings)
		{
			if (binding.Id != id && binding.Vk == vk && binding.Modifiers == modifiers)
			{
				return false;
			}
		}
		Unregister(id);
		_bindings.Add(new Binding
		{
			Id = id,
			Modifiers = modifiers,
			Vk = vk,
			Mode = mode,
			GameOnly = gameOnly,
			Allowed = allowed
		});
		_interesting = Rebuilt(_bindings);
		ForgetUnwatchedModifiers();
		return true;
	}

	public void Unregister(int id)
	{
		for (int num = _bindings.Count - 1; num >= 0; num--)
		{
			Binding binding = _bindings[num];
			if (binding.Id == id)
			{
				_bindings.RemoveAt(num);
				if (binding.Engaged)
				{
					binding.Engaged = false;
					Fired?.Invoke(binding.Id, arg2: false);
				}
			}
		}
		_interesting = Rebuilt(_bindings);
		ForgetUnwatchedModifiers();
	}

	public bool IsBound(int id)
	{
		return _bindings.Exists((Binding b) => b.Id == id);
	}

	public bool IsInteresting(int vk)
	{
		bool[] interesting = _interesting;
		if ((uint)vk < (uint)interesting.Length)
		{
			return interesting[vk];
		}
		return false;
	}

	public void Handle(InputEvent e)
	{
		int num = ModifierSide(e.VirtualKey);
		if (num >= 0)
		{
			if (e.Down)
			{
				_modifierSides |= 1 << num;
			}
			else
			{
				_modifierSides &= ~(1 << num);
			}
		}
		else if (e.Down)
		{
			OnDown(e.VirtualKey);
		}
		else
		{
			OnUp(e.VirtualKey);
		}
	}

	public void ReleaseAll()
	{
		_modifierSides = 0;
		foreach (Binding binding in _bindings)
		{
			binding.Held = false;
			if (binding.Engaged)
			{
				binding.Engaged = false;
				Fired?.Invoke(binding.Id, arg2: false);
			}
		}
	}

	private void OnDown(int vk)
	{
		uint num = HeldModifiers();
		Binding binding = null;
		int num2 = -1;
		foreach (Binding binding2 in _bindings)
		{
			if (binding2.Vk == vk && (binding2.Modifiers & ~num) == 0)
			{
				int num3 = PopCount(binding2.Modifiers);
				if (num3 > num2)
				{
					binding = binding2;
					num2 = num3;
				}
			}
		}
		if (binding == null || binding.Held)
		{
			return;
		}
		binding.Held = true;
		if (binding.GameOnly && !_gameRunning())
		{
			Ignored?.Invoke(binding.Id, "no game running");
			return;
		}
		if (binding.Allowed != null && !binding.Allowed())
		{
			Ignored?.Invoke(binding.Id, "not allowed");
			return;
		}
		if (binding.Mode == KeyMode.Hold)
		{
			binding.Engaged = true;
		}
		Fired?.Invoke(binding.Id, arg2: true);
	}

	private void OnUp(int vk)
	{
		foreach (Binding binding in _bindings)
		{
			if (binding.Vk == vk && binding.Held)
			{
				binding.Held = false;
				if (binding.Engaged)
				{
					binding.Engaged = false;
					Fired?.Invoke(binding.Id, arg2: false);
				}
			}
		}
	}

	private uint HeldModifiers()
	{
		uint num = 0u;
		if ((_modifierSides & 3) != 0)
		{
			num |= 4;
		}
		if ((_modifierSides & 0xC) != 0)
		{
			num |= 2;
		}
		if ((_modifierSides & 0x30) != 0)
		{
			num |= 1;
		}
		if ((_modifierSides & 0xC0) != 0)
		{
			num |= 8;
		}
		return num;
	}

	private static int ModifierSide(int vk)
	{
		switch (vk)
		{
		case 16:
		case 160:
			return 0;
		case 161:
			return 1;
		case 17:
		case 162:
			return 2;
		case 163:
			return 3;
		case 18:
		case 164:
			return 4;
		case 165:
			return 5;
		case 91:
			return 6;
		case 92:
			return 7;
		default:
			return -1;
		}
	}

	private static int PopCount(uint v)
	{
		return BitOperations.PopCount(v);
	}

	private static bool[] Rebuilt(IEnumerable<Binding> bindings)
	{
		bool[] array = new bool[256];
		uint num = 0u;
		foreach (Binding binding in bindings)
		{
			array[binding.Vk] = true;
			num |= binding.Modifiers;
		}
		if ((num & 4) != 0)
		{
			bool flag;
			array[160] = (flag = (array[161] = true));
			array[16] = flag;
		}
		if ((num & 2) != 0)
		{
			bool flag;
			array[162] = (flag = (array[163] = true));
			array[17] = flag;
		}
		if ((num & 1) != 0)
		{
			bool flag;
			array[164] = (flag = (array[165] = true));
			array[18] = flag;
		}
		if ((num & 8) != 0)
		{
			array[91] = (array[92] = true);
		}
		return array;
	}

	private void ForgetUnwatchedModifiers()
	{
		bool[] interesting = _interesting;
		int num = 0;
		if (interesting[160])
		{
			num |= 3;
		}
		if (interesting[162])
		{
			num |= 0xC;
		}
		if (interesting[164])
		{
			num |= 0x30;
		}
		if (interesting[91])
		{
			num |= 0xC0;
		}
		_modifierSides &= num;
	}
}
