using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VibranceHud.Remote;

internal sealed class PhoneListener : IDisposable
{
	private readonly PhoneControl _control;

	private readonly PhoneBackoff _backoff = new PhoneBackoff();

	private TcpListener? _listener;

	private CancellationTokenSource? _stopping;

	private int _everAnswered;

	private static readonly TimeSpan HeadTimeout = TimeSpan.FromSeconds(5.0);

	internal string Token { get; private set; } = "";

	internal int Port { get; private set; }

	internal string AdvertisedAddress { get; set; } = "";

	internal bool IsRunning => _listener != null;

	internal IPAddress BindAddress { get; set; } = IPAddress.Any;

	internal event Action? PhoneArrived;

	internal event Action<string, string>? WrongNetworkSeen;

	internal PhoneListener(PhoneControl control)
	{
		_control = control;
	}

	internal void Start()
	{
		if (_listener == null)
		{
			Token = PhoneSession.NewToken();
			_stopping = new CancellationTokenSource();
			TcpListener listener = new TcpListener(BindAddress, 0);
			listener.Start();
			_listener = listener;
			Port = ((IPEndPoint)listener.LocalEndpoint).Port;
			Interlocked.Exchange(ref _everAnswered, 0);
			Task.Run(() => AcceptLoopAsync(listener, _stopping.Token));
		}
	}

	internal void Stop()
	{
		TcpListener listener = _listener;
		_listener = null;
		Token = "";
		Port = 0;
		try
		{
			_stopping?.Cancel();
		}
		catch
		{
		}
		try
		{
			listener?.Stop();
		}
		catch
		{
		}
		_stopping?.Dispose();
		_stopping = null;
	}

	private async Task AcceptLoopAsync(TcpListener listener, CancellationToken stopping)
	{
		while (!stopping.IsCancellationRequested)
		{
			TcpClient client;
			try
			{
				client = await listener.AcceptTcpClientAsync(stopping);
			}
			catch
			{
				break;
			}
			Task.Run(async () =>
			{
				try
				{
					await ServeAsync(client, stopping);
				}
				catch
				{
				}
				finally
				{
					try
					{
						client.Dispose();
					}
					catch
					{
					}
				}
			}, CancellationToken.None);
		}
	}

	private async Task ServeAsync(TcpClient client, CancellationToken stopping)
	{
		client.ReceiveTimeout = 5000;
		client.SendTimeout = 5000;
		string remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address.ToString() ?? "";
		using NetworkStream stream = client.GetStream();
		using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(stopping);
		deadline.CancelAfter(HeadTimeout);
		(string, string, string) tuple;
		try
		{
			tuple = await ReadHeadAsync(stream, deadline.Token);
		}
		catch (OperationCanceledException)
		{
			return;
		}
		(string, string, string) tuple2 = tuple;
		string item = tuple2.Item1;
		string item2 = tuple2.Item2;
		string item3 = tuple2.Item3;
		PhoneDecision phoneDecision = PhoneRequest.Decide(item, item2, item3, remote, AdvertisedAddress, NetworkFacts.LocalAddresses(), Token, _backoff, DateTime.UtcNow);
		switch (phoneDecision.Verdict)
		{
		case PhoneVerdict.Page:
			await WriteAsync(stream, 200, "text/html; charset=utf-8", PhonePage.Html());
			break;
		case PhoneVerdict.State:
			NoteArrival();
			await WriteAsync(stream, 200, "application/json; charset=utf-8", _control.StateJson());
			break;
		case PhoneVerdict.Apply:
			NoteArrival();
			_control.Queue(phoneDecision.Field, phoneDecision.Value);
			await WriteAsync(stream, 200, "text/plain; charset=utf-8", "ok");
			break;
		case PhoneVerdict.BadRequest:
			await WriteAsync(stream, 400, "text/plain; charset=utf-8", "bad request");
			break;
		case PhoneVerdict.TooManyAttempts:
			await WriteAsync(stream, 429, "text/plain; charset=utf-8", "slow down");
			break;
		case PhoneVerdict.WrongNetwork:
			WrongNetworkSeen?.Invoke(remote, AdvertisedAddress);
			await WriteAsync(stream, 200, "text/html; charset=utf-8", PhonePage.WrongNetworkHtml(remote, AdvertisedAddress));
			break;
		default:
			await WriteAsync(stream, 404, "text/plain; charset=utf-8", "not found");
			break;
		}
	}

	private void NoteArrival()
	{
		if (Interlocked.Exchange(ref _everAnswered, 1) == 0)
		{
			PhoneArrived?.Invoke();
		}
	}

	private static async Task<(string Method, string Target, string? Host)> ReadHeadAsync(NetworkStream stream, CancellationToken stopping)
	{
		byte[] buffer = new byte[8192];
		int filled = 0;
		while (filled < buffer.Length)
		{
			int num = await stream.ReadAsync(buffer.AsMemory(filled, buffer.Length - filled), stopping);
			if (num <= 0)
			{
				break;
			}
			filled += num;
			string text = Encoding.ASCII.GetString(buffer, 0, filled);
			if (text.Contains("\r\n\r\n", StringComparison.Ordinal) || text.Contains("\n\n", StringComparison.Ordinal))
			{
				break;
			}
		}
		string s = Encoding.ASCII.GetString(buffer, 0, filled);
		string item = "";
		string item2 = "";
		string item3 = null;
		using StringReader stringReader = new StringReader(s);
		string text2 = stringReader.ReadLine();
		if (text2 != null)
		{
			string[] array = text2.Split(' ');
			if (array.Length >= 2)
			{
				item = array[0];
				item2 = array[1];
			}
		}
		while ((text2 = stringReader.ReadLine()) != null && text2.Length != 0)
		{
			int num2 = text2.IndexOf(':');
			if (num2 > 0 && MemoryExtensions.Equals(text2.AsSpan(0, num2).Trim(), "Host", StringComparison.OrdinalIgnoreCase))
			{
				item3 = text2.Substring(num2 + 1).Trim();
				break;
			}
		}
		return (Method: item, Target: item2, Host: item3);
	}

	private static async Task WriteAsync(NetworkStream stream, int status, string type, string body)
	{
		byte[] payload = Encoding.UTF8.GetBytes(body);
		string s = new StringBuilder().Append("HTTP/1.1 ").Append(status).Append(' ')
			.Append(Reason(status))
			.Append("\r\n")
			.Append("Content-Type: ")
			.Append(type)
			.Append("\r\n")
			.Append("Content-Length: ")
			.Append(payload.Length)
			.Append("\r\n")
			.Append("Cache-Control: no-store\r\n")
			.Append("X-Content-Type-Options: nosniff\r\n")
			.Append("Referrer-Policy: no-referrer\r\n")
			.Append("Connection: close\r\n\r\n")
			.ToString();
		byte[] bytes = Encoding.ASCII.GetBytes(s);
		await stream.WriteAsync(bytes);
		await stream.WriteAsync(payload);
		await stream.FlushAsync();
	}

	private static string Reason(int status)
	{
		return status switch
		{
			200 => "OK", 
			400 => "Bad Request", 
			404 => "Not Found", 
			429 => "Too Many Requests", 
			_ => "OK", 
		};
	}

	public void Dispose()
	{
		Stop();
	}
}
