import * as path from 'path';
import * as vscode from 'vscode';
import { resolvePbirProjectPath } from '../analyzer/project/pathing';
import { LSPModelService } from '../services/lsp/LSPModelService';
import { registerPbirCommands, pbirTreeProvider } from './pbirCommands';
import { PbirConfigPanel } from '../views/PbirConfigPanel';

export const PBIR_ANALYZER_COMMANDS = {
  openProject: 'pbirAnalyzer.openProject',
  refreshReports: 'pbirAnalyzer.refreshReports',
  scoreReport: 'pbirAnalyzer.scoreReport',
  configureScoring: 'pbirAnalyzer.configureScoring',
  checkGovernance: 'pbirAnalyzer.checkGovernance',
  exportGovernanceReport: 'pbirAnalyzer.exportGovernanceReport',
} as const;

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
  getDotnetBridge: () => LSPModelService | undefined,
): void {
  registerPbirCommands(context, getDotnetBridge);

  const outputChannel = vscode.window.createOutputChannel('PBIR Design Analyzer');
  context.subscriptions.push(outputChannel);

  context.subscriptions.push(
    vscode.commands.registerCommand(PBIR_ANALYZER_COMMANDS.openProject, async () => {
      await openPbirProject(outputChannel);
    }),
  );

  context.subscriptions.push(
    vscode.commands.registerCommand(PBIR_ANALYZER_COMMANDS.configureScoring, async () => {
      outputChannel.appendLine(`[${new Date().toISOString()}] Opening scoring configuration`);
      await PbirConfigPanel.createOrShow(context, getDotnetBridge());
    }),
  );

  registerCommandAlias(context, PBIR_ANALYZER_COMMANDS.refreshReports, 'pbir.refreshTree');
  registerCommandAlias(context, PBIR_ANALYZER_COMMANDS.scoreReport, 'pbir.scoreReport');
  registerCommandAlias(context, PBIR_ANALYZER_COMMANDS.checkGovernance, 'pbir.governanceCheck');
  registerCommandAlias(context, PBIR_ANALYZER_COMMANDS.exportGovernanceReport, 'pbir.exportGovernanceReport');
}
