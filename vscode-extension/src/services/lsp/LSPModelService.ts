import { EventEmitter } from 'events';
import * as vscode from 'vscode';
import { LanguageClient } from 'vscode-languageclient/node';

export enum LSPState {
  UNINITIALIZED = 'uninitialized',
  STARTING = 'starting',
  READY = 'ready',
  ERROR = 'error',
}

/**
 * PBIR-only request bridge over the packaged analyzer backend transport.
 */
export class LSPModelService extends EventEmitter {
  private static instance: LSPModelService | null = null;

  private client: LanguageClient | null = null;
  private state: LSPState = LSPState.UNINITIALIZED;
  private readonly defaultTimeout = 30000;
  private readonly outputChannel = vscode.window.createOutputChannel('PBIR Design Analyzer Backend');

  private constructor() {
    super();
  }

  static getInstance(): LSPModelService {
    if (!LSPModelService.instance) {
      LSPModelService.instance = new LSPModelService();
    }
    return LSPModelService.instance;
  }

  static resetInstance(): void {
    if (LSPModelService.instance) {
      void LSPModelService.instance.shutdown();
      LSPModelService.instance = null;
    }
  }

  async initialize(client: LanguageClient): Promise<void> {
    if (this.state !== LSPState.UNINITIALIZED) {
      return;
    }

    this.state = LSPState.STARTING;
    this.client = client;
    this.outputChannel.appendLine('[LSPModelService] Waiting for analyzer backend to become ready...');

    await new Promise((resolve) => setTimeout(resolve, 500));

    this.state = LSPState.READY;
    this.outputChannel.appendLine('[LSPModelService] Analyzer backend ready');
    this.emit('stateChange', LSPState.READY);
    this.emit('connected');
  }

  getState(): LSPState {
    return this.state;
  }

  isInitialized(): boolean {
    return this.state === LSPState.READY;
  }

  onStateChange(listener: (state: LSPState) => void): void {
    this.on('stateChange', listener);
  }

  async shutdown(): Promise<void> {
    if (this.client) {
      try {
        await this.client.stop();
      } catch {
        // Ignore shutdown races during extension teardown.
      }
      this.client = null;
    }

    this.state = LSPState.UNINITIALIZED;
    this.emit('stateChange', LSPState.UNINITIALIZED);
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
      throw new Error('LSPModelService not initialized. Call initialize() first.');
    }

    if (this.state !== LSPState.READY) {
      throw new Error(`LSPModelService not ready. Current state: ${this.state}`);
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

      throw new Error(`LSP request failed: ${message}`);
    }
  }
}
