# OpenSky 开发规范与架构准则 (Development & Architecture Guidelines)

> **核心哲学**：极致低耦合、单层单职责、多级目录层次化、免光标优先、Apple 级单色极简白光质感。

---

## 1. 目录架构规范：分层垂直解耦，严禁平铺堆积

OpenSky 摒弃一切单目录文件平铺的反模式，采用垂直正交的多级目录结构。每一级目录都有明确的物理与逻辑边界：

```
src/
├── core/                                 # [抽象层] 纯领域契约与模型（零任何平台/系统实现依赖）
│   ├── contracts/                        # 细粒度、正交的抽象接口 (遵循接口隔离原则 ISP)
│   │   ├── mouse.contract.ts             # IMouseDriver: 纯物理/盲鼠标动作
│   │   ├── keyboard.contract.ts          # IKeyboardDriver: 纯按键与文本注入
│   │   ├── semantic.contract.ts          # ISemanticDriver: 内存级无障碍树与 Pattern 直调
│   │   ├── vision.contract.ts            # IVisionDriver: 纯视觉捕获与截图
│   │   ├── window.contract.ts            # IWindowManager: 窗口枚举与生命周期
│   │   └── engine.contract.ts            # IComputerUseEngine: 组合门面
│   └── models/                           # 纯数据实体（按领域拆分子文件，严禁全部塞在单个 types.ts）
│       ├── geometry.model.ts             # Rect, Point, CursorPosition
│       ├── window.model.ts               # WindowInfo, AppInfo
│       ├── semantic.model.ts             # UIElement, AccessibilityTreeResult, PatternInvokeOptions
│       ├── input.model.ts                # ClickOptions, MoveOptions, DragOptions, ScrollOptions
│       └── vision.model.ts               # ScreenshotOptions, ScreenshotResult
│
├── driver/                               # [驱动实现层] 平台具体实现与组合
│   ├── transport/                        # IPC 传输基础设施 (Stdio, Named Pipe)
│   └── windows/                          # Windows 细分领域子驱动（相互解耦）
│       ├── mouse.driver.ts               # 仅实现 IMouseDriver，绝不依赖截图
│       ├── keyboard.driver.ts            # 仅实现 IKeyboardDriver
│       ├── semantic.driver.ts            # 仅实现 ISemanticDriver (UIA 内存直连)
│       ├── vision.driver.ts              # 仅实现 IVisionDriver (按需视觉抓取)
│       ├── window.driver.ts              # 仅实现 IWindowManager
│       └── windows.engine.ts             # 组合门面驱动（通过委托聚合子驱动）
│
└── native/                               # [Windows 原生层] C# 微服务按关注点多级划分
    ├── Common/                           # 跨模块 Win32 API 常量与内存结构体 (NativeTypes.cs)
    ├── Input/                            # 纯物理硬件输入模拟 (InputService.cs)
    ├── Semantic/                         # 纯无障碍树遍历与 Pattern 内存直调 (AccessibilityService, PatternService)
    ├── Vision/                           # 屏幕硬件与窗体截取 (CaptureService.cs)
    ├── Window/                           # 窗口枚举、激活与桌面隔离 (WindowService, DesktopManager)
    ├── Overlay/                          # 32位ARGB硬件分层窗体与物理弹簧动效 (Renderer, Models, Service, SpringPhysics)
    └── Host/                             # IPC 管道命令分发宿主 (Program.cs)
```

---

## 2. 职责与代码长度纪律 (Single Responsibility & Clean Files)

### ① 文件单一职责 (File SRP)
* **一个文件只做一件事**：每个文件只定义一个独立的契约、一个领域模型组、或一个特定功能的执行服务。
* **文件长度限制**：
  * 单个文件建议控制在 **50 ~ 200 行**以内；
  * **硬性上限**：任何新文件**绝对严禁超过 300 行**。一旦发现代码超过 300 行，必须立即按职责抽象拆分为子模块。

### ② 函数单一职责 (Function SRP)
* **一个函数只达成一个明确意图**：严禁把“解析参数、坐标换算、执行输入、抓取截屏、格式化输出”全部揉进一个上百行的超级函数中。
* **函数长度限制**：
  * 单个函数建议控制在 **10 ~ 40 行**以内；
  * **硬性上限**：任何单一函数**绝对严禁超过 60 行**；
  * 超过 60 行的函数必须提取私有子方法或独立计算工具类。

### ③ 极低耦合准则 (Loose Coupling & ISP)
* **调用者按需索取**：如果业务只需要滚动或拖拽滑块，只注入 `IMouseDriver`，严禁强依赖包含截图的大对象。
* **各子驱动相互正交**：`mouse.driver.ts` 绝对不能 `import` 任何截图或视觉模块；`semantic.driver.ts` 绝对不能包含物理光标的移动逻辑。

---

## 3. OpenSky 差异化核心技术戒律

### 戒律一：免光标内存直调优先 (Zero-Cursor Priority)
* 行业内几乎所有 Computer Use 产品都在暴力抢占用户的物理鼠标，破坏用户日常办公。
* **OpenSky 标准**：
  1. 对任何标准控件交互（按钮、复选框、输入框、下拉菜单、列表选中），**第一优先级永远是走 `PatternService` 进行内存级直调（`InvokePattern` / `ValuePattern` / `TogglePattern` / `SelectionItemPattern`）**；
  2. 严禁用无障碍树仅作“坐标查找器”再用物理鼠标点击；
  3. 内存直接执行必须达到 **0ms 鼠标占用、0 像素偏差、前台焦点不被打断**。

### 戒律二：无截图纯动作优先 (Blind Action Priority)
* 行业竞品死循环在于：动一下鼠标就强制截一张几兆的高清图并回传给大模型，耗时长达数秒，Token 消耗巨大。
* **OpenSky 标准**：
  1. 细化独立动作通道：`win_mouse_scroll`、`win_mouse_drag`、`win_mouse_move`、`win_blind_click` 为独立原子工具；
  2. 执行这类物理动作时，单步耗时必须 `< 15ms`，**绝对严禁在动作中夹带截屏**；
  3. 仅在 Agent 明确需要验证结果或迷失定位时，显式调用 `win_screenshot`。

### 戒律三：Apple 级极简白光与 Spring 物理动效 (Impeccable Aesthetic)
* 杜绝任何粗糙的红框、蓝色高亮块、黄色方框等程序员风格廉价反馈。
* **OpenSky 视觉标准**：
  1. **二阶阻尼弹簧动力学（Spring Physics）**：所有波纹扩散、状态胶囊缩放必须通过 `SpringPhysics.Evaluate()` 求解，阻尼比 $\zeta \approx 0.76$，固有角频率 $\omega \approx 12.5$，具有自然的回弹与惯性，严禁线性位移；
  2. **单色磨砂白（Apple Frosted Monochrome）**：仅允许半透明纯白（`Color.FromArgb(alpha, 255, 255, 255)`）与极微弱的漫反射白光。背景使用透明度微调的深灰磨砂（Charcoal Frost: `20, 20, 20`）；
  3. **Agent 光标修饰符（Action Pill / Halo）**：当必须操作物理光标时，自动附着光标呼吸微光环与 Action Pill 状态胶囊（展示当前动作如 `Click`、`Scroll`、`Drag`），操作完毕平滑淡出，消除人类恐慌。

---

## 4. 代码审查 Checklist (提交前必检)

- [ ] 目录是否垂直划分？是否有新增文件被随意平铺在根目录或上级目录下？
- [ ] 每个新增文件的代码行数是否在 200 行以内？
- [ ] 每个新增函数的代码行数是否在 40 行以内？
- [ ] 鼠标操作是否彻底与视觉截屏解耦？
- [ ] 能用 UIA Pattern 解决的操作，是否避免了移动物理光标？
- [ ] 动画是否使用了 Spring 物理阻尼与极简白色美学？
- [ ] 运行 `pwsh scripts/build-helper.ps1` 编译零错误？
- [ ] 运行 `pnpm test` 与解耦测试 100% 通过？
