using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public static class Prompt
{
	public static string Ask(string title, string initial)
	{
		return Ask(title, initial, null);
	}

	public static string Ask(string title, string initial, string? body)
	{
		GlassTextBox box;
		using GlassShellForm glassShellForm = Build(title, initial, body, out box);
		return (glassShellForm.ShowDialog() == DialogResult.OK) ? box.Text : "";
	}

	internal static GlassShellForm Build(string title, string initial, out GlassTextBox box)
	{
		return Build(title, initial, null, out box);
	}

	internal static GlassShellForm Build(string title, string initial, string? body, out GlassTextBox box)
	{
		int num = S(24);
		int num2 = S(400);
		int num3 = num2 - 2 * num;
		int num4 = S(32);
		Label label = SignalDialogWindow.TitleLabel(title, num + S(2), S(23), num3);
		Label label2 = null;
		int num5 = S(60);
		if (!string.IsNullOrEmpty(body))
		{
			int height = SignalDialogWindow.MeasureBody(body, num3);
			label2 = SignalDialogWindow.BodyLabel(body, num + S(2), S(60), num3, height);
			num5 = label2.Bottom + S(16);
		}
		int num6 = num5 + num4 + S(24);
		GlassShellForm form = new GlassShellForm(title)
		{
			PaintsTitle = false,
			ClientSize = new Size(num2, num6 + num4 + S(20))
		};
		GlassTextBox field = new GlassTextBox
		{
			Text = initial,
			MaxLength = 40,
			FocusOnEdge = true,
			Location = new Point(num, num5),
			Size = new Size(num3, num4)
		};
		GlassButton ok = new GlassButton
		{
			Text = "Save",
			Kind = GlassButtonKind.Primary
		};
		ok.Size = new Size(SignalDialogWindow.TwinButtonWidth(ok.Text, ok.Font, S(20), S(96)), num4);
		ok.Location = new Point(num2 - num - ok.Width, num6);
		ok.Click += (object? s, EventArgs e) =>
		{
			Finish(DialogResult.OK);
		};
		GlassButton glassButton = new GlassButton
		{
			Text = "Cancel",
			Kind = GlassButtonKind.Secondary
		};
		glassButton.Size = new Size(SignalDialogWindow.TwinButtonWidth(glassButton.Text, glassButton.Font, S(20), S(96)), num4);
		glassButton.Location = new Point(ok.Left - S(8) - glassButton.Width, num6);
		glassButton.Click += (object? s, EventArgs e) =>
		{
			Finish(DialogResult.Cancel);
		};
		field.TextChanged += (object? s, EventArgs e) =>
		{
			SyncSave();
		};
		SyncSave();
		form.KeyDown += (object? s, KeyEventArgs e) =>
		{
			if (e.KeyCode == Keys.Return)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				if (ok.Enabled)
				{
					Finish(DialogResult.OK);
				}
			}
		};
		form.ShowClose = false;
		form.Controls.Add(label);
		WindowDrag.Enable(label, form);
		if (label2 != null)
		{
			form.Controls.Add(label2);
			WindowDrag.Enable(label2, form);
		}
		form.Controls.Add(field);
		form.Controls.Add(ok);
		form.Controls.Add(glassButton);
		form.ActiveControl = field;
		form.Shown += (object? s, EventArgs e) =>
		{
			field.Inner.SelectionLength = 0;
			field.Inner.SelectionStart = field.Inner.TextLength;
		};
		box = field;
		return form;
		void Finish(DialogResult r)
		{
			form.DialogResult = r;
			form.Close();
		}
		static int S(int logical)
		{
			return Tokens.Scale(logical);
		}
		void SyncSave()
		{
			ok.Enabled = !string.IsNullOrWhiteSpace(field.Text);
		}
	}
}
