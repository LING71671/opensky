import type { ISemanticDriver } from "../../core/contracts/semantic.contract.js";
import type {
  AccessibilityTreeResult,
  PatternInvokeOptions,
  PatternInvokeResult,
} from "../../core/models/semantic.model.js";
import type { ITransport } from "../transport/transport.interface.js";

/**
 * Single Responsibility: Windows UI Automation semantic inspection & in-memory pattern invocation.
 * Zero-cursor priority: triggers actions directly via COM interface patterns without physical mouse movement.
 */
export class WindowsSemanticDriver implements ISemanticDriver {
  constructor(private readonly transport: ITransport) {}

  public async getAccessibilityTree(
    hwnd: number = 0,
    maxDepth: number = 6
  ): Promise<AccessibilityTreeResult> {
    const res = await this.transport.send("get_ui_tree", {
      hwnd,
      max_depth: maxDepth,
    });
    return res as AccessibilityTreeResult;
  }

  public async invokePattern(options: PatternInvokeOptions): Promise<PatternInvokeResult> {
    const res = await this.transport.send("invoke_element", {
      element_index: options.elementIndex,
      action: options.action ?? "auto",
      value: options.value ?? "",
    });
    return res as PatternInvokeResult;
  }

  public async setValue(elementIndex: number, value: string): Promise<boolean> {
    const res = await this.transport.send("set_value", {
      element_index: elementIndex,
      value,
    });
    return Boolean(res);
  }
}
