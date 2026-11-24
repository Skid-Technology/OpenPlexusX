using System;
using System.Text;

namespace VibranceHud.Display;

public readonly record struct MonitorCode(int Width, int Height, int RefreshHz, ScalingMode Scaling)
{
	private const string Prefix = "PXM-";

	private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ#";

	private const int BodyLength = 11;

	public const int MinDimension = 640;

	public const int MaxWidth = 15360;

	public const int MaxHeight = 8640;

	public const int MinRefresh = 1;

	public const int MaxRefresh = 1000;

	private static int ClampWidth(int v)
	{
		return Math.Clamp(v, 640, 15360);
	}

	private static int ClampHeight(int v)
	{
		return Math.Clamp(v, 640, 8640);
	}

	private static int ClampRefresh(int v)
	{
		return Math.Clamp(v, 1, 1000);
	}

	public string Label()
	{
		if (RefreshHz > 0)
		{
			return $"{Width} × {Height} @ {RefreshHz} Hz";
		}
		return $"{Width} × {Height}";
	}

	public static string Encode(MonitorCode mode)
	{
		StringBuilder stringBuilder = new StringBuilder(11);
		Append3(stringBuilder, ClampWidth(mode.Width));
		Append3(stringBuilder, ClampHeight(mode.Height));
		Append2(stringBuilder, ClampRefresh(mode.RefreshHz));
		Append2(stringBuilder, ScalingToWire(mode.Scaling));
		stringBuilder.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[Checksum(stringBuilder.ToString())]);
		return "PXM-" + stringBuilder;
	}

	internal static string EncodeUnclampedForTest(int width, int height, int refresh, int scalingWire)
	{
		StringBuilder stringBuilder = new StringBuilder(11);
		Append3(stringBuilder, width);
		Append3(stringBuilder, height);
		Append2(stringBuilder, refresh);
		Append2(stringBuilder, scalingWire);
		stringBuilder.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[Checksum(stringBuilder.ToString())]);
		return "PXM-" + stringBuilder;
	}

	internal static string WithWrongPrefixForTest(string code)
	{
		return "PXZ-" + code.Substring("PXM-".Length);
	}

	public static bool TryDecode(string? code, out MonitorCode mode)
	{
		mode = default;
		if (string.IsNullOrWhiteSpace(code))
		{
			return false;
		}
		string text = ShareCodeText.Normalize(code).Trim().ToUpperInvariant();
		if (!text.StartsWith("PXM-", StringComparison.Ordinal))
		{
			return false;
		}
		string text2 = text.Substring("PXM-".Length);
		if (text2.Length != 11)
		{
			return false;
		}
		int[] array = new int[text2.Length];
		for (int i = 0; i < text2.Length; i++)
		{
			array[i] = "23456789ABCDEFGHJKMNPQRSTUVWXYZ#".IndexOf(text2[i]);
			if (array[i] < 0)
			{
				return false;
			}
		}
		int num = array[^1];
		string text3 = text2;
		if (num != Checksum(text3.Substring(0, text3.Length - 1)))
		{
			return false;
		}
		int v = array[0] * 1024 + array[1] * 32 + array[2];
		int v2 = array[3] * 1024 + array[4] * 32 + array[5];
		int v3 = array[6] * 32 + array[7];
		int wire = array[8] * 32 + array[9];
		mode = new MonitorCode(ClampWidth(v), ClampHeight(v2), ClampRefresh(v3), ScalingFromWire(wire));
		return true;
	}

	private static void Append3(StringBuilder body, int value)
	{
		int num = Math.Clamp(value, 0, 32767);
		body.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[num / 1024]);
		body.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[num / 32 % 32]);
		body.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[num % 32]);
	}

	private static void Append2(StringBuilder body, int value)
	{
		int num = Math.Clamp(value, 0, 1023);
		body.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[num / 32]);
		body.Append("23456789ABCDEFGHJKMNPQRSTUVWXYZ#"[num % 32]);
	}

	private static int ScalingToWire(ScalingMode mode)
	{
		return mode switch
		{
			ScalingMode.AspectRatio => 1, 
			ScalingMode.NoScaling => 2, 
			ScalingMode.FullScreen => 3, 
			ScalingMode.Integer => 4, 
			_ => 3, 
		};
	}

	private static ScalingMode ScalingFromWire(int wire)
	{
		return wire switch
		{
			1 => ScalingMode.AspectRatio, 
			2 => ScalingMode.NoScaling, 
			3 => ScalingMode.FullScreen, 
			4 => ScalingMode.Integer, 
			_ => ScalingMode.FullScreen, 
		};
	}

	private static int Checksum(string body)
	{
		int num = 0;
		for (int i = 0; i < body.Length; i++)
		{
			num += ("23456789ABCDEFGHJKMNPQRSTUVWXYZ#".IndexOf(body[i]) + 1) * (2 * i + 1);
		}
		return num % 32;
	}
}
