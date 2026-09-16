using System.Windows;
using System.Threading;

namespace GhostWidget;

public partial class App : Application
{
    private Mutex? singleInstanceMutex;
    private bool ownsSingleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Render-only preview invocations are isolated from the live desktop pet. They need to
        // open a temporary window even while the normal single instance is running.
        bool previewMode = Environment.GetCommandLineArgs().Any(argument => argument.EndsWith("-preview", StringComparison.Ordinal));
        if (!previewMode)
        {
            singleInstanceMutex = new Mutex(true, "GhostFarmWidget.SingleInstance", out ownsSingleInstanceMutex);
            if (!ownsSingleInstanceMutex)
            {
                // A second double-click must never create a second desktop pet or duplicate timers.
                singleInstanceMutex.Dispose();
                Shutdown();
                return;
            }
        }
        base.OnStartup(e);
        new MainWindow().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (ownsSingleInstanceMutex) singleInstanceMutex?.ReleaseMutex();
        singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }
}
