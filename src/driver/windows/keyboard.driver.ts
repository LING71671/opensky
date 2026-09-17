import type { IKeyboardDriver } from "../../core/contracts/keyboard.contract.js";
import type { ITransport } from "../transport/transport.interface.js";

/**
 * Single Responsibility: Windows physical & unicode keyboard driver.
 * Clean, lightweight, and independent of mouse or visual pipelines.
 */
export class WindowsKeyboardDriver implements IKeyboardDriver {
  constructor(private readonly transport: ITransport) {}

  public async typeText(text: string): Promise<boolean> {
    const res = await this.transport.send("type_text", { text });
    return Boolean(res);
  }

  public async pressKey(chord: string): Promise<boolean> {
    const res = await this.transport.send("press_key", { chord });
    return Boolean(res);
  }
}
