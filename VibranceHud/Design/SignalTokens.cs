using System.Drawing;

namespace VibranceHud.Design;

public static class SignalTokens
{
	public static class Root
	{
		public static readonly Color Signal1 = Color.FromArgb(255, 252, 71, 56);

		public static readonly Color Signal1Paper = Color.FromArgb(255, 200, 38, 26);

		public static readonly Color Signal2 = Color.FromArgb(255, 253, 147, 32);

		public static readonly Color Signal2Paper = Color.FromArgb(255, 184, 90, 0);

		public static readonly Color Signal3 = Color.FromArgb(255, 250, 223, 39);

		public static readonly Color Signal3Paper = Color.FromArgb(255, 138, 122, 0);

		public static readonly Color Signal4 = Color.FromArgb(255, 89, 222, 101);

		public static readonly Color Signal4Paper = Color.FromArgb(255, 30, 138, 58);

		public static readonly Color Signal5 = Color.FromArgb(255, 54, 220, 236);

		public static readonly Color Signal5Paper = Color.FromArgb(255, 0, 127, 140);

		public static readonly Color Signal6 = Color.FromArgb(255, 55, 137, 253);

		public static readonly Color Signal6Paper = Color.FromArgb(255, 31, 99, 214);

		public static readonly Color SignalStart = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color SuccessPaper = Color.FromArgb(255, 5, 119, 59);

		public static readonly Color WarningPaper = Color.FromArgb(255, 159, 92, 12);

		public static readonly Color DangerPaper = Color.FromArgb(255, 197, 44, 42);

		public static readonly Color InfoPaper = Color.FromArgb(255, 13, 106, 187);

		public static readonly Color XBefore = Color.FromArgb(255, 255, 59, 48);

		public static readonly Color XAfter = Color.FromArgb(255, 34, 197, 94);

		public static readonly Color XHook = Color.FromArgb(255, 255, 230, 0);

		public static readonly Color XDiscord = Color.FromArgb(255, 88, 101, 242);

		public static readonly Color PPaper = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color PPaper2 = Color.FromArgb(255, 243, 243, 243);

		public static readonly Color PPaperRule = Color.FromArgb(255, 222, 222, 222);

		public static readonly Color PInk = Color.FromArgb(255, 17, 17, 20);

		public static readonly Color PInk2 = Color.FromArgb(255, 85, 85, 85);

		public static readonly Color PInk3 = Color.FromArgb(255, 108, 108, 108);

		public static readonly Color BrandGround = Color.FromArgb(255, 21, 21, 21);

		public static readonly Color BrandInk = Color.FromArgb(255, 255, 255, 255);

		public const string GlassFill = "color-mix(in srgb, #151515 58%, transparent)";

		public const string GlassFillFallback = "color-mix(in srgb, #151515 94%, transparent)";

		public const int GlassBlur = 22;

		public static readonly Color GlassHairline = Color.FromArgb(20, 255, 255, 255);

		public const string FontDisplayStack = "\"Sofia Sans Extra Condensed\", \"Arial Narrow\", sans-serif";

		public static readonly string[] FontDisplayFamilies = new string[2] { "Sofia Sans Extra Condensed", "Arial Narrow" };

		public const string FontTextStack = "\"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public static readonly string[] FontTextFamilies = new string[2] { "Host Grotesk", "Segoe UI" };

		public const string FontUiStack = "\"Segoe UI Variable Text\", \"Segoe UI\", system-ui, sans-serif";

		public static readonly string[] FontUiFamilies = new string[2] { "Segoe UI Variable Text", "Segoe UI" };

		public const string FontMonoStack = "\"Cascadia Mono\", Consolas, ui-monospace, monospace";

		public static readonly string[] FontMonoFamilies = new string[2] { "Cascadia Mono", "Consolas" };

		public const int AppCaptionWeight = 400;

		public const int AppCaptionSizePx = 12;

		public const int AppCaptionLinePx = 16;

		public const int AppCaptionStrongWeight = 600;

		public const int AppCaptionStrongSizePx = 12;

		public const int AppCaptionStrongLinePx = 16;

		public const int AppBodyWeight = 400;

		public const int AppBodySizePx = 14;

		public const int AppBodyLinePx = 20;

		public const int AppBodyStrongWeight = 600;

		public const int AppBodyStrongSizePx = 14;

		public const int AppBodyStrongLinePx = 20;

		public const int AppSubtitleWeight = 600;

		public const int AppSubtitleSizePx = 20;

		public const int AppSubtitleLinePx = 28;

		public const int AppTitleWeight = 600;

		public const int AppTitleSizePx = 28;

		public const int AppTitleLinePx = 36;

		public const int AppValueWeight = 600;

		public const int AppValueSizePx = 40;

		public const int AppValueLinePx = 48;

		public const string WebDisplayXl = "800 144px/0.88 \"Sofia Sans Extra Condensed\", \"Arial Narrow\", sans-serif";

		public const string WebDisplayL = "800 88px/0.9 \"Sofia Sans Extra Condensed\", \"Arial Narrow\", sans-serif";

		public const string WebDisplayM = "800 48px/0.95 \"Sofia Sans Extra Condensed\", \"Arial Narrow\", sans-serif";

		public const string WebHeading = "600 28px/1.2 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebSubheading = "600 20px/1.3 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebMenu = "500 18px/1.3 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebBodyL = "400 18px/1.55 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebBody = "400 16px/1.55 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebSmall = "400 14px/1.45 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebLabelL = "500 14px/1.3 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebLabel = "500 13px/1.3 \"Host Grotesk\", \"Segoe UI\", system-ui, sans-serif";

		public const string WebCode = "400 13px/1.5 \"Cascadia Mono\", Consolas, ui-monospace, monospace";

		public const string TrackDisplay = "-0.01em";

		public const string TrackWordmark = "-0.012em";

		public const int Space0 = 0;

		public const int Space1 = 2;

		public const int Space2 = 4;

		public const int Space3 = 8;

		public const int Space4 = 12;

		public const int Space5 = 16;

		public const int Space6 = 20;

		public const int Space7 = 24;

		public const int Space8 = 32;

		public const int Space9 = 40;

		public const int Space10 = 48;

		public const int Space11 = 64;

		public const int Space12 = 80;

		public const int Space13 = 96;

		public const int Space14 = 128;

		public const int Radius0 = 0;

		public const int Radius4 = 4;

		public const int Radius8 = 8;

		public const int RadiusPill = 999;

		public const int StrokeHairline = 1;

		public const float StrokeIcon = 1.5f;

		public const int StrokeFocus = 2;

		public const int FocusGap = 2;

		public const int SignalStrand = 1;

		public const int DurInstantMs = 0;

		public const int DurFadeMs = 83;

		public const int DurMoveMs = 167;

		public const int DurSplashMs = 900;

		public const string EaseStandard = "cubic-bezier(0.33, 0, 0.67, 1)";

		public const string EaseEnter = "cubic-bezier(0, 0, 0, 1)";
	}

	public static class Ash
	{
		public static readonly Color Bg = Color.FromArgb(255, 21, 21, 21);

		public static readonly Color Surface1 = Color.FromArgb(255, 28, 28, 28);

		public static readonly Color Surface2 = Color.FromArgb(255, 35, 35, 35);

		public static readonly Color Surface3 = Color.FromArgb(255, 42, 42, 42);

		public static readonly Color Hover = Color.FromArgb(255, 49, 49, 49);

		public static readonly Color Pressed = Color.FromArgb(255, 57, 57, 57);

		public static readonly Color Border = Color.FromArgb(255, 51, 51, 51);

		public static readonly Color WindowEdge = Color.FromArgb(255, 69, 69, 69);

		public static readonly Color Text1 = Color.FromArgb(255, 243, 243, 243);

		public static readonly Color Text2 = Color.FromArgb(255, 193, 193, 193);

		public static readonly Color TextDisabled = Color.FromArgb(255, 88, 88, 88);

		public static readonly Color Track = Color.FromArgb(255, 61, 61, 61);

		public static readonly Color Accent = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color AccentHover = Color.FromArgb(255, 222, 222, 222);

		public static readonly Color AccentPressed = Color.FromArgb(255, 196, 196, 196);

		public static readonly Color OnAccent = Color.FromArgb(255, 21, 21, 21);

		public static readonly Color Focus = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color PreviewWell = Color.FromArgb(255, 28, 28, 28);

		public static readonly Color Text3 = Color.FromArgb(255, 153, 153, 153);

		public static readonly Color BorderStrong = Color.FromArgb(255, 116, 116, 116);

		public static readonly Color Success = Color.FromArgb(255, 94, 206, 131);

		public static readonly Color Warning = Color.FromArgb(255, 233, 175, 45);

		public static readonly Color Danger = Color.FromArgb(255, 255, 158, 147);

		public static readonly Color Info = Color.FromArgb(255, 127, 190, 243);

		public static readonly Color Chrome = Color.FromArgb(255, 21, 21, 21);

		public static readonly Color Overlay = Color.FromArgb(255, 42, 42, 42);

		public static readonly Color KnobRing = Color.FromArgb(255, 28, 28, 28);

		public static readonly Color SelectedSuffix = Color.FromArgb(255, 85, 85, 85);

		public static readonly Color TextOnFill = Color.FromArgb(255, 193, 193, 193);

		public static readonly Color FieldFill = Color.FromArgb(0, 0, 0, 0);

		public static readonly Color Selection = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color SignalTrack = Color.FromArgb(255, 61, 61, 61);
	}

	public static class Slate
	{
		public const double GuiOpacity = 0.66;

		public static readonly Color SlateBase = Color.FromArgb(255, 15, 18, 22);

		public static readonly Color Bg = Color.FromArgb(255, 15, 18, 22);

		public static readonly Color ChromeDefault = Color.FromArgb(189, 15, 18, 22);

		public static readonly Color Surface1Default = Color.FromArgb(168, 15, 18, 22);

		public static readonly Color Surface2 = Color.FromArgb(18, 255, 255, 255);

		public static readonly Color Surface3 = Color.FromArgb(242, 15, 18, 22);

		public static readonly Color Overlay = Color.FromArgb(242, 15, 18, 22);

		public static readonly Color Hover = Color.FromArgb(26, 255, 255, 255);

		public static readonly Color Pressed = Color.FromArgb(36, 255, 255, 255);

		public static readonly Color Border = Color.FromArgb(20, 255, 255, 255);

		public static readonly Color BorderStrong = Color.FromArgb(128, 255, 255, 255);

		public static readonly Color WindowEdge = Color.FromArgb(36, 255, 255, 255);

		public static readonly Color Track = Color.FromArgb(56, 255, 255, 255);

		public static readonly Color Text1 = Color.FromArgb(255, 243, 245, 248);

		public static readonly Color Text2 = Color.FromArgb(255, 210, 214, 220);

		public static readonly Color Text3 = Color.FromArgb(255, 195, 200, 207);

		public static readonly Color TextDisabled = Color.FromArgb(97, 255, 255, 255);

		public static readonly Color TextOnFill = Color.FromArgb(255, 243, 245, 248);

		public static readonly Color Accent = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color AccentHover = Color.FromArgb(255, 222, 222, 222);

		public static readonly Color AccentPressed = Color.FromArgb(255, 196, 196, 196);

		public static readonly Color OnAccent = Color.FromArgb(255, 21, 21, 21);

		public static readonly Color Focus = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color PreviewWell = Color.FromArgb(255, 28, 28, 28);

		public static readonly Color Success = Color.FromArgb(255, 94, 206, 131);

		public static readonly Color Warning = Color.FromArgb(255, 233, 175, 45);

		public static readonly Color Danger = Color.FromArgb(255, 255, 158, 147);

		public static readonly Color Info = Color.FromArgb(255, 127, 190, 243);

		public static readonly Color KnobRing = Color.FromArgb(255, 15, 18, 22);

		public static readonly Color FieldFill = Color.FromArgb(255, 15, 18, 22);

		public static readonly Color SelectedSuffix = Color.FromArgb(255, 85, 85, 85);

		public static readonly Color Selection = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color SignalTrack = Color.FromArgb(56, 255, 255, 255);

		public const int MatteBlur = 16;

		public const int Gutter = 16;
	}

	public static class Paper
	{
		public static readonly Color Bg = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color Surface1 = Color.FromArgb(255, 243, 243, 243);

		public static readonly Color Surface2 = Color.FromArgb(255, 243, 243, 243);

		public static readonly Color Surface3 = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color Border = Color.FromArgb(255, 222, 222, 222);

		public static readonly Color BorderStrong = Color.FromArgb(255, 128, 128, 128);

		public static readonly Color Text1 = Color.FromArgb(255, 17, 17, 20);

		public static readonly Color Text2 = Color.FromArgb(255, 85, 85, 85);

		public static readonly Color Text3 = Color.FromArgb(255, 108, 108, 108);

		public static readonly Color Accent = Color.FromArgb(255, 17, 17, 20);

		public static readonly Color OnAccent = Color.FromArgb(255, 255, 255, 255);

		public static readonly Color Focus = Color.FromArgb(255, 17, 17, 20);

		public static readonly Color PreviewWell = Color.FromArgb(255, 243, 243, 243);

		public static readonly Color Success = Color.FromArgb(255, 5, 119, 59);

		public static readonly Color Warning = Color.FromArgb(255, 159, 92, 12);

		public static readonly Color Danger = Color.FromArgb(255, 197, 44, 42);

		public static readonly Color Info = Color.FromArgb(255, 13, 106, 187);

		public static readonly Color Signal1 = Color.FromArgb(255, 200, 38, 26);

		public static readonly Color Signal2 = Color.FromArgb(255, 184, 90, 0);

		public static readonly Color Signal3 = Color.FromArgb(255, 138, 122, 0);

		public static readonly Color Signal4 = Color.FromArgb(255, 30, 138, 58);

		public static readonly Color Signal5 = Color.FromArgb(255, 0, 127, 140);

		public static readonly Color Signal6 = Color.FromArgb(255, 31, 99, 214);

		public static readonly Color SignalStart = Color.FromArgb(255, 17, 17, 20);
	}

	public const string SourceFile = "Design/tokens/tokens.css";

	public const string SourceSha256 = "77EEB2F61406247D6315CE64314C7CA56409F413EDF19BC5F921C7FE5F1813D3";
}
