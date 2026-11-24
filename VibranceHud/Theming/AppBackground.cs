using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Theming;

public static class AppBackground
{
	private readonly record struct CacheKey(int Generation, int Frame, int Dim)
	{
		public static CacheKey None => new CacheKey(-1, -1, -1);
	}

	private readonly record struct FailedBuild(int W, int H, int Generation, int Dim);

	private sealed class Panel
	{
		public int W;

		public int H;

		public CacheKey Of = CacheKey.None;

		public Bitmap? Built;

		public Bitmap? Shown;

		public Bitmap?[] Kept = Array.Empty<Bitmap>();
	}

	private static Bitmap? _source;

	private static AnimatedPicture? _motion;

	private static Bitmap? _cache;

	private static int _cacheW;

	private static int _cacheH;

	private static CacheKey _cacheOf = CacheKey.None;

	public const int MaxBlur = 100;

	private static FailedBuild? _failedWindow;

	private static readonly HashSet<FailedBuild> _failedPanels = new HashSet<FailedBuild>();

	private static FailedBuild? _failedSoft;

	private static int _winW;

	private static int _winH;

	private static readonly object _drawGate = new object();

	private const int MostPanels = 4;

	private static readonly List<Panel> _panels = new List<Panel>();

	private static Bitmap? _sharpShown;

	private static int _decodedIndex = -1;

	internal const long FrameStoreBudgetBytes = 67108864L;

	private static Bitmap?[] _keptSharp = Array.Empty<Bitmap>();

	private static Bitmap?[] _keptSoft = Array.Empty<Bitmap>();

	private static SoftUpscale.Small?[] _keptSharpSmall = Array.Empty<SoftUpscale.Small>();

	private static SoftUpscale.Small?[] _keptSoftSmall = Array.Empty<SoftUpscale.Small>();

	private static CacheKey _keptOf = CacheKey.None;

	private static int _keptW;

	private static int _keptH;

	private static long _keptBytes;

	private static long _keptLittleBytes;

	private static long _keptExactBytes;

	private static Color _matte = BackgroundMatte.Default;

	private static bool _gateOpen;

	private static BackgroundFramePump? _pump;

	private static double _composeMsThisFrame;

	private static bool _frameShown;

	private static double[] _composeMsByFrame = Array.Empty<double>();

	private static int _shownIndex;

	private static Bitmap? _soft;

	private static int _softW;

	private static int _softH;

	private static CacheKey _softOf = CacheKey.None;

	private static int _softComposed;

	private static TextureBrush? _softBrush;

	private static int _softBrushOf = -1;

	private static Bitmap? _softBrushFrom;

	private static Bitmap? _softShown;

	internal static int WindowComposes { get; private set; }

	internal static int PanelComposes { get; private set; }

	internal static int WindowScales { get; private set; }

	private static CacheKey NowKey => new CacheKey(Generation, FrameSerial, EffectiveDim);

	public static int Dim { get; private set; }

	public static int Blur { get; private set; }

	public static bool IsSet => _source != null;

	public static int EffectiveDim
	{
		get
		{
			if (!Theme.IsSeeThrough)
			{
				return Dim;
			}
			return 0;
		}
	}

	internal static Action<string>? FailPointForTest { get; set; }

	internal static int FrameDecodes { get; private set; }

	private static int SharpDivisor => 1 + Blur / 6;

	private static int SoftDivisor => Math.Max(2, 8);

	private static bool SharpFromSmall
	{
		get
		{
			if (_motion != null && Blur > 0)
			{
				return SharpDivisor >= 2;
			}
			return false;
		}
	}

	private static bool SoftIsSharp
	{
		get
		{
			if (SharpFromSmall)
			{
				return SharpDivisor >= SoftDivisor;
			}
			return false;
		}
	}

	internal static long StoredFrameBytes
	{
		get
		{
			lock (_drawGate)
			{
				return _keptBytes;
			}
		}
	}

	public static Color Matte => _matte;

	public static bool NeedsMatte { get; private set; }

	public static bool HasMotion => _motion != null;

	public static int FrameCount => _motion?.Count ?? 1;

	public static int FrameIndex => _motion?.Index ?? 0;

	public static double CurrentFrameSeconds => _motion?.CurrentSeconds ?? 0.0;

	public static double SecondsUntilNextFrame => _motion?.SecondsUntilNext ?? 0.0;

	public static bool MotionAllowed => _gateOpen;

	public static Func<bool> GameIsRunning { get; set; } = () => false;

	public static bool ReducedMotion { get; set; }

	internal static bool MotionClockTicking => _pump?.Ticking ?? false;

	internal static int MotionClockIntervalMs => _pump?.IntervalMs ?? 0;

	public static int Generation { get; private set; }

	internal static int FrameSerial { get; private set; }

	public static double LastComposeMs { get; private set; }

	public static double ComposeMsPerFrame
	{
		get
		{
			lock (_drawGate)
			{
				double num = 0.0;
				int num2 = 0;
				double[] composeMsByFrame = _composeMsByFrame;
				foreach (double num3 in composeMsByFrame)
				{
					if (!double.IsNaN(num3))
					{
						num += num3;
						num2++;
					}
				}
				return (num2 == 0) ? LastComposeMs : (num / (double)num2);
			}
		}
	}

	internal static int SoftBrushBuilds { get; private set; }

	internal static int WindowSoftens { get; private set; }

	public static Rectangle WindowBoundsForSampling => new Rectangle(0, 0, _winW, _winH);

	public static bool Load(string path, int dim, int blur = 0)
	{
		AnimatedPicture animatedPicture = null;
		Bitmap bitmap = null;
		bool needsMatte;
		try
		{
			byte[] array = File.ReadAllBytes(path);
			using (MemoryStream stream = new MemoryStream(array, writable: false))
			{
				using Image image = Image.FromStream(stream);
				needsMatte = BackgroundMatte.NeedsOne(image);
			}
			animatedPicture = AnimatedPicture.From(array);
			FailPointForTest?.Invoke("build");
			if (animatedPicture != null)
			{
				bitmap = new Bitmap(animatedPicture.Current);
			}
			else
			{
				using MemoryStream stream2 = new MemoryStream(array, writable: false);
				using Image original = Image.FromStream(stream2);
				bitmap = new Bitmap(original);
			}
		}
		catch
		{
			bitmap?.Dispose();
			animatedPicture?.Dispose();
			return false;
		}
		Bitmap source;
		AnimatedPicture motion;
		lock (_drawGate)
		{
			source = _source;
			motion = _motion;
			_source = bitmap;
			_motion = animatedPicture;
			_decodedIndex = -1;
			NeedsMatte = needsMatte;
			Dim = Math.Clamp(dim, 0, 80);
			Blur = Math.Clamp(blur, 0, 100);
		}
		try
		{
			ForgetComposeCost();
			InvalidateCache();
			ReassessMotion();
		}
		finally
		{
			source?.Dispose();
			motion?.Dispose();
		}
		return true;
	}

	private static FailedBuild FailedOf(int w, int h, CacheKey key)
	{
		return new FailedBuild(w, h, key.Generation, key.Dim);
	}

	private static void ForgetFailedBuildsLocked()
	{
		_failedWindow = null;
		_failedSoft = null;
		_failedPanels.Clear();
	}

	private static Bitmap? SoftLocked(int w, int h)
	{
		if (CachedLocked(w, h) == null)
		{
			return null;
		}
		CacheKey nowKey = NowKey;
		if (_softShown != null && _softW == w && _softH == h && _softOf == nowKey)
		{
			return _softShown;
		}
		FailedBuild value = FailedOf(w, h, nowKey);
		if (_failedSoft == value)
		{
			return null;
		}
		try
		{
			FailPointForTest?.Invoke("soft");
			return SoftComposeLocked(w, h);
		}
		catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
		{
			_failedSoft = value;
			DropSoftBitmapLocked();
			PaintGuard.LogOnce(typeof(AppBackground), ex);
			return null;
		}
	}

	private static void DropSoftBitmapLocked()
	{
		FreeSoftBrushLocked();
		_soft?.Dispose();
		_soft = null;
		_softShown = null;
		_softW = (_softH = 0);
		_softOf = CacheKey.None;
	}

	private static void DropWindowBitmapLocked()
	{
		Bitmap cache = _cache;
		_cache?.Dispose();
		_cache = null;
		_cacheW = (_cacheH = 0);
		_cacheOf = CacheKey.None;
		if (cache != null && _sharpShown == cache)
		{
			_sharpShown = null;
		}
		if (cache != null && _softShown == cache)
		{
			_softShown = null;
			_softOf = CacheKey.None;
		}
	}

	public static void SetDim(int dim)
	{
		int num = Math.Clamp(dim, 0, 80);
		if (num != Dim)
		{
			Dim = num;
			InvalidateCache();
		}
	}

	public static void SetBlur(int blur)
	{
		int num = Math.Clamp(blur, 0, 100);
		if (num != Blur)
		{
			Blur = num;
			InvalidateCache();
		}
	}

	public static void Clear()
	{
		Bitmap source;
		AnimatedPicture motion;
		lock (_drawGate)
		{
			source = _source;
			motion = _motion;
			_source = null;
			_motion = null;
			_decodedIndex = -1;
			NeedsMatte = false;
			_matte = BackgroundMatte.Default;
			_gateOpen = false;
		}
		ForgetComposeCost();
		InvalidateCache();
		FreeCachedBitmaps();
		ReassessMotion();
		source?.Dispose();
		motion?.Dispose();
	}

	public static Color[] SamplePixels(int side = 64)
	{
		if (_source == null)
		{
			return Array.Empty<Color>();
		}
		using Bitmap bitmap = new Bitmap(side, side);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
			graphics.DrawImage(_source, 0, 0, side, side);
		}
		Color[] array = new Color[side * side];
		int i = 0;
		int num = 0;
		for (; i < side; i++)
		{
			for (int j = 0; j < side; j++)
			{
				array[num++] = bitmap.GetPixel(j, i);
			}
		}
		return array;
	}

	public static void Resize(int w, int h)
	{
		int num = Math.Max(1, w);
		int num2 = Math.Max(1, h);
		if (num == _winW && num2 == _winH)
		{
			return;
		}
		_winW = num;
		_winH = num2;
		lock (_drawGate)
		{
			ForgetFailedBuildsLocked();
		}
	}

	public static void Paint(Graphics g, int offsetX, int offsetY)
	{
		lock (_drawGate)
		{
			PaintLocked(g, offsetX, offsetY);
		}
	}

	private static void PaintLocked(Graphics g, int offsetX, int offsetY)
	{
		Bitmap bitmap = Cached(_winW, _winH);
		if (bitmap != null)
		{
			CopyOpaque(g, bitmap, -offsetX, -offsetY);
		}
	}

	private static void CopyOpaque(Graphics g, Bitmap bmp, int x, int y)
	{
		CompositingMode compositingMode = g.CompositingMode;
		g.CompositingMode = CompositingMode.SourceCopy;
		try
		{
			g.DrawImageUnscaled(bmp, x, y);
		}
		finally
		{
			g.CompositingMode = compositingMode;
		}
	}

	public static void PaintInto(Graphics g, int w, int h)
	{
		lock (_drawGate)
		{
			Bitmap bitmap = PanelCachedLocked(w, h);
			if (bitmap != null)
			{
				CopyOpaque(g, bitmap, 0, 0);
			}
		}
	}

	private static Panel PanelFor(int w, int h)
	{
		foreach (Panel panel3 in _panels)
		{
			if (panel3.W == w && panel3.H == h)
			{
				return panel3;
			}
		}
		if (_panels.Count >= 4)
		{
			Panel panel = _panels[0];
			_panels.RemoveAt(0);
			ForgetKeptPanelLocked(panel);
			panel.Built?.Dispose();
		}
		Panel panel2 = new Panel
		{
			W = w,
			H = h
		};
		_panels.Add(panel2);
		return panel2;
	}

	private static Bitmap? PanelCachedLocked(int w, int h)
	{
		if (w <= 0 || h <= 0 || _source == null)
		{
			return null;
		}
		CacheKey nowKey = NowKey;
		Panel panel = PanelFor(w, h);
		if (panel.Shown != null && panel.Of == nowKey)
		{
			return panel.Shown;
		}
		Bitmap bitmap = KeptPanelLocked(panel);
		if (bitmap != null)
		{
			panel.Shown = bitmap;
			panel.Of = nowKey;
			return bitmap;
		}
		Image image = Frame();
		if (image == null)
		{
			return null;
		}
		FailedBuild item = FailedOf(w, h, nowKey);
		if (_failedPanels.Contains(item))
		{
			return null;
		}
		try
		{
			FailPointForTest?.Invoke("build");
			Stopwatch spent = Stopwatch.StartNew();
			panel.Built = Build(panel.Built, image, w, h);
			ChargeCompose(spent);
			PanelComposes++;
			panel.Shown = panel.Built;
			panel.Of = nowKey;
			KeepPanelLocked(panel);
			return panel.Built;
		}
		catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
		{
			if (_failedPanels.Count >= 16)
			{
				_failedPanels.Clear();
			}
			_failedPanels.Add(item);
			panel.Built?.Dispose();
			panel.Built = null;
			panel.Shown = null;
			panel.Of = CacheKey.None;
			PaintGuard.LogOnce(typeof(AppBackground), ex);
			return null;
		}
	}

	private static Bitmap? Cached(int w, int h)
	{
		lock (_drawGate)
		{
			return CachedLocked(w, h);
		}
	}

	private static Bitmap? CachedLocked(int w, int h)
	{
		if (w <= 0 || h <= 0)
		{
			return null;
		}
		CacheKey nowKey = NowKey;
		if (_sharpShown != null && _cacheW == w && _cacheH == h && _cacheOf == nowKey)
		{
			NoteFrameShownLocked();
			return _sharpShown;
		}
		Bitmap bitmap = KeptSharpLocked(w, h);
		if (bitmap != null)
		{
			_sharpShown = bitmap;
			_cacheW = w;
			_cacheH = h;
			_cacheOf = nowKey;
			NoteFrameShownLocked();
			return bitmap;
		}
		if (_source == null)
		{
			return null;
		}
		FailedBuild value = FailedOf(w, h, nowKey);
		if (_failedWindow == value)
		{
			return null;
		}
		try
		{
			FailPointForTest?.Invoke("build");
			Stopwatch spent = Stopwatch.StartNew();
			bool sharpFromSmall = SharpFromSmall;
			if (sharpFromSmall)
			{
				SoftUpscale.Small small = SharpSmallLocked(w, h);
				if (small == null)
				{
					return null;
				}
				_cache = Reuse(_cache, w, h);
				SoftUpscale.CoverInto(small, CoverOf(w, h), _cache);
			}
			else
			{
				Image image = Frame();
				if (image == null)
				{
					return null;
				}
				_cache = Build(_cache, image, w, h);
				WindowScales++;
			}
			ChargeCompose(spent);
			WindowComposes++;
			_sharpShown = _cache;
			_cacheW = w;
			_cacheH = h;
			_cacheOf = nowKey;
			NoteFrameShownLocked();
			KeepSharpLocked(_cache, w, h, sharpFromSmall);
			return _cache;
		}
		catch (Exception ex) when (PaintGuard.IsGdiFailure(ex))
		{
			_failedWindow = value;
			DropWindowBitmapLocked();
			PaintGuard.LogOnce(typeof(AppBackground), ex);
			return null;
		}
	}

	private static Image? Frame()
	{
		if (_motion == null)
		{
			return _source;
		}
		if (_motion.Index != _decodedIndex)
		{
			FrameDecodes++;
			_decodedIndex = _motion.Index;
		}
		return _motion.Current;
	}

	private static Bitmap Reuse(Bitmap? existing, int w, int h, PixelFormat format = PixelFormat.Format32bppPArgb)
	{
		if (existing != null && existing.Width == w && existing.Height == h && existing.PixelFormat == format)
		{
			return existing;
		}
		existing?.Dispose();
		return new Bitmap(w, h, format);
	}

	private static Bitmap Build(Bitmap? into, Image source, int w, int h)
	{
		Bitmap bitmap = Reuse(into, w, h);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			using (SolidBrush brush = new SolidBrush(Matte))
			{
				graphics.FillRectangle(brush, 0, 0, w, h);
			}
			bool flag = _motion != null;
			graphics.InterpolationMode = (flag ? InterpolationMode.HighQualityBilinear : InterpolationMode.HighQualityBicubic);
			float num = Math.Max((float)w / (float)source.Width, (float)h / (float)source.Height);
			int num2 = (int)Math.Ceiling((float)source.Width * num);
			int num3 = (int)Math.Ceiling((float)source.Height * num);
			if (Blur > 0)
			{
				int num4 = 1 + Blur / 6;
				int width = Math.Max(2, num2 / num4);
				int height = Math.Max(2, num3 / num4);
				using Bitmap image = new Bitmap(width, height);
				using (Graphics graphics2 = Graphics.FromImage(image))
				{
					graphics2.InterpolationMode = InterpolationMode.HighQualityBilinear;
					graphics2.DrawImage(source, 0, 0, width, height);
				}
				graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.DrawImage(image, (w - num2) / 2, (h - num3) / 2, num2, num3);
			}
			else
			{
				graphics.DrawImage(source, (w - num2) / 2, (h - num3) / 2, num2, num3);
			}
			int effectiveDim = EffectiveDim;
			if (effectiveDim > 0)
			{
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb(effectiveDim * 255 / 100, 0, 0, 0));
				graphics.FillRectangle(brush2, 0, 0, w, h);
			}
		}
		return bitmap;
	}

	private static void InvalidateCache()
	{
		lock (_drawGate)
		{
			Generation++;
			ForgetFailedBuildsLocked();
			_cacheOf = CacheKey.None;
			foreach (Panel panel in _panels)
			{
				panel.Of = CacheKey.None;
			}
			_softOf = CacheKey.None;
			ForgetKeptFramesLocked();
			ForgetFrameCostsLocked();
		}
	}

	private static void FreeCachedBitmaps()
	{
		lock (_drawGate)
		{
			ForgetKeptFramesLocked();
			ForgetFailedBuildsLocked();
			_cache?.Dispose();
			_cache = null;
			_cacheW = (_cacheH = 0);
			foreach (Panel panel in _panels)
			{
				panel.Built?.Dispose();
			}
			_panels.Clear();
			_soft?.Dispose();
			_soft = null;
			_softW = (_softH = 0);
			_sharpShown = null;
			_softShown = null;
			FreeSoftBrushLocked();
		}
	}

	private static Rectangle CoverOf(int w, int h)
	{
		Size size = _source?.Size ?? new Size(1, 1);
		float num = Math.Max((float)w / (float)size.Width, (float)h / (float)size.Height);
		int num2 = (int)Math.Ceiling((float)size.Width * num);
		int num3 = (int)Math.Ceiling((float)size.Height * num);
		return new Rectangle((w - num2) / 2, (h - num3) / 2, num2, num3);
	}

	private static Size SmallSize(Rectangle cover, int divisor)
	{
		return new Size(Math.Max(2, cover.Width / divisor), Math.Max(2, cover.Height / divisor));
	}

	private static long SmallBytesPerFrame(int w, int h)
	{
		if (_motion == null)
		{
			return 0L;
		}
		Rectangle cover = CoverOf(w, h);
		long num = 0L;
		if (SharpFromSmall)
		{
			Size size = SmallSize(cover, SharpDivisor);
			num += (long)size.Width * (long)size.Height * 4;
		}
		if (!SoftIsSharp)
		{
			Size size2 = SmallSize(cover, SoftDivisor);
			num += (long)size2.Width * (long)size2.Height * 4;
		}
		return num;
	}

	private static SoftUpscale.Small? SharpSmallLocked(int w, int h)
	{
		SoftUpscale.Small small = KeptSharpSmallLocked(w, h);
		if (small != null)
		{
			return small;
		}
		Image image = Frame();
		if (image == null)
		{
			return null;
		}
		SoftUpscale.Small small2 = SmallOfFrame(image, SmallSize(CoverOf(w, h), SharpDivisor), soft: false);
		KeepSmallLocked(soft: false, small2, w, h);
		return small2;
	}

	private static SoftUpscale.Small? SoftSmallLocked(int w, int h)
	{
		SoftUpscale.Small small = KeptSoftSmallLocked(w, h);
		if (small != null)
		{
			return small;
		}
		Size size = SmallSize(CoverOf(w, h), SoftDivisor);
		SoftUpscale.Small small3;
		if (SharpFromSmall)
		{
			SoftUpscale.Small small2 = SharpSmallLocked(w, h);
			if (small2 == null)
			{
				return null;
			}
			using Bitmap picture = ToBitmap(small2);
			small3 = SmallOf(picture, size, soft: true);
		}
		else
		{
			Image image = Frame();
			if (image == null)
			{
				return null;
			}
			small3 = SmallOfFrame(image, size, soft: true);
		}
		KeepSmallLocked(soft: true, small3, w, h);
		return small3;
	}

	private static SoftUpscale.Small SmallOfFrame(Image frame, Size size, bool soft)
	{
		using Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			using (SolidBrush brush = new SolidBrush(Matte))
			{
				graphics.FillRectangle(brush, 0, 0, size.Width, size.Height);
			}
			Shrink(graphics, frame, size, soft);
			int effectiveDim = EffectiveDim;
			if (effectiveDim > 0)
			{
				using SolidBrush brush2 = new SolidBrush(Color.FromArgb(effectiveDim * 255 / 100, 0, 0, 0));
				graphics.FillRectangle(brush2, 0, 0, size.Width, size.Height);
			}
		}
		return SoftUpscale.Small.From(bitmap);
	}

	private static SoftUpscale.Small SmallOf(Bitmap picture, Size size, bool soft)
	{
		using Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.CompositingMode = CompositingMode.SourceCopy;
			Shrink(graphics, picture, size, soft);
		}
		return SoftUpscale.Small.From(bitmap);
	}

	private static void Shrink(Graphics g, Image from, Size size, bool soft)
	{
		g.InterpolationMode = InterpolationMode.HighQualityBilinear;
		if (!soft)
		{
			g.DrawImage(from, 0, 0, size.Width, size.Height);
			return;
		}
		g.PixelOffsetMode = PixelOffsetMode.HighQuality;
		using ImageAttributes imageAttributes = new ImageAttributes();
		imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
		g.DrawImage(from, new Rectangle(0, 0, size.Width, size.Height), 0, 0, from.Width, from.Height, GraphicsUnit.Pixel, imageAttributes);
	}

	private static Bitmap ToBitmap(SoftUpscale.Small small)
	{
		Bitmap bitmap = new Bitmap(small.Width, small.Height, PixelFormat.Format32bppPArgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, small.Width, small.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
		try
		{
			for (int i = 0; i < small.Height; i++)
			{
				Marshal.Copy(small.Pixels, i * small.Width, bitmapData.Scan0 + i * bitmapData.Stride, small.Width);
			}
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
		return bitmap;
	}

	internal static void ForgetKeptFrames()
	{
		lock (_drawGate)
		{
			ForgetKeptFramesLocked();
		}
	}

	private static void ForgetKeptFramesLocked()
	{
		if (_keptBytes == 0L && _keptSharp.Length == 0)
		{
			return;
		}
		ForgetFrameCostsLocked();
		Bitmap[][] array = new Bitmap[2][] { _keptSharp, _keptSoft };
		foreach (Bitmap[] array2 in array)
		{
			for (int j = 0; j < array2.Length; j++)
			{
				Bitmap bitmap = array2[j];
				if (bitmap != null)
				{
					if (bitmap == _sharpShown)
					{
						_sharpShown = null;
						_cacheOf = CacheKey.None;
					}
					if (bitmap == _softShown)
					{
						_softShown = null;
						_softOf = CacheKey.None;
					}
					if (bitmap == _softBrushFrom)
					{
						FreeSoftBrushLocked();
					}
					bitmap.Dispose();
					array2[j] = null;
				}
			}
		}
		_keptSharp = Array.Empty<Bitmap>();
		_keptSoft = Array.Empty<Bitmap>();
		_keptSharpSmall = Array.Empty<SoftUpscale.Small>();
		_keptSoftSmall = Array.Empty<SoftUpscale.Small>();
		foreach (Panel panel in _panels)
		{
			ForgetKeptPanelLocked(panel);
		}
		_keptOf = CacheKey.None;
		_keptW = (_keptH = 0);
		_keptBytes = 0L;
		_keptLittleBytes = 0L;
		_keptExactBytes = 0L;
	}

	private static bool StoreReadyLocked(int w, int h)
	{
		if (_motion == null)
		{
			return false;
		}
		CacheKey cacheKey = new CacheKey(Generation, -1, EffectiveDim);
		if (_keptOf == cacheKey && _keptW == w && _keptH == h && _keptSharp.Length == _motion.Count)
		{
			return true;
		}
		ForgetKeptFramesLocked();
		_keptSharp = new Bitmap[_motion.Count];
		_keptSoft = new Bitmap[_motion.Count];
		_keptSharpSmall = new SoftUpscale.Small[_motion.Count];
		_keptSoftSmall = new SoftUpscale.Small[_motion.Count];
		_keptOf = cacheKey;
		_keptW = w;
		_keptH = h;
		return true;
	}

	private static Bitmap? KeptSharpLocked(int w, int h)
	{
		if (!StoreReadyLocked(w, h))
		{
			return null;
		}
		return _keptSharp[_motion.Index];
	}

	private static Bitmap? KeptSoftLocked(int w, int h)
	{
		if (!StoreReadyLocked(w, h))
		{
			return null;
		}
		return _keptSoft[_motion.Index];
	}

	private static Bitmap? KeptPanelLocked(Panel panel)
	{
		if (_motion == null || _winW <= 0 || _winH <= 0 || !StoreReadyLocked(_winW, _winH))
		{
			return null;
		}
		if (panel.Kept.Length != _motion.Count)
		{
			return null;
		}
		return panel.Kept[_motion.Index];
	}

	private static void KeepPanelLocked(Panel panel)
	{
		if (!_gateOpen || _motion == null || _winW <= 0 || _winH <= 0 || !StoreReadyLocked(_winW, _winH))
		{
			return;
		}
		if (panel.Kept.Length != _motion.Count)
		{
			panel.Kept = new Bitmap[_motion.Count];
		}
		int index = _motion.Index;
		if (panel.Kept[index] != null || panel.Built == null)
		{
			return;
		}
		long num = (long)panel.W * (long)panel.H * 4;
		if (_keptBytes + num <= 67108864)
		{
			Bitmap bitmap = new Bitmap(panel.W, panel.H, PixelFormat.Format32bppPArgb);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.DrawImageUnscaled(panel.Built, 0, 0);
			}
			panel.Kept[index] = bitmap;
			_keptBytes += num;
			_keptLittleBytes += num;
		}
	}

	private static void ForgetKeptPanelLocked(Panel panel)
	{
		for (int i = 0; i < panel.Kept.Length; i++)
		{
			Bitmap bitmap = panel.Kept[i];
			if (bitmap != null)
			{
				if (panel.Shown == bitmap)
				{
					panel.Shown = null;
					panel.Of = CacheKey.None;
				}
				long num = (long)bitmap.Width * (long)bitmap.Height * 4;
				_keptBytes = Math.Max(0L, _keptBytes - num);
				_keptLittleBytes = Math.Max(0L, _keptLittleBytes - num);
				bitmap.Dispose();
			}
		}
		panel.Kept = Array.Empty<Bitmap>();
	}

	private static SoftUpscale.Small? KeptSharpSmallLocked(int w, int h)
	{
		if (!StoreReadyLocked(w, h))
		{
			return null;
		}
		return _keptSharpSmall[_motion.Index];
	}

	private static SoftUpscale.Small? KeptSoftSmallLocked(int w, int h)
	{
		if (!StoreReadyLocked(w, h))
		{
			return null;
		}
		return _keptSoftSmall[_motion.Index];
	}

	private static void KeepSmallLocked(bool soft, SoftUpscale.Small small, int w, int h)
	{
		if (_gateOpen && StoreReadyLocked(w, h))
		{
			SoftUpscale.Small[] array = (soft ? _keptSoftSmall : _keptSharpSmall);
			int index = _motion.Index;
			if (array[index] == null && _keptBytes + small.Bytes <= 67108864)
			{
				array[index] = small;
				_keptBytes += small.Bytes;
				_keptLittleBytes += small.Bytes;
			}
		}
	}

	private static void KeepSharpLocked(Bitmap composed, int w, int h, bool stretched = false)
	{
		Keep(soft: false, composed, w, h, stretched);
	}

	private static void KeepSoftLocked(Bitmap composed, int w, int h, bool stretched = false)
	{
		Keep(soft: true, composed, w, h, stretched);
	}

	private static long ExactRoomOwedLocked(int w, int h)
	{
		if (SharpFromSmall)
		{
			return 0L;
		}
		long num = (long)(_motion?.Count ?? 0) * (long)w * h * 4;
		return Math.Max(0L, num - _keptExactBytes);
	}

	private static long LittleBytesPerFrameLocked(int w, int h)
	{
		long num = SmallBytesPerFrame(w, h);
		foreach (Panel panel in _panels)
		{
			num += (long)panel.W * (long)panel.H * 4;
		}
		return num;
	}

	private static long LittleReserveLocked(int w, int h)
	{
		return Math.Min(16777216L, (_motion?.Count ?? 0) * LittleBytesPerFrameLocked(w, h));
	}

	private static void Keep(bool soft, Bitmap composed, int w, int h, bool stretched)
	{
		if (!_gateOpen || !StoreReadyLocked(w, h))
		{
			return;
		}
		Bitmap[] array = (soft ? _keptSoft : _keptSharp);
		int index = _motion.Index;
		if (array[index] != null)
		{
			return;
		}
		long num = (long)w * (long)h * 4;
		long num2 = _keptBytes - _keptLittleBytes;
		long num3 = 67108864 - LittleReserveLocked(w, h);
		if (stretched)
		{
			num3 -= ExactRoomOwedLocked(w, h);
		}
		if (num2 + num <= num3 && _keptBytes + num <= 67108864)
		{
			Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format32bppPArgb);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.CompositingMode = CompositingMode.SourceCopy;
				graphics.DrawImageUnscaled(composed, 0, 0);
			}
			array[index] = bitmap;
			_keptBytes += num;
			if (!stretched)
			{
				_keptExactBytes += num;
			}
		}
	}

	public static void SetMatte(Color colour)
	{
		Color matte = Color.FromArgb(255, colour.R, colour.G, colour.B);
		if (matte.ToArgb() != _matte.ToArgb())
		{
			_matte = matte;
			InvalidateCache();
		}
	}

	public static void SetMotionGate(bool mayPlay)
	{
		if (_gateOpen != mayPlay)
		{
			_gateOpen = mayPlay;
			if (!mayPlay)
			{
				_motion?.DropBankedTime();
			}
		}
	}

	public static bool Advance(double seconds)
	{
		if (!_gateOpen || _motion == null)
		{
			return false;
		}
		if (!_motion.Advance(seconds))
		{
			return false;
		}
		MarkNewFrame();
		return true;
	}

	public static void DriveMotionFrom(Form host, Action onPictureChanged)
	{
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		if (onPictureChanged == null)
		{
			throw new ArgumentNullException("onPictureChanged");
		}
		StopDrivingMotion();
		_pump = new BackgroundFramePump(host, onPictureChanged);
	}

	public static void StopDrivingMotion()
	{
		BackgroundFramePump? pump = _pump;
		_pump = null;
		pump?.Dispose();
	}

	internal static void ReassessMotion()
	{
		_pump?.Reassess();
	}

	internal static void PumpOnceForTests()
	{
		_pump?.Tick();
	}

	private static void ChargeCompose(Stopwatch spent)
	{
		spent.Stop();
		_composeMsThisFrame += spent.Elapsed.TotalMilliseconds;
	}

	private static void ForgetFrameCostsLocked()
	{
		int num = _motion?.Count ?? 1;
		if (_composeMsByFrame.Length != num)
		{
			_composeMsByFrame = new double[num];
		}
		Array.Fill(_composeMsByFrame, double.NaN);
	}

	private static void NoteFrameShownLocked()
	{
		_frameShown = true;
		_shownIndex = _motion?.Index ?? 0;
	}

	internal static void ShowFrameCostingForTests(double ms)
	{
		lock (_drawGate)
		{
			_composeMsThisFrame += ms;
			NoteFrameShownLocked();
		}
	}

	private static void ForgetComposeCost()
	{
		lock (_drawGate)
		{
			LastComposeMs = 0.0;
			_composeMsThisFrame = 0.0;
			ForgetFrameCostsLocked();
		}
	}

	private static void MarkNewFrame()
	{
		lock (_drawGate)
		{
			FrameSerial++;
			if (_frameShown)
			{
				LastComposeMs = _composeMsThisFrame;
				if (_shownIndex < _composeMsByFrame.Length)
				{
					_composeMsByFrame[_shownIndex] = _composeMsThisFrame;
				}
			}
			_composeMsThisFrame = 0.0;
			_frameShown = false;
		}
	}

	private static TextureBrush SoftBrushLocked(Bitmap soft)
	{
		if (_softBrush != null && _softBrushFrom == soft && _softBrushOf == _softComposed)
		{
			return _softBrush;
		}
		_softBrush?.Dispose();
		_softBrush = new TextureBrush(soft)
		{
			WrapMode = WrapMode.Clamp
		};
		_softBrushFrom = soft;
		_softBrushOf = _softComposed;
		SoftBrushBuilds++;
		return _softBrush;
	}

	private static void FreeSoftBrushLocked()
	{
		_softBrush?.Dispose();
		_softBrush = null;
		_softBrushFrom = null;
		_softBrushOf = -1;
	}

	public static void FillWithBlurred(Graphics g, GraphicsPath path, int offsetX, int offsetY)
	{
		lock (_drawGate)
		{
			Bitmap bitmap = SoftLocked(_winW, _winH);
			if (bitmap != null)
			{
				TextureBrush textureBrush = SoftBrushLocked(bitmap);
				textureBrush.ResetTransform();
				textureBrush.TranslateTransform(-offsetX, -offsetY);
				g.FillPath(textureBrush, path);
			}
		}
	}

	internal static void FillWithBlurred(Graphics g, RectangleF insidePanel, int offsetX, int offsetY)
	{
		lock (_drawGate)
		{
			Bitmap bitmap = SoftLocked(_winW, _winH);
			if (bitmap != null)
			{
				TextureBrush textureBrush = SoftBrushLocked(bitmap);
				textureBrush.ResetTransform();
				textureBrush.TranslateTransform(-offsetX, -offsetY);
				g.FillRectangle(textureBrush, insidePanel);
			}
		}
	}

	public static void PaintBlurred(Graphics g, int offsetX, int offsetY)
	{
		lock (_drawGate)
		{
			Bitmap bitmap = SoftLocked(_winW, _winH);
			if (bitmap != null)
			{
				g.DrawImageUnscaled(bitmap, -offsetX, -offsetY);
			}
		}
	}

	private static Bitmap? SoftComposeLocked(int w, int h)
	{
		Bitmap bitmap = CachedLocked(w, h);
		if (bitmap == null)
		{
			return null;
		}
		CacheKey nowKey = NowKey;
		if (_softShown != null && _softW == w && _softH == h && _softOf == nowKey)
		{
			return _softShown;
		}
		if (SoftIsSharp)
		{
			_softShown = bitmap;
			_softComposed++;
			_softW = w;
			_softH = h;
			_softOf = nowKey;
			return bitmap;
		}
		Bitmap bitmap2 = KeptSoftLocked(w, h);
		if (bitmap2 != null)
		{
			_softShown = bitmap2;
			_softW = w;
			_softH = h;
			_softOf = nowKey;
			return bitmap2;
		}
		Stopwatch spent = Stopwatch.StartNew();
		bool flag = _motion != null;
		if (flag)
		{
			SoftUpscale.Small small = SoftSmallLocked(w, h);
			if (small == null)
			{
				return null;
			}
			_soft = Reuse(_soft, w, h);
			SoftUpscale.CoverInto(small, CoverOf(w, h), _soft);
		}
		else
		{
			_soft = Soften(_soft, bitmap, 16);
			WindowSoftens++;
		}
		_softComposed++;
		ChargeCompose(spent);
		WindowComposes++;
		_softShown = _soft;
		_softW = w;
		_softH = h;
		_softOf = nowKey;
		KeepSoftLocked(_soft, w, h, flag);
		return _soft;
	}

	private static Bitmap Soften(Bitmap? into, Bitmap sharp, int radius)
	{
		int num = Math.Max(2, radius / 2);
		int num2 = Math.Max(2, sharp.Width / num);
		int num3 = Math.Max(2, sharp.Height / num);
		Bitmap bitmap = Reuse(into, sharp.Width, sharp.Height, PixelFormat.Format32bppArgb);
		using ImageAttributes imageAttributes = new ImageAttributes();
		imageAttributes.SetWrapMode(WrapMode.TileFlipXY);
		using (Bitmap image = new Bitmap(num2, num3))
		{
			using (Graphics graphics = Graphics.FromImage(image))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.DrawImage(sharp, new Rectangle(0, 0, num2, num3), 0, 0, sharp.Width, sharp.Height, GraphicsUnit.Pixel, imageAttributes);
			}
			using Graphics graphics2 = Graphics.FromImage(bitmap);
			graphics2.InterpolationMode = InterpolationMode.HighQualityBilinear;
			graphics2.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics2.DrawImage(image, new Rectangle(0, 0, sharp.Width, sharp.Height), 0, 0, num2, num3, GraphicsUnit.Pixel, imageAttributes);
		}
		return bitmap;
	}

	public static Color[] SampleBehind(Rectangle windowRect, bool blurred)
	{
		lock (_drawGate)
		{
			Bitmap bitmap = (blurred ? SoftLocked(_winW, _winH) : CachedLocked(_winW, _winH));
			if (bitmap == null)
			{
				return Array.Empty<Color>();
			}
			Rectangle rectangle = Rectangle.Intersect(windowRect, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
			if (rectangle.Width <= 0 || rectangle.Height <= 0)
			{
				return Array.Empty<Color>();
			}
			Color[] array = new Color[576];
			int i = 0;
			int num = 0;
			for (; i < 24; i++)
			{
				int num2 = 0;
				while (num2 < 24)
				{
					int value = rectangle.Left + (int)(((double)num2 + 0.5) * (double)rectangle.Width / 24.0);
					int value2 = rectangle.Top + (int)(((double)i + 0.5) * (double)rectangle.Height / 24.0);
					array[num] = bitmap.GetPixel(Math.Clamp(value, rectangle.Left, rectangle.Right - 1), Math.Clamp(value2, rectangle.Top, rectangle.Bottom - 1));
					num2++;
					num++;
				}
			}
			return array;
		}
	}
}
