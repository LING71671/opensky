/**
 * Single Responsibility: Screen perception and window screenshot parameters.
 */
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
