import * as path from 'path';
import * as vscode from 'vscode';
import { resolvePbirProjectPath } from '../analyzer/project/pathing';
import { AnalyzerBridgeService } from '../services/rpc/AnalyzerBridgeService';
import { registerPbirCommands, pbirTreeProvider } from './pbirCommands';
import { PbirConfigPanel } from '../views/PbirConfigPanel';
import { LEGACY_PBIR_COMMAND_ALIASES, PBIR_COMMANDS } from '../platform/extensionIds';
import { getExtensionOutputChannel } from '../platform/outputChannels';
import { AnalyzerHandoffService } from '../design-studio/materialization/analyzerHandoffService';
import type { MaterializedSurfaceCandidate } from '../design-studio/contracts/designStudioModels';

export { PBIR_COMMANDS };

async function promptForPbirProjectPath(): Promise<string | undefined> {
  const selection = await vscode.window.showOpenDialog({
    canSelectFiles: true,
    canSelectFolders: true,
    canSelectMany: false,
    title: 'Open PBIP Project',
    openLabel: 'Open Project',
    filters: {
      'Power BI Projects': ['pbip'],
    },
  });

  const selectionPath = selection?.[0]?.fsPath;
  if (!selectionPath) {
    return undefined;
  }

  const projectPath = resolvePbirProjectPath(selectionPath);
  if (projectPath) {
    return projectPath;
  }

  vscode.window.showErrorMessage(
    'Select a .pbip file, a PBIP project folder, or a .Report folder with a PBIR definition.',
  );
  return undefined;
}

async function openPbirProject(outputChannel: vscode.OutputChannel): Promise<void> {
  const projectPath = await promptForPbirProjectPath();
  if (!projectPath) {
    return;
  }

  pbirTreeProvider?.setProjectPath(projectPath);
  outputChannel.appendLine(`[${new Date().toISOString()}] Active PBIR project: ${projectPath}`);
  vscode.window.showInformationMessage(`Opened PBIP project: ${path.basename(projectPath)}`);
}

function registerCommandAlias(
  context: vscode.ExtensionContext,
  aliasCommand: string,
  targetCommand: string,
): void {
  context.subscriptions.push(
    vscode.commands.registerCommand(aliasCommand, async (...args: unknown[]) => {
      await vscode.commands.executeCommand(targetCommand, ...args);
    }),
  );
}

export function registerCommands(
  context: vscode.ExtensionContext,
  getDotnetBridge: () => AnalyzerBridgeService | undefined,
  getAnalyzerHandoffService?: () => AnalyzerHandoffService,
): void {
  registerPbirCommands(context, getDotnetBridge);

  const outputChannel = getExtensionOutputChannel();

  context.subscriptions.push(
    vscode.commands.registerCommand(PBIR_COMMANDS.openProject, async () => {
      await openPbirProject(outputChannel);
    }),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand(PBIR_COMMANDS.configureScoring, async () => {
      outputChannel.appendLine(`[${new Date().toISOString()}] Opening scoring configuration`);
      await PbirConfigPanel.createOrShow(context, getDotnetBridge());
    }),
  );

  if (getAnalyzerHandoffService) {
    context.subscriptions.push(
      vscode.commands.registerCommand(
        PBIR_COMMANDS.openAnalyzerWorkspaceHandoff,
        async (candidate: MaterializedSurfaceCandidate) => {
          const result = await getAnalyzerHandoffService().handoffCandidate(candidate);
          if (!result.ok) {
            void vscode.window.showWarningMessage(result.diagnostics.join(' '));
          }
        },
      ),
    );
  }

  for (const [legacyCommand, canonicalCommand] of Object.entries(LEGACY_PBIR_COMMAND_ALIASES)) {
    registerCommandAlias(context, legacyCommand, canonicalCommand);
  }
}
