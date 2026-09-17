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
    version: "0.0.1",
    description: "OpenSky: Universal Computer Use Engine for Windows",
    author: {
      name: "OpenSky Contributors",
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
1. **Accessibility Track (UIA)**: Call \`win_get_ui_tree\` to get element indexes, then \`win_click(elementIndex)\` or \`win_set_value(elementIndex, text)\`. This is fast, accurate, and token-efficient.
2. **Visual Track (Screenshots)**: Call \`win_screenshot\` to get real-time screen/window images, then \`win_click(x, y)\` or \`win_drag\`.

## Available Tools:
- \`win_list_windows\`: Find open application windows.
- \`win_activate_window\`: Bring a window to the foreground.
- \`win_screenshot\`: Capture high-resolution screenshot (fullscreen or window-specific).
- \`win_get_ui_tree\`: Inspect UI elements with indexes and names.
- \`win_click\`: Click by index or coordinate.
- \`win_type_text\`: Type Unicode text.
- \`win_press_key\`: Keyboard shortcuts (e.g. "Ctrl+S", "Enter").
- \`win_scroll\`: Mouse wheel scroll.
- \`win_set_value\`: Directly set text value of input fields.
`;

  fs.writeFileSync(path.join(pluginDir, "plugin.json"), JSON.stringify(pluginJson, null, 2), "utf8");
  fs.writeFileSync(path.join(outputDir, ".mcp.json"), JSON.stringify(mcpJson, null, 2), "utf8");
  fs.writeFileSync(path.join(skillsDir, "SKILL.md"), skillMd, "utf8");
}
