using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VibranceHud.Music;

internal sealed class MusicPoller
{
	private static readonly ConditionalWeakTable<INowPlayingSource, MusicPoller> Pollers = new ConditionalWeakTable<INowPlayingSource, MusicPoller>();

	private readonly INowPlayingSource _source;

	private readonly System.Windows.Forms.Timer _timer;

	private readonly Dictionary<Control, Action<NowPlaying?>> _views = new Dictionary<Control, Action<NowPlaying>>();

	private volatile bool _busy;

	private int _generation;

	private Control? _lastHost;

	private CancellationTokenSource? _readCancellation;

	private NowPlaying? _snapshot;

	internal bool Running => _timer.Enabled;

	private Control? Host => _views.Keys.FirstOrDefault((Control c) => !c.IsDisposed && c.Visible && c.IsHandleCreated);

	internal static MusicPoller For(INowPlayingSource source)
	{
		return Pollers.GetValue(source, (INowPlayingSource s) => new MusicPoller(s));
	}

	internal MusicPoller(INowPlayingSource source)
	{
		_source = source;
		_timer = new System.Windows.Forms.Timer
		{
			Interval = 1000
		};
		_timer.Tick += (object? _, EventArgs _) =>
		{
			Refresh();
		};
	}

	internal void Attach(Control owner, Action<NowPlaying?> show)
	{
		_views[owner] = show;
		owner.VisibleChanged += (object? _, EventArgs _) =>
		{
			Reassess();
		};
		owner.HandleDestroyed += (object? _, EventArgs _) =>
		{
			if (!owner.RecreatingHandle && owner == _lastHost)
			{
				_lastHost = null;
				_generation++;
				_readCancellation?.Cancel();
				_busy = false;
			}
		};
		owner.Disposed += (object? _, EventArgs _) =>
		{
			_views.Remove(owner);
			Reassess();
			if (_views.Count == 0)
			{
				_timer.Dispose();
				_snapshot?.Art?.Dispose();
				_snapshot = null;
				Pollers.Remove(_source);
			}
		};
		Reassess();
	}

	private void Reassess()
	{
		Control host = Host;
		bool flag = host != null;
		if (host != _lastHost)
		{
			_lastHost = host;
			_generation++;
			_readCancellation?.Cancel();
			_busy = false;
		}
		if (!flag)
		{
			_timer.Stop();
			_generation++;
			_readCancellation?.Cancel();
			_busy = false;
		}
		else if (!_timer.Enabled)
		{
			_timer.Start();
			Refresh();
		}
	}

	internal void Refresh()
	{
		Control host = Host;
		if (host == null || _busy)
		{
			return;
		}
		if (!_source.NeedsWorker)
		{
			Apply(SafeRead());
			return;
		}
		_busy = true;
		int generation = _generation;
		_readCancellation?.Dispose();
		_readCancellation = new CancellationTokenSource();
		CancellationToken cancellation = _readCancellation.Token;
		Task.Run(() =>
		{
			NowPlaying snapshot;
			try
			{
				snapshot = _source.Read(cancellation);
			}
			catch
			{
				snapshot = null;
			}
			try
			{
				if (host.IsDisposed || !host.IsHandleCreated)
				{
					snapshot?.Art?.Dispose();
					if (generation == _generation)
					{
						_busy = false;
					}
				}
				else
				{
					host.BeginInvoke(() =>
					{
						if (generation != _generation || Host == null)
						{
							snapshot?.Art?.Dispose();
						}
						else
						{
							_busy = false;
							Apply(snapshot);
						}
					});
				}
			}
			catch
			{
				snapshot?.Art?.Dispose();
				if (generation == _generation)
				{
					_busy = false;
				}
			}
		});
	}

	private NowPlaying? SafeRead()
	{
		try
		{
			return _source.Read();
		}
		catch
		{
			return null;
		}
	}

	private void Apply(NowPlaying? snapshot)
	{
		NowPlaying snapshot2 = _snapshot;
		_snapshot = snapshot;
		Action<NowPlaying>[] array = _views.Values.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i](snapshot);
		}
		if (snapshot2?.Art != snapshot?.Art)
		{
			snapshot2?.Art?.Dispose();
		}
	}

	internal void ReadForTestOrRefresh(Action<NowPlaying?> show)
	{
		if (_source.NeedsWorker)
		{
			Refresh();
		}
		else
		{
			show(SafeRead());
		}
	}

	internal void Command(MediaKey key, IMediaKeys fallback, Control? commandHost = null)
	{
		Control host = Host ?? commandHost;
		if (host != null && !host.IsHandleCreated)
		{
			_ = host.Handle;
		}
		if (!_source.NeedsWorker)
		{
			bool flag;
			try
			{
				flag = _source.TryCommand(key);
			}
			catch
			{
				flag = false;
			}
			if (!flag)
			{
				fallback.Send(key);
			}
			RefreshAfterCommand();
			return;
		}
		Task.Run(() =>
		{
			bool flag2;
			try
			{
				flag2 = _source.TryCommand(key);
			}
			catch
			{
				flag2 = false;
			}
			if (!flag2 && host != null)
			{
				try
				{
					host.BeginInvoke(() =>
					{
						fallback.Send(key);
					});
				}
				catch
				{
				}
			}
			if (host != null)
			{
				try
				{
					host.BeginInvoke(RefreshAfterCommand);
				}
				catch
				{
				}
			}
		});
	}

	internal async void RefreshAfterCommand()
	{
		if (Host != null)
		{
			await Task.Delay(300);
			Refresh();
		}
	}
}
