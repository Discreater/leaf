// Leaf's TSX entry: sets up the system tray icon, then mounts the Rust
// `App` component (the window's content). The reminder logic and UI stay in
// Rust; only the tray — which nsh exposes to TSX — lives here.

import { cleanup, mount } from "nsh";
import { Tray, currentWindow } from "nsh/desktop";
import { App, Controller } from "leaf/native";
import icon from "./assets/leaf.svg";

/** The root: the tray icon and the Rust window content. */
function Root() {
  const main = currentWindow();
  main.setBounds({ size: { width: 480, height: 320 } });
  Tray.set({
    icon,
    tooltip: "Leaf 休息提醒",
    onClick: () => main.focus(),
    menu: [
      { label: "重新加载配置", onSelect: () => Controller.reloadSettings() },
      { label: "立即提醒", onSelect: () => Controller.remindNow() },
      { type: "separator" },
      { label: "退出", onSelect: () => Controller.quit() },
    ],
  });

  // Remove the tray icon when the root unmounts.
  cleanup(() => Tray.set(null));

  return <App />;
}

mount(Root);
