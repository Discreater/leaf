//! Best-effort foreground full-screen detection.
//!
//! The original WinUI 3 app used `GetForegroundWindow` /
//! `GetWindowRect` / `GetMonitorInfo` on Windows. That behavior is kept on
//! Windows; the other desktops currently report `false` (a reminder is
//! never suppressed), which keeps the app cross-platform without adding
//! Objective-C and X11/Wayland backends.

/// Whether the foreground window covers its monitor, i.e. the user is in a
/// full-screen session and should not be interrupted.
#[cfg(target_os = "windows")]
pub(crate) fn is_fullscreen_foreground_window() -> bool {
    use windows_sys::Win32::Foundation::RECT;
    use windows_sys::Win32::Graphics::Gdi::{
        GetMonitorInfoW, MonitorFromWindow, MONITORINFO, MONITOR_DEFAULTTONEAREST,
    };
    use windows_sys::Win32::UI::WindowsAndMessaging::{GetForegroundWindow, GetWindowRect};

    unsafe {
        let foreground = GetForegroundWindow();
        if foreground.is_null() {
            return false;
        }

        let mut rect = RECT {
            left: 0,
            top: 0,
            right: 0,
            bottom: 0,
        };
        if GetWindowRect(foreground, &mut rect) == 0 {
            return false;
        }

        let monitor = MonitorFromWindow(foreground, MONITOR_DEFAULTTONEAREST);
        if monitor.is_null() {
            return false;
        }

        let mut info = MONITORINFO {
            cbSize: std::mem::size_of::<MONITORINFO>() as u32,
            rcMonitor: RECT {
                left: 0,
                top: 0,
                right: 0,
                bottom: 0,
            },
            rcWork: RECT {
                left: 0,
                top: 0,
                right: 0,
                bottom: 0,
            },
            dwFlags: 0,
        };
        if GetMonitorInfoW(monitor, &mut info) == 0 {
            return false;
        }

        rect.left <= info.rcMonitor.left
            && rect.top <= info.rcMonitor.top
            && rect.right >= info.rcMonitor.right
            && rect.bottom >= info.rcMonitor.bottom
    }
}

/// Not implemented yet on macOS and Linux: never suppress a reminder.
#[cfg(not(target_os = "windows"))]
pub(crate) fn is_fullscreen_foreground_window() -> bool {
    false
}
