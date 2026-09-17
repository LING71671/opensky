import * as fs from "node:fs";
import * as path from "node:path";

export interface CodexPluginExportOptions {
  outputDir: string;
  entryScriptPath: string;
}

export function exportCodexPlugin(options: CodexPluginExportOptions) {
  const { outputDir, entryScriptPath } = options;
  const pluginDir = path.join(outputDir, ".codex-plugin");
  const skillsDir = path.join(outputDir, "skills", "opensky");

  fs.mkdirSync(pluginDir, { recursive: true });
  fs.mkdirSync(skillsDir, { recursive: true });

  const pluginJson = {
    name: "opensky",
    version: "0.1.0",
    description: "OpenSky: Universal Computer Use Engine for Windows",
    author: {
      name: "LING71671",
    },
    skills: "./skills/",
    interface: {
      displayName: "OpenSky Computer Use",
      shortDescription: "Native Computer Use & UI Automation for AI Agents",
      category: "Productivity",
      brandColor: "#0EA5E9",
    },
  };

  const mcpJson = {
    mcpServers: {
      opensky: {
        command: "node",
        args: [entryScriptPath],
        enabled: true,
      },
    },
  };

  const skillMd = `---
name: opensky
description: Control Windows apps and desktop using UI Automation and Screenshots
---

# Windows Computer Use

Use this skill to automate Windows desktop apps. It supports dual-track interaction:
1. **Accessibility Track (UIA & Pattern)**: Call \`win_get_ui_tree\` to inspect elements, then \`win_invoke_element(elementIndex)\` for 0ms zero-cursor execution without moving the physical mouse, or \`win_click(elementIndex)\`.
2. **Visual Track (Screenshots)**: Call \`win_screenshot\` to get real-time screen/window images, then \`win_click(x, y)\` or \`win_drag\`.

## Available Tools:
- \`win_list_windows\`: Find open application windows.
- \`win_activate_window\`: Bring a window to the foreground.
- \`win_screenshot\`: Capture high-resolution screenshot (fullscreen or window-specific).
- \`win_get_ui_tree\`: Inspect UI elements with indexes and supported pattern actions.
- \`win_invoke_element\`: Zero-cursor in-memory pattern execution (Invoke, Toggle, Select, Value).
- \`win_click\`: Click by index or coordinate with spring-damped tactile feedback.
- \`win_type_text\`: Type Unicode text.
- \`win_press_key\`: Keyboard shortcuts (e.g. "Ctrl+S", "Enter").
- \`win_scroll\`: Mouse wheel scroll.
- \`win_set_value\`: Directly set text value of input fields.
`;

  fs.writeFileSync(path.join(pluginDir, "plugin.json"), JSON.stringify(pluginJson, null, 2), "utf8");
  fs.writeFileSync(path.join(outputDir, ".mcp.json"), JSON.stringify(mcpJson, null, 2), "utf8");
  fs.writeFileSync(path.join(skillsDir, "SKILL.md"), skillMd, "utf8");
}
