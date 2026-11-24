using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using VibranceHud.Design;
using VibranceHud.Monitors;

namespace VibranceHud.Controls;

internal sealed class MonitorNumberWindow : Form, IMonitorIdentifyWindow, IDisposable
{
	private readonly string _number;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams
	{
		get
		{
			CreateParams createParams = base.CreateParams;
			createParams.ExStyle |= 134217888;
			return createParams;
		}
	}

	internal MonitorNumberWindow(Rectangle screen, int number)
	{
		_number = number.ToString(CultureInfo.InvariantCulture);
		FormBorderStyle = FormBorderStyle.None;
		ShowInTaskbar = false;
		TopMost = true;
		StartPosition = FormStartPosition.Manual;
		BackColor = Theme.Surface;
		Size = new Size(Tokens.Scale(120), Tokens.Scale(96));
		Location = new Point(screen.Left + (screen.Width - Width) / 2, screen.Top + (screen.Height - Height) / 2);
		DoubleBuffered = true;
	}

	public void ShowNumber()
	{
		Show();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		TextRenderer.DrawText(e.Graphics, _number, Fonts.Value, ClientRectangle, Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
		using Pen pen = new Pen(Theme.Border);
		e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
	}
}
