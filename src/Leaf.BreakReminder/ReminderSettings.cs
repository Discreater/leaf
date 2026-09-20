namespace Leaf.BreakReminder;

internal sealed class ReminderSettings
{
    public int WorkIntervalMinutes { get; set; } = 45;

    public int BaseBreakDurationMinutes { get; set; } = 10;

    public int FullscreenCooldownMinutes { get; set; } = 5;

    public int OvertimeBlockMinutes { get; set; } = 15;

    public int BreakDurationExtensionPerOvertimeBlockMinutes { get; set; } = 2;

    public int PostponeBlockMinutes { get; set; } = 5;

    public int BreakDurationExtensionPerPostponeBlockMinutes { get; set; } = 1;
}
