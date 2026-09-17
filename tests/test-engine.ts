import * as path from "node:path";
import { StdioTransport } from "../src/driver/transport/stdio-transport.js";
import { NativeDriver } from "../src/driver/native-driver.js";
import { AnthropicComputerUseBridge } from "../src/adapters/anthropic/bridge.js";

async function runTests() {
  console.log("=== Testing win-computer-use (Decoupled Architecture) ===\n");

  const helperPath = path.resolve("bin/WinComputerUseHelper.exe");
  console.log(`[1] Initializing StdioTransport with helper: ${helperPath}`);
  const transport = new StdioTransport({ executablePath: helperPath });
  const engine = new NativeDriver(transport);

  try {
    // 1. Test Window Listing
    console.log("[2] Testing engine.listWindows()...");
    const windows = await engine.listWindows();
    console.log(`    Found ${windows.length} interactive windows:`);
    for (const w of windows.slice(0, 5)) {
      console.log(`    - [${w.id}] '${w.title}' (${w.processName}, bounds: ${w.bounds.width}x${w.bounds.height} at ${w.bounds.x},${w.bounds.y})`);
    }
    if (windows.length === 0) throw new Error("Expected at least 1 interactive window");

    // 2. Test Cursor Position
    console.log("\n[3] Testing engine.getCursorPosition()...");
    const curPos = await engine.getCursorPosition();
    console.log(`    Cursor position: (${curPos.x}, ${curPos.y})`);

    // 3. Test Screenshot
    console.log("\n[4] Testing engine.screenshot()...");
    const screen = await engine.screenshot({ quality: 20 });
    console.log(`    Screenshot captured: ${screen.width}x${screen.height}, dataUrl prefix: ${screen.dataUrl.slice(0, 30)}...`);
    if (screen.width <= 0 || screen.height <= 0 || !screen.dataUrl.startsWith("data:image/jpeg;base64,")) {
      throw new Error("Invalid screenshot result");
    }

    // 4. Test UI Automation Tree
    console.log("\n[5] Testing engine.getAccessibilityTree()...");
    const uiTree = await engine.getAccessibilityTree(0, 3);
    console.log(`    Extracted ${uiTree.elements.length} UI elements. Sample tree:`);
    const treeLines = uiTree.tree.split("\n").filter(Boolean).slice(0, 6);
    for (const line of treeLines) {
      console.log(`      ${line}`);
    }

    // 5. Test Anthropic Computer Use Bridge
    console.log("\n[6] Testing AnthropicComputerUseBridge...");
    const anthropicBridge = new AnthropicComputerUseBridge(engine);
    const anthropicPos = await anthropicBridge.execute({ action: "cursor_position" });
    console.log(`    Anthropic cursor_position output: ${anthropicPos.output}`);
    const anthropicShot = await anthropicBridge.execute({ action: "screenshot" });
    console.log(`    Anthropic screenshot image bytes (base64 length): ${anthropicShot.base64_image?.length}`);
    if (!anthropicShot.base64_image) throw new Error("Anthropic screenshot failed");

    console.log("\n>>> ALL TESTS PASSED SUCCESSFULLY! <<<");
  } finally {
    await engine.close();
  }
}

runTests().catch((err) => {
  console.error("Test failed:", err);
  process.exit(1);
});
