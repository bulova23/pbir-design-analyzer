import * as vscode from 'vscode';
import { AnalyzerBridgeService, BridgeState } from '../services/rpc/AnalyzerBridgeService';
import { resetOutputChannelsForTesting } from '../platform/outputChannels';

class FakeLanguageClient {
  private listener: ((event: { oldState: number; newState: number }) => void) | undefined;
  public stopCalls = 0;

  constructor(
    private readonly sendRequestImpl: (method: string, params: unknown) => Promise<unknown>,
    private readonly running: boolean = true,
  ) {}

  onDidChangeState(listener: (event: { oldState: number; newState: number }) => void): void {
    this.listener = listener;
  }

  async sendRequest(method: string, params: unknown): Promise<unknown> {
    return this.sendRequestImpl(method, params);
  }

  isRunning(): boolean {
    return this.running;
  }

  async stop(): Promise<void> {
    this.stopCalls += 1;
    this.listener?.({ oldState: 2, newState: 0 });
  }

  emitStop(): void {
    this.listener?.({ oldState: 2, newState: 0 });
  }
}

/**
 * vscode-jsonrpc's untyped `sendRequest(method, ...args)` decides object-vs-positional-array
 * param encoding from `args.length`, not from whether a trailing arg is `undefined`. This fake
 * mirrors that arity sensitivity (unlike FakeLanguageClient's fixed 2-param stub above) so a
 * regression that reintroduces an always-present trailing `cancellationToken` argument fails here
 * instead of only surfacing against the real VS Code backend.
 */
class ArityRecordingLanguageClient {
  private listener: ((event: { oldState: number; newState: number }) => void) | undefined;
  readonly recordedArgCounts: number[] = [];

  onDidChangeState(listener: (event: { oldState: number; newState: number }) => void): void {
    this.listener = listener;
  }

  async sendRequest(...args: unknown[]): Promise<unknown> {
    const [method] = args;
    if (method === 'model/ping') {
      return { success: true, data: { status: 'ready' } };
    }

    this.recordedArgCounts.push(args.length);
    return { succeeded: true };
  }

  isRunning(): boolean {
    return true;
  }

  async stop(): Promise<void> {
    this.listener?.({ oldState: 2, newState: 0 });
  }
}

describe('AnalyzerBridgeService', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    resetOutputChannelsForTesting();
    delete process.env.PBIR_ANALYZER_RPC_DIAGNOSTIC_MODE;
  });

  afterEach(async () => {
    const bridge = AnalyzerBridgeService.getInstance();
    await bridge.shutdown();
    AnalyzerBridgeService.resetInstance();
  });

  it('becomes ready only after a successful backend ping', async () => {
    const bridge = AnalyzerBridgeService.getInstance();
    const client = new FakeLanguageClient(async (method) => {
      if (method === 'model/ping') {
        return { success: true, data: { status: 'ready' } };
      }

      throw new Error(`unexpected:${method}`);
    });

    await bridge.initialize(client as never);

    expect(bridge.getState()).toBe(BridgeState.READY);
    expect(bridge.isInitialized()).toBe(true);
  });

  it('fails initialization when the backend ping does not report ready', async () => {
    const bridge = AnalyzerBridgeService.getInstance();
    const client = new FakeLanguageClient(async () => ({ success: true, data: { status: 'starting' } }));

    await expect(bridge.initialize(client as never)).rejects.toThrow('Backend ping did not report a ready status');
    expect(bridge.getState()).toBe(BridgeState.ERROR);
  });

  it('switches to error state when the backend stops unexpectedly after ready', async () => {
    const bridge = AnalyzerBridgeService.getInstance();
    const client = new FakeLanguageClient(async () => ({ success: true, data: { status: 'ready' } }));

    await bridge.initialize(client as never);
    client.emitStop();

    expect(bridge.getState()).toBe(BridgeState.ERROR);
    expect(bridge.isInitialized()).toBe(false);
  });

  it('does not call stop during shutdown when the client never reached a running state', async () => {
    const bridge = AnalyzerBridgeService.getInstance();
    const client = new FakeLanguageClient(async () => ({ success: true, data: { status: 'ready' } }), false);

    await bridge.initialize(client as never);
    await bridge.shutdown();

    expect(client.stopCalls).toBe(0);
    expect(bridge.getState()).toBe(BridgeState.UNINITIALIZED);
  });

  it('does not log request params or response payloads by default', async () => {
    const bridge = AnalyzerBridgeService.getInstance();
    const client = new FakeLanguageClient(async (method) => {
      if (method === 'model/ping') {
        return { success: true, data: { status: 'ready' } };
      }

      return { success: true, data: { findings: ['sensitive finding'], evidence: ['sensitive evidence'] } };
    });

    await bridge.initialize(client as never);
    await bridge.executeRequest('model/pbir/scoreReport', {
      reportPath: '/tmp/Sales.Report',
      reportContent: '{"secret":true}',
    });

    const outputChannel = (vscode.window.createOutputChannel as jest.Mock).mock.results[0].value;
    const lines = (outputChannel.appendLine as jest.Mock).mock.calls.map(([line]) => String(line));
    const joined = lines.join('\n');

    expect(joined).toContain('Method: model/pbir/scoreReport');
    expect(joined).toMatch(/Elapsed: \d+ms/);
    expect(joined).not.toContain('Params:');
    expect(joined).not.toContain('Response:');
    expect(joined).not.toContain('/tmp/Sales.Report');
    expect(joined).not.toContain('sensitive finding');
    expect(joined).not.toContain('sensitive evidence');
  });

  it('logs redacted payloads only in diagnostic mode', async () => {
    process.env.PBIR_ANALYZER_RPC_DIAGNOSTIC_MODE = 'true';

    const bridge = AnalyzerBridgeService.getInstance();
    const client = new FakeLanguageClient(async (method) => {
      if (method === 'model/ping') {
        return { success: true, data: { status: 'ready' } };
      }

      return {
        success: true,
        data: {
          reportPath: '/tmp/Sales.Report',
          findings: ['raw finding'],
          evidence: [{ filePath: '/tmp/evidence.json' }],
        },
      };
    });

    await bridge.initialize(client as never);
    await bridge.executeRequest('model/pbir/scoreReport', {
      reportPath: '/tmp/Sales.Report',
      findings: ['raw finding'],
      evidence: [{ filePath: '/tmp/evidence.json' }],
      reportContent: '{"secret":true}',
    });

    const outputChannel = (vscode.window.createOutputChannel as jest.Mock).mock.results[0].value;
    const lines = (outputChannel.appendLine as jest.Mock).mock.calls.map(([line]) => String(line));
    const joined = lines.join('\n');

    expect(joined).toContain('Params:');
    expect(joined).toContain('Response:');
    expect(joined).toContain('[REDACTED: path]');
    expect(joined).toContain('[REDACTED: findings]');
    expect(joined).toContain('[REDACTED: evidence]');
    expect(joined).toContain('[REDACTED: reportContent]');
    expect(joined).not.toContain('/tmp/Sales.Report');
    expect(joined).not.toContain('raw finding');
    expect(joined).not.toContain('/tmp/evidence.json');
  });

  it('sends a bare two-argument request when no cancellation token is provided', async () => {
    const bridge = AnalyzerBridgeService.getInstance();
    const client = new ArityRecordingLanguageClient();

    await bridge.initialize(client as never);
    await bridge.executeAuthoringRequest({ schemaVersion: 'pbir-authoring-rpc/v1', operation: 'import' });
    await bridge.executeRequest('model/pbir/getTree', { reportPath: '/tmp/Sales.Report' });

    // vscode-jsonrpc wraps params in a positional array whenever more than one argument follows
    // the method name — even an explicit `undefined` cancellation token counts as "more than one".
    // Exactly 2 arguments (method, params) is what keeps params encoded as a bare object.
    expect(client.recordedArgCounts).toEqual([2, 2]);
  });
});
