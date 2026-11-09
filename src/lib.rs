//! Leaf: a cross-platform desktop break reminder built with Native Shell
//! (nsh). The reminder logic lives in [`controller`], settings parsing in
//! [`settings`], and the UI in [`ui`].

mod controller;
mod fullscreen;
mod settings;
mod ui;

use nsh_core::prelude::*;

pub use ui::App;

/// The app's UI set (`nsh.json` `"rust"`).
#[native_module("leaf/native")]
fn native(registry: &mut Registry) {
    registry.register::<App>();
}
