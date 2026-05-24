import { bucketScore, telemetry } from '../telemetry/reporter';
import * as vscode from 'vscode';

describe('bucketScore', () => {
  it('returns "90-100" for scores >= 90', () => {
    expect(bucketScore(90)).toBe('90-100');
    expect(bucketScore(100)).toBe('90-100');
    expect(bucketScore(95.5)).toBe('90-100');
  });

  it('returns "70-89" for scores between 70 and 89', () => {
    expect(bucketScore(70)).toBe('70-89');
    expect(bucketScore(89.9)).toBe('70-89');
    expect(bucketScore(75)).toBe('70-89');
  });

  it('returns "50-69" for scores between 50 and 69', () => {
    expect(bucketScore(50)).toBe('50-69');
    expect(bucketScore(69.9)).toBe('50-69');
    expect(bucketScore(60)).toBe('50-69');
  });

  it('returns "0-49" for scores below 50', () => {
    expect(bucketScore(0)).toBe('0-49');
    expect(bucketScore(49.9)).toBe('0-49');
    expect(bucketScore(25)).toBe('0-49');
  });
});

describe('telemetry reporter', () => {
  const consoleSpy = jest.spyOn(console, 'log').mockImplementation(() => {});

  afterEach(() => {
    consoleSpy.mockClear();
    telemetry.dispose();
  });

  afterAll(() => {
    consoleSpy.mockRestore();
  });

  it('sendEvent is a no-op when reporter is not initialized', () => {
    telemetry.sendEvent('scoring.completed', { pageCount: 3, durationMs: 100, compositeScoreBucket: '70-89' });
    expect(consoleSpy).not.toHaveBeenCalled();
  });

  it('sendEvent is a no-op when isTelemetryEnabled is false', () => {
    const fakeContext = {
      extension: { id: 'test.id', packageJSON: { version: '1.0.0' } },
      subscriptions: [],
    } as unknown as vscode.ExtensionContext;

    Object.defineProperty(vscode.env, 'isTelemetryEnabled', { value: false, configurable: true });

    telemetry.initialize(fakeContext);
    telemetry.sendEvent('command.invoked', { commandName: 'test' });
    expect(consoleSpy).not.toHaveBeenCalled();

    Object.defineProperty(vscode.env, 'isTelemetryEnabled', { value: true, configurable: true });
  });

  it('initialize sets the reporter as ready', () => {
    const fakeContext = {
      extension: { id: 'bcrowell.pbir-design-analyzer', packageJSON: { version: '0.1.13' } },
      subscriptions: [],
    } as unknown as vscode.ExtensionContext;

    telemetry.initialize(fakeContext);
    // No error thrown — initialized without crashing
    telemetry.sendEvent('command.invoked', { commandName: 'pbir.scoreReport' });
  });
});
