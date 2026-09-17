#!/usr/bin/env node

import * as path from "node:path";
import * as fs from "node:fs";
import { fileURLToPath } from "node:url";
import { StdioTransport } from "../driver/transport/stdio-transport.js";
import { NativeDriver } from "../driver/native-driver.js";
import { createMcpServer } from "../adapters/mcp/server.js";
import { exportCodexPlugin } from "../adapters/codex/manifest.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function findHelperBinary(): string {
  const possiblePaths = [
    path.join(__dirname, "..", "bin", "WinComputerUseHelper.exe"),
    path.join(__dirname, "..", "..", "bin", "WinComputerUseHelper.exe"),
    path.join(process.cwd(), "bin", "WinComputerUseHelper.exe"),
  ];

  for (const p of possiblePaths) {
    if (fs.existsSync(p)) {
      return p;
    }
  }

  throw new Error(`WinComputerUseHelper.exe not found. Looked in:\n${possiblePaths.join("\n")}\nPlease run 'pnpm build:helper' first.`);
}

async function main() {
  const args = process.argv.slice(2);

  if (args.includes("--help") || args.includes("-h")) {
    console.log(`
OpenSky: Universal Computer Use Engine for AI Agents

Usage:
  opensky                      Start standard MCP server (default)
  opensky --export-codex <dir> Export Codex desktop plugin manifest & skills
  opensky --help               Show this help message
`);
    process.exit(0);
  }

  const exportIdx = args.indexOf("--export-codex");
  if (exportIdx !== -1 && args[exportIdx + 1]) {
    const outDir = path.resolve(args[exportIdx + 1]);
    const entryPath = path.resolve(__filename);
    exportCodexPlugin({ outputDir: outDir, entryScriptPath: entryPath });
    console.log(`Successfully exported Codex plugin to: ${outDir}`);
    process.exit(0);
  }

  // Default: Start MCP Server
  const helperPath = findHelperBinary();
  const transport = new StdioTransport({ executablePath: helperPath });
  const engine = new NativeDriver(transport);

  process.on("SIGINT", async () => {
    await engine.close();
    process.exit(0);
  });
  process.on("SIGTERM", async () => {
    await engine.close();
    process.exit(0);
  });

  const { startStdio } = createMcpServer({ engine, name: "opensky", version: "0.0.1" });
  await startStdio();
}

main().catch((err) => {
  console.error("Fatal error starting OpenSky:", err);
  process.exit(1);
});
