# Release Notes - v0.0.1

## OpenSky v0.0.1 (Initial Release)

Initial public release of OpenSky, a universal Computer Use engine and MCP server for operating system desktop and application automation.

### Features
- **Decoupled Architecture**: Abstract `IComputerUseEngine` interface with pluggable driver layer, designed for cross-platform extensibility.
- **Native Windows Driver**:
  - Single-responsibility C# micro-services compiled via system built-in `.NET 4.8 csc.exe`.
  - Desktop isolation bypass (`WinSta0\default`) for sandbox thread compatibility.
  - UI Automation tree inspection with sequential element indexing (`win_get_ui_tree`).
  - High-resolution screen and window capture (`win_screenshot`).
  - Input simulation via `SendInput` supporting Unicode text typing and key combinations (`win_click`, `win_type_text`, `win_press_key`).
- **Visual Feedback Overlay**:
  - Win32 click-through transparent layered overlay (`WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_NOACTIVATE`).
  - Click ripples and active window outline with automatic fadeout.
  - Zero input interception or focus theft (`ShowWithoutActivation`).
- **Universal AI Harness Support**:
  - Standard Model Context Protocol (MCP) server over STDIO.
  - Anthropic Computer Use API bridge (`computer_20241022`).
  - OpenAI Codex plugin export tool (`opensky --export-codex <dir>`).
  - Tested with OpenCode, Reasonix, Claude Desktop, Cursor, Antigravity, and Codex.
- **Brand Identity**:
  - Official light-mode logo and social media share banner in `assets/`.
