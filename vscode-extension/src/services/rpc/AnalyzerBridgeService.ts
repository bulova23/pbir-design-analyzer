import { EventEmitter } from 'events';
import { LanguageClient, State } from 'vscode-languageclient/node';
import { getBackendOutputChannel } from '../../platform/outputChannels';

export enum BridgeState {
  UNINITIALIZED = 'uninitialized',
  STARTING = 'starting',
  READY = 'ready',
  ERROR = 'error',
}

/**
 * PBIR-only request bridge over the packaged analyzer backend transport.
 */
export class AnalyzerBridgeService extends EventEmitter {
  private static instance: AnalyzerBridgeService | null = null;

  private client: LanguageClient | null = null;
  private state: BridgeState = BridgeState.UNINITIALIZED;
  private readonly defaultTimeout = 30000;
  private readonly outputChannel = getBackendOutputChannel();
  private isShuttingDown = false;
  private lastClientState: State = State.Stopped;

  private constructor() {
    super();
  }

  static getInstance(): AnalyzerBridgeService {
    if (!AnalyzerBridgeService.instance) {
      AnalyzerBridgeService.instance = new AnalyzerBridgeService();
    }
    return AnalyzerBridgeService.instance;
  }

  static resetInstance(): void {
    if (AnalyzerBridgeService.instance) {
      void AnalyzerBridgeService.instance.shutdown();
      AnalyzerBridgeService.instance = null;
    }
  }

  async initialize(client: LanguageClient): Promise<void> {
    if (this.state !== BridgeState.UNINITIALIZED) {
      return;
    }

    this.isShuttingDown = false;
    this.state = BridgeState.STARTING;
    this.client = client;
    this.outputChannel.appendLine('[AnalyzerBridge] Waiting for analyzer backend to become ready...');
    this.emit('stateChange', BridgeState.STARTING);

    this.client.onDidChangeState((event) => {
      this.lastClientState = event.newState;
      if (event.newState === State.Stopped) {
        if (this.isShuttingDown) {
          this.state = BridgeState.UNINITIALIZED;
          this.emit('stateChange', BridgeState.UNINITIALIZED);
          return;
        }

        this.outputChannel.appendLine('[AnalyzerBridge] Analyzer backend stopped unexpectedly');
        this.state = BridgeState.ERROR;
        this.emit('stateChange', BridgeState.ERROR);
      }
    });

    try {
      const pingResponse = await this.client.sendRequest('model/ping', {});
      const readyStatus = extractReadyStatus(pingResponse);
      if (readyStatus !== 'ready') {
        throw new Error(`Backend ping did not report a ready status: ${JSON.stringify(pingResponse)}`);
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      this.outputChannel.appendLine(`[AnalyzerBridge] Analyzer backend failed readiness handshake: ${message}`);
      this.state = BridgeState.ERROR;
      this.emit('stateChange', BridgeState.ERROR);
      throw error;
    }

    this.state = BridgeState.READY;
    this.lastClientState = State.Running;
    this.outputChannel.appendLine('[AnalyzerBridge] Analyzer backend ready');
    this.emit('stateChange', BridgeState.READY);
    this.emit('connected');
  }

  getState(): BridgeState {
    return this.state;
  }

  isInitialized(): boolean {
    return this.state === BridgeState.READY;
  }

  onStateChange(listener: (state: BridgeState) => void): void {
    this.on('stateChange', listener);
  }

  async shutdown(): Promise<void> {
    if (this.client) {
      try {
        this.isShuttingDown = true;
        if (this.lastClientState === State.Running && this.client.isRunning()) {
          await this.client.stop();
        }
      } catch {
        // Ignore shutdown races during extension teardown.
      }
      this.client = null;
    }

    this.lastClientState = State.Stopped;
    this.state = BridgeState.UNINITIALIZED;
    this.emit('stateChange', BridgeState.UNINITIALIZED);
  }

  async getPbirTree(reportPath: string): Promise<unknown> {
    return this.sendRequest('model/pbir/getTree', { reportPath });
  }

  async executeRequest<T = unknown>(method: string, params: unknown = {}): Promise<T> {
    return this.sendRequest<T>(method, params);
  }

  private async sendRequest<T>(
    method: string,
    params: unknown = {},
    timeout: number = this.defaultTimeout,
  ): Promise<T> {
    if (!this.client) {
      throw new Error('Analyzer backend not available. Call initialize() first.');
    }

    if (this.state !== BridgeState.READY) {
      throw new Error(`Analyzer backend not ready. Current state: ${this.state}`);
    }

    const timestamp = new Date().toISOString();
    const requestId = Math.random().toString(36).substring(7);
    const startTime = Date.now();

    this.outputChannel.appendLine(`\n[${timestamp}] >>> Outgoing Request [${requestId}]`);
    this.outputChannel.appendLine(`Method: ${method}`);
    this.outputChannel.appendLine(`Params: ${JSON.stringify(params, null, 2)}`);

    try {
      const timeoutPromise = new Promise<never>((_, reject) => {
        setTimeout(() => reject(new Error(`Request timeout after ${timeout}ms`)), timeout);
      });

      const resultPromise = this.client.sendRequest<T>(method, params);
      const result = await Promise.race([resultPromise, timeoutPromise]);
      const elapsed = Date.now() - startTime;

      this.outputChannel.appendLine(`\n[${new Date().toISOString()}] <<< Incoming Response [${requestId}]`);
      this.outputChannel.appendLine(`Method: ${method}`);
      this.outputChannel.appendLine(`Elapsed: ${elapsed}ms`);
      this.outputChannel.appendLine(`Response: ${JSON.stringify(result, null, 2)}`);

      return result;
    } catch (error) {
      const elapsed = Date.now() - startTime;
      const message = error instanceof Error ? error.message : String(error);

      this.outputChannel.appendLine(`\n[${new Date().toISOString()}] !!! Request Error [${requestId}]`);
      this.outputChannel.appendLine(`Method: ${method}`);
      this.outputChannel.appendLine(`Elapsed: ${elapsed}ms`);
      this.outputChannel.appendLine(`Error: ${message}`);

      if (error instanceof Error && error.stack) {
        this.outputChannel.appendLine(`Stack: ${error.stack}`);
      }

      throw new Error(`RPC request failed: ${message}`);
    }
  }
}

function extractReadyStatus(response: unknown): string | undefined {
  if (!response || typeof response !== 'object') {
    return undefined;
  }

  const directStatus = (response as { status?: unknown }).status;
  if (typeof directStatus === 'string') {
    return directStatus;
  }

  const data = (response as { data?: unknown }).data;
  if (data && typeof data === 'object') {
    const nestedStatus = (data as { status?: unknown }).status;
    if (typeof nestedStatus === 'string') {
      return nestedStatus;
    }
  }

  return undefined;
}
