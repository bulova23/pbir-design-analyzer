import * as vscode from 'vscode';
import { detectWorkspacePbirProjectPath } from './analyzer/project/discovery';
import { registerCommands } from './commands/register';
import { pbirTreeProvider } from './commands/pbirCommands';
import { initializeDesignAnalyzerConfig } from './analyzer/config/store';
import { createAnalyzerBackendClient, stopAnalyzerBackendClient } from './languageServer/analyzerBackendClient';
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

  backendClient = createAnalyzerBackendClient(context);
  if (!backendClient) {
    const errorMessage = 'Failed to create the analyzer backend client. Packaged PBIR backend binary not found.';
    extensionOutput.appendLine(errorMessage);
    vscode.window.showErrorMessage(errorMessage);
    daemonStatusBar.text = '$(error) PBIR Design Analyzer: Backend missing';
    daemonStatusBar.tooltip = 'PBIR Design Analyzer backend binary is missing';
    extensionOutput.appendLine('[Extension] Continuing in local-only mode');
    extensionOutput.appendLine('PBIR Design Analyzer activated');
    return;
  }

  try {
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
          daemonStatusBar.text = '$(error) PBIR Design Analyzer: Backend error';
          daemonStatusBar.tooltip = 'PBIR Design Analyzer backend failed';
          break;
        case BridgeState.UNINITIALIZED:
          daemonStatusBar.text = '$(warning) PBIR Design Analyzer: Backend stopped';
          daemonStatusBar.tooltip = 'PBIR Design Analyzer backend is not initialized';
          break;
      }
    });

    await bridgeService.initialize(backendClient);
    pbirTreeProvider?.setBridgeService(bridgeService);
    pbirTreeProvider?.refresh();
    extensionOutput.appendLine('[Extension] Analyzer backend initialized successfully');
  } catch (error) {
    extensionOutput.appendLine(`[Extension] Failed to initialize analyzer backend: ${error}`);
    vscode.window.showWarningMessage(
      'PBIR analyzer backend failed to start. Report analysis commands will be unavailable.',
    );
    if (daemonStatusBar) {
      daemonStatusBar.text = '$(error) PBIR Design Analyzer: Backend error';
      daemonStatusBar.tooltip = 'PBIR Design Analyzer backend failed to initialize';
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
