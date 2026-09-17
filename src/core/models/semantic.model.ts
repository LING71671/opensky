import type { Rect } from "./geometry.model.js";

/**
 * Single Responsibility: Accessibility elements and in-memory pattern action descriptors.
 */
export interface UIElement {
  index: number;
  type: string;
  name: string;
  x: number;
  y: number;
  bounds: Rect;
  value?: string;
  isFocused?: boolean;
  actions?: string[];
}

export interface AccessibilityTreeResult {
  tree: string;
  focusedElement?: string;
  elements: UIElement[];
}

export interface PatternInvokeOptions {
  elementIndex: number;
  action?: "auto" | "invoke" | "toggle" | "select" | "set_value" | "expand" | "collapse";
  value?: string;
}

export interface PatternInvokeResult {
  success: boolean;
  patternSupported: boolean;
  method?: string;
  elementName?: string;
  message?: string;
}
