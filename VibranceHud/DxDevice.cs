using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace VibranceHud;

internal sealed class DxDevice : IDisposable
{
	public sealed class OutputTarget
	{
		public SwapChain1 SwapChain;

		public RenderTargetView Rtv;

		public Output1 Output;

		public SharpDX.Direct3D11.Device Device;

		public nint Hwnd;

		public int Width;

		public int Height;
	}

	private struct WNDCLASSEX
	{
		public int cbSize;

		public int style;

		public nint lpfnWndProc;

		public int cbClsExtra;

		public int cbWndExtra;

		public nint hInstance;

		public nint hIcon;

		public nint hCursor;

		public nint hbrBackground;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpszMenuName;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string lpszClassName;

		public nint hIconSm;
	}

	private delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

	private Factory2? _factory;

	private readonly List<SharpDX.Direct3D11.Device> _devices = new List<SharpDX.Direct3D11.Device>();

	private const int WS_POPUP = int.MinValue;

	private const int WS_VISIBLE = 268435456;

	private const int WS_EX_NOACTIVATE = 134217728;

	private const int WS_EX_TOOLWINDOW = 128;

	private const int WS_EX_TRANSPARENT = 32;

	private const int SW_SHOWNOACTIVATE = 4;

	private static readonly WndProc s_wndProc = DefWindowProc;

	private static bool s_classRegistered;

	private const string OverlayClass = "PlexusXDxOverlayWindow";

	private readonly List<nint> _windows = new List<nint>();

	public List<OutputTarget> Targets { get; }

	public bool IsAvailable => Targets.Count > 0;

	public DxInitFailureKind LastFailure { get; private set; }

	public string LastFailureMessage { get; private set; } = "";

	public int LastFailureCode { get; private set; }

	private void RecordFailure(SharpDXException ex)
	{
		(DxInitFailureKind Kind, string Short, string Hint) tuple = DxInitFailureMapper.Map(ex);
		DxInitFailureKind item = tuple.Kind;
		string item2 = tuple.Short;
		LastFailure = item;
		LastFailureMessage = item2;
		LastFailureCode = ex?.HResult ?? 0;
	}

	private void RecordFailure(Exception ex)
	{
		(DxInitFailureKind Kind, string Short, string Hint) tuple = DxInitFailureMapper.MapGeneric(ex);
		DxInitFailureKind item = tuple.Kind;
		string item2 = tuple.Short;
		LastFailure = item;
		LastFailureMessage = item2;
		LastFailureCode = (ex?.InnerException as SharpDXException)?.HResult ?? ex?.HResult ?? 0;
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

	[DllImport("user32.dll")]
	private static extern nint DefWindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool DestroyWindow(nint hWnd);

	[DllImport("kernel32.dll")]
	private static extern nint GetModuleHandle(string? lpModuleName);

	public DxDevice()
	{
		Targets = new List<OutputTarget>();
		try
		{
			_factory = new Factory2();
			EnsureWindowClass();
			int num = 0;
			int num2 = 0;
			while (true)
			{
				Adapter1 adapter;
				try
				{
					adapter = _factory.GetAdapter1(num2);
				}
				catch (SharpDXException)
				{
					break;
				}
				num++;
				using (adapter)
				{
					SharpDX.Direct3D11.Device device;
					try
					{
						device = new SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport);
					}
					catch (SharpDXException ex2)
					{
						RecordFailure(ex2);
						goto end_IL_0063;
					}
					catch (Exception ex3)
					{
						RecordFailure(ex3);
						goto end_IL_0063;
					}
					int count = Targets.Count;
					try
					{
						CreateSwapChainsForOutputs(adapter, device);
					}
					catch (SharpDXException ex4)
					{
						RecordFailure(ex4);
					}
					catch (Exception ex5)
					{
						RecordFailure(ex5);
					}
					if (Targets.Count > count)
					{
						_devices.Add(device);
					}
					else
					{
						device.Dispose();
					}
					end_IL_0063:;
				}
				num2++;
			}
			if (num == 0)
			{
				LastFailure = DxInitFailureKind.NoCompatibleAdapter;
				LastFailureMessage = "No GPU with a usable driver was found";
			}
			else if (Targets.Count == 0 && LastFailure == DxInitFailureKind.None)
			{
				LastFailure = DxInitFailureKind.NoOutputs;
				LastFailureMessage = "No usable display output was found";
			}
			if (Targets.Count == 0)
			{
				Dispose();
			}
		}
		catch (SharpDXException ex6)
		{
			RecordFailure(ex6);
			Dispose();
		}
		catch (Exception ex7)
		{
			RecordFailure(ex7);
			Dispose();
		}
	}

	private static void EnsureWindowClass()
	{
		if (!s_classRegistered)
		{
			WNDCLASSEX lpwcx = new WNDCLASSEX
			{
				cbSize = Marshal.SizeOf<WNDCLASSEX>(),
				style = 0,
				lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
				hInstance = GetModuleHandle(null),
				lpszClassName = "PlexusXDxOverlayWindow"
			};
			RegisterClassEx(ref lpwcx);
			s_classRegistered = true;
		}
	}

	private void CreateSwapChainsForOutputs(Adapter1 adapter, SharpDX.Direct3D11.Device device)
	{
		Output[] outputs = adapter.Outputs;
		foreach (Output output in outputs)
		{
			Output1 output2;
			try
			{
				output2 = output.QueryInterface<Output1>();
			}
			catch
			{
				output.Dispose();
				continue;
			}
			RawRectangle desktopBounds = output2.Description.DesktopBounds;
			int num = desktopBounds.Right - desktopBounds.Left;
			int num2 = desktopBounds.Bottom - desktopBounds.Top;
			if (num <= 0 || num2 <= 0)
			{
				output2.Dispose();
				output.Dispose();
				continue;
			}
			nint num3 = CreateWindowEx(134217888, "PlexusXDxOverlayWindow", "PlexusX Overlay", -1879048192, desktopBounds.Left, desktopBounds.Top, num, num2, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
			if (num3 == IntPtr.Zero)
			{
				output2.Dispose();
				output.Dispose();
				continue;
			}
			_windows.Add(num3);
			ShowWindow(num3, 4);
			SwapChainDescription1 description = new SwapChainDescription1
			{
				Width = num,
				Height = num2,
				Format = Format.B8G8R8A8_UNorm,
				Stereo = false,
				SampleDescription = new SampleDescription(1, 0),
				Usage = Usage.RenderTargetOutput,
				BufferCount = 2,
				Scaling = Scaling.Stretch,
				SwapEffect = SwapEffect.FlipSequential,
				AlphaMode = AlphaMode.Ignore,
				Flags = SwapChainFlags.None
			};
			SwapChain1 swapChain = new SwapChain1(_factory, device, num3, ref description);
			using (Texture2D resource = swapChain.GetBackBuffer<Texture2D>(0))
			{
				RenderTargetView rtv = new RenderTargetView(device, resource);
				Targets.Add(new OutputTarget
				{
					SwapChain = swapChain,
					Rtv = rtv,
					Output = output2,
					Device = device,
					Hwnd = num3,
					Width = num,
					Height = num2
				});
			}
			output.Dispose();
		}
	}

	public void Dispose()
	{
		foreach (OutputTarget target in Targets)
		{
			target.Rtv?.Dispose();
			target.SwapChain?.Dispose();
			target.Output?.Dispose();
		}
		Targets.Clear();
		foreach (nint window in _windows)
		{
			if (window != IntPtr.Zero)
			{
				DestroyWindow(window);
			}
		}
		_windows.Clear();
		foreach (SharpDX.Direct3D11.Device device in _devices)
		{
			device.Dispose();
		}
		_devices.Clear();
		_factory?.Dispose();
	}
}
