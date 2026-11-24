using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace VibranceHud.Customize;

public static class CustomizeService
{
	internal interface IRegistryReadKey
	{
		object? GetValue(string name, object? defaultValue);
	}

	private sealed class RegistryReadKey : IRegistryReadKey
	{
		private readonly RegistryKey _key;

		public RegistryReadKey(RegistryKey key)
		{
			_key = key;
		}

		public object? GetValue(string name, object? defaultValue)
		{
			return _key.GetValue(name, defaultValue);
		}
	}

	internal struct ACCENT_POLICY
	{
		public uint AccentState;

		public uint AccentFlags;

		public uint GradientColor;

		public uint AnimationId;
	}

	internal struct WINDOW_COMPOSITION_ATTRIB_DATA
	{
		public uint Attribute;

		public nint Data;

		public int SizeOfData;
	}

	internal enum AccentState
	{
		Disabled,
		EnableGradient,
		EnableTransparentGradient,
		EnableBlurBehind,
		EnableAcrylicBlurBehind,
		EnableHostBackdrop,
		InvalidState
	}

	internal enum WindowCompositionAttribute
	{
		WCA_ACCENT_POLICY = 19
	}

	internal delegate bool EnumWindowsProc(nint hWnd, nint lParam);

	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<string, IRegistryReadKey?> _003C_003E9__6_0;

		public static Func<ACCENT_POLICY, bool> _003C_003E9__12_0;

		internal IRegistryReadKey? _003CReadFromRegistry_003Eb__6_0(string path)
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(path, writable: false);
			if (registryKey != null)
			{
				return new RegistryReadKey(registryKey);
			}
			return null;
		}

		internal bool _003CTaskbarWriterFor_003Eb__12_0(ACCENT_POLICY _)
		{
			return false;
		}
	}

	private const uint WM_SETTINGCHANGE = 26u;

	private const uint SMTO_ABORTIFHUNG = 2u;

	private static readonly nint HWND_BROADCAST = new IntPtr(-1);

	private const int SHCNE_ASSOCCHANGED = 134217728;

	private const uint SHCNF_IDLIST = 0u;

	private const string Shell_TrayWnd = "Shell_TrayWnd";

	public static CustomizationSettings MergeLook(CustomizationSettings current, CustomizationSettings preset)
	{
		return preset with
		{
			Taskbar = preset.Taskbar with
			{
				Alignment = current.Taskbar.Alignment,
				SmallButtons = current.Taskbar.SmallButtons,
				Search = current.Taskbar.Search,
				ShowTaskView = current.Taskbar.ShowTaskView,
				ShowWidgets = current.Taskbar.ShowWidgets
			},
			Explorer = current.Explorer,
			Windows = preset.Windows with
			{
				StartLayout = current.Windows.StartLayout
			}
		};
	}

	public static IReadOnlyList<RegistryChange> Plan(CustomizationSettings current, CustomizationSettings target)
	{
		List<RegistryChange> list = new List<RegistryChange>();
		string key = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
		string key2 = "Software\\Microsoft\\Windows\\DWM";
		if (current.System.DarkMode != target.System.DarkMode)
		{
			list.Add(new RegistryChange(key, "AppsUseLightTheme", (!target.System.DarkMode) ? 1u : 0u));
			list.Add(new RegistryChange(key, "SystemUsesLightTheme", (!target.System.DarkMode) ? 1u : 0u));
		}
		if (current.System.AccentColorArgb != target.System.AccentColorArgb)
		{
			list.Add(new RegistryChange(key2, "AccentColor", ArgbToDwmAbgr(target.System.AccentColorArgb)));
		}
		if (current.System.TitleBarsUseAccent != target.System.TitleBarsUseAccent)
		{
			list.Add(new RegistryChange(key2, "ColorPrevalence", target.System.TitleBarsUseAccent ? 1u : 0u));
		}
		if (current.System.BordersUseAccent != target.System.BordersUseAccent)
		{
			list.Add(new RegistryChange(key2, "AccentColorInactive", target.System.BordersUseAccent ? ArgbToDwmAbgr(target.System.AccentColorArgb) : 0u));
			list.Add(new RegistryChange(key2, "PlexusXBordersUseAccent", target.System.BordersUseAccent ? 1u : 0u));
		}
		string text = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced";
		if (current.Taskbar.Buttons != target.Taskbar.Buttons)
		{
			list.Add(new RegistryChange(text, "TaskbarGlomLevel", target.Taskbar.Buttons switch
			{
				TaskbarButtonGlom.Always => 0u, 
				TaskbarButtonGlom.Never => 1u, 
				_ => 2u, 
			}));
		}
		if (current.Taskbar.Alignment != target.Taskbar.Alignment)
		{
			list.Add(new RegistryChange(text, "TaskbarAl", (target.Taskbar.Alignment != TaskbarAlignment.Left) ? 1u : 0u));
		}
		if (current.Taskbar.SmallButtons != target.Taskbar.SmallButtons)
		{
			list.Add(new RegistryChange(text, "TaskbarSmallIcons", target.Taskbar.SmallButtons ? 1u : 0u));
		}
		if (current.Taskbar.ShowTaskView != target.Taskbar.ShowTaskView)
		{
			list.Add(new RegistryChange(text, "ShowTaskViewButton", target.Taskbar.ShowTaskView ? 1u : 0u));
		}
		if (current.Taskbar.ShowWidgets != target.Taskbar.ShowWidgets)
		{
			list.Add(new RegistryChange(text, "TaskbarDa", target.Taskbar.ShowWidgets ? 1u : 0u));
		}
		if (current.Taskbar.Search != target.Taskbar.Search)
		{
			List<RegistryChange> list2 = list;
			list2.Add(new RegistryChange("Software\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", target.Taskbar.Search switch
			{
				SearchBoxMode.Hidden => 0u, 
				SearchBoxMode.Icon => 1u, 
				_ => 2u, 
			}));
		}
		if (current.Explorer.ShowFileExtensions != target.Explorer.ShowFileExtensions)
		{
			list.Add(new RegistryChange(text, "HideFileExt", (!target.Explorer.ShowFileExtensions) ? 1u : 0u));
		}
		if (current.Explorer.ShowHiddenFiles != target.Explorer.ShowHiddenFiles)
		{
			list.Add(new RegistryChange(text, "Hidden", target.Explorer.ShowHiddenFiles ? 1u : 2u));
		}
		if (current.Windows.StartLayout != target.Windows.StartLayout)
		{
			List<RegistryChange> list2 = list;
			string key3 = text;
			list2.Add(new RegistryChange(key3, "Start_Layout", target.Windows.StartLayout switch
			{
				StartLayoutMode.MorePins => 1u, 
				StartLayoutMode.MoreRecommendations => 2u, 
				_ => 0u, 
			}));
		}
		if (current.Windows.CornerRadiusPx != target.Windows.CornerRadiusPx)
		{
			int cornerRadiusPx = target.Windows.CornerRadiusPx;
			uint num = ((cornerRadiusPx >= 6) ? 2u : ((cornerRadiusPx == 0) ? 1u : 3u));
			uint dwordValue = num;
			list.Add(new RegistryChange(key2, "WindowCornerPreference", dwordValue));
		}
		if (current.Windows.SnapAssistSuggestions != target.Windows.SnapAssistSuggestions)
		{
			list.Add(new RegistryChange(text, "SnapAssist", target.Windows.SnapAssistSuggestions ? 1u : 0u));
		}
		if (current.Windows.TransparencyEnabled != target.Windows.TransparencyEnabled)
		{
			list.Add(new RegistryChange(key, "EnableTransparency", target.Windows.TransparencyEnabled ? 1u : 0u));
		}
		return list;
	}

	internal static uint ArgbToDwmAbgr(uint argb)
	{
		uint num = (argb >> 24) & 0xFF;
		uint num2 = (argb >> 16) & 0xFF;
		uint num3 = (argb >> 8) & 0xFF;
		return ((argb & 0xFF) << 24) | (num3 << 16) | (num2 << 8) | num;
	}

	public static void Apply(CustomizationSettings target)
	{
		ApplyPlan(Plan(ReadFromRegistry(), target));
		BroadcastSettingChange();
	}

	internal static void Apply(string registryRoot, CustomizationSettings target)
	{
		ApplyPlan(Plan(ReadFromRegistry(registryRoot), target), registryRoot);
	}

	public static CustomizationSettings ReadFromRegistry()
	{
		return ReadFromRegistry("Software\\Microsoft\\Windows");
	}

	internal static CustomizationSettings ReadFromRegistry(string root)
	{
		return ReadFromRegistry(root, (string path) =>
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(path, writable: false);
			return (registryKey != null) ? new RegistryReadKey(registryKey) : null;
		});
	}

	internal static CustomizationSettings ReadFromRegistry(string root, Func<string, IRegistryReadKey?> openKey)
	{
		try
		{
			return ReadRegistry(root, openKey);
		}
		catch
		{
			return new CustomizationSettings();
		}
	}

	private static CustomizationSettings ReadRegistry(string root, Func<string, IRegistryReadKey?> openKey)
	{
		TaskbarSettings taskbarSettings = new TaskbarSettings();
		SystemSettings systemSettings = new SystemSettings();
		WindowSettings windowSettings = new WindowSettings();
		if (TryOpen(root + "\\CurrentVersion\\Themes\\Personalize", openKey, out IRegistryReadKey key))
		{
			systemSettings = systemSettings with
			{
				DarkMode = ((int)(key.GetValue("AppsUseLightTheme", 1) ?? ((object)1)) == 0)
			};
			windowSettings = windowSettings with
			{
				TransparencyEnabled = ((int)(key.GetValue("EnableTransparency", 1) ?? ((object)1)) == 1)
			};
		}
		if (TryOpen(root + "\\DWM", openKey, out IRegistryReadKey key2))
		{
			int dwm = (int)(key2.GetValue("AccentColor", 0) ?? ((object)0));
			systemSettings = systemSettings with
			{
				AccentColorArgb = DwmAbgrToArgb((uint)dwm),
				TitleBarsUseAccent = ((int)(key2.GetValue("ColorPrevalence", 0) ?? ((object)0)) == 1),
				BordersUseAccent = ((int)(key2.GetValue("PlexusXBordersUseAccent", 0) ?? ((object)0)) == 1)
			};
			int num = (int)(key2.GetValue("WindowCornerPreference", 0) ?? ((object)0));
			windowSettings = windowSettings with
			{
				CornerRadiusPx = num switch
				{
					1 => 0, 
					3 => 4, 
					_ => 8, 
				}
			};
		}
		ExplorerSettings explorerSettings = new ExplorerSettings();
		if (TryOpen(root + "\\CurrentVersion\\Explorer\\Advanced", openKey, out IRegistryReadKey key3))
		{
			int num2 = (int)(key3.GetValue("TaskbarGlomLevel", 2) ?? ((object)2));
			taskbarSettings = taskbarSettings with
			{
				Buttons = num2 switch
				{
					0 => TaskbarButtonGlom.Always, 
					1 => TaskbarButtonGlom.Never, 
					_ => TaskbarButtonGlom.Combine, 
				},
				Alignment = (((int)(key3.GetValue("TaskbarAl", 1) ?? ((object)1)) != 0) ? TaskbarAlignment.Centre : TaskbarAlignment.Left),
				SmallButtons = (int)(key3.GetValue("TaskbarSmallIcons", 0) ?? ((object)0)) == 1,
				ShowTaskView = (int)(key3.GetValue("ShowTaskViewButton", 1) ?? ((object)1)) == 1,
				ShowWidgets = (int)(key3.GetValue("TaskbarDa", 1) ?? ((object)1)) == 1
			};
			windowSettings = windowSettings with
			{
				SnapAssistSuggestions = ((int)(key3.GetValue("SnapAssist", 1) ?? ((object)1)) == 1),
				StartLayout = (int)(key3.GetValue("Start_Layout", 0) ?? ((object)0)) switch
				{
					1 => StartLayoutMode.MorePins, 
					2 => StartLayoutMode.MoreRecommendations, 
					_ => StartLayoutMode.Default, 
				}
			};
			explorerSettings = explorerSettings with
			{
				ShowFileExtensions = ((int)(key3.GetValue("HideFileExt", 1) ?? ((object)1)) == 0),
				ShowHiddenFiles = ((int)(key3.GetValue("Hidden", 2) ?? ((object)2)) == 1)
			};
		}
		if (TryOpen(root + "\\CurrentVersion\\Search", openKey, out IRegistryReadKey key4))
		{
			taskbarSettings = taskbarSettings with
			{
				Search = (int)(key4.GetValue("SearchboxTaskbarMode", 2) ?? ((object)2)) switch
				{
					0 => SearchBoxMode.Hidden, 
					1 => SearchBoxMode.Icon, 
					_ => SearchBoxMode.Box, 
				}
			};
		}
		return new CustomizationSettings
		{
			Taskbar = taskbarSettings,
			System = systemSettings,
			Windows = windowSettings,
			Explorer = explorerSettings
		};
	}

	public static TaskbarStyle StyleFromName(string? name)
	{
		if (!Enum.TryParse<TaskbarStyle>(name, ignoreCase: true, out var result))
		{
			return TaskbarStyle.Normal;
		}
		return result;
	}

	public static bool ShouldReapplyTaskbarStyle(AppSettings settings)
	{
		return ShouldReapplyTaskbarStyle(settings, FlavourPolicy.Current);
	}

	public static bool ShouldReapplyTaskbarStyle(AppSettings settings, FlavourPolicy policy)
	{
		if (policy.TouchesTheTaskbar)
		{
			return StyleFromName(settings.TaskbarStyleName) != TaskbarStyle.Normal;
		}
		return false;
	}

	internal static Func<ACCENT_POLICY, bool> TaskbarWriterFor(FlavourPolicy policy, Func<ACCENT_POLICY, bool> real)
	{
		Func<ACCENT_POLICY, bool> func;
		if (!policy.TouchesTheTaskbar)
		{
			func = _003C_003Ec._003C_003E9__12_0;
			if (func == null)
			{
				return _003C_003Ec._003C_003E9__12_0 = (ACCENT_POLICY _) => false;
			}
		}
		else
		{
			func = real;
		}
		return func;
	}

	public static bool NeedsExplorerRestart(CustomizeTweak tweak)
	{
		return tweak switch
		{
			CustomizeTweak.SmallTaskbarButtons => true, 
			CustomizeTweak.StartLayout => true, 
			CustomizeTweak.SearchBox => true, 
			_ => false, 
		};
	}

	private static void ApplyPlan(IReadOnlyList<RegistryChange> plan, string root = "Software\\Microsoft\\Windows")
	{
		foreach (RegistryChange item in plan)
		{
			if (!item.Key.StartsWith("Software\\Microsoft\\Windows", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("RegistryChange targets '" + item.Key + "', which ApplyPlan cannot rewrite. Every key must live under Software\\Microsoft\\Windows.");
			}
			string text = (item.Key.StartsWith("Software\\Microsoft\\Windows") ? item.Key.Substring("Software\\Microsoft\\Windows".Length).TrimStart('\\') : item.Key);
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(root + ((text.Length > 0) ? ("\\" + text) : ""));
			registryKey.SetValue(item.ValueName, (int)item.DwordValue, RegistryValueKind.DWord);
		}
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SendMessageTimeout(nint hWnd, uint Msg, nuint wParam, string lParam, uint flags, uint timeout, out nint result);

	[DllImport("shell32.dll")]
	private static extern void SHChangeNotify(int eventId, uint flags, nint item1, nint item2);

	private static void BroadcastSettingChange()
	{
		SendMessageTimeout(HWND_BROADCAST, 26u, UIntPtr.Zero, "ImmersiveColorSet", 2u, 100u, out var _);
		SHChangeNotify(134217728, 0u, IntPtr.Zero, IntPtr.Zero);
	}

	private static bool TryOpen(string path, Func<string, IRegistryReadKey?> openKey, [NotNullWhen(true)] out IRegistryReadKey? key)
	{
		key = openKey(path);
		return key != null;
	}

	internal static uint DwmAbgrToArgb(uint dwm)
	{
		uint num = (dwm >> 8) & 0xFF;
		uint num2 = (dwm >> 16) & 0xFF;
		uint num3 = (dwm >> 24) & 0xFF;
		return 0xFF000000u | (num << 16) | (num2 << 8) | num3;
	}

	[DllImport("user32.dll")]
	private static extern int SetWindowCompositionAttribute(nint hWnd, ref WINDOW_COMPOSITION_ATTRIB_DATA data);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint FindWindow(string lpClassName, string lpWindowName);

	[DllImport("user32.dll")]
	private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetClassName(nint hWnd, StringBuilder lpString, int nMaxCount);

	public static TaskbarStyleSupport ProbeTaskbarStyle()
	{
		return ProbeTaskbarStyleThrough(TaskbarWriterFor(FlavourPolicy.Current, WriteAccentToTheTaskbar));
	}

	internal static TaskbarStyleSupport ProbeTaskbarStyleThrough(Func<ACCENT_POLICY, bool> write)
	{
		try
		{
			ACCENT_POLICY arg = new ACCENT_POLICY
			{
				AccentState = 0u
			};
			return (!write(arg)) ? TaskbarStyleSupport.Unsupported : TaskbarStyleSupport.Working;
		}
		catch (DllNotFoundException)
		{
			return TaskbarStyleSupport.Unsupported;
		}
		catch (EntryPointNotFoundException)
		{
			return TaskbarStyleSupport.Unsupported;
		}
	}

	private static bool WriteAccentToTheTaskbar(ACCENT_POLICY accent)
	{
		nint num = FindTaskbarHandle();
		if (num == IntPtr.Zero)
		{
			return false;
		}
		int num2 = Marshal.SizeOf<ACCENT_POLICY>();
		WINDOW_COMPOSITION_ATTRIB_DATA data = new WINDOW_COMPOSITION_ATTRIB_DATA
		{
			Attribute = 19u,
			SizeOfData = num2,
			Data = Marshal.AllocHGlobal(num2)
		};
		try
		{
			Marshal.StructureToPtr(accent, data.Data, fDeleteOld: false);
			return SetWindowCompositionAttribute(num, ref data) != 0;
		}
		finally
		{
			Marshal.FreeHGlobal(data.Data);
		}
	}

	public static uint EffectiveTaskbarColour(TaskbarSettings taskbar, SystemSettings system)
	{
		uint num = ((!taskbar.UseAccentForTaskbar && taskbar.ColorArgb != 0) ? taskbar.ColorArgb : system.AccentColorArgb);
		if (taskbar.Style != TaskbarStyle.Normal)
		{
			uint num2 = (num >> 24) & 0xFF;
			if (num2 < 60)
			{
				num2 = 60u;
			}
			num = (num & 0xFFFFFF) | (num2 << 24);
		}
		return num;
	}

	public static uint EffectiveTaskbarGradientColor(TaskbarSettings taskbar, SystemSettings system)
	{
		return ArgbToDwmAbgr(EffectiveTaskbarColour(taskbar, system));
	}

	public static bool ApplyTaskbarStyle(TaskbarSettings taskbar)
	{
		return ApplyTaskbarStyle(taskbar, new SystemSettings());
	}

	public static bool ApplyTaskbarStyle(TaskbarSettings taskbar, SystemSettings system)
	{
		return ApplyTaskbarStyleThrough(TaskbarWriterFor(FlavourPolicy.Current, WriteAccentToTheTaskbar), taskbar, system);
	}

	internal static bool ApplyTaskbarStyleThrough(Func<ACCENT_POLICY, bool> write, TaskbarSettings taskbar, SystemSettings system)
	{
		try
		{
			ACCENT_POLICY arg = new ACCENT_POLICY
			{
				AccentState = taskbar.Style switch
				{
					TaskbarStyle.Normal => 0u, 
					TaskbarStyle.Acrylic => 4u, 
					TaskbarStyle.Clear => 2u, 
					TaskbarStyle.Blur => 3u, 
					TaskbarStyle.Fluent => 5u, 
					_ => 0u, 
				},
				AccentFlags = 2u,
				GradientColor = EffectiveTaskbarGradientColor(taskbar, system)
			};
			return write(arg);
		}
		catch
		{
			return false;
		}
	}

	private static nint FindTaskbarHandle()
	{
		nint found = IntPtr.Zero;
		EnumWindows((nint hWnd, nint lParam) =>
		{
			StringBuilder stringBuilder = new StringBuilder(256);
			GetClassName(hWnd, stringBuilder, stringBuilder.Capacity);
			if (stringBuilder.ToString() == "Shell_TrayWnd")
			{
				found = hWnd;
				return false;
			}
			return true;
		}, IntPtr.Zero);
		return found;
	}
}
