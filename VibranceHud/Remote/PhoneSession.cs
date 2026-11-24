using System;
using System.Security.Cryptography;
using System.Text;
using VibranceHud.Licensing;

namespace VibranceHud.Remote;

internal static class PhoneSession
{
	internal static readonly TimeSpan HintAfter = TimeSpan.FromSeconds(45.0);

	internal static string NewToken()
	{
		return NewToken(ServedPairing.Current);
	}

	internal static string NewToken(PairingFormat format)
	{
		if (format == null || format.Withheld)
		{
			return "";
		}
		int length = format.Alphabet.Length;
		if (length != 32)
		{
			return "";
		}
		int num = format.TokenBits / 8;
		if (num < 8 || num > 64)
		{
			return "";
		}
		Span<byte> span = stackalloc byte[64];
		span = span.Slice(0, num);
		RandomNumberGenerator.Fill(span);
		StringBuilder stringBuilder = new StringBuilder(format.TokenLength);
		int num2 = 0;
		int num3 = 0;
		Span<byte> span2 = span;
		for (int i = 0; i < span2.Length; i++)
		{
			byte b = span2[i];
			num3 = (num3 << 8) | b;
			num2 += 8;
			while (num2 >= 5)
			{
				num2 -= 5;
				stringBuilder.Append(format.Alphabet[(num3 >> num2) & (length - 1)]);
			}
		}
		if (num2 > 0)
		{
			stringBuilder.Append(format.Alphabet[(num3 << 5 - num2) & (length - 1)]);
		}
		return stringBuilder.ToString();
	}

	internal static string Url(string address, int port, string token)
	{
		return ServedPairing.Current.Url(address, port, token);
	}

	internal static string FirewallAdvice(NetworkCategory category)
	{
		return category switch
		{
			NetworkCategory.Public => "Windows will ask whether to let PlexusX use the network. It will say \"Publisher: Unknown\" — that's expected, PlexusX isn't code-signed yet.\n\nYour network is set to Public, so you need to tick \"Public networks\" — the box Windows leaves unticked. The pre-ticked \"Private\" box on its own will not let your phone through.", 
			NetworkCategory.Private => "Windows will ask whether to let PlexusX use the network. It will say \"Publisher: Unknown\" — that's expected, PlexusX isn't code-signed yet.\n\nYour network is set to Private, so the box Windows has already ticked is the right one. Just press Allow.", 
			_ => "Windows will ask whether to let PlexusX use the network. It will say \"Publisher: Unknown\" — that's expected, PlexusX isn't code-signed yet.\n\nTick whichever box matches the network you're on. If you're not sure, tick both — your phone can only reach PlexusX through the one Windows is actually using.", 
		};
	}

	internal static string RepairAdvice()
	{
		return "If you pressed Cancel on that prompt, PlexusX cannot undo it — Windows only lets an administrator change firewall rules.\n\nOpen Windows Security → Firewall & network protection → Allow an app through firewall, find PlexusX, and tick the network you're on.";
	}
}
