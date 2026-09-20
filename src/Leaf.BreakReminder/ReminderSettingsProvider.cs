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
        var originalSnapshot = JsonSerializer.Serialize(settings, WriteOptions);
        var normalized = Normalize(settings);
        var normalizedSnapshot = JsonSerializer.Serialize(normalized, WriteOptions);

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
        return new ReminderSettings
        {
            WorkIntervalMinutes = Math.Max(1, settings.WorkIntervalMinutes),
            BaseBreakDurationMinutes = Math.Max(1, settings.BaseBreakDurationMinutes),
            FullscreenCooldownMinutes = Math.Max(0, settings.FullscreenCooldownMinutes),
            OvertimeBlockMinutes = Math.Max(1, settings.OvertimeBlockMinutes),
            BreakDurationExtensionPerOvertimeBlockMinutes = Math.Max(0, settings.BreakDurationExtensionPerOvertimeBlockMinutes),
            PostponeBlockMinutes = Math.Max(1, settings.PostponeBlockMinutes),
            BreakDurationExtensionPerPostponeBlockMinutes = Math.Max(0, settings.BreakDurationExtensionPerPostponeBlockMinutes)
        };
    }
}
