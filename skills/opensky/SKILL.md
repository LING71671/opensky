---
name: opensky
description: Windows desktop and application automation via UI Automation, Screenshots, and Input simulation
---

# OpenSky: Windows Computer Use Skill

OpenSky provides Windows desktop and application automation capabilities with visual overlay feedback.

## Interaction Modes

### 1. UI Automation (Recommended)
- Call `win_get_ui_tree` to inspect interactive controls and their numeric indices (e.g. `[12] Button "Save"`).
- Call `win_click(elementIndex: 12)` to click directly on the element.
- Call `win_set_value(elementIndex: 12, value: "Text")` to edit fields directly via ValuePattern.
- Independent of display resolution and DPI scaling, without requiring coordinate calculation.

### 2. Screenshot & Coordinates
- Call `win_screenshot` to capture the desktop or a specific window (`hwnd`).
- Call `win_click(x: ..., y: ...)`, `win_drag`, or `win_scroll` using pixel coordinates.
- Suitable for custom canvas controls, game interfaces, or webviews.

## Window Management
- Call `win_list_windows` to list application windows.
- Call `win_activate_window(hwnd: ...)` to bring a window to the foreground.

## Visual Feedback
- Clicks trigger a ripple ring animation on the screen.
- Target windows display an outline highlight when activated.
