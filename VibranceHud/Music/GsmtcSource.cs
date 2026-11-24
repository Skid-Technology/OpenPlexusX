using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace VibranceHud.Music;

internal sealed class GsmtcSource : INowPlayingSource
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetPointer(nint self, out nint value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetInt(nint self, out int value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int GetBool(nint self, out byte value);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate int NoArgs(nint self);

	private sealed class Com : IDisposable
	{
		internal nint Ptr;

		internal Com(nint ptr)
		{
			Ptr = ptr;
		}

		internal Com As(Guid iid)
		{
			_cancellation.ThrowIfCancellationRequested();
			Check(Marshal.QueryInterface(Ptr, in iid, out var ppv));
			return new Com(ppv);
		}

		public void Dispose()
		{
			if (Ptr != IntPtr.Zero)
			{
				Marshal.Release(Ptr);
				Ptr = IntPtr.Zero;
			}
		}
	}

	[ThreadStatic]
	private static CancellationToken _cancellation;

	private static readonly Guid Statics = new Guid("2050C4EE-11A0-57DE-AED7-C97C70338245");

	private static readonly Guid Manager = new Guid("CACE8EAC-E86E-504A-AB31-5FF8FF1BCE49");

	private static readonly Guid Session = new Guid("7148C835-9B14-5AE2-AB85-DC9B1C14E1A8");

	private static readonly Guid Properties = new Guid("68856CF6-ADB4-54B2-AC16-05837907ACB6");

	private static readonly Guid Playback = new Guid("94B4B6CF-E8BA-51AD-87A7-C10ADE106127");

	private static readonly Guid Controls = new Guid("6501A3E6-BC7A-503A-BB1B-68F158F3FB03");

	private static readonly Guid StreamReference = new Guid("33EE3134-1DD6-4E3A-8067-D1C162E8642B");

	private static readonly Guid AsyncInfo = new Guid("00000036-0000-0000-C000-000000000046");

	private static readonly Guid StreamIid = new Guid("0000000C-0000-0000-C000-000000000046");

	public bool NeedsWorker => true;

	[DllImport("combase.dll")]
	private static extern int RoInitialize(uint type);

	[DllImport("combase.dll")]
	private static extern void RoUninitialize();

	[DllImport("combase.dll", CharSet = CharSet.Unicode)]
	private static extern int WindowsCreateString(string value, uint length, out nint result);

	[DllImport("combase.dll")]
	private static extern int WindowsDeleteString(nint value);

	[DllImport("combase.dll")]
	private static extern nint WindowsGetStringRawBuffer(nint value, out uint length);

	[DllImport("combase.dll")]
	private static extern int RoGetActivationFactory(nint name, in Guid iid, out nint result);

	[DllImport("shcore.dll")]
	private static extern int CreateStreamOverRandomAccessStream(nint random, in Guid iid, out nint stream);

	private static void Check(int hr)
	{
		Marshal.ThrowExceptionForHR(hr);
	}

	private static T Slot<T>(Com c, int slot) where T : Delegate
	{
		_cancellation.ThrowIfCancellationRequested();
		return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(c.Ptr), slot * IntPtr.Size));
	}

	private static Com Pointer(Com c, int slot)
	{
		Check(Slot<GetPointer>(c, slot)(c.Ptr, out var value));
		return new Com(value);
	}

	private static int Integer(Com c, int slot)
	{
		Check(Slot<GetInt>(c, slot)(c.Ptr, out var value));
		return value;
	}

	private static bool Boolean(Com c, int slot)
	{
		Check(Slot<GetBool>(c, slot)(c.Ptr, out var value));
		return value != 0;
	}

	private static string Text(Com c, int slot)
	{
		using Com com = Pointer(c, slot);
		nint ptr = com.Ptr;
		com.Ptr = IntPtr.Zero;
		try
		{
			uint length;
			return Marshal.PtrToStringUni(WindowsGetStringRawBuffer(ptr, out length), checked((int)length)) ?? "";
		}
		finally
		{
			WindowsDeleteString(ptr);
		}
	}

	private static void Wait(Com operation)
	{
		using Com com = operation.As(AsyncInfo);
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (Integer(com, 7) == 0)
		{
			if (stopwatch.ElapsedMilliseconds >= 1400)
			{
				Slot<NoArgs>(com, 9)(com.Ptr);
				throw new TimeoutException();
			}
			Thread.Sleep(20);
		}
		if (Integer(com, 7) != 1)
		{
			throw new InvalidOperationException("Media operation did not complete.");
		}
	}

	private static Com Result(Com operation)
	{
		Wait(operation);
		return Pointer(operation, 8);
	}

	private static Com Current()
	{
		_cancellation.ThrowIfCancellationRequested();
		Check(WindowsCreateString("Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager", (uint)"Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager".Length, out var result));
		try
		{
			Check(RoGetActivationFactory(result, in Statics, out var result2));
			using Com c = new Com(result2);
			using Com operation = Pointer(c, 6);
			using Com com = Result(operation);
			using Com c2 = com.As(Manager);
			using Com com2 = Pointer(c2, 6);
			return (com2.Ptr == IntPtr.Zero) ? new Com(IntPtr.Zero) : com2.As(Session);
		}
		finally
		{
			WindowsDeleteString(result);
		}
	}

	private static T OnMta<T>(Func<T> read, CancellationToken cancellation = default(CancellationToken))
	{
		cancellation.ThrowIfCancellationRequested();
		if (Thread.CurrentThread.GetApartmentState() != ApartmentState.MTA)
		{
			throw new InvalidOperationException("Windows media must be read on an MTA worker.");
		}
		Check(RoInitialize(1u));
		_cancellation = cancellation;
		try
		{
			return read();
		}
		finally
		{
			_cancellation = default;
			RoUninitialize();
		}
	}

	public NowPlaying? Read()
	{
		return Read(CancellationToken.None);
	}

	public NowPlaying? Read(CancellationToken cancellation)
	{
		return OnMta(() =>
		{
			using Com com = Current();
			if (com.Ptr != IntPtr.Zero)
			{
				using Com operation = Pointer(com, 7);
				using Com com2 = Result(operation);
				using Com com3 = com2.As(Properties);
				using Com com4 = Pointer(com, 9);
				using Com c = com4.As(Playback);
				using Com com5 = Pointer(c, 6);
				using Com c2 = com5.As(Controls);
				string text = Text(com3, 6);
				if (string.IsNullOrWhiteSpace(text))
				{
					return (NowPlaying)null;
				}
				return new NowPlaying(Text(com3, 9), text)
				{
					AppName = Text(com, 6),
					IsPlaying = (Integer(c, 7) == 4),
					CanNext = Boolean(c2, 12),
					CanPrevious = Boolean(c2, 13),
					CanPlayPause = Boolean(c2, 16),
					Art = ReadArt(com3)
				};
			}
			return (NowPlaying)null;
		}, cancellation);
	}

	public bool TryCommand(MediaKey key)
	{
		return OnMta(() =>
		{
			using Com com = Current();
			if (com.Ptr != IntPtr.Zero)
			{
				Com c = com;
				int slot = ((key == MediaKey.Next) ? 16 : ((key == MediaKey.Previous) ? 17 : 20));
				using Com com2 = Pointer(c, slot);
				Wait(com2);
				return Boolean(com2, 8);
			}
			return false;
		});
	}

	private static Bitmap? ReadArt(Com properties)
	{
		try
		{
			using Com com = Pointer(properties, 15);
			if (com.Ptr == IntPtr.Zero)
			{
				return null;
			}
			using Com c = com.As(StreamReference);
			using Com operation = Pointer(c, 6);
			using Com com2 = Result(operation);
			Check(CreateStreamOverRandomAccessStream(com2.Ptr, in StreamIid, out var stream));
			object obj = null;
			try
			{
				obj = Marshal.GetObjectForIUnknown(stream);
				IStream stream2 = (IStream)obj;
				stream2.Stat(out var pstatstg, 1);
				if (pstatstg.cbSize <= 0 || pstatstg.cbSize > 8388608)
				{
					return null;
				}
				byte[] array = new byte[(int)pstatstg.cbSize];
				nint num = Marshal.AllocCoTaskMem(4);
				try
				{
					stream2.Read(array, array.Length, num);
					int num2 = Marshal.ReadInt32(num);
					if (num2 <= 0)
					{
						return null;
					}
					using MemoryStream stream3 = new MemoryStream(array, 0, num2);
					using Image image = Image.FromStream(stream3);
					double num3 = Math.Min(1.0, 256.0 / (double)Math.Max(image.Width, image.Height));
					return new Bitmap(image, Math.Max(1, (int)((double)image.Width * num3)), Math.Max(1, (int)((double)image.Height * num3)));
				}
				finally
				{
					Marshal.FreeCoTaskMem(num);
				}
			}
			finally
			{
				if (obj != null)
				{
					Marshal.ReleaseComObject(obj);
				}
				Marshal.Release(stream);
			}
		}
		catch
		{
			return null;
		}
	}
}
