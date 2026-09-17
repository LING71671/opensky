/**
 * Core type definitions for Windows Computer Use.
 * Completely decoupled from any specific protocol (MCP, Anthropic, Codex, etc.).
 */

export interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
}

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

export interface UIElement {
  index: number;
  type: string;
  name: string;
  x: number;
  y: number;
  bounds: Rect;
  value?: string;
  isFocused?: boolean;
}

export interface AccessibilityTreeResult {
  tree: string;
  focusedElement?: string;
  elements: UIElement[];
}

export interface ScreenshotOptions {
  hwnd?: number;
  format?: "jpeg" | "png";
  quality?: number;
  outFile?: string;
}

export interface ScreenshotResult {
  width: number;
  height: number;
  originX: number;
  originY: number;
  dataUrl: string;
  filepath: string;
}

export interface ClickOptions {
  hwnd?: number;
  elementIndex?: number;
  x?: number;
  y?: number;
  button?: "left" | "right" | "middle";
  clickCount?: number;
}

export interface MoveOptions {
  hwnd?: number;
  x: number;
  y: number;
}

export interface DragOptions {
  hwnd?: number;
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
}

export interface ScrollOptions {
  hwnd?: number;
  x?: number;
  y?: number;
  deltaX?: number;
  deltaY?: number;
}

export interface CursorPosition {
  x: number;
  y: number;
}
