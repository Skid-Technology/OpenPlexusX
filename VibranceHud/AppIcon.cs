using System.Drawing;
using System.IO;
using System.Reflection;

namespace VibranceHud;

public static class AppIcon
{
	private static Icon? _icon;

	public static Icon Value => _icon ?? (_icon = Load());

	private static Icon Load()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlexusX.ico");
		return (stream != null) ? new Icon(stream) : SystemIcons.Application;
	}
}
