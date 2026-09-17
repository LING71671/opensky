import type { IComputerUseEngine } from "../../core/contracts/engine.contract.js";
import type { IMouseDriver } from "../../core/contracts/mouse.contract.js";
import type { IKeyboardDriver } from "../../core/contracts/keyboard.contract.js";
import type { ISemanticDriver } from "../../core/contracts/semantic.contract.js";
import type { IVisionDriver } from "../../core/contracts/vision.contract.js";
import type { IWindowManager } from "../../core/contracts/window.contract.js";

import type {
  ClickOptions,
  MoveOptions,
  DragOptions,
  ScrollOptions,
} from "../../core/models/input.model.js";
import type { CursorPosition } from "../../core/models/geometry.model.js";
import type { WindowInfo, AppInfo } from "../../core/models/window.model.js";
import type {
  AccessibilityTreeResult,
  PatternInvokeOptions,
  PatternInvokeResult,
} from "../../core/models/semantic.model.js";
import type {
  ScreenshotOptions,
  ScreenshotResult,
} from "../../core/models/vision.model.js";

import type { ITransport } from "../transport/transport.interface.js";
import { WindowsMouseDriver } from "./mouse.driver.js";
import { WindowsKeyboardDriver } from "./keyboard.driver.js";
import { WindowsSemanticDriver } from "./semantic.driver.js";
import { WindowsVisionDriver } from "./vision.driver.js";
import { WindowsWindowManager } from "./window.driver.js";

/**
 * Composite Facade: WindowsEngine
 * Implements IComputerUseEngine by delegating to dedicated, single-responsibility sub-drivers.
 * Provides direct access to individual sub-drivers for fine-grained dependency injection.
 */
export class WindowsEngine implements IComputerUseEngine {
  public readonly mouse: IMouseDriver;
  public readonly keyboard: IKeyboardDriver;
  public readonly semantic: ISemanticDriver;
  public readonly vision: IVisionDriver;
  public readonly window: IWindowManager;

  constructor(private readonly transport: ITransport) {
    this.mouse = new WindowsMouseDriver(transport);
    this.keyboard = new WindowsKeyboardDriver(transport);
    this.semantic = new WindowsSemanticDriver(transport);
    this.vision = new WindowsVisionDriver(transport);
    this.window = new WindowsWindowManager(transport);
  }

  // --- IMouseDriver delegation ---
  public click(options: ClickOptions): Promise<boolean> {
    return this.mouse.click(options);
  }
  public move(options: MoveOptions): Promise<boolean> {
    return this.mouse.move(options);
  }
  public drag(options: DragOptions): Promise<boolean> {
    return this.mouse.drag(options);
  }
  public scroll(options: ScrollOptions): Promise<boolean> {
    return this.mouse.scroll(options);
  }
  public getCursorPosition(): Promise<CursorPosition> {
    return this.mouse.getCursorPosition();
  }

  // --- IKeyboardDriver delegation ---
  public typeText(text: string): Promise<boolean> {
    return this.keyboard.typeText(text);
  }
  public pressKey(chord: string): Promise<boolean> {
    return this.keyboard.pressKey(chord);
  }

  // --- ISemanticDriver delegation ---
  public getAccessibilityTree(hwnd?: number, maxDepth?: number): Promise<AccessibilityTreeResult> {
    return this.semantic.getAccessibilityTree(hwnd, maxDepth);
  }
  public invokePattern(options: PatternInvokeOptions): Promise<PatternInvokeResult> {
    return this.semantic.invokePattern(options);
  }
  public setValue(elementIndex: number, value: string): Promise<boolean> {
    return this.semantic.setValue(elementIndex, value);
  }

  // --- IVisionDriver delegation ---
  public screenshot(options?: ScreenshotOptions): Promise<ScreenshotResult> {
    return this.vision.screenshot(options);
  }

  // --- IWindowManager delegation ---
  public listWindows(): Promise<WindowInfo[]> {
    return this.window.listWindows();
  }
  public listApps(): Promise<AppInfo[]> {
    return this.window.listApps();
  }
  public activateWindow(hwnd: number): Promise<boolean> {
    return this.window.activateWindow(hwnd);
  }
  public launchApp(app: string): Promise<boolean> {
    return this.window.launchApp(app);
  }

  // --- Lifecycle ---
  public async close(): Promise<void> {
    await this.transport.close();
  }
}
