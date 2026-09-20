using System.Runtime.InteropServices;

namespace Leaf.BreakReminder;

internal static class FullscreenDetector
{
    private const uint MonitorDefaultToNearest = 2;

    public static bool IsFullscreenForegroundWindow(IReadOnlyCollection<nint> excludedHandles)
    {
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero || excludedHandles.Contains(foregroundWindow))
        {
            return false;
        }

        if (!GetWindowRect(foregroundWindow, out var windowRect))
        {
            return false;
        }

        var monitor = MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MonitorInfo();
        monitorInfo.cbSize = Marshal.SizeOf<MonitorInfo>();
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return false;
        }

        return windowRect.Left <= monitorInfo.rcMonitor.Left
            && windowRect.Top <= monitorInfo.rcMonitor.Top
            && windowRect.Right >= monitorInfo.rcMonitor.Right
            && windowRect.Bottom >= monitorInfo.rcMonitor.Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
    }
}
