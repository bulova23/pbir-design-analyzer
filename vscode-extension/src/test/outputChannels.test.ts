import * as vscode from 'vscode';
import {
  getBackendOutputChannel,
  getBackendTraceOutputChannel,
  getDiagnosticsOutputChannel,
  getExtensionOutputChannel,
  resetOutputChannelsForTesting,
} from '../platform/outputChannels';

describe('output channel registry', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    resetOutputChannelsForTesting();
  });

  it('reuses singleton channels instead of recreating duplicate instances', () => {
    const extensionA = getExtensionOutputChannel();
    const extensionB = getExtensionOutputChannel();
    const backendA = getBackendOutputChannel();
    const backendB = getBackendOutputChannel();
    const traceA = getBackendTraceOutputChannel();
    const traceB = getBackendTraceOutputChannel();
    const diagnosticsA = getDiagnosticsOutputChannel();
    const diagnosticsB = getDiagnosticsOutputChannel();

    expect(extensionA).toBe(extensionB);
    expect(backendA).toBe(backendB);
    expect(traceA).toBe(traceB);
    expect(diagnosticsA).toBe(diagnosticsB);
    expect(vscode.window.createOutputChannel).toHaveBeenCalledTimes(4);
    expect(vscode.window.createOutputChannel).toHaveBeenNthCalledWith(1, 'PBIR Design Analyzer');
    expect(vscode.window.createOutputChannel).toHaveBeenNthCalledWith(2, 'PBIR Design Analyzer Backend');
    expect(vscode.window.createOutputChannel).toHaveBeenNthCalledWith(3, 'PBIR Design Analyzer Backend Trace');
    expect(vscode.window.createOutputChannel).toHaveBeenNthCalledWith(4, 'PBIR Score Diagnostics');
  });
});
