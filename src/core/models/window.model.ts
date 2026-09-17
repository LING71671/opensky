import type { Rect } from "./geometry.model.js";

/**
 * Single Responsibility: Window and process descriptors.
 */
export interface WindowInfo {
  id: number;
  title: string;
  processName: string;
  processId: number;
  bounds: Rect;
  isMinimized: boolean;
}

export interface AppInfo {
  id: string;
  displayName: string;
  isRunning: boolean;
  windows: WindowInfo[];
}
