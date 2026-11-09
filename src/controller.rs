//! The reminder state machine, platform-neutral and driven by wall-clock
//! time. UI reads reactive accessors; every second a tick recomputes the
//! state exactly as the original WinUI 3 app did.

use std::time::{Duration, Instant};

use nsh_core::{
    desktop::WindowLevel,
    element::desktop::{
        close_window, order_front, popup_menu_at, set_application_menu, set_level, set_title,
        MenuItem,
    },
    geometry::{Point, Points},
    prelude::*,
};

use crate::settings::{ReminderSettings, SettingsProvider};

/// The postpone choices offered on the reminder window, in minutes.
pub const POSTPONE_OPTIONS_MINUTES: [u32; 4] = [1, 5, 10, 20];

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Phase {
    Idle,
    Reminder,
}

#[derive(Clone)]
struct State {
    provider: SettingsProvider,
    settings: ReminderSettings,
    phase: Phase,
    last_break_ended_at: Instant,
    next_reminder_at: Instant,
    last_fullscreen_exited_at: Option<Instant>,
    break_ends_at: Option<Instant>,
    reminder_pending: bool,
    was_fullscreen: bool,
    postpone_handled: bool,
    active_break_duration_minutes: i32,
    active_work_session_minutes: i32,
    total_postponed_minutes: i32,
}

impl Default for State {
    fn default() -> Self {
        let provider = SettingsProvider::default();
        let settings = provider.load();
        let now = Instant::now();
        Self {
            next_reminder_at: now + Duration::from_secs(work_interval_secs(&settings)),
            provider,
            settings,
            phase: Phase::Idle,
            last_break_ended_at: now,
            last_fullscreen_exited_at: None,
            break_ends_at: None,
            reminder_pending: false,
            was_fullscreen: false,
            postpone_handled: false,
            active_break_duration_minutes: 0,
            active_work_session_minutes: 0,
            total_postponed_minutes: 0,
        }
    }
}

fn state() -> Signal<State> {
    global::<State>()
}

/// Starts the controller: the application menu, the floating-window effect
/// and the one-second tick. Called once by the root component.
pub fn start() {
    set_title("Leaf 休息提醒");
    set_application_menu(application_menu());

    effect(move || {
        if is_reminder() {
            // The reminder floats above normal windows (macOS / Windows);
            // order_front also raises it on Linux, where window levels are
            // a no-op in nsh's GTK backend.
            set_level(WindowLevel::Floating);
            order_front();
        } else {
            set_level(WindowLevel::Normal);
        }
    });

    set_interval(Duration::from_secs(1), tick);
}

/// The menu shown as the application menu and as the window's context menu.
pub fn application_menu() -> Vec<MenuItem> {
    vec![
        MenuItem::new("重新加载配置").on_select(reload_settings),
        MenuItem::new("立即提醒").on_select(remind_now),
        MenuItem::separator(),
        MenuItem::new("退出").on_select(close_window),
    ]
}

/// Shows the app menu as a context menu at `at` (window coordinates).
pub fn show_context_menu(at: Point<Points>) {
    let _ = popup_menu_at(application_menu(), at);
}

/// Postpones the active reminder by `minutes`.
pub fn postpone(minutes: u32) {
    state().update(|s| {
        if s.break_ends_at.is_none() || s.postpone_handled {
            return;
        }

        s.postpone_handled = true;
        s.break_ends_at = None;
        s.total_postponed_minutes += minutes as i32;
        s.next_reminder_at = Instant::now() + Duration::from_secs(u64::from(minutes) * 60);
        s.reminder_pending = false;
        s.active_work_session_minutes = 0;
        s.phase = Phase::Idle;
    });
}

/// Whether the reminder view is currently showing.
pub fn is_reminder() -> bool {
    state().with(|s| s.phase == Phase::Reminder)
}

/// The reminder summary line ("你已经连续工作 … 分钟…").
pub fn summary_text() -> String {
    state().with(|s| {
        format!(
            "你已经连续工作 {} 分钟，本次建议休息 {} 分钟。",
            s.active_work_session_minutes, s.active_break_duration_minutes
        )
    })
}

/// The break countdown (`MM:SS`, or `HH:MM:SS` past one hour).
pub fn countdown_text() -> String {
    state().with(|s| match s.break_ends_at {
        Some(ends_at) => format_remaining(ends_at.saturating_duration_since(Instant::now())),
        None => String::new(),
    })
}

/// The idle countdown to the next reminder.
pub fn next_reminder_text() -> String {
    state().with(|s| {
        format!(
            "距下次提醒还剩 {}",
            format_remaining(s.next_reminder_at.saturating_duration_since(Instant::now()))
        )
    })
}

/// Whether the postpone buttons may be pressed.
pub fn postpone_enabled() -> bool {
    state().with(|s| s.break_ends_at.is_some() && !s.postpone_handled)
}

/// A one-line summary of the active settings, shown while idle.
pub fn settings_summary() -> String {
    state().with(|s| {
        format!(
            "工作 {} 分钟 · 基础休息 {} 分钟 · 全屏冷却 {} 分钟",
            s.settings.work_interval_minutes,
            s.settings.base_break_duration_minutes,
            s.settings.fullscreen_cooldown_minutes
        )
    })
}

fn reload_settings() {
    state().update(|s| {
        s.settings = s.provider.load();
        if s.break_ends_at.is_none() {
            s.next_reminder_at =
                s.last_break_ended_at + Duration::from_secs(work_interval_secs(&s.settings));
        }
    });
}

fn remind_now() {
    state().update(|s| {
        s.next_reminder_at = Instant::now();
        s.reminder_pending = true;
    });
}

fn tick() {
    let now = Instant::now();
    let is_fullscreen = crate::fullscreen::is_fullscreen_foreground_window();

    state().update(|s| {
        if s.was_fullscreen && !is_fullscreen {
            s.last_fullscreen_exited_at = Some(now);
        }
        s.was_fullscreen = is_fullscreen;

        if let Some(ends_at) = s.break_ends_at {
            if now >= ends_at {
                complete_reminder(s, now);
            }
            return;
        }

        if now >= s.next_reminder_at {
            s.reminder_pending = true;
        }

        if s.reminder_pending && can_show_reminder(s, now) {
            show_reminder(s, now);
        }
    });
}

fn can_show_reminder(s: &State, now: Instant) -> bool {
    if s.was_fullscreen {
        return false;
    }

    match s.last_fullscreen_exited_at {
        None => true,
        Some(exited_at) => {
            now.saturating_duration_since(exited_at)
                >= Duration::from_secs(s.settings.fullscreen_cooldown_minutes.max(0) as u64 * 60)
        }
    }
}

fn show_reminder(s: &mut State, now: Instant) {
    s.reminder_pending = false;
    s.postpone_handled = false;
    s.active_work_session_minutes = elapsed_work_minutes(s, now);
    s.active_break_duration_minutes = break_duration_minutes(s, now);
    s.break_ends_at = Some(
        now + Duration::from_secs(s.active_break_duration_minutes.max(1) as u64 * 60),
    );
    s.phase = Phase::Reminder;
}

fn complete_reminder(s: &mut State, now: Instant) {
    s.break_ends_at = None;
    s.total_postponed_minutes = 0;
    s.last_break_ended_at = now;
    s.next_reminder_at = now + Duration::from_secs(work_interval_secs(&s.settings));
    s.reminder_pending = false;
    s.active_work_session_minutes = 0;
    s.postpone_handled = false;
    s.phase = Phase::Idle;
}

fn elapsed_work_minutes(s: &State, now: Instant) -> i32 {
    let seconds = now.saturating_duration_since(s.last_break_ended_at).as_secs();
    ((seconds + 59) / 60) as i32
}

fn break_duration_minutes(s: &State, now: Instant) -> i32 {
    let elapsed = elapsed_work_minutes(s, now);
    let overtime = (elapsed - s.settings.work_interval_minutes).max(0);
    let overtime_blocks = ceiling_blocks(overtime, s.settings.overtime_block_minutes);
    let postponed_blocks = ceiling_blocks(s.total_postponed_minutes, s.settings.postpone_block_minutes);

    s.settings.base_break_duration_minutes
        + overtime_blocks * s.settings.break_duration_extension_per_overtime_block_minutes
        + postponed_blocks * s.settings.break_duration_extension_per_postpone_block_minutes
}

fn ceiling_blocks(total_minutes: i32, block_minutes: i32) -> i32 {
    if total_minutes <= 0 {
        return 0;
    }
    (total_minutes + block_minutes - 1) / block_minutes
}

fn work_interval_secs(settings: &ReminderSettings) -> u64 {
    settings.work_interval_minutes.max(1) as u64 * 60
}

fn format_remaining(remaining: Duration) -> String {
    let total = remaining.as_secs();
    if total >= 3600 {
        format!(
            "{:02}:{:02}:{:02}",
            total / 3600,
            (total % 3600) / 60,
            total % 60
        )
    } else {
        format!("{:02}:{:02}", total / 60, total % 60)
    }
}
