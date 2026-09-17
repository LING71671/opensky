import type { IComputerUseEngine } from "../../core/engine.js";

export type AnthropicComputerAction =
  | "screenshot"
  | "mouse_move"
  | "left_click"
  | "right_click"
  | "middle_click"
  | "double_click"
  | "triple_click"
  | "left_click_drag"
  | "cursor_position"
  | "type"
  | "key"
  | "scroll";

export interface AnthropicToolInput {
  action: AnthropicComputerAction;
  coordinate?: [number, number];
  text?: string;
  keys?: string[];
  delta_x?: number;
  delta_y?: number;
}

export interface AnthropicToolResult {
  output?: string;
  error?: string;
  base64_image?: string;
}

export class AnthropicComputerUseBridge {
  private readonly engine: IComputerUseEngine;

  constructor(engine: IComputerUseEngine) {
    this.engine = engine;
  }

  public getToolDefinition() {
    return {
      name: "computer",
      type: "computer_20241022",
      display_width_px: 1920,
      display_height_px: 1080,
      display_number: 1,
    };
  }

  public async execute(input: AnthropicToolInput): Promise<AnthropicToolResult> {
    try {
      switch (input.action) {
        case "screenshot": {
          const res = await this.engine.screenshot({ format: "jpeg", quality: 80 });
          const rawBase64 = res.dataUrl.replace(/^data:image\/[a-z]+;base64,/, "");
          return { base64_image: rawBase64 };
        }

        case "mouse_move": {
          if (!input.coordinate) throw new Error("coordinate required for mouse_move");
          const [x, y] = input.coordinate;
          await this.engine.move({ x, y });
          return { output: `Moved mouse to (${x}, ${y})` };
        }

        case "left_click": {
          const [x, y] = input.coordinate ?? [undefined, undefined];
          await this.engine.click({ x, y, button: "left", clickCount: 1 });
          return { output: "Clicked left mouse button" };
        }

        case "right_click": {
          const [x, y] = input.coordinate ?? [undefined, undefined];
          await this.engine.click({ x, y, button: "right", clickCount: 1 });
          return { output: "Clicked right mouse button" };
        }

        case "middle_click": {
          const [x, y] = input.coordinate ?? [undefined, undefined];
          await this.engine.click({ x, y, button: "middle", clickCount: 1 });
          return { output: "Clicked middle mouse button" };
        }

        case "double_click": {
          const [x, y] = input.coordinate ?? [undefined, undefined];
          await this.engine.click({ x, y, button: "left", clickCount: 2 });
          return { output: "Double clicked left mouse button" };
        }

        case "triple_click": {
          const [x, y] = input.coordinate ?? [undefined, undefined];
          await this.engine.click({ x, y, button: "left", clickCount: 3 });
          return { output: "Triple clicked left mouse button" };
        }

        case "left_click_drag": {
          if (!input.coordinate) throw new Error("coordinate required for left_click_drag");
          const currentPos = await this.engine.getCursorPosition();
          const [toX, toY] = input.coordinate;
          await this.engine.drag({
            fromX: currentPos.x,
            fromY: currentPos.y,
            toX,
            toY,
          });
          return { output: `Dragged from (${currentPos.x}, ${currentPos.y}) to (${toX}, ${toY})` };
        }

        case "cursor_position": {
          const pos = await this.engine.getCursorPosition();
          return { output: `Coordinates: (${pos.x}, ${pos.y})` };
        }

        case "type": {
          if (input.text === undefined) throw new Error("text required for type action");
          await this.engine.typeText(input.text);
          return { output: `Typed text: "${input.text}"` };
        }

        case "key": {
          const keys = input.keys ?? (input.text ? [input.text] : []);
          for (const k of keys) {
            await this.engine.pressKey(k);
          }
          return { output: `Pressed key chord: ${keys.join("+")}` };
        }

        case "scroll": {
          const [x, y] = input.coordinate ?? [undefined, undefined];
          const deltaX = input.delta_x ?? 0;
          const deltaY = input.delta_y ?? 0;
          await this.engine.scroll({ x, y, deltaX, deltaY });
          return { output: `Scrolled delta: (${deltaX}, ${deltaY})` };
        }

        default:
          throw new Error(`Unsupported Anthropic action: ${(input as { action: string }).action}`);
      }
    } catch (err) {
      return { error: err instanceof Error ? err.message : String(err) };
    }
  }
}
