# leaf

一个使用 [Native Shell (nsh)](https://nsh.dev) 构建的跨平台桌面休息提醒程序，运行在
**macOS / Windows / Linux** 上。业务逻辑（计时、休息时长计算、推迟、全屏抑制）与原
WinUI 3 版本保持一致；UI 与状态机在 Rust 中实现，托盘图标通过 nsh 的 TSX `Tray` 接
口接入。

> nsh 依赖当前锁定到 GitHub `main`（`fc8058ed`），因为托盘 `Tray` 已合并进 `main`
> 但尚未发布到 crates.io 的 0.4.0。

## 功能

- 系统托盘图标 + 菜单（重新加载配置 / 立即提醒 / 退出），点击托盘图标唤回主窗口。
- 启动后默认每 45 分钟提醒一次，可通过配置调整。
- 提醒时窗口自动置顶（`WindowLevel::Floating`；Linux 上同时 `focus()` 提升到前台）。
- 休息倒计时默认 10 分钟，可通过配置调整。
- 休息时间会根据连续工作时长和累计推迟时间自动增加。
- 支持 1 / 5 / 10 / 20 分钟四种推迟按钮。
- 全屏期间不弹窗，但计时继续；退出全屏 5 分钟后才允许弹窗（Windows 上检测真实全屏，
  macOS / Linux 目前不抑制弹窗）。
- 窗口右键菜单也可重新加载配置、立即提醒或退出。

## 运行

需要 [Rust](https://rustup.rs)（1.85+）、[nsh CLI](https://nsh.dev/docs/installation)
以及各平台依赖：

- **Linux**：GTK 4.14+（`libgtk-4-dev`、`libpango1.0-dev`、`libcairo2-dev`）以及
  GStreamer（`libgstreamer1.0-dev`、`libgstreamer-plugins-base1.0-dev`）。
- **macOS**：Xcode Command Line Tools。
- **Windows**：Visual Studio Build Tools（MSVC）或 MinGW-w64。

```bash
nsh run                       # 带托盘的完整应用（当前平台后端）
nsh package --target linux|macos|windows
```

`cargo run` 仍可直接运行 Rust 端（无托盘），便于快速迭代：

```bash
cargo run
```

程序目录或工作目录中的 `appsettings.json` 可用于调整提醒参数：

- `WorkIntervalMinutes`：工作间隔分钟数
- `BaseBreakDurationMinutes`：基础休息分钟数
- `FullscreenCooldownMinutes`：退出全屏后允许弹窗前的等待分钟数
- `OvertimeBlockMinutes`：超过工作间隔后，每多少分钟增加一次休息时长
- `BreakDurationExtensionPerOvertimeBlockMinutes`：每个超时区块增加的休息分钟数
- `PostponeBlockMinutes`：每累计多少分钟推迟，增加一次休息时长
- `BreakDurationExtensionPerPostponeBlockMinutes`：每个推迟区块增加的休息分钟数

## 项目结构

```text
leaf/
├── Cargo.toml          crate 与 git 依赖（nsh main）
├── nsh.json            nsh 应用清单（rust 入口 + App.tsx）
├── App.tsx             TSX 入口：托盘图标 + 挂载 Rust App
├── tsconfig.json       TSX 编辑/检查配置
├── assets/leaf.svg     托盘图标
├── appsettings.json    默认提醒参数
└── src/
    ├── lib.rs          nsh 入口（#[native_module]：App 组件 + Controller 模块）
    ├── main.rs         cargo run 的 Rust-only 桌面 runner
    ├── controller.rs   提醒状态机 + 暴露给 TSX 的 Controller 模块
    ├── settings.rs     appsettings.json 读取与规范化
    ├── fullscreen.rs   全屏检测（Windows 实现，其余平台占位）
    └── ui.rs           空闲 / 提醒两个视图
```

## 与 WinUI 3 版本的差异

- 原版本使用 `NotifyIcon` 且「每显示器一个提醒窗口」；nsh 目前仍只有单窗口模型，因此
  本版本是一个常驻窗口：空闲时显示下次提醒倒计时，提醒时切换为倒计时 + 推迟按钮。
- 托盘图标通过 TSX 的 `Tray.set` 接入；提醒逻辑、窗口菜单与右键菜单仍在 Rust 中。
