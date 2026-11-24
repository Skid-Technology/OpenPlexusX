using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Monitors;

namespace VibranceHud.Controls;

internal sealed class MonitorSelectionCard : IDisposable
{
	private readonly MonitorSelection _selection;

	private readonly MonitorIdentifier _identifier;

	private readonly Action _save;

	private readonly Label _heading = Label("Apply to", Fonts.BodyBold);

	private readonly Label _hint = Label("Unticked monitors keep their own colours.", Fonts.Caption);

	private readonly Label _summary = Label("", Fonts.Caption);

	private readonly Label _limitation = Label("", Fonts.Caption);

	private readonly Label _empty = Label("No monitors found.", Fonts.Caption);

	private readonly GlassButton _identify = new GlassButton
	{
		Text = "Identify screens",
		AccessibleName = "Identify screens"
	};

	private readonly List<MonitorSelectionTile> _tiles = new List<MonitorSelectionTile>();

	public CardPanel View { get; } = new CardPanel
	{
		Name = "monitor-selection"
	};

	public MonitorSelectionCard(MonitorSelection selection, MonitorIdentifier identifier, Action save)
	{
		_selection = selection;
		_identifier = identifier;
		_save = save;
		View.Controls.AddRange(new Control[6] { _heading, _hint, _identify, _summary, _limitation, _empty });
		_identify.Click += (object? _, EventArgs _) =>
		{
			_identifier.Identify(_selection.Groups);
		};
		_selection.Changed += Refresh;
		Refresh();
	}

	private static Label Label(string text, Font font)
	{
		return new Label
		{
			Text = text,
			Font = font,
			ForeColor = Theme.TextDim,
			BackColor = Color.Transparent,
			AutoSize = false,
			TabStop = false
		};
	}

	private void Refresh()
	{
		IReadOnlyList<ColourMonitorGroup> groups = _selection.Groups;
		View.Visible = _selection.WorthShowing;
		if (_tiles.Count != groups.Count || _tiles.Where((MonitorSelectionTile t, int i) => t.Group.Identity != groups[i].Identity).Any())
		{
			foreach (MonitorSelectionTile tile2 in _tiles)
			{
				View.Controls.Remove(tile2);
				tile2.Dispose();
			}
			_tiles.Clear();
			for (int num = 0; num < groups.Count; num++)
			{
				MonitorSelectionTile tile = new MonitorSelectionTile(groups[num], num + 1);
				tile.SelectionRequested += (bool chosen) =>
				{
					_selection.SetSelected(tile.Group, chosen);
					_save();
				};
				_tiles.Add(tile);
				View.Controls.Add(tile);
			}
		}
		for (int num2 = 0; num2 < _tiles.Count; num2++)
		{
			_tiles[num2].Group = groups[num2];
			_tiles[num2].Checked = _selection.IsSelected(groups[num2]);
			_tiles[num2].Enabled = true;
		}
		_identify.Enabled = groups.Count > 0;
		_empty.Visible = groups.Count == 0;
		IEnumerable<string> enumerable = from x in groups.Select((ColourMonitorGroup g, int i) => (g: g, i: i))
			where _selection.IncludesDevice(x.g.DeviceName)
			select "Monitor " + (x.i + 1);
		_summary.Text = "Ticked: " + (enumerable.Any() ? string.Join(", ", enumerable) : "None");
		Label limitation = _limitation;
		string text;
		if (groups.Count != 0)
		{
			text = (_selection.AllowsMatrix ? "" : "Saturation, hue and other whole-desktop colour effects are paused while a monitor is unticked. Brightness, contrast, temperature, red/green/blue, gamma and NVIDIA Digital Vibrance use the ticked monitors.");
		}
		else
		{
			text = "";
		}
		limitation.Text = text;
		if (groups.Count > 0 && groups.All((ColourMonitorGroup g) => !_selection.IsSelected(g)))
		{
			_limitation.Text = "No monitors selected. Colour changes are paused.";
		}
		if (View.Width > 0)
		{
			Layout(View.Width);
		}
		View.Parent?.PerformLayout();
	}

	public int Layout(int width)
	{
		if (!_selection.WorthShowing)
		{
			View.Height = 0;
			return 0;
		}
		int num = Tokens.Scale(21);
		int num2 = Tokens.Scale(12);
		int num3 = Math.Max(1, width - 2 * num);
		int height = Tokens.Scale(32);
		int width2 = _identify.GetPreferredSize(Size.Empty).Width;
		_heading.ForeColor = Theme.Text;
		_heading.SetBounds(num, num, Math.Max(1, num3 - width2 - num2), Fonts.LineHeight(FontRole.BodyBold));
		_identify.SetBounds(width - num - width2, num, width2, height);
		int y = _heading.Bottom + Tokens.Scale(3);
		int width3 = Math.Max(1, num3 - width2 - num2);
		int height2 = Measure(_hint, width3);
		_hint.SetBounds(num, y, width3, height2);
		int num4 = Math.Max(_hint.Bottom, _identify.Bottom) + Tokens.Scale(16);
		int num5 = ((num3 < Tokens.Scale(640)) ? 1 : 2);
		int tileW = (num3 - num2 * (num5 - 1)) / num5;
		for (int i = 0; i < _tiles.Count; i += num5)
		{
			int num6 = _tiles.Skip(i).Take(num5).Max((MonitorSelectionTile t) => t.GetPreferredSize(new Size(tileW, 0)).Height);
			for (int num7 = 0; num7 < num5 && i + num7 < _tiles.Count; num7++)
			{
				_tiles[i + num7].SetBounds(num + num7 * (tileW + num2), num4, tileW, num6);
			}
			num4 += num6 + num2;
		}
		if (_tiles.Count == 0)
		{
			_empty.SetBounds(num, num4, num3, Measure(_empty, num3));
			num4 = _empty.Bottom + num2;
		}
		else
		{
			_empty.SetBounds(num, num, 0, 0);
		}
		_summary.SetBounds(num, num4, num3, Measure(_summary, num3));
		num4 = _summary.Bottom;
		int num8 = ((_limitation.Text.Length != 0) ? Measure(_limitation, num3) : 0);
		_limitation.SetBounds(num, num4 + ((num8 > 0) ? num2 : 0), num3, num8);
		int num9 = _limitation.Bottom + num;
		View.Height = num9;
		return num9;
	}

	private static int Measure(Label label, int width)
	{
		return TextRenderer.MeasureText(label.Text, label.Font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
	}

	public void Dispose()
	{
		_selection.Changed -= Refresh;
		_identifier.Dispose();
	}
}
