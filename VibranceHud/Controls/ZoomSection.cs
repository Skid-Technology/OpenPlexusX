using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Zoom;

namespace VibranceHud.Controls;

public sealed class ZoomSection : Panel
{
	internal const string Title = "Hold to zoom";

	internal const string FairPlay = "Zoom uses Windows' own Magnifier. Some games' rules treat zoom as an advantage - check yours.";

	internal const string LockedLine = "Zoom comes with the Dark Boost & Zoom add-on.";

	internal const string PickAKey = "Pick a key to hold.";

	internal const string EdgesWord = "Smooth edges";

	internal const string EdgesHint = "Off: sharp pixels. On: softer, a little blurry.";

	private readonly AppSettings _settings;

	private readonly Action _save;

	private readonly Func<bool> _allowed;

	private readonly Func<uint, uint, bool> _bindKey;

	private readonly Func<string> _whyRefused;

	private bool _syncing;

	private readonly Label _title;

	private readonly Label _keyWord;

	private readonly Label _levelWord;

	private readonly Label _levelValue;

	private readonly Label _smoothWord;

	private readonly Label _edgesWord;

	private readonly Label _edgesHint;

	private readonly Label _status;

	private readonly Label _fair;

	private readonly ToggleSwitch _on;

	private readonly ToggleSwitch _smooth;

	private readonly ToggleSwitch _edges;

	private readonly HotkeyPicker _key;

	private readonly FlatSlider _level;

	public int WantedHeight => RowPitch * 4 + EdgesBlockHeight() + Tokens.Scale(20) + FairHeight() + Tokens.Scale(8);

	private int RowPitch => Tokens.Scale(40);

	public int Gutter => FlatSlider.EdgeInsetAt(ControlInk.Dpi(this));

	internal ToggleSwitch SwitchForTest => _on;

	internal ToggleSwitch EdgesForTest => _edges;

	internal HotkeyPicker KeyForTest => _key;

	internal FlatSlider LevelForTest => _level;

	internal string StatusForTest => _status.Text;

	public event Action? Changed;

	public ZoomSection(AppSettings settings, Action save, Func<bool> allowed, Func<uint, uint, bool> bindKey, Func<string> whyRefused)
	{
		_settings = settings;
		_save = save;
		_allowed = allowed;
		_bindKey = bindKey;
		_whyRefused = whyRefused;
		BackColor = Color.Transparent;
		Name = "zoomSection";
		_title = Words("Hold to zoom", Fonts.BodyBold, Theme.Text);
		_on = new ToggleSwitch
		{
			Size = new Size(40, 20),
			AccessibleName = "Hold to zoom",
			Name = "zoomSwitch"
		};
		_on.CheckedChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetEnabled(_on.Checked);
			}
		};
		_keyWord = Words("Zoom key", Fonts.Body, Theme.TextDim);
		_key = new HotkeyPicker
		{
			AllowMouseButtons = true,
			AccessibleName = "Zoom key",
			Name = "zoomKey"
		};
		_key.HotkeyChanged += (uint mask, uint vk) =>
		{
			ChooseKey(mask, vk);
		};
		_levelWord = Words("Zoom level", Fonts.Body, Theme.TextDim);
		_levelValue = Words("", Fonts.BodyBold, Theme.Text);
		_levelValue.TextAlign = ContentAlignment.MiddleRight;
		_level = new FlatSlider
		{
			AccessibleName = "Zoom level",
			Name = "zoomLevel",
			Minimum = (int)Math.Round(6.0),
			Maximum = (int)Math.Round(16.0),
			Value = (int)Math.Round(8.0)
		};
		_level.ValueChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetLevel((float)_level.Value * 0.25f);
			}
		};
		_smoothWord = Words("Smooth", Fonts.Body, Theme.TextDim);
		_smooth = new ToggleSwitch
		{
			Size = new Size(40, 20),
			AccessibleName = "Smooth zoom",
			Name = "zoomSmooth"
		};
		_smooth.CheckedChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetSmooth(_smooth.Checked);
			}
		};
		_edgesWord = Words("Smooth edges", Fonts.Body, Theme.TextDim);
		_edges = new ToggleSwitch
		{
			Size = new Size(40, 20),
			AccessibleName = "Smooth edges",
			Name = "zoomSmoothEdges"
		};
		_edges.CheckedChanged += (object? _, EventArgs _) =>
		{
			if (!_syncing)
			{
				SetSmoothEdges(_edges.Checked);
			}
		};
		_edgesHint = Words("Off: sharp pixels. On: softer, a little blurry.", Fonts.Caption, Theme.TextFaint);
		_status = Words("", Fonts.Caption, Theme.TextDim);
		_fair = Words("Zoom uses Windows' own Magnifier. Some games' rules treat zoom as an advantage - check yours.", Fonts.Caption, Theme.TextFaint);
		Controls.AddRange(new Control[14]
		{
			_title, _on, _keyWord, _key, _levelWord, _levelValue, _level, _smoothWord, _smooth, _edgesWord,
			_edges, _edgesHint, _status, _fair
		});
		Sync();
	}

	private Label Words(string text, Font font, Color colour)
	{
		return new Label
		{
			Text = text,
			Font = font,
			ForeColor = colour,
			BackColor = Color.Transparent,
			TextAlign = ContentAlignment.MiddleLeft,
			AutoSize = false,
			UseMnemonic = false
		};
	}

	private static int EdgesBlockHeight()
	{
		return Tokens.Scale(26) + Tokens.Scale(20) + Tokens.Scale(4);
	}

	private int FairHeight()
	{
		return Tokens.Scale(36);
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		int gutter = Gutter;
		int num = Math.Max(Tokens.Scale(200), ClientSize.Width - 2 * gutter);
		int rowPitch = RowPitch;
		int num2 = Tokens.Scale(26);
		int num3 = Tokens.Scale(40);
		int num4 = Tokens.Scale(20);
		int num5 = 0;
		_title.SetBounds(gutter - Ink(_title), num5, num - num3 - 8, num2);
		_on.SetBounds(gutter + num - num3, num5 + (num2 - num4) / 2, num3, num4);
		num5 += rowPitch;
		int num6 = Math.Min(num - Tokens.Scale(110), Tokens.Scale(300));
		_keyWord.SetBounds(gutter - Ink(_keyWord), num5, Tokens.Scale(100), num2);
		_key.SetBounds(gutter + num - num6, num5 - Tokens.Scale(4), num6, Tokens.Scale(36));
		num5 += rowPitch;
		_levelWord.SetBounds(gutter - Ink(_levelWord), num5, num - Tokens.Scale(70), num2);
		_levelValue.SetBounds(gutter + num - Tokens.Scale(70) + Ink(_levelValue), num5, Tokens.Scale(70), num2);
		_level.SetTrackBounds(gutter, num5 + num2 + Tokens.Scale(2), num, Tokens.Scale(16));
		num5 += rowPitch + Tokens.Scale(8);
		_smoothWord.SetBounds(gutter - Ink(_smoothWord), num5, num - num3 - 8, num2);
		_smooth.SetBounds(gutter + num - num3, num5 + (num2 - num4) / 2, num3, num4);
		num5 += rowPitch - Tokens.Scale(8);
		_edgesWord.SetBounds(gutter - Ink(_edgesWord), num5, num - num3 - 8, num2);
		_edges.SetBounds(gutter + num - num3, num5 + (num2 - num4) / 2, num3, num4);
		num5 += num2;
		_edgesHint.SetBounds(gutter - Ink(_edgesHint), num5, num, Tokens.Scale(20));
		num5 += Tokens.Scale(20) + Tokens.Scale(4);
		_status.SetBounds(gutter - Ink(_status), num5, num, Tokens.Scale(20));
		_fair.SetBounds(gutter - Ink(_fair), num5 + Tokens.Scale(20), num, FairHeight());
	}

	private static int Ink(Label l)
	{
		return ControlInk.LabelInkInset(l.Font);
	}

	public void Sync()
	{
		_syncing = true;
		try
		{
			bool flag = _allowed();
			_on.Checked = _settings.ZoomEnabled & flag;
			ToggleSwitch toggleSwitch = _on;
			HotkeyPicker key = _key;
			FlatSlider level = _level;
			ToggleSwitch smooth = _smooth;
			bool flag2 = (_edges.Enabled = flag);
			bool flag4 = (smooth.Enabled = flag2);
			bool flag6 = (level.Enabled = flag4);
			bool enabled = (key.Enabled = flag6);
			toggleSwitch.Enabled = enabled;
			_key.ModifierMask = _settings.ZoomHotkeyModifierMask;
			_key.VirtualKey = _settings.ZoomHotkeyVirtualKey;
			float num = ZoomMath.ClampSetting(_settings.ZoomLevel);
			_level.Value = (int)Math.Round(num / 0.25f);
			_levelValue.Text = num.ToString("0.0#", CultureInfo.InvariantCulture) + "x";
			_smooth.Checked = _settings.ZoomSmooth;
			_edges.Checked = _settings.ZoomSmoothEdges;
			_status.Text = StatusLine();
		}
		finally
		{
			_syncing = false;
		}
	}

	internal string StatusLine()
	{
		if (!_allowed())
		{
			return "Zoom comes with the Dark Boost & Zoom add-on.";
		}
		string text = _whyRefused();
		if (text.Length > 0)
		{
			return text;
		}
		if (!_settings.ZoomEnabled)
		{
			return "";
		}
		if (_settings.ZoomHotkeyVirtualKey == 0)
		{
			return "Pick a key to hold.";
		}
		string display = HotkeyPicker.GetDisplay(_settings.ZoomHotkeyModifierMask, _settings.ZoomHotkeyVirtualKey);
		HotkeySlot? hotkeySlot = HotkeyConflicts.Owner(_settings, HotkeySlot.Zoom, _settings.ZoomHotkeyModifierMask, _settings.ZoomHotkeyVirtualKey);
		if (hotkeySlot.HasValue)
		{
			HotkeySlot valueOrDefault = hotkeySlot.GetValueOrDefault();
			return display + " is taken by " + HotkeyConflicts.Describe(valueOrDefault) + ". Pick another key.";
		}
		return "Hold " + display + (_settings.HotkeysOnlyInGames ? " in a game to zoom." : " to zoom.");
	}

	internal void SetEnabled(bool on)
	{
		if (!_allowed())
		{
			Sync();
			return;
		}
		_settings.ZoomEnabled = on;
		_save();
		Changed?.Invoke();
		Sync();
	}

	internal void ChooseKey(uint mask, uint vk)
	{
		if (_syncing)
		{
			return;
		}
		if (!_allowed())
		{
			Sync();
			return;
		}
		if (vk == 0 || _bindKey(mask, vk))
		{
			_settings.ZoomHotkeyModifierMask = ((vk != 0) ? mask : 0u);
			_settings.ZoomHotkeyVirtualKey = vk;
			_save();
			Changed?.Invoke();
		}
		Sync();
	}

	internal void SetLevel(float level)
	{
		if (!_allowed())
		{
			Sync();
			return;
		}
		_settings.ZoomLevel = ZoomMath.ClampSetting(level);
		_save();
		Changed?.Invoke();
		Sync();
	}

	internal void SetSmooth(bool on)
	{
		if (!_allowed())
		{
			Sync();
			return;
		}
		_settings.ZoomSmooth = on;
		_save();
		Changed?.Invoke();
		Sync();
	}

	internal void SetSmoothEdges(bool on)
	{
		if (!_allowed())
		{
			Sync();
			return;
		}
		_settings.ZoomSmoothEdges = on;
		_save();
		Changed?.Invoke();
		Sync();
	}
}
