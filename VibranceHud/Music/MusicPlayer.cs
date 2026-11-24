using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Hotkeys;

namespace VibranceHud.Music;

internal static class MusicPlayer
{
	internal sealed class TrayAttachment : IDisposable
	{
		private readonly INowPlayingSource _source;

		private readonly IMediaKeys _keys;

		private readonly List<MusicHotkey> _hotkeys = new List<MusicHotkey>();

		private readonly AppSettings _settings;

		private readonly Action? _savePosition;

		private readonly ContextMenuStrip _menu;

		private readonly HotkeyCaptureRelease _captureRelease;

		private MusicMiniWindow? _mini;

		internal ToolStripItem NowPlayingItem { get; }

		internal MusicMiniWindow? MiniWindow => _mini;

		internal bool KeyBound => _hotkeys.Count != 0;

		internal TrayAttachment(ContextMenuStrip menu, AppSettings settings, INowPlayingSource source, IMediaKeys keys, Action? savePosition)
		{
			_source = source;
			_keys = keys;
			_settings = settings;
			_savePosition = savePosition;
			_menu = menu;
			_captureRelease = new HotkeyCaptureRelease(HotkeyCaptureGate.Shared, ReleaseKeys, () =>
			{
				Rebind();
			});
			menu.Items.Add(new ToolStripSeparator
			{
				Tag = TrayTag
			});
			NowPlayingItem = Add(menu, "Now playing: ", null);
			NowPlayingItem.Enabled = false;
			NowPlayingItem.Visible = false;
			Add(menu, "Play / pause", () =>
			{
				Command(MediaKey.PlayPause);
			});
			Add(menu, "Next track", () =>
			{
				Command(MediaKey.Next);
			});
			Add(menu, "Previous track", () =>
			{
				Command(MediaKey.Previous);
			});
			Add(menu, "Music mini player", ToggleMini);
			menu.Opening += (object? _, CancelEventArgs _) =>
			{
				ReadNow();
			};
			Rebind();
			HotkeysChanged += Rebind;
			menu.Disposed += (object? _, EventArgs _) =>
			{
				Dispose();
			};
			Application.ApplicationExit += OnExit;
		}

		private static ToolStripItem Add(ContextMenuStrip menu, string words, Action? onClick)
		{
			EventHandler onClick2 = ((onClick == null) ? null : ((EventHandler)((object? _, EventArgs _) =>
			{
				onClick();
			})));
			ToolStripItem toolStripItem = menu.Items.Add(words, null, onClick2);
			toolStripItem.Tag = TrayTag;
			return toolStripItem;
		}

		internal void ReadNow()
		{
			if (_source.NeedsWorker)
			{
				Task.Run(() =>
				{
					NowPlaying result;
					try
					{
						result = _source.Read();
					}
					catch
					{
						result = null;
					}
					try
					{
						_menu.BeginInvoke(() =>
						{
							SetTraySnapshot(result);
							result?.Art?.Dispose();
						});
					}
					catch
					{
						result?.Art?.Dispose();
					}
				});
			}
			else
			{
				NowPlaying traySnapshot;
				try
				{
					traySnapshot = _source.Read();
				}
				catch
				{
					traySnapshot = null;
				}
				SetTraySnapshot(traySnapshot);
			}
		}

		private void SetTraySnapshot(NowPlaying? now)
		{
			if (!_menu.IsDisposed)
			{
				NowPlayingItem.Text = ((now == null) ? "Now playing: " : ("Now playing: " + now.Line));
				NowPlayingItem.Visible = now != null;
			}
		}

		private void Command(MediaKey key)
		{
			MusicPoller.For(_source).Command(key, _keys, _menu);
		}

		private bool Rebind(HotkeySlot? requested = null)
		{
			ReleaseKeys();
			if (HotkeyCaptureGate.Shared.IsCapturing)
			{
				return true;
			}
			bool result = true;
			HotkeySlot[] slots = MusicHotkey.Slots;
			foreach (HotkeySlot slot in slots)
			{
				(bool, uint, uint) tuple = MusicHotkey.Binding(_settings, slot);
				if (!tuple.Item1 || tuple.Item3 == 0)
				{
					continue;
				}
				Action onPressed = ((slot == HotkeySlot.MusicMini) ? new Action(ToggleMini) : ((Action)(() =>
				{
					Command((slot == HotkeySlot.MusicNext) ? MediaKey.Next : ((slot == HotkeySlot.MusicPrevious) ? MediaKey.Previous : MediaKey.PlayPause));
				})));
				MusicHotkey musicHotkey = MusicHotkey.TryRegister(_settings, slot, onPressed);
				if (musicHotkey == null)
				{
					if (!requested.HasValue || requested == slot)
					{
						result = false;
					}
				}
				else
				{
					_hotkeys.Add(musicHotkey);
				}
			}
			return result;
		}

		private void ReleaseKeys()
		{
			foreach (MusicHotkey hotkey in _hotkeys)
			{
				hotkey.Dispose();
			}
			_hotkeys.Clear();
		}

		internal void ToggleMini()
		{
			if (_mini == null || _mini.IsDisposed)
			{
				_mini = new MusicMiniWindow(_source, _keys, _settings, _savePosition);
			}
			_mini.Toggle();
		}

		private void OnExit(object? sender, EventArgs e)
		{
			Dispose();
		}

		public void Dispose()
		{
			Application.ApplicationExit -= OnExit;
			HotkeysChanged -= Rebind;
			_captureRelease.Dispose();
			ReleaseKeys();
			if (_mini != null && !_mini.IsDisposed)
			{
				_mini.Dispose();
			}
			_mini = null;
		}
	}

	internal const bool Enabled = true;

	internal static readonly object TrayTag = new object();

	internal const string NowPlayingPrefix = "Now playing: ";

	internal const string PlayPauseWords = "Play / pause";

	internal const string NextWords = "Next track";

	internal const string PreviousWords = "Previous track";

	internal const string MiniPlayerWords = "Music mini player";

	internal static INowPlayingSource DefaultSource { get; set; } = new WindowsSessionSource();

	internal static IMediaKeys DefaultKeys { get; set; } = new WindowsMediaKeys();

	internal static event Func<HotkeySlot?, bool>? HotkeysChanged;

	internal static bool RebindHotkeys(HotkeySlot? slot = null)
	{
		return HotkeysChanged?.Invoke(slot) ?? true;
	}

	internal static TrayAttachment? AttachToTray(ContextMenuStrip menu, AppSettings settings, bool enabled = true, INowPlayingSource? source = null, IMediaKeys? keys = null, Action? savePosition = null)
	{
		if (!enabled)
		{
			return null;
		}
		return new TrayAttachment(menu, settings, source ?? DefaultSource, keys ?? DefaultKeys, savePosition);
	}

	internal static MusicStrip? AttachToPopup(Form popup, Control slot, bool enabled = true, INowPlayingSource? source = null, IMediaKeys? keys = null)
	{
		if (!enabled)
		{
			return null;
		}
		MusicStrip strip = new MusicStrip(slot, source ?? DefaultSource, keys ?? DefaultKeys);
		popup.Controls.Add(strip);
		strip.BringToFront();
		strip.AttachPoller(popup);
		popup.VisibleChanged += (object? _, EventArgs _) =>
		{
			if (popup.Visible)
			{
				strip.ReadNow();
			}
		};
		popup.Activated += (object? _, EventArgs _) =>
		{
			strip.ReadNow();
		};
		return strip;
	}
}
