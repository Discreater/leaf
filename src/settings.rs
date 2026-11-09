//! Reminder settings: the same `appsettings.json` shape the original
//! WinUI 3 app used, so existing configuration files keep working.

use std::path::PathBuf;

use serde::{Deserialize, Serialize};

/// The user-tunable reminder parameters, keyed in `appsettings.json` by
/// their PascalCase names (`WorkIntervalMinutes`, ...).
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase")]
pub struct ReminderSettings {
    /// Work interval in minutes: how long a work session lasts.
    #[serde(default = "default_work_interval_minutes")]
    pub work_interval_minutes: i32,
    /// The base break duration in minutes.
    #[serde(default = "default_base_break_duration_minutes")]
    pub base_break_duration_minutes: i32,
    /// Minutes to wait after leaving full screen before a reminder may show.
    #[serde(default = "default_fullscreen_cooldown_minutes")]
    pub fullscreen_cooldown_minutes: i32,
    /// Every `overtime_block_minutes` past the work interval adds one block.
    #[serde(default = "default_overtime_block_minutes")]
    pub overtime_block_minutes: i32,
    /// Minutes added to the break per overtime block.
    #[serde(default = "default_break_duration_extension_per_overtime_block_minutes")]
    pub break_duration_extension_per_overtime_block_minutes: i32,
    /// Every `postpone_block_minutes` of accumulated postponing adds one block.
    #[serde(default = "default_postpone_block_minutes")]
    pub postpone_block_minutes: i32,
    /// Minutes added to the break per postpone block.
    #[serde(default = "default_break_duration_extension_per_postpone_block_minutes")]
    pub break_duration_extension_per_postpone_block_minutes: i32,
}

impl Default for ReminderSettings {
    fn default() -> Self {
        Self {
            work_interval_minutes: 45,
            base_break_duration_minutes: 10,
            fullscreen_cooldown_minutes: 5,
            overtime_block_minutes: 15,
            break_duration_extension_per_overtime_block_minutes: 2,
            postpone_block_minutes: 5,
            break_duration_extension_per_postpone_block_minutes: 1,
        }
    }
}

impl ReminderSettings {
    /// Clamps every field to a valid range, mirroring the original app's
    /// normalization so a hand-edited file cannot disable the reminder.
    pub fn normalized(self) -> Self {
        Self {
            work_interval_minutes: self.work_interval_minutes.max(1),
            base_break_duration_minutes: self.base_break_duration_minutes.max(1),
            fullscreen_cooldown_minutes: self.fullscreen_cooldown_minutes.max(0),
            overtime_block_minutes: self.overtime_block_minutes.max(1),
            break_duration_extension_per_overtime_block_minutes: self
                .break_duration_extension_per_overtime_block_minutes
                .max(0),
            postpone_block_minutes: self.postpone_block_minutes.max(1),
            break_duration_extension_per_postpone_block_minutes: self
                .break_duration_extension_per_postpone_block_minutes
                .max(0),
        }
    }
}

/// Loads settings from `appsettings.json`, writing a normalized copy when
/// the file is missing or contains values that needed clamping.
#[derive(Debug, Clone)]
pub struct SettingsProvider {
    path: Option<PathBuf>,
}

impl Default for SettingsProvider {
    fn default() -> Self {
        Self::discover()
    }
}

impl SettingsProvider {
    /// Finds `appsettings.json` next to the executable, then in the current
    /// directory. Neither is required: without a file the defaults apply.
    pub fn discover() -> Self {
        let mut candidates = Vec::new();
        if let Ok(exe) = std::env::current_exe()
            && let Some(dir) = exe.parent()
        {
            candidates.push(dir.join("appsettings.json"));
        }
        if let Ok(cwd) = std::env::current_dir() {
            candidates.push(cwd.join("appsettings.json"));
        }

        Self {
            path: candidates.into_iter().find(|path| path.is_file()),
        }
    }

    pub fn load(&self) -> ReminderSettings {
        match &self.path {
            Some(path) => Self::load_from(path),
            None => {
                let defaults = ReminderSettings::default();
                self.write_best_effort(&defaults);
                defaults
            }
        }
    }

    fn load_from(path: &PathBuf) -> ReminderSettings {
        let raw = std::fs::read_to_string(path).unwrap_or_default();
        let parsed: ReminderSettings = serde_json::from_str(&raw).unwrap_or_default();
        let normalized = parsed.normalized();

        let before = serde_json::to_string_pretty(&parsed).unwrap_or_default();
        let after = serde_json::to_string_pretty(&normalized).unwrap_or_default();
        if before != after {
            let _ = std::fs::write(path, after);
        }

        normalized
    }

    fn write_best_effort(&self, settings: &ReminderSettings) {
        let Some(json) = serde_json::to_string_pretty(settings).ok() else {
            return;
        };
        let dir = std::env::current_exe()
            .ok()
            .and_then(|exe| exe.parent().map(|dir| dir.to_path_buf()))
            .or_else(|| std::env::current_dir().ok());
        if let Some(dir) = dir {
            let _ = std::fs::create_dir_all(&dir);
            let _ = std::fs::write(dir.join("appsettings.json"), json);
        }
    }
}

fn default_work_interval_minutes() -> i32 {
    45
}

fn default_base_break_duration_minutes() -> i32 {
    10
}

fn default_fullscreen_cooldown_minutes() -> i32 {
    5
}

fn default_overtime_block_minutes() -> i32 {
    15
}

fn default_break_duration_extension_per_overtime_block_minutes() -> i32 {
    2
}

fn default_postpone_block_minutes() -> i32 {
    5
}

fn default_break_duration_extension_per_postpone_block_minutes() -> i32 {
    1
}
