import * as vscode from 'vscode';
import { detectWorkspacePbirProjectPath } from './analyzer/project/discovery';
import { registerCommands } from './commands/register';
import { pbirTreeProvider } from './commands/pbirCommands';
import { initializeDesignAnalyzerConfig } from './analyzer/config/store';
import {
  createAnalyzerBackendClient,
  describeBackendStartupFailure,
  formatBackendLaunchDiagnostics,
  getRecordedBackendIssue,
  getBackendRuntimeDescriptor,
  recordBackendIssue,
  runBackendLaunchPreflight,
  stopAnalyzerBackendClient,
} from './languageServer/analyzerBackendClient';
import { AnalyzerBridgeService, BridgeState } from './services/rpc/AnalyzerBridgeService';
import { LanguageClient } from 'vscode-languageclient/node';
import { telemetry } from './telemetry/reporter';

let bridgeService: AnalyzerBridgeService | undefined;
let daemonStatusBar: vscode.StatusBarItem | undefined;
let backendClient: LanguageClient | undefined;
let extensionOutput: vscode.OutputChannel | undefined;

export async function activate(context: vscode.ExtensionContext) {
  extensionOutput = vscode.window.createOutputChannel('PBIR Design Analyzer');
  context.subscriptions.push(extensionOutput);
  extensionOutput.appendLine('PBIR Design Analyzer is activating...');
  extensionOutput.appendLine(`Extension id: ${context.extension.id}`);
  extensionOutput.appendLine(`Extension path: ${context.extensionPath}`);

  telemetry.initialize(context);
  context.subscriptions.push({ dispose: () => telemetry.dispose() });

  await initializeDesignAnalyzerConfig(context);

  daemonStatusBar = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
  daemonStatusBar.text = '$(sync~spin) PBIR Design Analyzer: Starting backend';
  daemonStatusBar.tooltip = 'PBIR Design Analyzer backend is starting';
  daemonStatusBar.show();
  context.subscriptions.push(daemonStatusBar);

  registerCommands(context, () => bridgeService);
  await autoLoadPbipProject(extensionOutput);

  const runtimeDescriptorResult = getBackendRuntimeDescriptor();
  const runtimeDescriptor = 'descriptor' in runtimeDescriptorResult ? runtimeDescriptorResult.descriptor : undefined;
  const backendClientResult = createAnalyzerBackendClient(context);
  backendClient = backendClientResult.client;
  if (!backendClient) {
    const message = backendClientResult.issue?.message
      ?? 'PBIR Design Analyzer backend is unavailable. The extension will continue in degraded mode.';
    extensionOutput.appendLine(message);
    if (backendClientResult.issue?.detail) {
      extensionOutput.appendLine(backendClientResult.issue.detail);
    }
    vscode.window.showWarningMessage(message);
    daemonStatusBar.text = '$(warning) PBIR Design Analyzer: Degraded mode';
    daemonStatusBar.tooltip = message;
    extensionOutput.appendLine('[Extension] Continuing in local-only mode');
    extensionOutput.appendLine('PBIR Design Analyzer activated');
    pbirTreeProvider?.setBridgeService(undefined);
    pbirTreeProvider?.refresh();
    return;
  }

  try {
    let startupDiagnostics = backendClientResult.diagnostics;
    if (startupDiagnostics) {
      extensionOutput.appendLine('[Extension] Backend launch diagnostics:');
      extensionOutput.appendLine(formatBackendLaunchDiagnostics(startupDiagnostics));
      startupDiagnostics = await runBackendLaunchPreflight(startupDiagnostics);
      extensionOutput.appendLine('[Extension] Backend launch preflight:');
      extensionOutput.appendLine(formatBackendLaunchDiagnostics(startupDiagnostics));

      if (startupDiagnostics.preflight?.exitedEarly) {
        const issue = describeBackendStartupFailure(
          new Error('Backend exited during launch preflight.'),
          startupDiagnostics,
        );
        backendClient = undefined;
        recordBackendIssue(issue);
        extensionOutput.appendLine(`[Extension] Backend preflight failed: ${issue.message}`);
        if (issue.detail) {
          extensionOutput.appendLine(issue.detail);
        }
        vscode.window.showWarningMessage(issue.message);
        daemonStatusBar.text = '$(warning) PBIR Design Analyzer: Degraded mode';
        daemonStatusBar.tooltip = issue.message;
        pbirTreeProvider?.setBridgeService(undefined);
        pbirTreeProvider?.refresh();
        extensionOutput.appendLine('PBIR Design Analyzer activated');
        return;
      }
    }

    extensionOutput.appendLine('[Extension] Starting analyzer backend client...');
    await backendClient.start();
    context.subscriptions.push(backendClient);

    bridgeService = AnalyzerBridgeService.getInstance();
    bridgeService.onStateChange((state: BridgeState) => {
      if (!daemonStatusBar) {
        return;
      }

      switch (state) {
        case BridgeState.STARTING:
          daemonStatusBar.text = '$(sync~spin) PBIR Design Analyzer: Starting backend';
          daemonStatusBar.tooltip = 'PBIR Design Analyzer backend is starting';
          break;
        case BridgeState.READY:
          daemonStatusBar.text = '$(check) PBIR Design Analyzer: Ready';
          daemonStatusBar.tooltip = 'PBIR Design Analyzer backend is ready';
          break;
        case BridgeState.ERROR:
          const recordedIssue = getRecordedBackendIssue();
          const degradedMessage = recordedIssue?.message
            ?? 'PBIR Design Analyzer backend stopped. Local tree browsing remains available.';
          daemonStatusBar.text = '$(warning) PBIR Design Analyzer: Degraded mode';
          daemonStatusBar.tooltip = degradedMessage;
          pbirTreeProvider?.setBridgeService(undefined);
          pbirTreeProvider?.refresh();
          void vscode.window.showWarningMessage(
            recordedIssue?.message
              ?? 'PBIR Design Analyzer backend stopped. Scoring and governance commands are unavailable until the extension is reloaded. Local tree browsing remains available.',
          );
          break;
        case BridgeState.UNINITIALIZED:
          daemonStatusBar.text = '$(warning) PBIR Design Analyzer: Backend stopped';
          daemonStatusBar.tooltip = 'PBIR Design Analyzer backend is not initialized';
          break;
      }
    });

    await bridgeService.initialize(backendClient);
    recordBackendIssue(undefined);
    pbirTreeProvider?.setBridgeService(bridgeService);
    pbirTreeProvider?.refresh();
    extensionOutput.appendLine('[Extension] Analyzer backend initialized successfully');
  } catch (error) {
    const issue = describeBackendStartupFailure(error, backendClientResult.diagnostics ?? runtimeDescriptor);
    backendClient = undefined;
    recordBackendIssue(issue);
    extensionOutput.appendLine(`[Extension] Failed to initialize analyzer backend: ${issue.message}`);
    if (issue.detail) {
      extensionOutput.appendLine(issue.detail);
    }
    vscode.window.showWarningMessage(issue.message);
    if (daemonStatusBar) {
      daemonStatusBar.text = '$(warning) PBIR Design Analyzer: Degraded mode';
      daemonStatusBar.tooltip = issue.message;
    }
    pbirTreeProvider?.setBridgeService(undefined);
    pbirTreeProvider?.refresh();
  }

  extensionOutput.appendLine('PBIR Design Analyzer activated');
}

async function autoLoadPbipProject(outputChannel: vscode.OutputChannel | undefined): Promise<void> {
  if (!vscode.workspace.workspaceFolders || vscode.workspace.workspaceFolders.length === 0) {
    outputChannel?.appendLine('[Extension] No workspace folder open, skipping PBIP auto-open');
    return;
  }

  const projectPath = await detectWorkspacePbirProjectPath();
  if (!projectPath) {
    outputChannel?.appendLine('[Extension] No PBIP/PBIR project found in the active workspace');
    return;
  }

  pbirTreeProvider?.setProjectPath(projectPath);
  outputChannel?.appendLine(`[Extension] Auto-opened PBIR project: ${projectPath}`);
}

export async function deactivate() {
  if (extensionOutput) {
    extensionOutput.appendLine('PBIR Design Analyzer deactivating');
  }

  if (bridgeService) {
    await bridgeService.shutdown();
  }

  if (backendClient) {
    await stopAnalyzerBackendClient(backendClient);
  }
}
