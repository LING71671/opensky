/**
 * Single Responsibility: Geometric coordinates and bounding rectangles.
 */
export interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface Point {
  x: number;
  y: number;
}

export interface CursorPosition {
  x: number;
  y: number;
}
