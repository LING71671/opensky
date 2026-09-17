import type { IMouseDriver } from "./mouse.contract.js";
import type { IKeyboardDriver } from "./keyboard.contract.js";
import type { ISemanticDriver } from "./semantic.contract.js";
import type { IVisionDriver } from "./vision.contract.js";
import type { IWindowManager } from "./window.contract.js";

/**
 * Composite Facade: Combines the segregated domain drivers into a unified engine.
 * Callers with specific needs should depend only on the individual sub-drivers.
 */
export interface IComputerUseEngine
  extends IMouseDriver,
    IKeyboardDriver,
    ISemanticDriver,
    IVisionDriver,
    IWindowManager {
  /** Clean up resources and close native helpers */
  close(): Promise<void>;
}
