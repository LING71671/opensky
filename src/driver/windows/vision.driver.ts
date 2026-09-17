import type { IVisionDriver } from "../../core/contracts/vision.contract.js";
import type {
  ScreenshotOptions,
  ScreenshotResult,
} from "../../core/models/vision.model.js";
import type { ITransport } from "../transport/transport.interface.js";

/**
 * Single Responsibility: Windows visual capture and screenshot driver.
 * Purely on-demand: never coupled to mouse or keyboard input loops.
 */
export class WindowsVisionDriver implements IVisionDriver {
  constructor(private readonly transport: ITransport) {}

  public async screenshot(options: ScreenshotOptions = {}): Promise<ScreenshotResult> {
    const res = await this.transport.send("screenshot", {
      hwnd: options.hwnd ?? 0,
      format: options.format ?? "jpeg",
      quality: options.quality ?? 80,
      out: options.outFile ?? "",
    });
    return res as ScreenshotResult;
  }
}
