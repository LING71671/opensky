# Release Notes - v0.1.0

## OpenSky v0.1.0 (Major Architectural Refinement & Differentiation)

A major milestone release introducing decoupled multi-level architecture, zero-cursor in-memory pattern execution, isolated blind mouse gestures, and Apple-grade monochrome frosted white Spring physics animations.

### 🌟 Key Highlights

#### 1. Zero-Cursor In-Memory Pattern Execution (`win_invoke_element`)
- **No Mouse Theft**: Standard UI controls (Buttons, Checkboxes, Switches, Tabs, Value inputs) can now be directly invoked via UI Automation Control Patterns (`InvokePattern`, `TogglePattern`, `SelectionItemPattern`, `ValuePattern`).
- **Zero-Pixel Deviation**: Triggers actions directly inside the application process memory in `< 5ms`.
- **Co-presence**: Physical mouse cursor is not moved, and the user's foreground focus is never interrupted.
- **Visual Pulse**: Emits a subtle, non-intrusive white ambient pulse around the target element.

#### 2. Segregated Domain Architecture & Loose Coupling
- **Interface Segregation (ISP)**: Deconstructed the monolithic engine into clean, dedicated contracts:
  - `IMouseDriver`: Pure physical and blind mouse gestures (scroll, drag, move, blind click).
  - `IKeyboardDriver`: Pure Unicode text and chord keyboard injection.
  - `ISemanticDriver`: UIA tree inspection and pattern execution.
  - `IVisionDriver`: Pure visual capture.
  - `IWindowManager`: Application lifecycle and window enumeration.
- **Hierarchical Directory Layout**: Eliminated flat directory clutter. Organized into `src/core/contracts/`, `src/core/models/`, `src/driver/windows/`, and functional C# subdirectories (`Common`, `Input`, `Semantic`, `Vision`, `Window`, `Overlay`, `Host`).

#### 3. Blind Mouse Actions (No Screenshot Overhead)
- Mouse gestures (`win_mouse_scroll`, `win_mouse_drag`, `win_blind_click`) execute independently as high-frequency atomic commands.
- **Zero Vision Latency**: Bypasses the 2-5s screenshot capture and multi-thousand-token vision encoding tax.

#### 4. Apple Frosted Monochrome & 2nd-Order Spring Physics
- **Spring Damping Physics**: All visual overlays, ripples, and reticles compute displacement using 2nd-order harmonic oscillator dynamics (`SpringPhysics.Evaluate`, damping ratio $\zeta \approx 0.76$, natural frequency $\omega \approx 12.5$).
- **Agent Cursor Halo & Action Pill**: Renders a delicate frosted-white breathing ring around the mouse pointer and hangs an actionable micro-pill (`[Click]`, `[Scroll]`, `[Invoke]`) below the cursor, eliminating user panic.
- **Monochrome Aesthetic**: Exclusively uses clean frosted white (`rgba(255, 255, 255, alpha)`) and subtle charcoal backdrops, completely eliminating garish, saturated borders.

#### 5. Documentation & Engineering Standard
- Added comprehensive **`docs/DEVELOPMENT.md`** defining strict single-responsibility principles, file/function size limits, and architectural rules.
- Published **`docs/RESEARCH.md`** with in-depth reverse engineering and technical dissection of existing open-source and closed-source Computer Use implementations.
