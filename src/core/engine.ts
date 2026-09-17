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
} from "./types.js";

/**
 * Standard interface for Windows Computer Use operations.
 * Any underlying driver (Native C#, Rust, PowerShell, or Mock) can implement this.
 */
export interface IComputerUseEngine {
  /** List all visible desktop windows across sessions/desktops */
  listWindows(): Promise<WindowInfo[]>;

  /** List applications and their child windows */
  listApps(): Promise<AppInfo[]>;

  /** Bring window to foreground and restore if minimized */
  activateWindow(hwnd: number): Promise<boolean>;

  /** Launch an application by executable path or registered app name */
  launchApp(app: string): Promise<boolean>;

  /** Capture screenshot of full desktop or a specific target window */
  screenshot(options?: ScreenshotOptions): Promise<ScreenshotResult>;

  /** Read structured accessibility tree from UI Automation */
  getAccessibilityTree(hwnd?: number, maxDepth?: number): Promise<AccessibilityTreeResult>;

  /** Click an element by index or click coordinates */
  click(options: ClickOptions): Promise<boolean>;

  /** Move mouse cursor */
  move(options: MoveOptions): Promise<boolean>;

  /** Drag mouse from one point to another */
  drag(options: DragOptions): Promise<boolean>;

  /** Scroll wheel horizontally or vertically */
  scroll(options: ScrollOptions): Promise<boolean>;

  /** Type literal Unicode text into current focus */
  typeText(text: string): Promise<boolean>;

  /** Press a key or keyboard chord (e.g. "Ctrl+Shift+P", "Alt+Tab", "Return") */
  pressKey(chord: string): Promise<boolean>;

  /** Replace value of an editable element directly */
  setValue(elementIndex: number, value: string): Promise<boolean>;

  /** Get current mouse cursor position */
  getCursorPosition(): Promise<CursorPosition>;

  /** Close engine and clean up resources */
  close(): Promise<void>;
}
