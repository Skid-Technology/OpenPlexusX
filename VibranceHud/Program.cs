using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VibranceHud.Controls;
using VibranceHud.Licensing;
using VibranceHud.Onboarding;
using VibranceHud.Presets;
using VibranceHud.Startup;
using VibranceHud.Support;
using VibranceHud.Updates;
using VibranceHud.Zoom;

namespace VibranceHud;

internal static class Program
{
	private static int _fatalShowing;

	internal static Func<string, string, MessageBoxButtons, DialogResult>? ShowBoxForTest;

	[STAThread]
	private static int Main(string[] args)
	{
		Func<bool>? launchedAtSignIn = StoreHooks.LaunchedAtSignIn;
		if (launchedAtSignIn != null && launchedAtSignIn())
		{
			args = LaunchOptions.WithTraySwitch(args);
		}
		int? num2 = RestoreMonitorSwitch.TryRun(args);
		if (num2.HasValue)
		{
			return num2.Value;
		}
		string text = FirstLinkIn(args);
		if (!SingleInstance.TryAcquire() && (!SingleInstance.IsRelaunch(args) || !SingleInstance.TryAcquireWaiting(SingleInstance.RelaunchWait)))
		{
			StartupStep.Skippable("handing over to the PlexusX that is already running", () =>
			{
				SingleInstance.LetTheRunningCopyTakeTheForeground();
			});
			if (text != null)
			{
				SingleInstance.SendLink(text);
			}
			bool flag = SingleInstance.SignalExistingInstance();
			if (SingleInstance.OtherCopyHasHigherRights && !flag)
			{
				TellItIsRunningAsAdministrator();
				return 0;
			}
			Thread.Sleep(SingleInstance.AnswerGrace);
			bool otherCopyResponding = SingleInstance.AnotherCopyIsResponding(out var anyFound);
			if (SingleInstance.DecideForSecondCopy(anyFound, otherCopyResponding) == SecondCopyAction.HandOffAndExit)
			{
				return 0;
			}
			OfferToEndStuckCopy();
			return 0;
		}
		StoreHooks.RegisterRestart?.Invoke();
		LaunchOptions.Parse(args);
		if (text != null)
		{
			LinkInbox.Offer(text);
		}
		StartupStep.Skippable("listening for links, a second launch and the installer", () =>
		{
			SingleInstance.StartRequestServer(RemoteCommands.Deliver);
		});
		CrashLog.Cleanup();
		Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(defaultValue: false);
		WheelRouter.Install();
		TaskScheduler.UnobservedTaskException += (object? s, UnobservedTaskExceptionEventArgs e) =>
		{
			try
			{
				CrashLog.Write(e.Exception);
			}
			catch
			{
			}
			e.SetObserved();
		};
		Application.ThreadException += (object s, ThreadExceptionEventArgs e) =>
		{
			ShowFatal("PlexusX hit a problem, but it is still running.\n\nWhat you just did may not have worked. If PlexusX acts strange, close it and open it again.", e.Exception);
		};
		AppDomain.CurrentDomain.ProcessExit += (object? s, EventArgs e) =>
		{
			ZoomEmergency.ResetIfWeZoomed();
		};
		AppDomain.CurrentDomain.UnhandledException += (object s, UnhandledExceptionEventArgs e) =>
		{
			Exception ex2 = e.ExceptionObject as Exception;
			try
			{
				CrashHandBack.Run();
			}
			catch
			{
			}
			try
			{
				ZoomEmergency.ResetIfWeZoomed();
			}
			catch
			{
			}
			ShowFatal("PlexusX hit a problem and had to close.\n\nOpen PlexusX again. If this keeps happening, email support@plexusx.lol.", ex2 ?? new Exception("(no exception info)"));
		};
		ISaturationOverlay overlay = null;
		VibranceEngine demoEngine = null;
		bool flag2 = false;
		try
		{
			Timing.Mark("Program: entering the startup try");
			StartupStep.Skippable("checking this copy of PlexusX", RemoteSwitches.LoadCached);
			Timing.Mark("Program: beta gate done");
			Timing.Measure("TaskbarPins.PointAtThisCopy", () =>
			{
				StartupStep.Skippable("pointing your taskbar pin at this PlexusX", () =>
				{
					TaskbarPins.PointAtThisCopy();
				});
			});
			Timing.Mark("Program: integrity check done");
			SettingsStore store = StartupStep.Required("reading your settings", (Func<SettingsStore>)SettingsStore.Default);
			AppSettings settings = StartupStep.Required("reading your settings", (Func<AppSettings>)store.Load);
			Timing.Mark("Program: settings loaded");
			StartupStep.Skippable("putting your screen back after PlexusX was closed unexpectedly", () =>
			{
				DirtyExitMarker.Shared = DirtyExitMarker.InLocalAppData();
				ColdStartRestore.RunIfTheLastRunDied(settings, store);
			});
			settings = StartupStep.Required("reading your settings", (Func<AppSettings>)store.Load);
			if (!settings.OnboardingComplete)
			{
				DemoEngine.Built built = DemoEngine.TryBuild(TrayApplicationContext.TryCreateOverlay, TrayApplicationContext.CreateVibranceController, (Exception ex2) =>
				{
					CrashLog.Write(ex2);
				});
				overlay = built.Overlay;
				demoEngine = built.Engine;
				StartupStep.Skippable("showing the first-run setup", () =>
				{
					using OnboardingForm onboardingForm = new OnboardingForm(settings, store, demoEngine);
					onboardingForm.ShowDialog();
				});
				settings = StartupStep.Required("reading your settings", (Func<AppSettings>)store.Load);
			}
			if (FlavourPolicy.Current.Startup == StartupMechanism.RunKey)
			{
				StartupStep.Skippable("remembering whether PlexusX starts with Windows", () =>
				{
					StartupManager.RemoveLegacyEntry();
					if (settings.StartWithWindows != StartupManager.RunEntryExists())
					{
						StartupManager.SetEnabled(settings.StartWithWindows);
					}
				});
			}
			ReleaseDemoOverlay(ref overlay, ref demoEngine);
			Timing.Mark("Program: startup checks done, building the tray context");
			TrayApplicationContext tray = null;
			Timing.Measure("TrayApplicationContext constructor", () =>
			{
				tray = StartupStep.Required("setting up the monitors attached to this PC", () => new TrayApplicationContext());
			});
			Timing.Mark("Program: handing control to the message loop");
			Application.Run(tray);
			flag2 = true;
		}
		catch (Exception ex)
		{
			try
			{
				CrashHandBack.Run();
			}
			catch
			{
			}
			ShowFatal(StartupFailure.Describe(ex), ex);
		}
		finally
		{
			ReleaseDemoOverlay(ref overlay, ref demoEngine);
			if (flag2)
			{
				try
				{
					DirtyExitMarker.Shared.Clear();
				}
				catch
				{
				}
			}
			SingleInstance.Release();
		}
		return 0;
	}

	private static void ReleaseDemoOverlay(ref ISaturationOverlay? overlay, ref VibranceEngine? engine)
	{
		engine = null;
		if (overlay != null)
		{
			try
			{
				overlay.Clear();
			}
			catch
			{
			}
			(overlay as IDisposable)?.Dispose();
			overlay = null;
		}
	}

	private static string? FirstLinkIn(string[] args)
	{
		if (args == null)
		{
			return null;
		}
		foreach (string text in args)
		{
			if (PlexusLinks.Parse(text).Action == LinkAction.Apply)
			{
				return text;
			}
		}
		return null;
	}

	private static void TellItIsRunningAsAdministrator()
	{
		try
		{
			MessageBox.Show("PlexusX is already running as administrator. Open it from the tray icon.", "PlexusX", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}
		catch
		{
		}
	}

	private static void OfferToEndStuckCopy()
	{
		try
		{
			if (MessageBox.Show("PlexusX is already open, but it is stuck.\n\nThis can happen when a game or your graphics card crashes.\n\nClose the stuck copy and start PlexusX again?", "PlexusX", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
			{
				return;
			}
			if (!SingleInstance.EndStuckCopies())
			{
				MessageBox.Show("PlexusX couldn't close the stuck copy.\n\nPress Ctrl+Shift+Esc to open Task Manager. Pick PlexusX, press End task, then start PlexusX again.", "PlexusX", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			try
			{
				Process.Start(new ProcessStartInfo(Application.ExecutablePath)
				{
					UseShellExecute = true
				});
			}
			catch
			{
				MessageBox.Show("The stuck copy is closed. Start PlexusX again.", "PlexusX", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}
		catch
		{
		}
	}

	internal static void ShowFatal(string prefix, Exception ex)
	{
		if (Interlocked.Exchange(ref _fatalShowing, 1) == 1)
		{
			try
			{
				CrashLog.Write(ex);
				return;
			}
			catch
			{
				return;
			}
		}
		try
		{
			ShowFatalOnce(prefix, ex);
		}
		finally
		{
			Volatile.Write(ref _fatalShowing, 0);
		}
	}

	private static DialogResult ShowBox(string text, MessageBoxButtons buttons, MessageBoxIcon icon)
	{
		return ShowBoxForTest?.Invoke(text, "PlexusX", buttons) ?? MessageBox.Show(text, "PlexusX", buttons, icon);
	}

	private static void ShowFatalOnce(string prefix, Exception ex)
	{
		string text = "";
		try
		{
			text = CrashLog.Write(ex);
		}
		catch
		{
		}
		try
		{
			string body = prefix + "\n\n" + (StartupFailure.Detail(ex)?.Message ?? ex.Message);
			if (string.IsNullOrEmpty(text))
			{
				ShowBox(body, MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			body = body + "\n\nA crash report was written to:\n" + text + "\n\nSend this crash report to PlexusX so it can be fixed?\n\nIt holds the error and where it happened. It has no licence key, no account name and no contact details. Nothing is sent unless you press Yes.";
			bool consented = false;
			string version = "";
			try
			{
				version = UpdateService.CurrentVersion.ToString(3);
			}
			catch
			{
			}
			bool result = CrashReportOffer.OfferAsync(text, version, () =>
			{
				consented = ShowBox(body, MessageBoxButtons.YesNo, MessageBoxIcon.Hand) == DialogResult.Yes;
				return consented;
			}, (string b) => BugReportClient.SendAsync(b)).GetAwaiter().GetResult();
			if (consented)
			{
				ShowBox(result ? "Sent - thank you. PlexusX will look at it." : ("Couldn't send it. Email support@plexusx.lol and attach this file:\n" + text), MessageBoxButtons.OK, result ? MessageBoxIcon.Asterisk : MessageBoxIcon.Exclamation);
			}
		}
		catch
		{
		}
	}
}
