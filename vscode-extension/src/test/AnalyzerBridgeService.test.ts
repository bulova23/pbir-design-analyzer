import { AnalyzerBridgeService, BridgeState } from '../services/rpc/AnalyzerBridgeService';

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

describe('AnalyzerBridgeService', () => {
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
});
