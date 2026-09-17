/**
 * Single Responsibility: Physical and blind input parameters (mouse and keyboard).
 */
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
  durationMs?: number;
}

export interface ScrollOptions {
  hwnd?: number;
  x?: number;
  y?: number;
  deltaX?: number;
  deltaY?: number;
}
