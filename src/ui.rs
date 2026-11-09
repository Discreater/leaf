//! The app's UI: a single window that shows the idle status while working
//! and the break countdown while resting.

use nsh_base::{button, theme};
use nsh_core::prelude::*;

use crate::controller;

/// The app's root: the whole surface on the theme's background.
#[component]
pub fn App() -> impl IntoElement {
    controller::start();

    view()
        .flex_1()
        .bg(move || theme().background)
        .on_context_menu(move |event: &ContextMenuEvent| {
            controller::show_context_menu(event.page_position);
        })
        .child(when(move || controller::is_reminder(), reminder_view).otherwise(idle_view))
}

/// What shows while a break is running: the countdown and postpone buttons.
fn reminder_view() -> impl IntoElement {
    view()
        .flex_1()
        .p_4()
        .child(
            text("该休息了")
                .text_size(px(30.0))
                .font_weight(FontWeight::SEMIBOLD)
                .text_center()
                .text_color(move || theme().foreground),
        )
        .child(
            text(move || controller::summary_text())
                .text_size(px(18.0))
                .text_center()
                .text_color(move || theme().foreground),
        )
        .child(
            text(move || controller::countdown_text())
                .text_size(px(32.0))
                .font_weight(FontWeight::BOLD)
                .text_center()
                .text_color(move || theme().foreground),
        )
        .child(
            text("倒计时结束后窗口会自动关闭")
                .text_size(px(14.0))
                .text_center()
                .text_color(move || theme().muted_foreground),
        )
        .child(postpone_buttons())
}

/// The row of `推迟 N 分钟` buttons.
fn postpone_buttons() -> impl IntoElement {
    view()
        .flex_row()
        .justify_center()
        .gap_2()
        .mt_4()
        .child(
            controller::POSTPONE_OPTIONS_MINUTES
                .iter()
                .map(|minutes| postpone_button(*minutes)),
        )
}

fn postpone_button(minutes: u32) -> impl IntoElement {
    let label = format!("推迟 {minutes} 分钟");
    button()
        .on_press(move || controller::postpone(minutes))
        .disabled(move || !controller::postpone_enabled())
        .px_3()
        .py_2()
        .rounded(move || theme().radius)
        .bg(move || theme().primary)
        .active(|s| s.opacity(0.6))
        .disabled_style(|s| s.opacity(0.4))
        .child(text(label).text_color(move || theme().primary_foreground))
}

/// What shows while working: the countdown to the next reminder.
fn idle_view() -> impl IntoElement {
    view()
        .flex_1()
        .p_4()
        .child(
            text("Leaf 休息提醒")
                .text_size(px(22.0))
                .font_weight(FontWeight::SEMIBOLD)
                .text_center()
                .text_color(move || theme().foreground),
        )
        .child(
            text(move || controller::next_reminder_text())
                .text_size(px(18.0))
                .text_center()
                .text_color(move || theme().foreground),
        )
        .child(
            text(move || controller::settings_summary())
                .text_sm()
                .text_center()
                .text_color(move || theme().muted_foreground),
        )
        .child(
            text("右键菜单可重新加载配置、立即提醒或退出")
                .text_sm()
                .text_center()
                .text_color(move || theme().muted_foreground),
        )
}
