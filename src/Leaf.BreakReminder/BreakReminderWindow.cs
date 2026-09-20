using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;
using Button = Microsoft.UI.Xaml.Controls.Button;
using Screen = System.Windows.Forms.Screen;

namespace Leaf.BreakReminder;

internal sealed class BreakReminderWindow : Window
{
    private const int WindowWidth = 460;
    private const int WindowHeight = 280;
    private const int GwlStyle = -16;
    private const int WsSysMenu = 0x00080000;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;

    private readonly List<Button> _postponeButtons = [];
    private readonly TextBlock _summaryTextBlock;
    private readonly TextBlock _countdownTextBlock;
    private readonly nint _windowHandle;
    private bool _isClosed;

    public BreakReminderWindow(Screen screen, IReadOnlyList<int> postponeOptionsMinutes)
    {
        Title = "休息提醒";

        _summaryTextBlock = new TextBlock
        {
            FontSize = 18,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        };

        _countdownTextBlock = new TextBlock
        {
            FontSize = 32,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };

        Content = BuildContent(postponeOptionsMinutes);
        Closed += (_, _) => _isClosed = true;

        _windowHandle = WindowNative.GetWindowHandle(this);
        if (_windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("无法创建 WinUI 窗口句柄。");
        }

        ConfigureWindow(screen);
    }

    public event EventHandler<int>? PostponeRequested;

    public bool IsDisposed => _isClosed;

    public nint Handle => _isClosed ? IntPtr.Zero : _windowHandle;

    public void UpdateContent(int elapsedWorkMinutes, int breakDurationMinutes, TimeSpan remaining)
    {
        _summaryTextBlock.Text = $"你已经连续工作 {elapsedWorkMinutes} 分钟，本次建议休息 {breakDurationMinutes} 分钟。";
        _countdownTextBlock.Text = remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
            : $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
    }

    public void SetPostponeButtonsEnabled(bool enabled)
    {
        foreach (var button in _postponeButtons)
        {
            button.IsEnabled = enabled;
        }
    }

    private UIElement BuildContent(IReadOnlyList<int> postponeOptionsMinutes)
    {
        var titleTextBlock = new TextBlock
        {
            Text = "该休息了",
            FontSize = 30,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center
        };

        var detailTextBlock = new TextBlock
        {
            Text = "倒计时结束后窗口会自动关闭",
            FontSize = 14,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.DimGray),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var buttonsPanel = new StackPanel
        {
            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
            Spacing = 12,
            Margin = new Thickness(0, 28, 0, 0)
        };

        foreach (var minutes in postponeOptionsMinutes)
        {
            var button = new Button
            {
                Content = $"推迟 {minutes} 分钟",
                Padding = new Thickness(12, 8, 12, 8),
                MinWidth = 100
            };

            button.Click += (_, _) => PostponeRequested?.Invoke(this, minutes);
            _postponeButtons.Add(button);
            buttonsPanel.Children.Add(button);
        }

        return new Grid
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.WhiteSmoke),
            Padding = new Thickness(24),
            Children =
            {
                new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Spacing = 4,
                    Children =
                    {
                        titleTextBlock,
                        _summaryTextBlock,
                        _countdownTextBlock,
                        detailTextBlock,
                        buttonsPanel
                    }
                }
            }
        };
    }

    private void ConfigureWindow(Screen screen)
    {
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_windowHandle));
        appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
        appWindow.IsShownInSwitchers = false;

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
        }

        var workingArea = screen.WorkingArea;
        appWindow.Move(
            new PointInt32(
                workingArea.Left + ((workingArea.Width - WindowWidth) / 2),
                workingArea.Top + ((workingArea.Height - WindowHeight) / 2)));

        Marshal.SetLastPInvokeError(0);
        var style = GetWindowLongPtr(_windowHandle, GwlStyle);
        if (style == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
        {
            return;
        }

        var updatedStyle = new IntPtr(style.ToInt64() & ~WsSysMenu);
        Marshal.SetLastPInvokeError(0);
        var previousStyle = SetWindowLongPtr(_windowHandle, GwlStyle, updatedStyle);
        if (previousStyle == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
        {
            return;
        }

        SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoZOrder | SwpFrameChanged);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);
}
