using System.Text.Json;

namespace Leaf.BreakReminder;

internal sealed class ReminderSettingsProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public ReminderSettingsProvider(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public ReminderSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = Normalize(new ReminderSettings());
            Save(defaults);
            return defaults;
        }

        var raw = File.ReadAllText(_settingsPath);
        var settings = JsonSerializer.Deserialize<ReminderSettings>(raw, SerializerOptions) ?? new ReminderSettings();
        settings = Normalize(settings);
        Save(settings);
        return settings;
    }

    private void Save(ReminderSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, SerializerOptions));
    }

    private static ReminderSettings Normalize(ReminderSettings settings)
    {
        settings.WorkIntervalMinutes = Math.Max(1, settings.WorkIntervalMinutes);
        settings.BaseBreakDurationMinutes = Math.Max(1, settings.BaseBreakDurationMinutes);
        settings.FullscreenCooldownMinutes = Math.Max(0, settings.FullscreenCooldownMinutes);
        settings.OvertimeBlockMinutes = Math.Max(1, settings.OvertimeBlockMinutes);
        settings.BreakDurationExtensionPerOvertimeBlockMinutes = Math.Max(0, settings.BreakDurationExtensionPerOvertimeBlockMinutes);
        settings.PostponeBlockMinutes = Math.Max(1, settings.PostponeBlockMinutes);
        settings.BreakDurationExtensionPerPostponeBlockMinutes = Math.Max(0, settings.BreakDurationExtensionPerPostponeBlockMinutes);
        return settings;
    }
}
