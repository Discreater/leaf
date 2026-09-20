using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;
using XamlApplication = Microsoft.UI.Xaml.Application;

namespace Leaf.BreakReminder;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ComWrappersSupport.InitializeComWrappers();

        XamlApplication.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new LeafApplication();
        });
    }
}

internal sealed class LeafApplication : XamlApplication, IDisposable
{
    private ReminderController? _controller;
    private bool _disposed;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _controller = new ReminderController();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _controller?.Dispose();
        _controller = null;
    }

    public void RequestExit()
    {
        Dispose();
        Exit();
    }
}