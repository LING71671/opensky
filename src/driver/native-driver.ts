import type { IComputerUseEngine } from "../core/engine.js";
import type {
  WindowInfo,
  AppInfo,
  AccessibilityTreeResult,
  ScreenshotOptions,
  ScreenshotResult,
  ClickOptions,
  MoveOptions,
  DragOptions,
  ScrollOptions,
  CursorPosition,
} from "../core/types.js";
import type { ITransport } from "./transport/transport.interface.js";

export class NativeDriver implements IComputerUseEngine {
  private readonly transport: ITransport;

  constructor(transport: ITransport) {
    this.transport = transport;
  }

  public async listWindows(): Promise<WindowInfo[]> {
    const res = await this.transport.send("list_windows", {});
    return res as WindowInfo[];
  }

  public async listApps(): Promise<AppInfo[]> {
    const res = await this.transport.send("list_apps", {});
    return res as AppInfo[];
  }

  public async activateWindow(hwnd: number): Promise<boolean> {
    const res = await this.transport.send("activate_window", { hwnd });
    return Boolean(res);
  }

  public async launchApp(app: string): Promise<boolean> {
    const res = await this.transport.send("launch_app", { app });
    return Boolean(res);
  }

  public async screenshot(options: ScreenshotOptions = {}): Promise<ScreenshotResult> {
    const res = await this.transport.send("screenshot", {
      hwnd: options.hwnd ?? 0,
      format: options.format ?? "jpeg",
      quality: options.quality ?? 80,
      out: options.outFile ?? "",
    });
    return res as ScreenshotResult;
  }

  public async getAccessibilityTree(hwnd: number = 0, maxDepth: number = 6): Promise<AccessibilityTreeResult> {
    const res = await this.transport.send("get_ui_tree", {
      hwnd,
      max_depth: maxDepth,
    });
    return res as AccessibilityTreeResult;
  }

  public async click(options: ClickOptions): Promise<boolean> {
    const res = await this.transport.send("click", {
      hwnd: options.hwnd ?? 0,
      element_index: options.elementIndex ?? -1,
      x: options.x,
      y: options.y,
      mouse_button: options.button ?? "left",
      click_count: options.clickCount ?? 1,
    });
    return Boolean(res);
  }

  public async move(options: MoveOptions): Promise<boolean> {
    const res = await this.transport.send("move", {
      hwnd: options.hwnd ?? 0,
      x: options.x,
      y: options.y,
    });
    return Boolean(res);
  }

  public async drag(options: DragOptions): Promise<boolean> {
    const res = await this.transport.send("drag", {
      hwnd: options.hwnd ?? 0,
      from_x: options.fromX,
      from_y: options.fromY,
      to_x: options.toX,
      to_y: options.toY,
    });
    return Boolean(res);
  }

  public async scroll(options: ScrollOptions): Promise<boolean> {
    const res = await this.transport.send("scroll", {
      hwnd: options.hwnd ?? 0,
      x: options.x,
      y: options.y,
      delta_x: options.deltaX ?? 0,
      delta_y: options.deltaY ?? 0,
    });
    return Boolean(res);
  }

  public async typeText(text: string): Promise<boolean> {
    const res = await this.transport.send("type_text", { text });
    return Boolean(res);
  }

  public async pressKey(chord: string): Promise<boolean> {
    const res = await this.transport.send("press_key", { chord });
    return Boolean(res);
  }

  public async setValue(elementIndex: number, value: string): Promise<boolean> {
    const res = await this.transport.send("set_value", {
      element_index: elementIndex,
      value,
    });
    return Boolean(res);
  }

  public async getCursorPosition(): Promise<CursorPosition> {
    const res = await this.transport.send("get_cursor_position", {});
    return res as CursorPosition;
  }

  public async close(): Promise<void> {
    await this.transport.close();
  }
}
