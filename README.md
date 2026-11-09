# leaf

一个使用 [Native Shell (nsh)](https://nsh.dev) 构建的跨平台桌面休息提醒程序，运行在
**macOS / Windows / Linux** 上。业务逻辑（计时、休息时长计算、推迟、全屏抑制）与原
WinUI 3 版本保持一致，UI 使用 nsh 的 Rust 组件重写。

## 功能

- 启动后默认每 45 分钟提醒一次，可通过配置调整。
- 提醒时窗口自动置顶（`WindowLevel::Floating`；Linux 上通过 `order_front` 提升到前台）。
- 休息倒计时默认 10 分钟，可通过配置调整。
- 休息时间会根据连续工作时长和累计推迟时间自动增加。
- 支持 1 / 5 / 10 / 20 分钟四种推迟按钮。
- 全屏期间不弹窗，但计时继续；退出全屏 5 分钟后才允许弹窗（Windows 上检测真实全屏，
  macOS / Linux 目前不抑制弹窗）。
- 右键窗口或使用应用菜单可重新加载配置、立即提醒或退出。

## 运行

需要 [Rust](https://rustup.rs)（1.85+）以及各平台依赖：

- **Linux**：GTK 4.14+（`libgtk-4-dev`、`libpango1.0-dev`、`libcairo2-dev` 等）。
- **macOS**：Xcode Command Line Tools。
- **Windows**：Visual Studio Build Tools（MSVC）或 MinGW-w64。

```bash
cargo run
```

也可以通过 nsh CLI 运行与打包：

```bash
nsh run        # 当前平台后端
nsh package --target linux|macos|windows
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
├── Cargo.toml          crate 与平台依赖
├── nsh.json            nsh 应用清单（root 组件为 App）
├── appsettings.json    默认提醒参数
└── src/
    ├── lib.rs          nsh 入口（#[native_module] + App）
    ├── main.rs         cargo run 的桌面后端 runner
    ├── controller.rs   提醒状态机（纯逻辑，跨平台）
    ├── settings.rs     appsettings.json 读取与规范化
    ├── fullscreen.rs   全屏检测（Windows 实现，其余平台占位）
    └── ui.rs           空闲 / 提醒两个视图
```

## 与 WinUI 3 版本的差异

- 原版本使用系统托盘（`NotifyIcon`）并「每显示器一个提醒窗口」；nsh 0.4 的核心只提供
  单窗口模型，因此本版本改为一个常驻窗口，空闲时显示下次提醒倒计时，提醒时切换为
  倒计时 + 推迟按钮。
- 托盘菜单改为应用菜单 + 窗口右键菜单（重新加载配置 / 立即提醒 / 退出）。
