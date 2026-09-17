import type {
  AccessibilityTreeResult,
  PatternInvokeOptions,
  PatternInvokeResult,
} from "../models/semantic.model.js";

/**
 * Single Responsibility & Interface Segregation:
 * UI Automation semantic inspection and in-memory pattern invocation.
 * Zero cursor interference: does not steal or move the physical mouse.
 */
export interface ISemanticDriver {
  /** Inspect structured accessibility tree of a window or desktop */
  getAccessibilityTree(hwnd?: number, maxDepth?: number): Promise<AccessibilityTreeResult>;

  /** Directly invoke in-memory control pattern (Invoke, Toggle, Select, Value) */
  invokePattern(options: PatternInvokeOptions): Promise<PatternInvokeResult>;

  /** Replace value directly via ValuePattern */
  setValue(elementIndex: number, value: string): Promise<boolean>;
}
