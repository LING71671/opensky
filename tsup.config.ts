import { defineConfig } from "tsup";

export default defineConfig({
  entry: {
    index: "src/index.ts",
    "bin/opensky": "src/bin/opensky.ts",
  },
  format: ["esm"],
  target: "node20",
  sourcemap: true,
  clean: true,
  dts: false,
  splitting: false,
});
