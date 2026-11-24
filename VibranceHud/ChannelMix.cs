using System;
using System.Globalization;
using System.Linq;

namespace VibranceHud;

public sealed record ChannelMix(float RR, float RG, float RB, float GR, float GG, float GB, float BR, float BG, float BB)
{
	public bool IsIdentity => Near(Identity);

	public static readonly ChannelMix Identity = new ChannelMix(1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f);

	private const float Tolerance = 0.0005f;

	public bool Near(ChannelMix other)
	{
		return ToArray().Zip(other.ToArray(), (float a, float b) => Math.Abs(a - b)).All((float d) => d <= 0.0005f);
	}

	public float[] ToArray()
	{
		return new float[9] { RR, RG, RB, GR, GG, GB, BR, BG, BB };
	}

	public static ChannelMix FromArray(float[]? nine)
	{
		if (nine == null || nine.Length != 9 || nine.Any((float v) => float.IsNaN(v) || float.IsInfinity(v)))
		{
			return Identity;
		}
		return new ChannelMix(nine[0], nine[1], nine[2], nine[3], nine[4], nine[5], nine[6], nine[7], nine[8]);
	}

	public static ChannelMix FromPercent(params int[] nine)
	{
		if (nine.Length != 9)
		{
			throw new ArgumentException("a channel mix is nine numbers", "nine");
		}
		return FromArray(nine.Select((int v) => (float)v / 100f).ToArray());
	}

	public float[] ToMatrix()
	{
		return new float[25]
		{
			RR, RG, RB, 0f, 0f, GR, GG, GB, 0f, 0f,
			BR, BG, BB, 0f, 0f, 0f, 0f, 0f, 1f, 0f,
			0f, 0f, 0f, 0f, 1f
		};
	}

	public float[] ComposeInto(float[] matrix)
	{
		if (!IsIdentity)
		{
			return ColourVision.Compose(matrix, ToMatrix());
		}
		return matrix;
	}

	public override string ToString()
	{
		return "[" + string.Join(",", from v in ToArray()
			select Math.Round(v * 100f).ToString(CultureInfo.InvariantCulture)) + "]";
	}
}
