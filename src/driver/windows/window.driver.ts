import type { IWindowManager } from "../../core/contracts/window.contract.js";
import type { WindowInfo, AppInfo } from "../../core/models/window.model.js";
import type { ITransport } from "../transport/transport.interface.js";

/**
 * Single Responsibility: Windows application and window lifecycle management.
 */
export class WindowsWindowManager implements IWindowManager {
  constructor(private readonly transport: ITransport) {}

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
}
