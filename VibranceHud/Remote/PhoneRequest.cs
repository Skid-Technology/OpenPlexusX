using System;
using System.Collections.Generic;
using System.Globalization;

namespace VibranceHud.Remote;

internal static class PhoneRequest
{
	internal const int AttemptsBeforeBackoff = 10;

	internal static readonly TimeSpan BackoffWindow = TimeSpan.FromMinutes(1.0);

	internal static readonly string[] Fields = BuildFieldNames();

	private static string[] BuildFieldNames()
	{
		List<string> list = new List<string>();
		PhoneField[] sliders = PhoneFields.Sliders;
		foreach (PhoneField phoneField in sliders)
		{
			list.Add(phoneField.Key);
		}
		list.Add("preset");
		list.Add("compare");
		return list.ToArray();
	}

	internal static PhoneDecision Decide(string method, string target, string? hostHeader, string remoteAddress, string advertisedAddress, IReadOnlyCollection<string> localAddresses, string sessionToken, PhoneBackoff backoff, DateTime now)
	{
		string text = HostName(hostHeader);
		if (string.IsNullOrEmpty(text))
		{
			return PhoneDecision.Of(PhoneVerdict.BadRequest);
		}
		if (!IsOurs(text, localAddresses))
		{
			return PhoneDecision.Of(PhoneVerdict.NotFound);
		}
		if (!IsAllowedSource(remoteAddress, advertisedAddress))
		{
			return PhoneDecision.Of(PhoneVerdict.WrongNetwork);
		}
		if (backoff.IsBlocked(remoteAddress, now))
		{
			return PhoneDecision.Of(PhoneVerdict.TooManyAttempts);
		}
		string text2 = PathOf(target);
		if (!TokensMatch(QueryValue(target, "k"), sessionToken))
		{
			if (text2 == "/" && Method(method) == "GET")
			{
				return PhoneDecision.Of(PhoneVerdict.Page);
			}
			backoff.RecordFailure(remoteAddress, now);
			return PhoneDecision.Of(PhoneVerdict.NotFound);
		}
		backoff.RecordSuccess(remoteAddress);
		string text3 = Method(method);
		if (!(text3 == "GET"))
		{
			if (text3 == "POST" && text2 == "/a")
			{
				return ApplyFrom(target);
			}
		}
		else
		{
			if (text2 == "/")
			{
				return PhoneDecision.Of(PhoneVerdict.Page);
			}
			if (text2 == "/s")
			{
				return PhoneDecision.Of(PhoneVerdict.State);
			}
		}
		return PhoneDecision.Of(PhoneVerdict.NotFound);
	}

	private static PhoneDecision ApplyFrom(string target)
	{
		string text = QueryValue(target, "f");
		string text2 = QueryValue(target, "v");
		if (text == null || text2 == null)
		{
			return PhoneDecision.Of(PhoneVerdict.NotFound);
		}
		if (!PhoneFields.IsAllowed(text))
		{
			return PhoneDecision.Of(PhoneVerdict.NotFound);
		}
		if (!int.TryParse(text2, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return PhoneDecision.Of(PhoneVerdict.NotFound);
		}
		result = PhoneFields.Clamp(text, result);
		return new PhoneDecision(PhoneVerdict.Apply, text, result);
	}

	internal static bool TokensMatch(string? candidate, string expected)
	{
		if (string.IsNullOrEmpty(expected))
		{
			return false;
		}
		if (candidate == null || candidate.Length != expected.Length)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < expected.Length; i++)
		{
			num |= candidate[i] ^ expected[i];
		}
		return num == 0;
	}

	internal static bool IsAllowedSource(string remote, string advertised)
	{
		if (string.IsNullOrEmpty(remote))
		{
			return false;
		}
		if (remote == "127.0.0.1" || remote == "::1")
		{
			return true;
		}
		if (string.IsNullOrEmpty(advertised))
		{
			return false;
		}
		string text = SubnetOf(remote);
		if (text != null && text.Length > 0)
		{
			string text2 = SubnetOf(advertised);
			if (text2 != null && text2.Length > 0)
			{
				return string.Equals(text, text2, StringComparison.Ordinal);
			}
		}
		return false;
	}

	private static string SubnetOf(string address)
	{
		int num = address.LastIndexOf('.');
		if (num > 0)
		{
			return address.Substring(0, num);
		}
		return "";
	}

	internal static bool IsOurs(string host, IReadOnlyCollection<string> localAddresses)
	{
		if (host == "localhost" || host == "127.0.0.1")
		{
			return true;
		}
		foreach (string localAddress in localAddresses)
		{
			if (string.Equals(host, localAddress, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	internal static string HostName(string? header)
	{
		if (string.IsNullOrWhiteSpace(header))
		{
			return "";
		}
		string text = header.Trim();
		if (text.StartsWith("[", StringComparison.Ordinal))
		{
			int num = text.IndexOf(']');
			if (num <= 0)
			{
				return "";
			}
			return text.Substring(1, num - 1);
		}
		int num2 = text.IndexOf(':');
		if (num2 < 0)
		{
			return text;
		}
		return text.Substring(0, num2);
	}

	internal static string Method(string method)
	{
		return (method ?? "").Trim().ToUpperInvariant();
	}

	internal static string PathOf(string target)
	{
		if (string.IsNullOrEmpty(target))
		{
			return "/";
		}
		int num = target.IndexOf('?');
		string text = ((num >= 0) ? target.Substring(0, num) : target);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "/";
	}

	internal static string? QueryValue(string target, string name)
	{
		int num = target.IndexOf('?');
		if (num < 0)
		{
			return null;
		}
		string[] array = target.Substring(num + 1).Split('&');
		foreach (string text in array)
		{
			int num2 = text.IndexOf('=');
			if (num2 > 0 && string.Equals(text.Substring(0, num2), name, StringComparison.Ordinal))
			{
				return Uri.UnescapeDataString(text.Substring(num2 + 1));
			}
		}
		return null;
	}
}
