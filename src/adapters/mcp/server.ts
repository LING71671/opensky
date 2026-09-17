import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  type Tool,
} from "@modelcontextprotocol/sdk/types.js";
import type { IComputerUseEngine } from "../../core/engine.js";

export interface McpServerAdapterOptions {
  engine: IComputerUseEngine;
  name?: string;
  version?: string;
}

export function createMcpServer(options: McpServerAdapterOptions) {
  const { engine, name = "win-computer-use", version = "1.0.0" } = options;

  const server = new Server(
    { name, version },
    {
      capabilities: {
        tools: {},
      },
    }
  );

  const tools: Tool[] = [
    {
      name: "win_list_windows",
      description: "List all visible interactive desktop application windows with their titles, process names, IDs, and screen coordinates.",
      inputSchema: {
        type: "object",
        properties: {},
      },
    },
    {
      name: "win_list_apps",
      description: "List running applications and group their open windows.",
      inputSchema: {
        type: "object",
        properties: {},
      },
    },
    {
      name: "win_activate_window",
      description: "Bring a specific window to the foreground and focus it.",
      inputSchema: {
        type: "object",
        properties: {
          hwnd: { type: "number", description: "Window handle ID (from win_list_windows)" },
        },
        required: ["hwnd"],
      },
    },
    {
      name: "win_launch_app",
      description: "Launch an application by its registered name or executable path.",
      inputSchema: {
        type: "object",
        properties: {
          app: { type: "string", description: "Application name or full executable path" },
        },
        required: ["app"],
      },
    },
    {
      name: "win_screenshot",
      description: "Capture a screenshot of either the full desktop or a specific target window. Returns image base64 dataUrl and dimensions.",
      inputSchema: {
        type: "object",
        properties: {
          hwnd: { type: "number", description: "Optional window handle ID to capture only that window. If omitted, captures full desktop." },
          format: { type: "string", enum: ["jpeg", "png"], default: "jpeg" },
          quality: { type: "number", minimum: 1, maximum: 100, default: 80 },
          outFile: { type: "string", description: "Optional local file path to save image to" },
        },
      },
    },
    {
      name: "win_get_ui_tree",
      description: "Extract the structured accessibility tree (UI Automation) of a target window or desktop. Gives element indexes, bounding boxes, and current values.",
      inputSchema: {
        type: "object",
        properties: {
          hwnd: { type: "number", description: "Optional window handle ID. If omitted, inspects root desktop." },
          maxDepth: { type: "number", default: 6, description: "Maximum hierarchy depth to traverse" },
        },
      },
    },
    {
      name: "win_invoke_element",
      description: "Directly trigger a UI Automation Control Pattern on an element by index (Invoke, Toggle, Select, Value, Expand/Collapse). Zero cursor movement: physical mouse pointer is NOT moved and foreground focus is preserved.",
      inputSchema: {
        type: "object",
        properties: {
          elementIndex: { type: "number", description: "Index of UI element from win_get_ui_tree" },
          action: {
            type: "string",
            enum: ["auto", "invoke", "toggle", "select", "set_value", "expand", "collapse"],
            default: "auto",
            description: "Action type. Defaults to auto (infers best supported pattern: Invoke -> Toggle -> Select)",
          },
          value: { type: "string", description: "Value to set if action is set_value or for input controls" },
        },
        required: ["elementIndex"],
      },
    },
    {
      name: "win_click",
      description: "Click a target element by index or at specific coordinates. Supports single, double, right-click, etc.",
      inputSchema: {
        type: "object",
        properties: {
          elementIndex: { type: "number", description: "Index of UI element from win_get_ui_tree. Recommended over raw coordinates." },
          hwnd: { type: "number", description: "Optional window handle ID if providing window-relative x, y" },
          x: { type: "number", description: "Screen or window-relative X coordinate" },
          y: { type: "number", description: "Screen or window-relative Y coordinate" },
          button: { type: "string", enum: ["left", "right", "middle"], default: "left" },
          clickCount: { type: "number", default: 1 },
        },
      },
    },
    {
      name: "win_move",
      description: "Move mouse cursor to coordinates.",
      inputSchema: {
        type: "object",
        properties: {
          hwnd: { type: "number", description: "Optional window handle ID if coordinates are window-relative" },
          x: { type: "number", description: "X coordinate" },
          y: { type: "number", description: "Y coordinate" },
        },
        required: ["x", "y"],
      },
    },
    {
      name: "win_drag",
      description: "Drag mouse from starting coordinates to ending coordinates.",
      inputSchema: {
        type: "object",
        properties: {
          hwnd: { type: "number" },
          fromX: { type: "number" },
          fromY: { type: "number" },
          toX: { type: "number" },
          toY: { type: "number" },
        },
        required: ["fromX", "fromY", "toX", "toY"],
      },
    },
    {
      name: "win_scroll",
      description: "Scroll mouse wheel vertically or horizontally.",
      inputSchema: {
        type: "object",
        properties: {
          hwnd: { type: "number" },
          x: { type: "number" },
          y: { type: "number" },
          deltaX: { type: "number", default: 0, description: "Horizontal scroll delta" },
          deltaY: { type: "number", default: 0, description: "Vertical scroll delta (positive = scroll up, negative = scroll down)" },
        },
      },
    },
    {
      name: "win_type_text",
      description: "Type Unicode text into the current focus.",
      inputSchema: {
        type: "object",
        properties: {
          text: { type: "string", description: "Text to type" },
        },
        required: ["text"],
      },
    },
    {
      name: "win_press_key",
      description: "Press a keyboard shortcut or key chord (e.g. 'Ctrl+S', 'Enter', 'Alt+Tab', 'Escape', 'Ctrl+Shift+P').",
      inputSchema: {
        type: "object",
        properties: {
          chord: { type: "string", description: "Key or chord string" },
        },
        required: ["chord"],
      },
    },
    {
      name: "win_set_value",
      description: "Directly replace the text value of an editable element by its element index from win_get_ui_tree.",
      inputSchema: {
        type: "object",
        properties: {
          elementIndex: { type: "number", description: "Element index from win_get_ui_tree" },
          value: { type: "string", description: "New value to set" },
        },
        required: ["elementIndex", "value"],
      },
    },
    {
      name: "win_get_cursor_position",
      description: "Get the current physical mouse cursor position on screen.",
      inputSchema: {
        type: "object",
        properties: {},
      },
    },
  ];

  server.setRequestHandler(ListToolsRequestSchema, async () => {
    return { tools };
  });

  server.setRequestHandler(CallToolRequestSchema, async (request) => {
    const { name, arguments: args = {} } = request.params;

    try {
      switch (name) {
        case "win_list_windows": {
          const windows = await engine.listWindows();
          return { content: [{ type: "text", text: JSON.stringify(windows, null, 2) }] };
        }
        case "win_list_apps": {
          const apps = await engine.listApps();
          return { content: [{ type: "text", text: JSON.stringify(apps, null, 2) }] };
        }
        case "win_activate_window": {
          const ok = await engine.activateWindow(Number(args.hwnd));
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_launch_app": {
          const ok = await engine.launchApp(String(args.app));
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_screenshot": {
          const res = await engine.screenshot({
            hwnd: args.hwnd ? Number(args.hwnd) : undefined,
            format: (args.format as "jpeg" | "png") || "jpeg",
            quality: args.quality ? Number(args.quality) : 80,
            outFile: args.outFile ? String(args.outFile) : undefined,
          });
          return {
            content: [
              {
                type: "image",
                data: res.dataUrl.replace(/^data:image\/[a-z]+;base64,/, ""),
                mimeType: args.format === "png" ? "image/png" : "image/jpeg",
              },
              {
                type: "text",
                text: JSON.stringify({
                  width: res.width,
                  height: res.height,
                  originX: res.originX,
                  originY: res.originY,
                  filepath: res.filepath,
                }),
              },
            ],
          };
        }
        case "win_get_ui_tree": {
          const res = await engine.getAccessibilityTree(
            args.hwnd ? Number(args.hwnd) : 0,
            args.maxDepth ? Number(args.maxDepth) : 6
          );
          return {
            content: [
              {
                type: "text",
                text: res.tree || "No UI elements found.",
              },
              {
                type: "text",
                text: `Focused: ${res.focusedElement || "None"}, Total Elements: ${res.elements.length}`,
              },
            ],
          };
        }
        case "win_invoke_element": {
          const res = await engine.invokePattern({
            elementIndex: Number(args.elementIndex),
            action: (args.action as any) || "auto",
            value: args.value !== undefined ? String(args.value) : undefined,
          });
          return { content: [{ type: "text", text: JSON.stringify(res) }] };
        }
        case "win_click": {
          const ok = await engine.click({
            hwnd: args.hwnd ? Number(args.hwnd) : undefined,
            elementIndex: args.elementIndex !== undefined ? Number(args.elementIndex) : undefined,
            x: args.x !== undefined ? Number(args.x) : undefined,
            y: args.y !== undefined ? Number(args.y) : undefined,
            button: (args.button as "left" | "right" | "middle") || "left",
            clickCount: args.clickCount ? Number(args.clickCount) : 1,
          });
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_move": {
          const ok = await engine.move({
            hwnd: args.hwnd ? Number(args.hwnd) : undefined,
            x: Number(args.x),
            y: Number(args.y),
          });
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_drag": {
          const ok = await engine.drag({
            hwnd: args.hwnd ? Number(args.hwnd) : undefined,
            fromX: Number(args.fromX),
            fromY: Number(args.fromY),
            toX: Number(args.toX),
            toY: Number(args.toY),
          });
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_scroll": {
          const ok = await engine.scroll({
            hwnd: args.hwnd ? Number(args.hwnd) : undefined,
            x: args.x !== undefined ? Number(args.x) : undefined,
            y: args.y !== undefined ? Number(args.y) : undefined,
            deltaX: args.deltaX !== undefined ? Number(args.deltaX) : 0,
            deltaY: args.deltaY !== undefined ? Number(args.deltaY) : 0,
          });
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_type_text": {
          const ok = await engine.typeText(String(args.text));
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_press_key": {
          const ok = await engine.pressKey(String(args.chord));
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_set_value": {
          const ok = await engine.setValue(Number(args.elementIndex), String(args.value));
          return { content: [{ type: "text", text: JSON.stringify({ success: ok }) }] };
        }
        case "win_get_cursor_position": {
          const pos = await engine.getCursorPosition();
          return { content: [{ type: "text", text: JSON.stringify(pos) }] };
        }
        default:
          throw new Error(`Unknown tool: ${name}`);
      }
    } catch (error) {
      return {
        isError: true,
        content: [{ type: "text", text: error instanceof Error ? error.message : String(error) }],
      };
    }
  });

  return {
    server,
    async startStdio() {
      const transport = new StdioServerTransport();
      await server.connect(transport);
    },
  };
}
