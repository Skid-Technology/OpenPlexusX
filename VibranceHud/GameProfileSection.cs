using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud;

public sealed class GameProfileSection
{
	private readonly IVibranceEngine _engine;

	private readonly string _gameId;

	private readonly string _gameName;

	private readonly AppSettings? _settings;

	private readonly Func<string, GameProfile?> _lookupProfile;

	private readonly Action<GameProfile> _saveProfile;

	private readonly Action<string> _removeProfile;

	private readonly Action? _onChanged;

	private readonly Label _caption;

	private readonly Label _summary;

	private readonly GlassButton _save;

	private readonly GlassButton _forget;

	public int PreferredHeight => Tokens.Scale(22) + Tokens.Scale(6) + Tokens.Scale(38) + Tokens.Scale(38);

	public GameProfileSection(Control parent, string gameId, string gameName, IVibranceEngine engine, Font captionFont, Font bodyFont, AppSettings? settings = null, Func<string, GameProfile?>? lookup = null, Action<GameProfile>? save = null, Action<string>? remove = null, Action? onChanged = null)
	{
		_engine = engine;
		_gameId = gameId;
		_gameName = gameName;
		_settings = settings;
		_lookupProfile = lookup ?? new Func<string, GameProfile>(GameProfileStore.Get);
		_saveProfile = save ?? new Action<GameProfile>(GameProfileStore.Set);
		_removeProfile = remove ?? new Action<string>(GameProfileStore.Remove);
		_onChanged = onChanged;
		_caption = new Label
		{
			Text = UiHelpers.Spaced("PROFILE"),
			Font = captionFont,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.MiddleLeft
		};
		parent.Controls.Add(_caption);
		_summary = new Label
		{
			Font = bodyFont,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TextAlign = ContentAlignment.TopLeft
		};
		parent.Controls.Add(_summary);
		_save = new GlassButton
		{
			Text = "Use my current look for " + Shorten(gameName),
			Kind = GlassButtonKind.Primary
		};
		_save.Click += (object? _, EventArgs _) =>
		{
			Save();
		};
		parent.Controls.Add(_save);
		_forget = new GlassButton
		{
			Text = "Forget",
			Kind = GlassButtonKind.Secondary
		};
		_forget.Click += (object? _, EventArgs _) =>
		{
			Forget();
		};
		parent.Controls.Add(_forget);
		Refresh();
	}

	internal static string Shorten(string name)
	{
		if (name.Length > 14)
		{
			return name.Substring(0, 13).TrimEnd() + "…";
		}
		return name;
	}

	private void Save()
	{
		_saveProfile(Capture(_gameId, _gameName, _engine, _lookupProfile(_gameId)));
		if (_settings != null)
		{
			new GameProfileApplyGate(_settings).UserOptedBackIn();
		}
		Refresh();
		_onChanged?.Invoke();
	}

	internal static GameProfile Capture(string gameId, string gameName, IVibranceEngine engine, GameProfile? existing)
	{
		return new GameProfile
		{
			GameId = gameId,
			DisplayName = (string.IsNullOrWhiteSpace(existing?.DisplayName) ? gameName : existing.DisplayName),
			Vibrance = engine.Vibrance,
			Saturation = engine.Saturation,
			Brightness = engine.Brightness,
			Gamma = engine.Gamma,
			Contrast = engine.Contrast,
			Temperature = engine.Temperature,
			Tone = engine.Tone,
			GameHub = (existing?.GameHub ?? new GameHubOptions()),
			LastUpdated = DateTime.UtcNow
		};
	}

	internal static GameProfile CaptureFromPreset(string gameId, string gameName, DisplayPreset preset, GameProfile? existing)
	{
		return new GameProfile
		{
			GameId = gameId,
			DisplayName = (string.IsNullOrWhiteSpace(existing?.DisplayName) ? gameName : existing.DisplayName),
			Vibrance = preset.Vibrance,
			Saturation = preset.Saturation,
			Brightness = preset.Brightness,
			Gamma = preset.Gamma,
			Contrast = preset.Contrast,
			Temperature = preset.Temperature,
			Tone = new ToneSettings(preset.Gamma, preset.Highlights, preset.Shadows),
			GameHub = (existing?.GameHub ?? new GameHubOptions()),
			LastUpdated = DateTime.UtcNow
		};
	}

	private void Forget()
	{
		_removeProfile(_gameId);
		Refresh();
		_onChanged?.Invoke();
	}

	public void Refresh()
	{
		GameProfile gameProfile = _lookupProfile(_gameId);
		if (gameProfile == null)
		{
			_summary.Text = "No profile yet. Set your colours on Display, then save them here and PlexusX will switch to them whenever " + _gameName + " opens.";
			_forget.Visible = false;
			_save.Text = "Use my current look for " + Shorten(_gameName);
		}
		else
		{
			_summary.Text = Describe(gameProfile) + "  —  applied when " + _gameName + " opens, put back when it closes.";
			_forget.Visible = true;
			_save.Text = "Update to my current look";
		}
	}

	internal static string Describe(GameProfile p)
	{
		List<string> list = new List<string>();
		if (p.Saturation != 100)
		{
			list.Add($"saturation {p.Saturation}");
		}
		if (p.Vibrance != 100)
		{
			list.Add($"vibrance {p.Vibrance}");
		}
		if (p.Brightness != 100)
		{
			list.Add($"brightness {p.Brightness}");
		}
		if (p.Gamma != 100)
		{
			list.Add("gamma " + ((float)p.Gamma / 100f).ToString("0.00", CultureInfo.InvariantCulture));
		}
		if (p.ResolvedContrast != 100)
		{
			list.Add($"contrast {p.ResolvedContrast}");
		}
		if (p.ResolvedTemperature != 0)
		{
			list.Add((p.ResolvedTemperature > 0) ? $"warm {p.ResolvedTemperature}" : $"cool {-p.ResolvedTemperature}");
		}
		if (!p.ResolvedTone.IsGammaOnly)
		{
			list.Add("advanced colour");
		}
		if (list.Count != 0)
		{
			return "Saved: " + string.Join(", ", list);
		}
		return "Saved: neutral";
	}

	internal static bool ChangesAnything(GameProfile p)
	{
		return Describe(p) != "Saved: neutral";
	}

	public static CardPanel BuildCard(string gameId, string gameName, IVibranceEngine engine, int x, int y, int width, AppSettings? settings = null, Func<string, GameProfile?>? lookup = null, Action<GameProfile>? save = null, Action<string>? remove = null, Action? onChanged = null)
	{
		CardPanel cardPanel = new CardPanel
		{
			Location = new Point(x, y),
			Size = new Size(width, 10)
		};
		GameProfileSection gameProfileSection = new GameProfileSection(cardPanel, gameId, gameName, engine, Fonts.Micro, Fonts.Caption, settings, lookup, save, remove, onChanged);
		int num = Tokens.Scale(18);
		gameProfileSection.Place(num, num, width - 2 * num);
		cardPanel.Height = gameProfileSection.PreferredHeight + 2 * num;
		return cardPanel;
	}

	public void Place(int x, int y, int width)
	{
		int num = Tokens.Scale(22);
		int height = Tokens.Scale(32);
		int num2 = Tokens.Scale(84);
		int num3 = Tokens.Scale(8);
		_caption.SetBounds(x, y, width, num);
		y += num + Tokens.Scale(6);
		_summary.SetBounds(x, y, width, Tokens.Scale(36));
		y += Tokens.Scale(38);
		int num4 = Math.Min(Tokens.Scale(260), width - num2 - num3);
		_save.SetBounds(x, y, num4, height);
		_forget.SetBounds(x + num4 + num3, y, num2, height);
	}
}
