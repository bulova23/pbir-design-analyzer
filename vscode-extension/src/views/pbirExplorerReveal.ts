import * as vscode from 'vscode';
import { PbirTreeItem, PbirTreeProvider } from '../providers/PbirTreeProvider';

let explorerProvider: PbirTreeProvider | undefined;
let explorerView: vscode.TreeView<PbirTreeItem> | undefined;

export function registerPbirExplorerReveal(
  provider: PbirTreeProvider,
  treeView: vscode.TreeView<PbirTreeItem>,
): void {
  explorerProvider = provider;
  explorerView = treeView;
}

export async function revealVisualInPbirExplorer(
  pageName: string,
  visualId: string,
): Promise<boolean> {
  if (!explorerProvider || !explorerView) {
    return false;
  }

  const visualItem = await explorerProvider.findVisualItem(pageName, visualId);
  if (!visualItem) {
    return false;
  }

  await explorerView.reveal(visualItem, {
    select: true,
    focus: false,
    expand: true,
  });

  if (visualItem.resourceUri) {
    await vscode.commands.executeCommand('vscode.open', visualItem.resourceUri);
  }

  return true;
}
