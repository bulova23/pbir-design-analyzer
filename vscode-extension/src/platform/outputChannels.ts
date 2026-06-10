import * as vscode from 'vscode';

const CHANNEL_NAMES = {
  extension: 'PBIR Design Analyzer',
  backend: 'PBIR Design Analyzer Backend',
  backendTrace: 'PBIR Design Analyzer Backend Trace',
  diagnostics: 'PBIR Score Diagnostics',
} as const;

let extensionOutput: vscode.OutputChannel | undefined;
let backendOutput: vscode.OutputChannel | undefined;
let backendTraceOutput: vscode.OutputChannel | undefined;
let diagnosticsOutput: vscode.OutputChannel | undefined;

export function getExtensionOutputChannel(): vscode.OutputChannel {
  extensionOutput ??= vscode.window.createOutputChannel(CHANNEL_NAMES.extension);
  return extensionOutput;
}

export function getBackendOutputChannel(): vscode.OutputChannel {
  backendOutput ??= vscode.window.createOutputChannel(CHANNEL_NAMES.backend);
  return backendOutput;
}

export function getBackendTraceOutputChannel(): vscode.OutputChannel {
  backendTraceOutput ??= vscode.window.createOutputChannel(CHANNEL_NAMES.backendTrace);
  return backendTraceOutput;
}

export function getDiagnosticsOutputChannel(): vscode.OutputChannel {
  diagnosticsOutput ??= vscode.window.createOutputChannel(CHANNEL_NAMES.diagnostics);
  return diagnosticsOutput;
}

export function registerSharedOutputChannels(context: vscode.ExtensionContext): void {
  context.subscriptions.push(
    getExtensionOutputChannel(),
    getBackendOutputChannel(),
    getBackendTraceOutputChannel(),
    getDiagnosticsOutputChannel(),
  );
}

export function resetOutputChannelsForTesting(): void {
  extensionOutput = undefined;
  backendOutput = undefined;
  backendTraceOutput = undefined;
  diagnosticsOutput = undefined;
}
