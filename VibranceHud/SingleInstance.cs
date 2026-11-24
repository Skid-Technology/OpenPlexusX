using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace VibranceHud;

internal static class SingleInstance
{
	private struct SECURITY_ATTRIBUTES
	{
		public int nLength;

		public nint lpSecurityDescriptor;

		public int bInheritHandle;
	}

	public static readonly TimeSpan AnswerGrace = TimeSpan.FromMilliseconds(1500.0);

	private const string MutexName = "PlexusX.SingleInstance.v1";

	private static Mutex? _mutex;

	public static readonly int ShowWindowMessage = RegisterWindowMessage("PlexusX.ShowMainWindow");

	private static readonly nint HwndBroadcast = new IntPtr(65535);

	public const int ASFW_ANY = -1;

	public const string RelaunchSwitch = "--relaunch";

	public static readonly TimeSpan RelaunchWait = TimeSpan.FromSeconds(10.0);

	private static CancellationTokenSource? _linkServer;

	private const int ERROR_ACCESS_DENIED = 5;

	private const int ERROR_ALREADY_EXISTS = 183;

	private static readonly nint INVALID_HANDLE = new IntPtr(-1);

	private const uint PIPE_ACCESS_INBOUND = 1u;

	private const uint FILE_FLAG_OVERLAPPED = 1073741824u;

	private const uint PIPE_TYPE_BYTE = 0u;

	private const uint PIPE_READMODE_BYTE = 0u;

	private const uint PIPE_WAIT = 0u;

	private const uint PIPE_REJECT_REMOTE_CLIENTS = 8u;

	internal const int MaxLinkChars = 4096;

	public static bool OtherCopyHasHigherRights { get; private set; }

	private static string PipeName => PipeNameFor(Environment.UserName);

	public static SecondCopyAction DecideForSecondCopy(bool otherCopyExists, bool otherCopyResponding)
	{
		if (!(otherCopyExists & otherCopyResponding))
		{
			return SecondCopyAction.OfferToEndStuckCopy;
		}
		return SecondCopyAction.HandOffAndExit;
	}

	public static bool AnotherCopyIsResponding(out bool anyFound)
	{
		anyFound = false;
		try
		{
			int processId = Environment.ProcessId;
			Process[] processesByName = Process.GetProcessesByName("PlexusX");
			foreach (Process process in processesByName)
			{
				using (process)
				{
					if (process.Id == processId)
					{
						continue;
					}
					anyFound = true;
					try
					{
						if (process.Responding)
						{
							return true;
						}
					}
					catch
					{
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int RegisterWindowMessage(string lpString);

	[DllImport("user32.dll")]
	private static extern bool PostMessage(nint hWnd, int msg, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool AllowSetForegroundWindow(int dwProcessId);

	public static bool LetTheRunningCopyTakeTheForeground()
	{
		try
		{
			return AllowSetForegroundWindow(-1);
		}
		catch
		{
			return false;
		}
	}

	public static bool TryAcquire()
	{
		OtherCopyHasHigherRights = false;
		if (!TryAcquireOn("PlexusX.SingleInstance.v1", out Mutex held, out bool deniedByOtherRights))
		{
			OtherCopyHasHigherRights = deniedByOtherRights;
			return false;
		}
		_mutex = held;
		return true;
	}

	internal static string ReachableAcrossRightsSddl()
	{
		using WindowsIdentity windowsIdentity = WindowsIdentity.GetCurrent();
		return "D:(A;;GA;;;" + windowsIdentity.User.Value + ")(A;;GA;;;SY)S:(ML;;NW;;;ME)";
	}

	internal static bool TryAcquireOn(string name, out Mutex? held, out bool deniedByOtherRights)
	{
		held = null;
		deniedByOtherRights = false;
		try
		{
			nint num = IntPtr.Zero;
			bool createdNew;
			Mutex mutex;
			try
			{
				num = SecurityDescriptorFromSddl(ReachableAcrossRightsSddl());
				SECURITY_ATTRIBUTES attributes = new SECURITY_ATTRIBUTES
				{
					nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
					lpSecurityDescriptor = num
				};
				nint num2 = CreateMutexW(ref attributes, initialOwner: true, name);
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (num2 == IntPtr.Zero)
				{
					if (lastWin32Error == 5)
					{
						throw new UnauthorizedAccessException();
					}
					throw new Win32Exception(lastWin32Error);
				}
				createdNew = lastWin32Error != 183;
				mutex = new Mutex();
				mutex.SafeWaitHandle = new SafeWaitHandle(num2, ownsHandle: true);
			}
			catch (UnauthorizedAccessException)
			{
				throw;
			}
			catch (Exception)
			{
				mutex = new Mutex(initiallyOwned: true, name, out createdNew);
			}
			finally
			{
				if (num != IntPtr.Zero)
				{
					LocalFree(num);
				}
			}
			if (!createdNew)
			{
				mutex.Dispose();
				return false;
			}
			held = mutex;
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			deniedByOtherRights = true;
			return false;
		}
	}

	public static bool IsRelaunch(string[]? args)
	{
		if (args != null)
		{
			return Array.Exists(args, (string a) => string.Equals(a, "--relaunch", StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	public static bool TryAcquireWaiting(TimeSpan wait)
	{
		DateTime dateTime = DateTime.UtcNow + wait;
		while (true)
		{
			if (TryAcquire())
			{
				return true;
			}
			if (DateTime.UtcNow >= dateTime)
			{
				break;
			}
			Thread.Sleep(200);
		}
		return false;
	}

	public static bool EndStuckCopies()
	{
		bool result = true;
		try
		{
			int processId = Environment.ProcessId;
			Process[] processesByName = Process.GetProcessesByName("PlexusX");
			foreach (Process process in processesByName)
			{
				using (process)
				{
					if (process.Id == processId)
					{
						continue;
					}
					try
					{
						process.Kill();
						if (!process.WaitForExit((int)AnswerGrace.TotalMilliseconds))
						{
							result = false;
						}
					}
					catch
					{
						result = false;
					}
				}
			}
		}
		catch
		{
			result = false;
		}
		return result;
	}

	public static bool SignalExistingInstance()
	{
		return AskForTheWindow(() => SendShow(), BroadcastShowWindowMessage);
	}

	internal static bool AskForTheWindow(Func<bool> overThePipe, Action broadcast)
	{
		if (overThePipe())
		{
			return true;
		}
		broadcast();
		return false;
	}

	private static void BroadcastShowWindowMessage()
	{
		PostMessage(HwndBroadcast, ShowWindowMessage, IntPtr.Zero, IntPtr.Zero);
	}

	internal static string PipeNameFor(string? userName)
	{
		string? obj = userName ?? "";
		StringBuilder stringBuilder = new StringBuilder(obj.Length);
		string text = obj;
		foreach (char c in text)
		{
			stringBuilder.Append(char.IsLetterOrDigit(c) ? c : '_');
		}
		return "PlexusX.Link.v1." + stringBuilder;
	}

	public static void StartRequestServer(Action<PipeRequest> onRequest)
	{
		if (_linkServer == null)
		{
			_linkServer = StartServingOn(PipeName, onRequest);
		}
	}

	internal static CancellationTokenSource StartServingOn(string pipeName, Action<PipeRequest> onRequest)
	{
		CancellationTokenSource cts = new CancellationTokenSource();
		Thread thread = new Thread(() =>
		{
			Serve(pipeName, onRequest, cts.Token);
		});
		thread.IsBackground = true;
		thread.Name = "PlexusX request server";
		thread.Start();
		return cts;
	}

	private static void Serve(string pipeName, Action<PipeRequest> onRequest, CancellationToken stopping)
	{
		while (!stopping.IsCancellationRequested)
		{
			try
			{
				using NamedPipeServerStream namedPipeServerStream = CreateServerPipe(pipeName);
				namedPipeServerStream.WaitForConnectionAsync(stopping).GetAwaiter().GetResult();
				if (stopping.IsCancellationRequested)
				{
					break;
				}
				string text = ReadRequest(namedPipeServerStream);
				if (text != null)
				{
					try
					{
						onRequest(PipeRequest.Parse(text));
					}
					catch
					{
					}
				}
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch
			{
				if (stopping.IsCancellationRequested)
				{
					break;
				}
			}
		}
	}

	internal static NamedPipeServerStream CreateServerPipe(string pipeName)
	{
		nint num;
		try
		{
			num = SecurityDescriptorFromSddl(ReachableAcrossRightsSddl());
		}
		catch
		{
			return new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
		}
		try
		{
			SECURITY_ATTRIBUTES attributes = new SECURITY_ATTRIBUTES
			{
				nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
				lpSecurityDescriptor = num
			};
			nint num2 = CreateNamedPipeW("\\\\.\\pipe\\" + pipeName, 1073741825u, 8u, 1u, 0u, 0u, 0u, ref attributes);
			if (num2 == INVALID_HANDLE)
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				if (lastWin32Error == 5)
				{
					throw new UnauthorizedAccessException();
				}
				throw new Win32Exception(lastWin32Error);
			}
			return new NamedPipeServerStream(PipeDirection.In, isAsync: true, isConnected: false, new SafePipeHandle(num2, ownsHandle: true));
		}
		finally
		{
			LocalFree(num);
		}
	}

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptorW(string sddl, int revision, out nint descriptor, nint size);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint LocalFree(nint memory);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateMutexW(ref SECURITY_ATTRIBUTES attributes, bool initialOwner, string name);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateNamedPipeW(string name, uint openMode, uint pipeMode, uint maxInstances, uint outBuffer, uint inBuffer, uint defaultTimeout, ref SECURITY_ATTRIBUTES attributes);

	private static nint SecurityDescriptorFromSddl(string sddl)
	{
		if (!ConvertStringSecurityDescriptorToSecurityDescriptorW(sddl, 1, out var descriptor, IntPtr.Zero))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		return descriptor;
	}

	internal static string? ReadRequest(Stream stream)
	{
		using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, 1024, leaveOpen: true);
		char[] array = new char[4097];
		int i;
		int num;
		for (i = 0; i < array.Length; i += num)
		{
			num = streamReader.Read(array, i, array.Length - i);
			if (num == 0)
			{
				break;
			}
		}
		return (i == 0 || i > 4096) ? null : new string(array, 0, i);
	}

	public static bool SendLink(string link, int timeoutMs = 2000)
	{
		return SendLinkTo(PipeName, link, timeoutMs);
	}

	internal static bool SendLinkTo(string pipeName, string link, int timeoutMs)
	{
		if (string.IsNullOrWhiteSpace(link))
		{
			return false;
		}
		if (link.Length > 4096)
		{
			return false;
		}
		if (PipeRequest.Parse(link).Verb != PipeVerb.Link)
		{
			return false;
		}
		return Send(pipeName, link, timeoutMs);
	}

	public static bool SendShow(int timeoutMs = 2000)
	{
		return SendShowTo(PipeName, timeoutMs);
	}

	internal static bool SendShowTo(string pipeName, int timeoutMs)
	{
		return Send(pipeName, "plexusx-control:show", timeoutMs);
	}

	public static bool SendQuit(int timeoutMs = 2000)
	{
		return SendQuitTo(PipeName, timeoutMs);
	}

	internal static bool SendQuitTo(string pipeName, int timeoutMs)
	{
		return Send(pipeName, "plexusx-control:quit", timeoutMs);
	}

	private static bool Send(string pipeName, string payload, int timeoutMs)
	{
		try
		{
			using NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
			namedPipeClientStream.Connect(timeoutMs);
			using StreamWriter streamWriter = new StreamWriter(namedPipeClientStream, Encoding.UTF8);
			streamWriter.Write(payload);
			streamWriter.Flush();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void StopLinkServer()
	{
		CancellationTokenSource linkServer = _linkServer;
		_linkServer = null;
		if (linkServer == null)
		{
			return;
		}
		try
		{
			linkServer.Cancel();
		}
		catch
		{
		}
		try
		{
			linkServer.Dispose();
		}
		catch
		{
		}
	}

	public static void Release()
	{
		if (_mutex != null)
		{
			try
			{
				_mutex.ReleaseMutex();
			}
			catch (ApplicationException)
			{
			}
			_mutex.Dispose();
			_mutex = null;
		}
	}
}
