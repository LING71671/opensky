import type { WindowInfo, AppInfo } from "../models/window.model.js";

/**
 * Single Responsibility & Interface Segregation:
 * Window enumeration, activation, and application lifecycle.
 */
export interface IWindowManager {
  /** List visible interactive application windows */
  listWindows(): Promise<WindowInfo[]>;

  /** List applications and grouped window instances */
  listApps(): Promise<AppInfo[]>;

  /** Bring window to foreground and restore if minimized */
  activateWindow(hwnd: number): Promise<boolean>;

  /** Launch application by name or executable path */
  launchApp(app: string): Promise<boolean>;
}
