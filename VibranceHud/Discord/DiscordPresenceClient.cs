using System;
using System.IO;
using System.IO.Pipes;

namespace VibranceHud.Discord;

public sealed class DiscordPresenceClient : IDisposable
{
	private readonly Func<Stream?> _open;

	private readonly bool _leaveOpen;

	private readonly Func<DateTime> _now;

	private Stream? _pipe;

	internal static readonly TimeSpan RescanAfterMiss = TimeSpan.FromSeconds(30.0);

	private DateTime _dontLookUntil = DateTime.MinValue;

	public bool Connected => _pipe != null;

	public DiscordPresenceClient()
		: this(OpenDiscordPipe, leaveOpen: false)
	{
	}

	public DiscordPresenceClient(Func<Stream?> open, bool leaveOpen, Func<DateTime>? now = null)
	{
		_now = now ?? ((Func<DateTime>)(() => DateTime.UtcNow));
		_open = open;
		_leaveOpen = leaveOpen;
	}

	private static Stream? OpenDiscordPipe()
	{
		for (int i = 0; i < 10; i++)
		{
			try
			{
				NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
				namedPipeClientStream.Connect(250);
				return namedPipeClientStream;
			}
			catch
			{
			}
		}
		return null;
	}

	public void Connect()
	{
		if (_pipe != null || _now() < _dontLookUntil)
		{
			return;
		}
		try
		{
			_pipe = _open();
			if (_pipe == null)
			{
				_dontLookUntil = _now() + RescanAfterMiss;
			}
			else
			{
				Write(DiscordRpc.Frame(0, DiscordRpc.HandshakeJson()));
			}
		}
		catch
		{
			Drop();
			_dontLookUntil = _now() + RescanAfterMiss;
		}
	}

	public void SetPreset(string? presetName)
	{
		if (_pipe == null)
		{
			Connect();
		}
		if (_pipe == null)
		{
			return;
		}
		try
		{
			Write(DiscordRpc.Frame(1, DiscordRpc.SetActivityJson(Environment.ProcessId, presetName)));
		}
		catch
		{
			Drop();
		}
	}

	public void Clear()
	{
		Drop();
	}

	private void Write(byte[] frame)
	{
		_pipe.Write(frame, 0, frame.Length);
		_pipe.Flush();
	}

	private void Drop()
	{
		Stream pipe = _pipe;
		_pipe = null;
		if (!_leaveOpen)
		{
			try
			{
				pipe?.Dispose();
			}
			catch
			{
			}
		}
	}

	public void Dispose()
	{
		Drop();
	}
}
