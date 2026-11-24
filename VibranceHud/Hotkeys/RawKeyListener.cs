using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VibranceHud.Hotkeys;

public sealed class RawKeyListener : IRawInputSource, IDisposable
{
	internal struct RAWINPUTDEVICE
	{
		public ushort UsagePage;

		public ushort Usage;

		public uint Flags;

		public nint Target;
	}

	private sealed class Sink : NativeWindow
	{
		private readonly RawKeyListener _owner;

		public Sink(RawKeyListener owner)
		{
			_owner = owner;
		}

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
			case 255:
				_owner.Read(m.LParam);
				break;
			case 32849:
			{
				int num = ((IntPtr)m.WParam).ToInt32();
				_owner.ChangeRegistration((num & 1) != 0, (num & 2) != 0, Handle);
				return;
			}
			case 32850:
				_owner.ChangeRegistration(keyboard: false, mouse: false, IntPtr.Zero);
				DestroyHandle();
				Application.ExitThread();
				return;
			}
			base.WndProc(ref m);
		}
	}

	internal const uint RIDEV_REMOVE = 1u;

	internal const uint RIDEV_INPUTSINK = 256u;

	internal const ushort UsagePageGeneric = 1;

	internal const ushort UsageMouse = 2;

	internal const ushort UsageKeyboard = 6;

	private const int WM_INPUT = 255;

	private const int WM_APPLY = 32849;

	private const int WM_STOP = 32850;

	private const uint RID_INPUT = 268435459u;

	private const int RIM_TYPEMOUSE = 0;

	private const int RIM_TYPEKEYBOARD = 1;

	private const ushort RI_KEY_BREAK = 1;

	private const ushort RI_KEY_E0 = 2;

	private const ushort ButtonFlagsWeRead = 1008;

	internal static readonly int HeaderSize = 8 + 2 * IntPtr.Size;

	private const int BufferSize = 64;

	private readonly InputRelay _relay;

	private readonly byte[] _buffer = new byte[64];

	private GCHandle _pin;

	private Thread? _thread;

	private Sink? _sink;

	private readonly ManualResetEventSlim _ready = new ManualResetEventSlim(initialState: false);

	private bool _registeredKeyboard;

	private bool _registeredMouse;

	private readonly Func<RAWINPUTDEVICE[], bool>? _registrar;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterRawInputDevices([In] RAWINPUTDEVICE[] devices, uint count, uint size);

	[DllImport("user32.dll")]
	private static extern uint GetRawInputData(nint rawInput, uint command, nint data, ref uint size, uint headerSize);

	[DllImport("user32.dll")]
	private static extern bool PostMessage(nint hWnd, int msg, nint wParam, nint lParam);

	public RawKeyListener(InputRelay relay)
		: this(relay, null)
	{
	}

	internal RawKeyListener(InputRelay relay, Func<RAWINPUTDEVICE[], bool>? registrar)
	{
		_relay = relay;
		_registrar = registrar;
		_pin = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
	}

	public void Apply(bool keyboard, bool mouse)
	{
		if (!keyboard && !mouse)
		{
			if (_thread != null)
			{
				Sink sink = _sink;
				if (sink != null)
				{
					PostMessage(sink.Handle, 32850, IntPtr.Zero, IntPtr.Zero);
				}
				_thread.Join(2000);
				_thread = null;
				_sink = null;
				_ready.Reset();
			}
			return;
		}
		if (_thread == null)
		{
			_thread = new Thread(Run)
			{
				IsBackground = true,
				Name = "PlexusX Dark Boost keys"
			};
			_thread.Start();
			if (!_ready.Wait(2000))
			{
				return;
			}
		}
		Sink sink2 = _sink;
		if (sink2 != null)
		{
			int value = (keyboard ? 1 : 0) | (mouse ? 2 : 0);
			PostMessage(sink2.Handle, 32849, new IntPtr(value), IntPtr.Zero);
		}
	}

	public static void Post(nint window, int message)
	{
		if (window != IntPtr.Zero)
		{
			PostMessage(window, message, IntPtr.Zero, IntPtr.Zero);
		}
	}

	public void Dispose()
	{
		Apply(keyboard: false, mouse: false);
		if (_pin.IsAllocated)
		{
			_pin.Free();
		}
		_ready.Dispose();
	}

	private void Run()
	{
		try
		{
			Sink sink = new Sink(this);
			sink.CreateHandle(new CreateParams
			{
				Parent = new IntPtr(-3)
			});
			_sink = sink;
			_ready.Set();
			Application.Run();
		}
		catch
		{
			_ready.Set();
		}
	}

	internal static RAWINPUTDEVICE Device(ushort usage, bool add, nint target)
	{
		if (!add)
		{
			return new RAWINPUTDEVICE
			{
				UsagePage = 1,
				Usage = usage,
				Flags = 1u,
				Target = IntPtr.Zero
			};
		}
		return new RAWINPUTDEVICE
		{
			UsagePage = 1,
			Usage = usage,
			Flags = 256u,
			Target = target
		};
	}

	private void ChangeRegistration(bool keyboard, bool mouse, nint target)
	{
		RAWINPUTDEVICE[] array = new RAWINPUTDEVICE[2];
		int num = 0;
		if (keyboard != _registeredKeyboard)
		{
			array[num++] = Device(6, keyboard, target);
		}
		if (mouse != _registeredMouse)
		{
			array[num++] = Device(2, mouse, target);
		}
		switch (num)
		{
		case 0:
			return;
		case 1:
			Array.Resize(ref array, 1);
			break;
		}
		if ((_registrar != null) ? _registrar(array) : RegisterRawInputDevices(array, (uint)num, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
		{
			_registeredKeyboard = keyboard;
			_registeredMouse = mouse;
		}
	}

	private void Read(nint rawInput)
	{
		uint size = 64u;
		uint rawInputData = GetRawInputData(rawInput, 268435459u, _pin.AddrOfPinnedObject(), ref size, (uint)HeaderSize);
		if (rawInputData != 0 && rawInputData != uint.MaxValue && rawInputData <= 64)
		{
			Decode(new ReadOnlySpan<byte>(_buffer, 0, (int)rawInputData), _relay);
		}
	}

	internal static void Decode(ReadOnlySpan<byte> raw, InputRelay relay)
	{
		if (raw.Length < HeaderSize + 8)
		{
			return;
		}
		uint num = BinaryPrimitives.ReadUInt32LittleEndian(raw);
		ReadOnlySpan<byte> source = raw.Slice(HeaderSize);
		switch (num)
		{
		case 0u:
		{
			ushort num5 = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(4));
			if ((num5 & 0x3F0) != 0)
			{
				if ((num5 & 0x10) != 0)
				{
					relay.OnRawEvent(4, down: true);
				}
				if ((num5 & 0x20) != 0)
				{
					relay.OnRawEvent(4, down: false);
				}
				if ((num5 & 0x40) != 0)
				{
					relay.OnRawEvent(5, down: true);
				}
				if ((num5 & 0x80) != 0)
				{
					relay.OnRawEvent(5, down: false);
				}
				if ((num5 & 0x100) != 0)
				{
					relay.OnRawEvent(6, down: true);
				}
				if ((num5 & 0x200) != 0)
				{
					relay.OnRawEvent(6, down: false);
				}
			}
			break;
		}
		case 1u:
		{
			ushort num2 = BinaryPrimitives.ReadUInt16LittleEndian(source);
			ushort num3 = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(2));
			int num4 = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(6));
			if (num4 != 0 && num4 < 255)
			{
				bool flag = (num3 & 2) != 0;
				relay.OnRawEvent(num4 switch
				{
					16 => (num2 == 54) ? 161 : 160, 
					17 => flag ? 163 : 162, 
					18 => flag ? 165 : 164, 
					_ => num4, 
				}, (num3 & 1) == 0);
			}
			break;
		}
		}
	}
}
