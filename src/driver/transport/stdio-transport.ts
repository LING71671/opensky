import { spawn, type ChildProcess } from "node:child_process";
import * as readline from "node:readline";
import type { ITransport } from "./transport.interface.js";

export interface StdioTransportOptions {
  executablePath: string;
  args?: string[];
  timeoutMs?: number;
}

interface PendingRequest {
  resolve: (value: unknown) => void;
  reject: (reason: Error) => void;
  timeout: NodeJS.Timeout;
}

export class StdioTransport implements ITransport {
  private child: ChildProcess | null = null;
  private rl: readline.Interface | null = null;
  private nextId = 1;
  private pending = new Map<number, PendingRequest>();
  private readonly options: StdioTransportOptions;

  constructor(options: StdioTransportOptions) {
    this.options = {
      timeoutMs: 15000,
      args: ["--daemon"],
      ...options,
    };
  }

  private ensureProcess(): ChildProcess {
    if (this.child && !this.child.killed && this.child.exitCode === null) {
      return this.child;
    }

    const child = spawn(this.options.executablePath, this.options.args ?? ["--daemon"], {
      stdio: ["pipe", "pipe", "pipe"],
      windowsHide: true,
    });

    child.on("error", (err) => {
      this.rejectAll(new Error(`Helper process error: ${err.message}`));
    });

    child.on("exit", (code, signal) => {
      this.rejectAll(new Error(`Helper process exited unexpectedly (code: ${code}, signal: ${signal})`));
      this.child = null;
    });

    const rl = readline.createInterface({
      input: child.stdout!,
      crlfDelay: Infinity,
    });

    rl.on("line", (line) => {
      const trimmed = line.trim();
      if (!trimmed) return;
      try {
        const msg = JSON.parse(trimmed);
        if (typeof msg.id === "number") {
          const req = this.pending.get(msg.id);
          if (req) {
            clearTimeout(req.timeout);
            this.pending.delete(msg.id);
            if (msg.ok) {
              req.resolve(msg.result);
            } else {
              req.reject(new Error(msg.error || "Unknown helper error"));
            }
          }
        }
      } catch {
        // Ignore unformatted debug output
      }
    });

    this.child = child;
    this.rl = rl;
    return child;
  }

  public async send(method: string, params: Record<string, unknown> = {}): Promise<unknown> {
    const child = this.ensureProcess();
    const id = this.nextId++;

    const payload = JSON.stringify({
      id,
      method,
      params,
    }) + "\n";

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(`Request timed out: ${method}`));
      }, this.options.timeoutMs);

      this.pending.set(id, { resolve, reject, timeout });
      child.stdin!.write(payload, "utf8", (err) => {
        if (err) {
          clearTimeout(timeout);
          this.pending.delete(id);
          reject(err);
        }
      });
    });
  }

  public isAlive(): boolean {
    return this.child !== null && !this.child.killed && this.child.exitCode === null;
  }

  public async close(): Promise<void> {
    this.rejectAll(new Error("Transport closed"));
    if (this.rl) {
      this.rl.close();
      this.rl = null;
    }
    if (this.child) {
      try {
        this.child.stdin?.write("exit\n");
      } catch {}
      this.child.kill();
      this.child = null;
    }
  }

  private rejectAll(err: Error): void {
    for (const req of this.pending.values()) {
      clearTimeout(req.timeout);
      req.reject(err);
    }
    this.pending.clear();
  }
}
