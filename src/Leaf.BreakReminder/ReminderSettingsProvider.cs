using System.Text.Json;

namespace Leaf.BreakReminder;

internal sealed class ReminderSettingsProvider
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
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
        var settings = JsonSerializer.Deserialize<ReminderSettings>(raw, ReadOptions) ?? new ReminderSettings();
        var originalSnapshot = JsonSerializer.Serialize(settings);
        var normalized = Normalize(settings);
        var normalizedSnapshot = JsonSerializer.Serialize(normalized);

        if (!string.Equals(originalSnapshot, normalizedSnapshot, StringComparison.Ordinal))
        {
            Save(normalized);
        }

        return normalized;
    }

    private void Save(ReminderSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(settings, WriteOptions));
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
