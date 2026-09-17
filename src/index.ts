// Core Contracts & Domain Models
export type * from "./core/models/index.js";
export type * from "./core/contracts/index.js";
export type { IComputerUseEngine } from "./core/contracts/engine.contract.js";

// Transports
export type { ITransport } from "./driver/transport/transport.interface.js";
export { StdioTransport } from "./driver/transport/stdio-transport.js";
export { PipeTransport } from "./driver/transport/pipe-transport.js";

// Windows Modular Drivers (Loose Coupling)
export { WindowsEngine } from "./driver/windows/windows.engine.js";
export { WindowsMouseDriver } from "./driver/windows/mouse.driver.js";
export { WindowsKeyboardDriver } from "./driver/windows/keyboard.driver.js";
export { WindowsSemanticDriver } from "./driver/windows/semantic.driver.js";
export { WindowsVisionDriver } from "./driver/windows/vision.driver.js";
export { WindowsWindowManager } from "./driver/windows/window.driver.js";

// Backward Compatibility
export { NativeDriver } from "./driver/native-driver.js";

// Adapters
export { createMcpServer } from "./adapters/mcp/server.js";
export { AnthropicComputerUseBridge } from "./adapters/anthropic/bridge.js";
export { exportCodexPlugin } from "./adapters/codex/manifest.js";
