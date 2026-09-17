---
name: opensky
description: Control Windows apps and desktop using UI Automation and Screenshots
---

# Windows Computer Use

Use this skill to automate Windows desktop apps. It supports dual-track interaction:
1. **Accessibility Track (UIA & Pattern)**: Call `win_get_ui_tree` to inspect elements, then `win_invoke_element(elementIndex)` for 0ms zero-cursor execution without moving the physical mouse, or `win_click(elementIndex)`.
2. **Visual Track (Screenshots)**: Call `win_screenshot` to get real-time screen/window images, then `win_click(x, y)` or `win_drag`.

## Available Tools:
- `win_list_windows`: Find open application windows.
- `win_activate_window`: Bring a window to the foreground.
- `win_screenshot`: Capture high-resolution screenshot (fullscreen or window-specific).
- `win_get_ui_tree`: Inspect UI elements with indexes and supported pattern actions.
- `win_invoke_element`: Zero-cursor in-memory pattern execution (Invoke, Toggle, Select, Value).
- `win_click`: Click by index or coordinate with spring-damped tactile feedback.
- `win_type_text`: Type Unicode text.
- `win_press_key`: Keyboard shortcuts (e.g. "Ctrl+S", "Enter").
- `win_scroll`: Mouse wheel scroll.
- `win_set_value`: Directly set text value of input fields.
