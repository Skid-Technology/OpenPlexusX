using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VibranceHud.Remote;

internal static class NetworkFacts
{
	internal static string AdvertisedAddress()
	{
		IReadOnlyList<string> readOnlyList = Candidates();
		if (readOnlyList.Count <= 0)
		{
			return "";
		}
		return readOnlyList[0];
	}

	internal static IReadOnlyList<string> Candidates()
	{
		try
		{
			List<(string, int)> list = new List<(string, int)>();
			List<string> list2 = new List<string>();
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in allNetworkInterfaces)
			{
				if (networkInterface.OperationalStatus != OperationalStatus.Up || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
				{
					continue;
				}
				IPInterfaceProperties iPProperties;
				try
				{
					iPProperties = networkInterface.GetIPProperties();
				}
				catch
				{
					continue;
				}
				bool flag = iPProperties.GatewayAddresses.Any((GatewayIPAddressInformation g) => g.Address != null && g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));
				int item = SafeMetric(iPProperties);
				foreach (UnicastIPAddressInformation unicastAddress in iPProperties.UnicastAddresses)
				{
					if (unicastAddress.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(unicastAddress.Address))
					{
						continue;
					}
					string text = unicastAddress.Address.ToString();
					if (!text.StartsWith("169.254.", StringComparison.Ordinal))
					{
						if (flag)
						{
							list.Add((text, item));
						}
						else
						{
							list2.Add(text);
						}
					}
				}
			}
			return list.OrderBy(((string Address, int Metric) r) => r.Metric).Select(((string Address, int Metric) r) => r.Address).Concat(list2)
				.Distinct(StringComparer.Ordinal)
				.ToList();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	private static int SafeMetric(IPInterfaceProperties props)
	{
		try
		{
			return props.GetIPv4Properties()?.Index ?? int.MaxValue;
		}
		catch
		{
			return int.MaxValue;
		}
	}

	internal static IReadOnlyCollection<string> LocalAddresses()
	{
		try
		{
			return (from u in (from n in NetworkInterface.GetAllNetworkInterfaces()
					where n.OperationalStatus == OperationalStatus.Up
					select n).SelectMany((NetworkInterface n) =>
				{
					try
					{
						return n.GetIPProperties().UnicastAddresses.AsEnumerable();
					}
					catch
					{
						return Enumerable.Empty<UnicastIPAddressInformation>();
					}
				})
				where u.Address.AddressFamily == AddressFamily.InterNetwork
				select u.Address.ToString()).Distinct(StringComparer.Ordinal).ToList();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	internal static NetworkCategory Category()
	{
		try
		{
			Type typeFromCLSID = Type.GetTypeFromCLSID(new Guid("DCB00C01-570F-4A9B-8D69-199FDBA5723B"));
			if (typeFromCLSID == null)
			{
				return NetworkCategory.Unknown;
			}
			object obj = Activator.CreateInstance(typeFromCLSID);
			if (obj == null)
			{
				return NetworkCategory.Unknown;
			}
			try
			{
				if (!(typeFromCLSID.InvokeMember("GetNetworks", BindingFlags.InvokeMethod, null, obj, new object[1] { 1 }) is IEnumerable enumerable))
				{
					return NetworkCategory.Unknown;
				}
				NetworkCategory result = NetworkCategory.Unknown;
				foreach (object item in enumerable)
				{
					object obj2 = item.GetType().InvokeMember("GetCategory", BindingFlags.InvokeMethod, null, item, null);
					if (obj2 != null)
					{
						switch (Convert.ToInt32(obj2))
						{
						case 0:
							return NetworkCategory.Public;
						case 1:
						case 2:
							result = NetworkCategory.Private;
							break;
						}
					}
				}
				return result;
			}
			finally
			{
				if (Marshal.IsComObject(obj))
				{
					Marshal.ReleaseComObject(obj);
				}
			}
		}
		catch
		{
			return NetworkCategory.Unknown;
		}
	}
}
