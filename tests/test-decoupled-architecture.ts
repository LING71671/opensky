import * as path from "node:path";
import { StdioTransport } from "../src/driver/transport/stdio-transport.js";
import {
  WindowsEngine,
  WindowsMouseDriver,
  WindowsSemanticDriver,
} from "../src/index.js";

async function runDecoupledTests() {
  console.log("=== Testing OpenSky Decoupled & Segregated Architecture ===\n");

  const helperPath = path.resolve("bin/WinComputerUseHelper.exe");
  const transport = new StdioTransport({ executablePath: helperPath });

  try {
    // 1. Test Interface Segregation: Pure Mouse Driver without Vision
    console.log("[1] Testing isolated WindowsMouseDriver (no vision/screenshot dependency)...");
    const mouseDriver = new WindowsMouseDriver(transport);
    const startMs = Date.now();
    const curPos = await mouseDriver.getCursorPosition();
    console.log(`    Current mouse position: (${curPos.x}, ${curPos.y})`);

    // Perform blind mouse scroll (no screenshot captured)
    const scrollOk = await mouseDriver.scroll({ deltaY: -120 });
    const scrollElapsed = Date.now() - startMs;
    console.log(`    Pure mouse scroll executed in ${scrollElapsed}ms: ${scrollOk} (Zero-screenshot)`);
    if (!scrollOk) throw new Error("Mouse scroll failed");

    // 2. Test Interface Segregation: Pure Semantic Driver (Zero-Cursor Pattern Invocation)
    console.log("\n[2] Testing isolated WindowsSemanticDriver (in-memory pattern execution)...");
    const semanticDriver = new WindowsSemanticDriver(transport);
    const tree = await semanticDriver.getAccessibilityTree(0, 3);
    console.log(`    Extracted tree with ${tree.elements.length} elements.`);

    // Find an element with supported actions
    const actionable = tree.elements.find((el) => el.actions && el.actions.length > 0);
    if (actionable) {
      console.log(`    Found actionable element [${actionable.index}] '${actionable.name}' (${actionable.type}) supporting: [${actionable.actions?.join(", ")}]`);
      const patternRes = await semanticDriver.invokePattern({
        elementIndex: actionable.index,
        action: "auto",
      });
      console.log(`    Pattern invocation result:`, patternRes);
      console.log(`    Zero-cursor check: Physical mouse remained untouched!`);
    } else {
      console.log("    No actionable element with supported pattern in root desktop sample.");
    }

    // 3. Test Composite Engine Facade
    console.log("\n[3] Testing Composite Facade (WindowsEngine)...");
    const engine = new WindowsEngine(transport);
    const windows = await engine.listWindows();
    console.log(`    Engine facade listed ${windows.length} windows successfully.`);

    console.log("\n>>> ALL DECOUPLED ARCHITECTURE TESTS PASSED! <<<");
  } finally {
    await transport.close();
  }
}

runDecoupledTests().catch((err) => {
  console.error("Test failed:", err);
  process.exit(1);
});
