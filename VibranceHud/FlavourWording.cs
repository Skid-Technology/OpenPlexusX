namespace VibranceHud;

public static class FlavourWording
{
	public static string ModifiedCopyBody(FlavourPolicy p)
	{
		return "This copy of PlexusX is not the one we published - the file has been changed since it was signed.\n\nInstall a clean copy from " + p.ReinstallFrom + ". If you paid for PlexusX, your licence is untouched and will work as soon as you do.";
	}

	public static string ModifiedCopyStatus(FlavourPolicy p)
	{
		return "This copy of PlexusX has been modified. Reinstall it from " + p.ReinstallFrom + ".";
	}

	public static string ModifiedCopyRefusal(FlavourPolicy p)
	{
		return "This copy of PlexusX has been modified and will not activate. Reinstall from " + p.ReinstallFrom + " - your licence is fine and will work straight away.";
	}

	public static string MissingFileBecause(FlavourPolicy p)
	{
		return "A file PlexusX needs is missing or broken. Install PlexusX again from " + p.ReinstallFrom + " to get it back.";
	}

	public static string VerifyCaption(FlavourPolicy p)
	{
		if (!p.UsesSignedManifestHash)
		{
			return "The SHA-256 of the PlexusX now running.";
		}
		return "The SHA-256 of the PlexusX now running. It should match the one on plexusx.lol.";
	}
}
