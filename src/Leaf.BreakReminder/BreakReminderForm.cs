namespace Leaf.BreakReminder;

internal sealed class BreakReminderForm : Form
{
    private readonly Font _titleFont;
    private readonly Font _summaryFont;
    private readonly Font _countdownFont;
    private readonly Font _detailFont;
    private readonly List<Button> _postponeButtons = [];
    private readonly Label _titleLabel;
    private readonly Label _summaryLabel;
    private readonly Label _countdownLabel;
    private readonly Label _detailLabel;

    public BreakReminderForm(Screen screen, IReadOnlyList<int> postponeOptionsMinutes)
    {
        var baseFont = SystemFonts.MessageBoxFont ?? DefaultFont;

        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        ShowInTaskbar = false;
        TopMost = true;
        ControlBox = false;
        Text = "休息提醒";
        ClientSize = new Size(460, 280);
        BackColor = SystemColors.Control;
        _titleFont = new Font(baseFont.FontFamily, 22, FontStyle.Bold);
        _summaryFont = new Font(baseFont.FontFamily, 11, FontStyle.Regular);
        _countdownFont = new Font(baseFont.FontFamily, 26, FontStyle.Bold);
        _detailFont = new Font(baseFont.FontFamily, 10, FontStyle.Regular);

        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Font = _titleFont,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "该休息了"
        };

        _summaryLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Font = _summaryFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _countdownLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 72,
            Font = _countdownFont,
            TextAlign = ContentAlignment.MiddleCenter
        };

        _detailLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Font = _detailFont,
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "倒计时结束后窗口会自动关闭"
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(12, 18, 12, 12)
        };

        foreach (var minutes in postponeOptionsMinutes)
        {
            var button = new Button
            {
                AutoSize = true,
                Margin = new Padding(8),
                Padding = new Padding(10, 8, 10, 8),
                Text = $"推迟 {minutes} 分钟"
            };

            button.Click += (_, _) => PostponeRequested?.Invoke(this, minutes);
            _postponeButtons.Add(button);
            buttonPanel.Controls.Add(button);
        }

        Controls.Add(buttonPanel);
        Controls.Add(_detailLabel);
        Controls.Add(_countdownLabel);
        Controls.Add(_summaryLabel);
        Controls.Add(_titleLabel);

        CenterOnScreen(screen);
    }

    public event EventHandler<int>? PostponeRequested;

    public void UpdateContent(int elapsedWorkMinutes, int breakDurationMinutes, TimeSpan remaining)
    {
        _summaryLabel.Text = $"你已经连续工作 {elapsedWorkMinutes} 分钟，本次建议休息 {breakDurationMinutes} 分钟。";
        _countdownLabel.Text = remaining.TotalHours >= 1
            ? $"{(int)remaining.TotalHours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
            : $"{(int)remaining.TotalMinutes:00}:{remaining.Seconds:00}";
    }

    public void SetPostponeButtonsEnabled(bool enabled)
    {
        foreach (var button in _postponeButtons)
        {
            button.Enabled = enabled;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _titleLabel.Font = DefaultFont;
            _summaryLabel.Font = DefaultFont;
            _countdownLabel.Font = DefaultFont;
            _detailLabel.Font = DefaultFont;

            _titleFont.Dispose();
            _summaryFont.Dispose();
            _countdownFont.Dispose();
            _detailFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void CenterOnScreen(Screen screen)
    {
        var workingArea = screen.WorkingArea;
        Left = workingArea.Left + ((workingArea.Width - Width) / 2);
        Top = workingArea.Top + ((workingArea.Height - Height) / 2);
    }
}
