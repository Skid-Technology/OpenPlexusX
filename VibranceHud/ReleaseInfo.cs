using System;

namespace VibranceHud;

public sealed record ReleaseInfo(Version Version, string Tag, string InstallerUrl, string PageUrl, string Notes, string? Sha256 = null, string? MirrorUrl = null, string? RawMirrorUrl = null, string? SignedManifest = null);
