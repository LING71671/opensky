import type {
  ScreenshotOptions,
  ScreenshotResult,
} from "../models/vision.model.js";

/**
 * Single Responsibility & Interface Segregation:
 * Screen capture and pixel visual perception.
 * Strictly invoked on-demand; never coupled to input loops.
 */
export interface IVisionDriver {
  /** Capture screenshot of target window or entire desktop */
  screenshot(options?: ScreenshotOptions): Promise<ScreenshotResult>;
}
