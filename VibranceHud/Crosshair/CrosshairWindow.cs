using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceHud.Crosshair;

public sealed class CrosshairWindow : Form
{
	private struct POINT
	{
		public int X;

		public int Y;
	}

	private struct SIZE
	{
		public int cx;

		public int cy;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	private struct BLENDFUNCTION
	{
		public byte BlendOp;

		public byte BlendFlags;

		public byte SourceConstantAlpha;

		public byte AlphaFormat;
	}

	private struct BITMAPINFOHEADER
	{
		public uint biSize;

		public int biWidth;

		public int biHeight;

		public ushort biPlanes;

		public ushort biBitCount;

		public uint biCompression;

		public uint biSizeImage;

		public int biXPelsPerMeter;

		public int biYPelsPerMeter;

		public uint biClrUsed;

		public uint biClrImportant;
	}

	private const int WS_EX_LAYERED = 524288;

	private const int WS_EX_TRANSPARENT = 32;

	private const int WS_EX_TOOLWINDOW = 128;

	private const int WS_EX_NOACTIVATE = 134217728;

	private const int WS_EX_TOPMOST = 8;

	private const int ULW_ALPHA = 2;

	private const byte AC_SRC_OVER = 0;

	private const byte AC_SRC_ALPHA = 1;

	private const uint DIB_RGB_COLORS = 0u;

	private const uint BI_RGB = 0u;

	private CrosshairConfig _config = new CrosshairConfig();

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 134742184;
			return createParams;
		}
	}

	protected override bool ShowWithoutActivation => true;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UpdateLayeredWindow(nint hwnd, nint hdcDst, ref POINT pptDst, ref SIZE psize, nint hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

	[DllImport("user32.dll")]
	private static extern nint GetDC(nint hWnd);

	[DllImport("user32.dll")]
	private static extern int ReleaseDC(nint hWnd, nint hDC);

	[DllImport("gdi32.dll")]
	private static extern nint CreateCompatibleDC(nint hDC);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteDC(nint hdc);

	[DllImport("gdi32.dll")]
	private static extern nint SelectObject(nint hdc, nint h);

	[DllImport("gdi32.dll")]
	private static extern bool DeleteObject(nint ho);

	[DllImport("gdi32.dll")]
	private static extern nint CreateDIBSection(nint hdc, ref BITMAPINFOHEADER pbmi, uint iUsage, out nint ppvBits, nint hSection, uint dwOffset);

	[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
	private static extern void CopyMemory(nint dest, nint src, uint count);

	public CrosshairWindow()
	{
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		TopMost = true;
		StartPosition = FormStartPosition.Manual;
		SetStyle(ControlStyles.Opaque, value: true);
	}

	protected override void OnPaintBackground(PaintEventArgs e)
	{
	}

	protected override void OnPaint(PaintEventArgs e)
	{
	}

	public void Apply(CrosshairConfig config)
	{
		_config = config;
		Redraw();
	}

	public void Redraw()
	{
		if (!IsHandleCreated)
		{
			return;
		}
		Size size = CrosshairGeometry.OverlaySize(_config);
		int width = size.Width;
		int height = size.Height;
		Rectangle bounds = Screen.FromPoint(System.Windows.Forms.Cursor.Position).Bounds;
		int left = bounds.Left + (bounds.Width - width) / 2;
		int top = bounds.Top + (bounds.Height - height) / 2;
		using Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.TranslateTransform((float)width / 2f, (float)height / 2f);
			CrosshairRender.Draw(graphics, _config);
		}
		Push(bitmap, left, top);
	}

	private void Push(Bitmap bmp, int left, int top)
	{
		nint dC = GetDC(IntPtr.Zero);
		nint num = CreateCompatibleDC(dC);
		nint num2 = IntPtr.Zero;
		nint h = IntPtr.Zero;
		try
		{
			BITMAPINFOHEADER pbmi = new BITMAPINFOHEADER
			{
				biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
				biWidth = bmp.Width,
				biHeight = -bmp.Height,
				biPlanes = 1,
				biBitCount = 32,
				biCompression = 0u
			};
			num2 = CreateDIBSection(dC, ref pbmi, 0u, out var ppvBits, IntPtr.Zero, 0u);
			if (num2 != IntPtr.Zero && ppvBits != IntPtr.Zero)
			{
				BitmapData bitmapData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
				try
				{
					CopyMemory(ppvBits, bitmapData.Scan0, (uint)(bmp.Width * bmp.Height * 4));
				}
				finally
				{
					bmp.UnlockBits(bitmapData);
				}
				h = SelectObject(num, num2);
				SIZE psize = new SIZE
				{
					cx = bmp.Width,
					cy = bmp.Height
				};
				POINT pptSrc = new POINT
				{
					X = 0,
					Y = 0
				};
				POINT pptDst = new POINT
				{
					X = left,
					Y = top
				};
				BLENDFUNCTION pblend = new BLENDFUNCTION
				{
					BlendOp = 0,
					BlendFlags = 0,
					SourceConstantAlpha = byte.MaxValue,
					AlphaFormat = 1
				};
				UpdateLayeredWindow(Handle, dC, ref pptDst, ref psize, num, ref pptSrc, 0, ref pblend, 2);
			}
		}
		finally
		{
			ReleaseDC(IntPtr.Zero, dC);
			if (num2 != IntPtr.Zero)
			{
				SelectObject(num, h);
				DeleteObject(num2);
			}
			DeleteDC(num);
		}
	}
}
