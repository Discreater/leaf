namespace Leaf.BreakReminder;

internal sealed class ReminderApplicationContext : ApplicationContext
{
    private static readonly int[] PostponeOptionsMinutes = [1, 5, 10, 20];

    private readonly ReminderSettingsProvider _settingsProvider;
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly List<BreakReminderForm> _activeForms = [];
    private ReminderSettings _settings;
    private DateTimeOffset _lastBreakEndedAt;
    private DateTimeOffset _nextReminderAt;
    private DateTimeOffset? _lastFullscreenExitedAt;
    private DateTimeOffset? _breakEndsAt;
    private bool _reminderPending;
    private bool _wasFullscreen;
    private int _activeBreakDurationMinutes;
    private int _totalPostponedMinutes;

    public ReminderApplicationContext()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        _settingsProvider = new ReminderSettingsProvider(settingsPath);
        _settings = _settingsProvider.Load();

        _lastBreakEndedAt = DateTimeOffset.Now;
        _nextReminderAt = _lastBreakEndedAt.AddMinutes(_settings.WorkIntervalMinutes);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "Leaf 休息提醒",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _timer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
            CloseActiveForms();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("重新加载配置", null, (_, _) => ReloadSettings());
        menu.Items.Add("立即提醒", null, (_, _) => QueueImmediateReminder());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private void ReloadSettings()
    {
        _settings = _settingsProvider.Load();

        if (_breakEndsAt is null)
        {
            _nextReminderAt = _lastBreakEndedAt.AddMinutes(_settings.WorkIntervalMinutes);
        }
    }

    private void QueueImmediateReminder()
    {
        _nextReminderAt = DateTimeOffset.Now;
        _reminderPending = true;
    }

    private void OnTick()
    {
        var now = DateTimeOffset.Now;
        UpdateFullscreenState(now);

        if (_breakEndsAt is not null)
        {
            UpdateActiveReminder(now);
            return;
        }

        if (now >= _nextReminderAt)
        {
            _reminderPending = true;
        }

        if (_reminderPending && CanShowReminder(now))
        {
            ShowReminder(now);
        }
    }

    private void UpdateFullscreenState(DateTimeOffset now)
    {
        var currentWindowHandles = _activeForms
            .Where(form => form.IsHandleCreated)
            .Select(form => form.Handle)
            .ToArray();

        var isFullscreen = FullscreenDetector.IsFullscreenForegroundWindow(currentWindowHandles);
        if (_wasFullscreen && !isFullscreen)
        {
            _lastFullscreenExitedAt = now;
        }

        _wasFullscreen = isFullscreen;
    }

    private bool CanShowReminder(DateTimeOffset now)
    {
        if (_wasFullscreen)
        {
            return false;
        }

        if (_lastFullscreenExitedAt is null)
        {
            return true;
        }

        return now - _lastFullscreenExitedAt.Value >= TimeSpan.FromMinutes(_settings.FullscreenCooldownMinutes);
    }

    private void ShowReminder(DateTimeOffset now)
    {
        _reminderPending = false;
        _activeBreakDurationMinutes = CalculateBreakDurationMinutes(now);
        _breakEndsAt = now.AddMinutes(_activeBreakDurationMinutes);

        CloseActiveForms();

        foreach (var screen in Screen.AllScreens)
        {
            var form = new BreakReminderForm(screen, PostponeOptionsMinutes);
            form.PostponeRequested += (_, minutes) => PostponeReminder(minutes);
            form.FormClosed += (_, _) => _activeForms.Remove(form);
            _activeForms.Add(form);
            form.Show();
        }

        UpdateActiveReminder(now);
    }

    private int CalculateBreakDurationMinutes(DateTimeOffset now)
    {
        var elapsedWorkMinutes = GetElapsedWorkMinutes(now);
        var overtimeMinutes = Math.Max(0, elapsedWorkMinutes - _settings.WorkIntervalMinutes);
        var overtimeBlocks = CalculateCeilingBlocks(overtimeMinutes, _settings.OvertimeBlockMinutes);
        var postponedBlocks = CalculateCeilingBlocks(_totalPostponedMinutes, _settings.PostponeBlockMinutes);

        return _settings.BaseBreakDurationMinutes
            + (overtimeBlocks * _settings.BreakDurationExtensionPerOvertimeBlockMinutes)
            + (postponedBlocks * _settings.BreakDurationExtensionPerPostponeBlockMinutes);
    }

    private int GetElapsedWorkMinutes(DateTimeOffset now)
    {
        return Math.Max(1, (int)Math.Ceiling((now - _lastBreakEndedAt).TotalMinutes));
    }

    private static int CalculateCeilingBlocks(int totalMinutes, int blockMinutes)
    {
        if (totalMinutes <= 0)
        {
            return 0;
        }

        return (totalMinutes + blockMinutes - 1) / blockMinutes;
    }

    private void UpdateActiveReminder(DateTimeOffset now)
    {
        if (_breakEndsAt is null)
        {
            return;
        }

        var remaining = _breakEndsAt.Value - now;
        if (remaining <= TimeSpan.Zero)
        {
            CompleteReminder(now);
            return;
        }

        var elapsedWorkMinutes = GetElapsedWorkMinutes(now);
        foreach (var form in _activeForms.ToArray())
        {
            form.UpdateContent(elapsedWorkMinutes, _activeBreakDurationMinutes, remaining);
        }
    }

    private void CompleteReminder(DateTimeOffset now)
    {
        _breakEndsAt = null;
        CloseActiveForms();
        _totalPostponedMinutes = 0;
        _lastBreakEndedAt = now;
        _nextReminderAt = now.AddMinutes(_settings.WorkIntervalMinutes);
        _reminderPending = false;
    }

    private void PostponeReminder(int minutes)
    {
        if (_breakEndsAt is null)
        {
            return;
        }

        _breakEndsAt = null;
        _totalPostponedMinutes += minutes;
        _nextReminderAt = DateTimeOffset.Now.AddMinutes(minutes);
        _reminderPending = false;
        CloseActiveForms();
    }

    private void CloseActiveForms()
    {
        foreach (var form in _activeForms.ToArray())
        {
            if (!form.IsDisposed)
            {
                form.Close();
            }
        }

        _activeForms.Clear();
    }
}
