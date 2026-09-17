export interface ITransport {
  send(method: string, params: Record<string, unknown>): Promise<unknown>;
  close(): Promise<void>;
  isAlive(): boolean;
}
