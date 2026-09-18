using System.Windows;
using System.Threading;

namespace GhostWidget;

public partial class App : Application
{
    private const string SingleInstanceMutexName = "GhostFarmWidget.SingleInstance";
    private const string ShutdownSignalName = "GhostFarmWidget.ShutdownSignal";

    private Mutex? singleInstanceMutex;
    private bool ownsSingleInstanceMutex;
    private EventWaitHandle? shutdownSignal;
    private RegisteredWaitHandle? shutdownWait;
    private MainWindow? mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Render-only preview invocations are isolated from the live desktop pet. They need to
        // open a temporary window even while the normal single instance is running.
        bool previewMode = Environment.GetCommandLineArgs().Any(argument => argument.EndsWith("-preview", StringComparison.Ordinal));
        if (!previewMode)
        {
            singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out ownsSingleInstanceMutex);
            if (!ownsSingleInstanceMutex)
            {
                // Another instance is already running. The newest launch wins: ask it to close
                // (which saves and shuts down through its own Window_Closed, same as a normal
                // exit), then take over its slot instead of quietly giving up.
                try
                {
                    using EventWaitHandle signal = EventWaitHandle.OpenExisting(ShutdownSignalName);
                    signal.Set();
                }
                catch (WaitHandleCannotBeOpenedException) { /* running instance predates this signal; nothing to wake */ }

                bool acquired;
                try { acquired = singleInstanceMutex.WaitOne(TimeSpan.FromSeconds(5)); }
                catch (AbandonedMutexException) { acquired = true; }
                if (!acquired)
                {
                    singleInstanceMutex.Dispose();
                    Shutdown();
                    return;
                }
                ownsSingleInstanceMutex = true;
            }
            shutdownSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShutdownSignalName);
            shutdownWait = ThreadPool.RegisterWaitForSingleObject(shutdownSignal,
                (_, _) => Dispatcher.Invoke(() => mainWindow?.Close()), null, Timeout.Infinite, executeOnlyOnce: true);
        }
        base.OnStartup(e);
        mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        shutdownWait?.Unregister(null);
        shutdownSignal?.Dispose();
        if (ownsSingleInstanceMutex) singleInstanceMutex?.ReleaseMutex();
        singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
