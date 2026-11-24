using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace VibranceHud.Theming;

public sealed class AnimatedPicture : IDisposable
{
	public const double MinFrameSeconds = 1.0 / 30.0;

	public const double EarlyWakeSeconds = 0.002;

	private const int PropertyTagFrameDelay = 20736;

	private readonly MemoryStream _bytes;

	private readonly Image _image;

	private readonly double[] _delays;

	private int _index;

	private double _owed;

	public int Count => _delays.Length;

	public int Index => _index;

	public double CurrentSeconds => _delays[_index];

	public double SecondsUntilNext => Math.Max(0.0, _delays[_index] - _owed);

	public Image Current
	{
		get
		{
			_image.SelectActiveFrame(FrameDimension.Time, _index);
			return _image;
		}
	}

	private AnimatedPicture(MemoryStream bytes, Image image, double[] delays)
	{
		_bytes = bytes;
		_image = image;
		_delays = delays;
	}

	public static AnimatedPicture? From(byte[] fileBytes)
	{
		MemoryStream memoryStream = null;
		Image image = null;
		try
		{
			memoryStream = new MemoryStream(fileBytes, writable: false);
			image = Image.FromStream(memoryStream);
			int num;
			try
			{
				num = image.GetFrameCount(FrameDimension.Time);
			}
			catch
			{
				num = 1;
			}
			if (num <= 1)
			{
				image.Dispose();
				memoryStream.Dispose();
				return null;
			}
			return new AnimatedPicture(memoryStream, image, ReadDelays(image, num));
		}
		catch
		{
			image?.Dispose();
			memoryStream?.Dispose();
			return null;
		}
	}

	private static double[] ReadDelays(Image image, int count)
	{
		double[] array = new double[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = 1.0 / 30.0;
		}
		try
		{
			byte[] array2 = image.GetPropertyItem(20736)?.Value;
			if (array2 != null)
			{
				for (int j = 0; j < count && (j + 1) * 4 <= array2.Length; j++)
				{
					int num = BitConverter.ToInt32(array2, j * 4);
					array[j] = Math.Max(1.0 / 30.0, (double)num / 100.0);
				}
			}
		}
		catch
		{
		}
		return array;
	}

	public bool Advance(double seconds)
	{
		if (seconds <= 0.0)
		{
			return false;
		}
		_owed += seconds;
		double num = _delays[_index];
		if (_owed + 0.002 < num)
		{
			return false;
		}
		_owed -= num;
		_index = (_index + 1) % _delays.Length;
		_owed = Math.Clamp(_owed, -0.002, _delays[_index] / 2.0);
		return true;
	}

	public void DropBankedTime()
	{
		_owed = 0.0;
	}

	public void Dispose()
	{
		_image.Dispose();
		_bytes.Dispose();
	}
}
