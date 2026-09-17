import type { IMouseDriver } from "../../core/contracts/mouse.contract.js";
import type {
  ClickOptions,
  MoveOptions,
  DragOptions,
  ScrollOptions,
} from "../../core/models/input.model.js";
import type { CursorPosition } from "../../core/models/geometry.model.js";
import type { ITransport } from "../transport/transport.interface.js";

/**
 * Single Responsibility: Windows physical & blind mouse driver.
 * Communicates directly with native helper without pulling in vision or keyboard dependencies.
 */
export class WindowsMouseDriver implements IMouseDriver {
  constructor(private readonly transport: ITransport) {}

  public async click(options: ClickOptions): Promise<boolean> {
    const res = await this.transport.send("click", {
      hwnd: options.hwnd ?? 0,
      element_index: options.elementIndex ?? -1,
      x: options.x,
      y: options.y,
      mouse_button: options.button ?? "left",
      click_count: options.clickCount ?? 1,
    });
    return Boolean(res);
  }

  public async move(options: MoveOptions): Promise<boolean> {
    const res = await this.transport.send("move", {
      hwnd: options.hwnd ?? 0,
      x: options.x,
      y: options.y,
    });
    return Boolean(res);
  }

  public async drag(options: DragOptions): Promise<boolean> {
    const res = await this.transport.send("mouse_drag", {
      hwnd: options.hwnd ?? 0,
      from_x: options.fromX,
      from_y: options.fromY,
      to_x: options.toX,
      to_y: options.toY,
    });
    return Boolean(res);
  }

  public async scroll(options: ScrollOptions): Promise<boolean> {
    const res = await this.transport.send("mouse_scroll", {
      hwnd: options.hwnd ?? 0,
      x: options.x,
      y: options.y,
      delta_x: options.deltaX ?? 0,
      delta_y: options.deltaY ?? 0,
    });
    return Boolean(res);
  }

  public async getCursorPosition(): Promise<CursorPosition> {
    const res = await this.transport.send("get_cursor_position", {});
    return res as CursorPosition;
  }
}
