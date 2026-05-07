import * as vscode from 'vscode';
import { resolvePbirProjectPath } from './pathing';

export async function detectWorkspacePbirProjectPath(): Promise<string | undefined> {
  const workspaceFolders = vscode.workspace.workspaceFolders ?? [];
  for (const folder of workspaceFolders) {
    const projectPath = resolvePbirProjectPath(folder.uri.fsPath);
    if (projectPath) {
      return projectPath;
    }
  }

  const pbipFiles = await vscode.workspace.findFiles('**/*.pbip', '**/node_modules/**', 1);
  const pbipPath = pbipFiles[0] ? resolvePbirProjectPath(pbipFiles[0].fsPath) : undefined;
  if (pbipPath) {
    return pbipPath;
  }

  const reportDefinitionFiles = await vscode.workspace.findFiles(
    '**/*.Report/definition.pbir',
    '**/node_modules/**',
    1,
  );
  const reportPath = reportDefinitionFiles[0]
    ? resolvePbirProjectPath(reportDefinitionFiles[0].fsPath)
    : undefined;
  if (reportPath) {
    return reportPath;
  }

  const reportJsonFiles = await vscode.workspace.findFiles(
    '**/*.Report/definition/report.json',
    '**/node_modules/**',
    1,
  );
  return reportJsonFiles[0]
    ? resolvePbirProjectPath(reportJsonFiles[0].fsPath)
    : undefined;
}
