<div align="center">

<img src="./assets/banner.jpg" alt="OpenSky Banner" width="100%" />

# OpenSky

**跨平台 Computer Use 引擎与 MCP 服务端**

[English](./README.md) | 简体中文

</div>

OpenSky 是一个跨平台的 Computer Use 引擎与 Model Context Protocol (MCP) 服务端实现，用于为 AI Agent（如 OpenCode、Reasonix、Claude Desktop、Cursor、Antigravity 与 OpenAI Codex）提供操作系统桌面与应用自动化能力。

目前项目优先提供 Windows 平台的原生驱动实现，接口层保持平台无关，支持后续扩展至 macOS 与 Linux。

---

## 核心设计

### 1. 双轨交互支持
- **UI Automation (无障碍树)**：解析当前界面的控件树结构并赋予数字索引（例如 `[15] Button "提交"`）。Agent 可通过元素索引直接执行点击或修改文本内容。该方式基于系统无障碍接口，不受分辨率缩放（DPI）影响，且无需模型通过坐标估算进行盲猜。
- **截图与坐标 (Visual Perception)**：支持全屏或指定窗口截图，并返回图像数据。当面对非标准控件、Canvas 或自绘 UI 时，Agent 可结合坐标执行点击与拖拽。

### 2. 视觉反馈覆盖层
内置基于 Win32 分层透明窗口（`WS_EX_TRANSPARENT | WS_EX_LAYERED`）的反馈机制：
- **点击波纹**：在执行点击的位置渲染短暂扩散的波纹动画，便于用户直观确认操作位置。
- **活动窗口高亮**：对当前正在操作的目标窗口提供边缘发光反馈。
- **事件穿透**：覆盖层不接收输入焦点，不阻挡用户的键鼠操作。

### 3. 解耦架构
- **核心契约 (`src/core`)**：定义通用的 `IComputerUseEngine` 接口与基础数据类型。
- **传输层 (`src/driver/transport`)**：提供进程间通信支持（STDIO、Named Pipe）。
- **驱动层 (`src/driver`)**：
  - Windows：拆分为窗口枚举、屏幕截取、无障碍树遍历、输入模拟及覆盖层渲染等独立模块，使用系统内置的 `csc.exe` 编译。
  - macOS / Linux：保留驱动接口以供后续接入。
- **适配层 (`src/adapters`)**：提供 MCP 服务端、Anthropic 格式桥接及 Codex 插件导出工具。

---

## 架构

```
+-------------------------------------------------------------+
|                     AI Agent / Client                       |
|   (OpenCode, Reasonix, Claude Desktop, Cursor, Codex 等)   |
+------------------------------+------------------------------+
                               |
+------------------------------v------------------------------+
|                        Adapters 层                          |
|   - MCP Server (JSON-RPC)                                   |
|   - Anthropic Bridge (computer_20241022)                    |
|   - Codex Plugin Exporter                                   |
+------------------------------+------------------------------+
                               |
+------------------------------v------------------------------+
|                     Core Engine 接口                         |
|                   (IComputerUseEngine)                      |
+------------------------------+------------------------------+
                               |
               +---------------+---------------+
               |                               |
+--------------v---------------+ +--------------v---------------+
|        Windows Driver        | |   macOS / Linux (规划中)     |
|   (WinComputerUseHelper)     | |   (Accessibility / AT-SPI)   |
| - 窗口管理 (WindowService)   | +------------------------------+
| - 截图捕获 (CaptureService)  |
| - 控件遍历 (Accessibility)   |
| - 键鼠模拟 (InputService)    |
| - 反馈动画 (OverlayService)  |
+------------------------------+
```

---

## 环境要求与构建

### 环境要求
- Windows 10 / 11 (64位)
- Node.js >= 20.0.0
- pnpm
- Windows 内置 .NET Framework 4.8（`csc.exe` 位于 `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`）

### 构建流程

```bash
# 安装依赖
pnpm install

# 编译 C# 原生辅助程序并打包 TypeScript 代码
pnpm run build

# 运行功能测试
pnpm test
```

---

## 客户端配置

### OpenCode
在 `opencode.jsonc` 中添加本地 MCP 服务：
```jsonc
{
  "mcp": {
    "opensky": {
      "type": "local",
      "command": ["node", "b:/computer-use/dist/bin/opensky.js"]
    }
  }
}
```

### Reasonix / Google Antigravity / Cursor
项目根目录已包含 `.mcp.json`，可直接被支持项目级 MCP 配置的客户端识别：
```json
{
  "mcpServers": {
    "opensky": {
      "command": "node",
      "args": ["b:/computer-use/dist/bin/opensky.js"]
    }
  }
}
```

### Claude Desktop
编辑 `%APPDATA%\Claude\claude_desktop_config.json`：
```json
{
  "mcpServers": {
    "opensky": {
      "command": "node",
      "args": ["b:/computer-use/dist/bin/opensky.js"]
    }
  }
}
```

### OpenAI Codex
运行以下命令可导出 Codex 插件格式：
```bash
node dist/bin/opensky.js --export-codex ./opensky-codex-plugin
```
将在指定目录下生成 `.codex-plugin/plugin.json`、`.mcp.json` 以及 `skills/opensky/SKILL.md`。

---

## 工具列表

| 工具名称 | 参数 | 说明 |
| :--- | :--- | :--- |
| `win_list_windows` | 无 | 获取当前可见的应用窗口列表（句柄、标题、所属进程、矩形坐标）。 |
| `win_activate_window` | `hwnd: number` | 将目标窗口置于前台并展示呼吸边框反馈。 |
| `win_screenshot` | `hwnd?: number` | 截取全屏或指定窗口图像，返回 Base64 编码的 JPEG。 |
| `win_get_ui_tree` | `hwnd?: number`, `maxDepth?: number` | 获取无障碍控件树及每个节点的序号索引。 |
| `win_click` | `elementIndex?: number`, `x?: number`, `y?: number` | 点击指定索引的控件或指定屏幕坐标，并触发点击波纹反馈。 |
| `win_double_click` | `elementIndex?: number`, `x?: number`, `y?: number` | 双击指定控件或屏幕坐标。 |
| `win_right_click` | `elementIndex?: number`, `x?: number`, `y?: number` | 右键点击指定控件或屏幕坐标。 |
| `win_drag` | `startX, startY, endX, endY, durationMs?` | 鼠标平滑拖拽。 |
| `win_type_text` | `text: string` | 向当前焦点控件输入 Unicode 文本。 |
| `win_press_key` | `key: string` | 发送单个按键或组合快捷键（如 `Ctrl+S`、`Enter`、`Alt+F4`、`Win+R`）。 |
| `win_scroll` | `deltaY: number`, `x?: number`, `y?: number` | 在指定位置滚动鼠标滚轮。 |
| `win_set_value` | `elementIndex: number`, `value: string` | 通过 UI Automation ValuePattern 直接修改输入控件的文本值。 |

---

## 许可证

本项目采用 [MIT 许可证](./LICENSE)。
