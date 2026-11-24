using System;
using System.Windows.Forms;
using VibranceHud.Controls;

namespace VibranceHud.DarkBoost;

internal sealed class DarkBoostBanGate
{
	internal const string CardTitle = "Dark Boost can get you banned";

	internal const string Body = "Some games, like Rust, ban players who see in the dark. It mostly happens when another player reports you. PlexusX never touches the game, but the ban risk is yours.";

	internal const string DialogTitle = "Before you turn on Dark Boost";

	internal const string AcceptButton = "I understand the risk";

	internal const string CancelButton = "Cancel";

	private readonly AppSettings _settings;

	private readonly Action _save;

	private readonly Func<IWin32Window?, bool>? _ask;

	internal bool Accepted => _settings.DarkBoostBanRiskAccepted;

	internal DarkBoostBanGate(AppSettings settings, Action save, Func<IWin32Window?, bool>? ask = null)
	{
		_settings = settings ?? throw new ArgumentNullException("settings");
		_save = save ?? ((Action)(() =>
		{
		}));
		_ask = ask;
	}

	internal bool Pass(IWin32Window? owner)
	{
		if (_settings.DarkBoostBanRiskAccepted)
		{
			return true;
		}
		if (!((_ask != null) ? _ask(owner) : AskWithDialog(owner)))
		{
			return false;
		}
		_settings.DarkBoostBanRiskAccepted = true;
		_save();
		return true;
	}

	internal static GlassDialog BuildDialog()
	{
		return new GlassDialog("Before you turn on Dark Boost", "Some games, like Rust, ban players who see in the dark. It mostly happens when another player reports you. PlexusX never touches the game, but the ban risk is yours.", GlassDialogButtons.ConfirmSafeEnter, GlassDialogTone.Warning, "I understand the risk", "Cancel");
	}

	internal static bool AskWithDialog(IWin32Window? owner)
	{
		return GlassDialog.Show(owner, "Before you turn on Dark Boost", "Some games, like Rust, ban players who see in the dark. It mostly happens when another player reports you. PlexusX never touches the game, but the ban risk is yours.", GlassDialogButtons.ConfirmSafeEnter, GlassDialogTone.Warning, "I understand the risk", "Cancel") == DialogResult.Yes;
	}
}
