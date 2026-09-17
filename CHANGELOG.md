# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-09-17

### Added
- **Zero-Cursor In-Memory Pattern Execution (`win_invoke_element`)**: Direct invocation of Windows UI Automation control patterns (`InvokePattern`, `TogglePattern`, `SelectionItemPattern`, `ValuePattern`) directly in application memory in `< 5ms` without moving the physical cursor or stealing window focus.
- **Blind Mouse Actions**: High-frequency atomic mouse gestures (`win_mouse_scroll`, `win_mouse_drag`, `win_blind_click`) operating with zero screenshot latency and no vision-token overhead.
- **2nd-Order Spring Physics Engine**: Integrated `SpringPhysics.cs` harmonic oscillator dynamics ($\zeta \approx 0.76$, $\omega \approx 12.5$) powering smooth visual transitions for all overlay reticles and ripples.
- **Apple-Grade Frosted Monochrome Overlay**: Redesigned visual feedback utilizing pure frosted white (`rgba(255, 255, 255, alpha)`) with an Agent Cursor Halo and dynamic action pill (`[Click]`, `[Scroll]`, `[Invoke]`).
- **Segregated Domain Contracts (`src/core/contracts/`)**: Introduced clean interfaces adhering to the Interface Segregation Principle (ISP):
  - `IMouseDriver`: Physical gestures and blind mouse actions.
  - `IKeyboardDriver`: Text injection and chord key combinations.
  - `ISemanticDriver`: UIA accessibility tree and pattern invocation.
  - `IVisionDriver`: Screen capture and visual encoding.
  - `IWindowManager`: Process lifecycle and window enumeration.
  - `IComputerUseEngine`: Unified orchestrator engine contract.
- **Domain Data Models (`src/core/models/`)**: Strongly-typed domain definitions for geometry, input events, semantic nodes, vision options, and window descriptors.
- **Development Standards & Guidelines (`docs/DEVELOPMENT.md`)**: Formalized architecture and coding standards specifying strict Single Responsibility Principle (SRP) limits (<200 lines per file, <40 lines per function, no flat directories).
- **Technical Research Report (`docs/RESEARCH.md`)**: Comprehensive investigation and architectural analysis of existing open-source and proprietary Computer Use implementations.

### Changed
- **Hierarchical Multi-Level Directory Restructuring**:
  - Modularized C# native implementation into dedicated namespaces: `OpenSky.Native.Common`, `OpenSky.Native.Input`, `OpenSky.Native.Semantic`, `OpenSky.Native.Vision`, `OpenSky.Native.Window`, `OpenSky.Native.Overlay`, and `OpenSky.Native.Host`.
  - Refactored TypeScript driver layer into dedicated drivers under `src/driver/windows/`.
- **Recursive Native Build Script**: Updated `scripts/build-helper.ps1` to automatically discover and compile all C# files recursively.
- **Documentation Migration**: Consolidated auxiliary technical specifications and release notes under `docs/`.

### Removed
- Deprecated legacy flat C# files in `src/native/` in favor of modular domain subdirectories.

---

## [0.0.1] - 2026-09-17

### Added
- Initial open-source release of OpenSky Universal Computer Use Engine.
- Model Context Protocol (MCP) server integration (`src/adapters/mcp/`).
- Codex desktop plugin export generator (`src/adapters/codex/`).
- Native Windows C# helper binary bridging UI Automation, Win32 input, and GDI+ capture.
- CLI executable (`opensky`) with full standard I/O and pipe communication.
