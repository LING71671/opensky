import { WindowsEngine } from "./windows/windows.engine.js";
import type { ITransport } from "./transport/transport.interface.js";

/**
 * NativeDriver: Backward compatibility alias for WindowsEngine.
 */
export class NativeDriver extends WindowsEngine {
  constructor(transport: ITransport) {
    super(transport);
  }
}
