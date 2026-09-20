namespace Leaf.BreakReminder;

internal sealed class BreakReminderForm : Form
{
    private readonly Label _summaryLabel;
    private readonly Label _countdownLabel;
    private readonly Label _detailLabel;

    public BreakReminderForm(Screen screen, IReadOnlyList<int> postponeOptionsMinutes)
    {
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

        var titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 22, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "该休息了"
        };

        _summaryLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 11, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _countdownLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 72,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 26, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _detailLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10, FontStyle.Regular),
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
            buttonPanel.Controls.Add(button);
        }

        Controls.Add(buttonPanel);
        Controls.Add(_detailLabel);
        Controls.Add(_countdownLabel);
        Controls.Add(_summaryLabel);
        Controls.Add(titleLabel);

        CenterOnScreen(screen);
    }

    public event EventHandler<int>? PostponeRequested;

    public void UpdateContent(int elapsedWorkMinutes, int breakDurationMinutes, TimeSpan remaining)
    {
        _summaryLabel.Text = $"你已经连续工作 {elapsedWorkMinutes} 分钟，本次建议休息 {breakDurationMinutes} 分钟。";
        _countdownLabel.Text = remaining.ToString(@"mm\:ss");
    }

    private void CenterOnScreen(Screen screen)
    {
        var workingArea = screen.WorkingArea;
        Left = workingArea.Left + ((workingArea.Width - Width) / 2);
        Top = workingArea.Top + ((workingArea.Height - Height) / 2);
    }
}
