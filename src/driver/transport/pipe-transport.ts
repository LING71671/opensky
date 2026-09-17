import * as net from "node:net";
import { Buffer } from "node:buffer";
import type { ITransport } from "./transport.interface.js";

const HEADER_BYTES = 4;

export interface PipeTransportOptions {
  pipePath: string;
  timeoutMs?: number;
}

interface PendingRequest {
  resolve: (value: unknown) => void;
  reject: (reason: Error) => void;
  timeout: NodeJS.Timeout;
}

export class PipeTransport implements ITransport {
  private socket: net.Socket | null = null;
  private buffer: Buffer = Buffer.alloc(0);
  private nextId = 1;
  private pending = new Map<number, PendingRequest>();
  private readonly options: PipeTransportOptions;

  constructor(options: PipeTransportOptions) {
    this.options = {
      timeoutMs: 15000,
      ...options,
    };
  }

  private async ensureConnected(): Promise<net.Socket> {
    if (this.socket && !this.socket.destroyed) {
      return this.socket;
    }

    return new Promise((resolve, reject) => {
      const socket = net.createConnection(this.options.pipePath, () => {
        this.socket = socket;
        resolve(socket);
      });

      socket.on("data", (chunk) => this.handleData(chunk));
      socket.on("error", (err) => {
        this.rejectAll(new Error(`Pipe error: ${err.message}`));
        reject(err);
      });
      socket.on("close", () => {
        this.rejectAll(new Error("Named pipe closed"));
        this.socket = null;
      });
    });
  }

  private handleData(chunk: Buffer): void {
    this.buffer = Buffer.concat([this.buffer, chunk]);
    while (this.buffer.length >= HEADER_BYTES) {
      const payloadLen = this.buffer.readUInt32LE(0);
      const totalLen = HEADER_BYTES + payloadLen;
      if (this.buffer.length < totalLen) break;

      const rawMsg = this.buffer.subarray(HEADER_BYTES, totalLen).toString("utf8");
      this.buffer = this.buffer.subarray(totalLen);

      try {
        const msg = JSON.parse(rawMsg);
        if (typeof msg.id === "number") {
          const req = this.pending.get(msg.id);
          if (req) {
            clearTimeout(req.timeout);
            this.pending.delete(msg.id);
            if (msg.error) {
              req.reject(new Error(msg.error.message || msg.error));
            } else {
              req.resolve(msg.result);
            }
          }
        }
      } catch {}
    }
  }

  public async send(method: string, params: Record<string, unknown> = {}): Promise<unknown> {
    const socket = await this.ensureConnected();
    const id = this.nextId++;

    const payload = JSON.stringify({
      id,
      jsonrpc: "2.0",
      method,
      params,
    });

    const payloadBuf = Buffer.from(payload, "utf8");
    const frame = Buffer.alloc(HEADER_BYTES + payloadBuf.length);
    frame.writeUInt32LE(payloadBuf.length, 0);
    payloadBuf.copy(frame, HEADER_BYTES);

    return new Promise((resolve, reject) => {
      const timeout = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(`Pipe request timed out: ${method}`));
      }, this.options.timeoutMs);

      this.pending.set(id, { resolve, reject, timeout });
      socket.write(frame, (err) => {
        if (err) {
          clearTimeout(timeout);
          this.pending.delete(id);
          reject(err);
        }
      });
    });
  }

  public isAlive(): boolean {
    return this.socket !== null && !this.socket.destroyed;
  }

  public async close(): Promise<void> {
    this.rejectAll(new Error("Pipe transport closed"));
    if (this.socket) {
      this.socket.end();
      this.socket.destroy();
      this.socket = null;
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
