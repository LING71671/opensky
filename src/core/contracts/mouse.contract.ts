import type {
  ClickOptions,
  MoveOptions,
  DragOptions,
  ScrollOptions,
} from "../models/input.model.js";
import type { CursorPosition } from "../models/geometry.model.js";

/**
 * Single Responsibility & Interface Segregation:
 * Pure mouse operations and gestures.
 * Strictly decoupled from screenshots, vision, and semantic trees.
 */
export interface IMouseDriver {
  /** Click at coordinates or element center with physical or blind delivery */
  click(options: ClickOptions): Promise<boolean>;

  /** Move cursor with optional spring trajectory */
  move(options: MoveOptions): Promise<boolean>;

  /** Drag mouse smoothly from start to end */
  drag(options: DragOptions): Promise<boolean>;

  /** Scroll wheel horizontally or vertically */
  scroll(options: ScrollOptions): Promise<boolean>;

  /** Get current cursor coordinates */
  getCursorPosition(): Promise<CursorPosition>;
}
