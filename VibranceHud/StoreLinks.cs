using System;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;

namespace VibranceHud;

internal static class StoreLinks
{
	private const string DownloadsAndUpdates = "ms-windows-store://downloadsandupdates";

	private const string ProductPageFor = "ms-windows-store://pdp/?PFN=";

	internal static void OpenUpdates()
	{
		Open("ms-windows-store://downloadsandupdates");
	}

	internal static void OpenProductPage()
	{
		Open(ProductPage());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string ProductPage()
	{
		return "ms-windows-store://pdp/?PFN=" + Package.Current.Id.FamilyName;
	}

	private static void Open(string link)
	{
		if (!Links.TryOpen(link))
		{
			throw new InvalidOperationException("Windows would not open " + link);
		}
	}
}
