namespace VibranceHud;

public sealed record FlavourPolicy(string Flavour, bool SelfUpdates, bool UsesSignedManifestHash, StartupMechanism Startup, bool OffersElevation, bool TouchesTheTaskbar, string DataFolderName, string ReinstallFrom, string UpdatesLine)
{
	public static FlavourPolicy Current { get; } = For("store");

	public static FlavourPolicy For(string flavour)
	{
		if (!(flavour == "store"))
		{
			return new FlavourPolicy(flavour, SelfUpdates: true, UsesSignedManifestHash: true, StartupMechanism.RunKey, OffersElevation: true, TouchesTheTaskbar: true, "PlexusX", "plexusx.lol", "· checked every two hours");
		}
		return new FlavourPolicy("store", SelfUpdates: false, UsesSignedManifestHash: false, StartupMechanism.StartupTask, OffersElevation: false, TouchesTheTaskbar: false, "PlexusX Store", "the Microsoft Store", "· via the Microsoft Store");
	}
}
