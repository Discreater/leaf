# leaf

一个使用 WinUI 3 构建界面的原生 Windows 桌面休息提醒程序。

## 功能

- 启动后默认每 45 分钟提醒一次，可通过配置调整。
- 提醒时会在所有显示器上弹出强制置顶窗口。
- 休息倒计时默认 10 分钟，可通过配置调整。
- 休息时间会根据连续工作时长和累计推迟时间自动增加。
- 支持 1 / 5 / 10 / 20 分钟四种推迟按钮。
- 全屏期间不弹窗，但计时继续；退出全屏 5 分钟后才允许弹窗。

## 运行

```bash
dotnet build leaf.slnx
```

程序输出目录中的 `appsettings.json` 可用于调整提醒参数：

- `WorkIntervalMinutes`：工作间隔分钟数
- `BaseBreakDurationMinutes`：基础休息分钟数
- `FullscreenCooldownMinutes`：退出全屏后允许弹窗前的等待分钟数
- `OvertimeBlockMinutes`：超过工作间隔后，每多少分钟增加一次休息时长
- `BreakDurationExtensionPerOvertimeBlockMinutes`：每个超时区块增加的休息分钟数
- `PostponeBlockMinutes`：每累计多少分钟推迟，增加一次休息时长
- `BreakDurationExtensionPerPostponeBlockMinutes`：每个推迟区块增加的休息分钟数
