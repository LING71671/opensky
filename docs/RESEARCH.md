# 桌面级 Computer Use 深度技术调查与架构解密报告

> 本文档基于对 GitHub 上各代表性开源项目（`bytedance/UI-TARS-desktop`、`trycua/cua`、`CursorTouch/Windows-MCP`、`iFurySt/open-codex-computer-use` 等）的代码级真实实勘与架构对比而成。

---

## 1. 行业主流代表项目源码解密

| 仓库名称 | 核心技术栈 | 鼠标控制方式 | 截图机制 | UIA/无障碍使用情况 | 视觉反馈/动效 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`trycua/cua`** | Rust 多 Crate (`cua-driver`, `cursor-overlay`) | **两级梯度**：默认 `PostMessage` 后台消息；仅在必要时借调焦点 `SendInput`，并在刷出后还原前台 | 按需捕获，支持 WGC (Windows Graphics Capture) | 独立 Named Pipe worker，`uiAccess` 提权穿透 | **业界顶尖**：贝塞尔曲线轨迹、`spring: 0.72` 弹簧阻尼、光标下方挂载 Session Badge 胶囊 |
| **`open-codex-computer-use`** | Go + PowerShell 运行时 (`runtime.ps1`) | 尝试通过 `ScreenToClient` 换算坐标后发送 `PostMessage` 模拟点击 | 窗口截图转 Base64 PNG | 尝试调用 `InvokePattern` / `TogglePattern`，但采用禁止的 `FindAll(Descendants)` 递归易卡死 | **零动效**：完全无 Overlay 渲染 |
| **`Windows-MCP`** | Python + `fastmcp` + `ctypes` | 粗暴调用 `ctypes.windll.user32.SendInput` 物理抢占鼠标 | 每次动作强制要求先调用 Snapshot | 仅用来做“坐标查询尺”，未调用 Pattern 内存执行 | **粗糙警告灯**：截屏时用 `UpdateLayeredWindow` 闪烁 2.5 秒刺眼橙红边框 |
| **`bytedance/UI-TARS-desktop`** | Electron + NutJS / PyAutoGUI | 粗暴底层 `SetCursorPos` / `SendInput` 瞬间位移 | 动作与截图强绑定闭环 | 完全放弃无障碍，纯靠 VLM 视觉大模型推理坐标 | **零动效**：黑盒脚本操作 |

---

## 2. 核心技术死穴分析

### ① 强行绑定“动鼠标必须截图”的算力税
* **现状**：大量现有方案把“环境作为黑盒”，大模型充当单步闭环。导致即使是已知界面的连续滚动、滑块拖拽、列表点击，也必须每步截一张几兆的高清图并回传给大模型。
* **代价**：单步耗时长达 2 ~ 5 秒，单步消耗 1000 ~ 2000 Tokens，成本高出 45 倍。
* **破局**：在接口与驱动层彻底正交解耦，提供原子级的独立物理动作管道（`win_mouse_scroll`、`win_mouse_drag`、`win_mouse_move`、`win_blind_click`），耗时 `< 15ms`，零截图开销。

### ② Windows 物理鼠标抢占与“人机互殴”
* **现状**：Windows `user32.dll` 全局只有一个硬件光标指针。几乎所有产品在 Windows 本地运行时，都会强行霸占用户的鼠标，导致用户正打着字或看着屏幕突然被夺走焦点。
* **破局**：
  1. **第一优先级**：无光标内存直调（`InvokePattern.Invoke()` / `ValuePattern.SetValue()`），物理光标停留在原地，前台窗口不切换，0 毫秒响应，100% 精准；
  2. **第二优先级**：后台窗口消息注入（`PostMessage`）；
  3. **第三优先级**：当必须物理移动鼠标时，叠加 Apple 质感的极简白光呼吸光晕（Halo）与 Action Pill 状态微胶囊，操作完成后弹簧平滑淡出，消除用户不确定感。

### ③ 动画与视觉质感的代差
* **粗糙方案**：无动效，或普通的 WinForms 窗体加 `TransparencyKey` 导致黑边、锯齿、剧烈闪烁；
* **OpenSky 方案**：
  * 基于 Win32 硬件级 32-bit ARGB 透明分层窗体（`UpdateLayeredWindow`）；
  * 引入二阶阻尼弹簧运动方程（$ \zeta \approx 0.76, \omega \approx 12.5 $），赋予所有波纹扩散与标签缩放真实的物理惯性与微回弹；
  * 坚持单色磨砂白（Apple Frosted Monochrome）高级微光，杜绝红绿蓝廉价彩灯。
