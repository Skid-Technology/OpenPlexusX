using System;
using System.Drawing;
using System.Windows.Forms;
using VibranceHud.Design;

namespace VibranceHud.Controls;

public sealed class GlassDialog : SignalDialogWindow
{
	private const int WidthLogical = 460;

	private const int MaxBodyLogical = 420;

	private readonly GlassDialogButtons _buttons;

	private readonly GlassButton _safe;

	private readonly GlassButton? _action;

	internal GlassButton TestOpeningButton => (GlassButton)ActiveControl;

	internal GlassButton? TestActionButton => _action;

	internal GlassButton TestSafeButton => _safe;

	internal static Func<string, string, DialogResult>? InsteadOfShowingForTest { get; set; }

	public static int ButtonCount(GlassDialogButtons b)
	{
		if (b != GlassDialogButtons.Ok)
		{
			return 2;
		}
		return 1;
	}

	public static int MeasureHeight(string body, int width)
	{
		int logical = Math.Max(80, width - 48);
		int val = (int)Math.Ceiling((double)(SignalDialogWindow.MeasureBody(body, Tokens.Scale(logical)) * 96) / (double)Tokens.Dpi);
		return 136 + Math.Min(val, 420);
	}

	internal GlassDialog(string title, string body, GlassDialogButtons buttons, GlassDialogTone tone, string? primaryText = null, string? secondaryText = null)
	{
		GlassDialog glassDialog = this;
		_buttons = buttons;
		ChromeTitle = title ?? "PlexusX";
		ChromeBody = body ?? "";
		Text = ChromeTitle;
		AccessibleName = ChromeTitle;
		AccessibleDescription = ChromeBody;
		int num = S(460);
		ChromeBodyHeight = Math.Min(SignalDialogWindow.MeasureBody(ChromeBody, num - 2 * S(24)), S(420));
		ClientSize = new Size(num, BodyTop + ChromeBodyHeight + S(24) + S(32) + S(20));
		if (buttons == GlassDialogButtons.Ok)
		{
			_safe = FooterButton(secondaryText ?? primaryText ?? "Close", GlassButtonKind.Secondary, "DialogClose");
			_safe.Click += (object? _, EventArgs _) =>
			{
				glassDialog.Finish(DialogResult.OK);
			};
			Controls.Add(_safe);
			PlaceFooter(_safe);
		}
		else
		{
			bool flag = buttons == GlassDialogButtons.Destructive;
			_action = FooterButton(primaryText ?? "Continue", flag ? GlassButtonKind.Destructive : GlassButtonKind.Primary, "DialogAction");
			_action.Click += (object? _, EventArgs _) =>
			{
				glassDialog.Finish(Affirmative(buttons));
			};
			_safe = FooterButton(secondaryText ?? "Cancel", GlassButtonKind.Secondary, "DialogSafe");
			_safe.Click += (object? _, EventArgs _) =>
			{
				glassDialog.Finish(Negative(buttons));
			};
			Controls.Add(_safe);
			Controls.Add(_action);
			PlaceFooter(_safe, _action);
		}
		GlassDialogButtons glassDialogButtons = buttons;
		GlassButton glassButton = (((uint)(glassDialogButtons - 1) <= 1u) ? _action : _safe);
		if (_action != null)
		{
			_safe.TabIndex = ((glassButton != _safe) ? 1 : 0);
			_action.TabIndex = ((glassButton != _action) ? 1 : 0);
		}
		ActiveControl = glassButton;
		KeyDown += (object? _, KeyEventArgs e) =>
		{
			Keys keyCode = e.KeyCode;
			if ((keyCode == Keys.Return || keyCode == Keys.Escape) ? true : false)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				glassDialog.Answer(e.KeyCode);
			}
		};
		WindowDrag.Enable(this, this);
	}

	private void Answer(Keys key)
	{
		if (key == Keys.Escape)
		{
			Finish(Negative(_buttons));
			return;
		}
		GlassDialogButtons buttons = _buttons;
		bool flag = (uint)(buttons - 1) <= 1u;
		Finish(flag ? Affirmative(_buttons) : Negative(_buttons));
	}

	protected override bool ProcessDialogKey(Keys keyData)
	{
		bool flag = (keyData & (Keys.Control | Keys.Alt)) == 0;
		if (flag)
		{
			Keys keys = keyData & Keys.KeyCode;
			bool flag2 = ((keys == Keys.Return || keys == Keys.Escape) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			Answer(keyData & Keys.KeyCode);
			return true;
		}
		return base.ProcessDialogKey(keyData);
	}

	private static DialogResult Affirmative(GlassDialogButtons b)
	{
		if ((uint)(b - 2) <= 2u)
		{
			return DialogResult.Yes;
		}
		return DialogResult.OK;
	}

	private static DialogResult Negative(GlassDialogButtons b)
	{
		switch (b)
		{
		case GlassDialogButtons.Ok:
			return DialogResult.OK;
		case GlassDialogButtons.YesNo:
		case GlassDialogButtons.Destructive:
		case GlassDialogButtons.ConfirmSafeEnter:
			return DialogResult.No;
		default:
			return DialogResult.Cancel;
		}
	}

	private void Finish(DialogResult result)
	{
		DialogResult = result;
		Close();
	}

	internal void TestPressKey(Keys key)
	{
		Answer(key);
	}

	public static DialogResult Show(IWin32Window? owner, string title, string body, GlassDialogButtons buttons = GlassDialogButtons.Ok, GlassDialogTone tone = GlassDialogTone.Info, string? primaryText = null, string? secondaryText = null)
	{
		Func<string, string, DialogResult> insteadOfShowingForTest = InsteadOfShowingForTest;
		if (insteadOfShowingForTest != null)
		{
			return insteadOfShowingForTest(title, body);
		}
		using GlassDialog glassDialog = new GlassDialog(title, body, buttons, tone, primaryText, secondaryText);
		return (owner != null) ? glassDialog.ShowDialog(owner) : glassDialog.ShowDialog();
	}
}
