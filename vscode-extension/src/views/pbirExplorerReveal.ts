import * as vscode from 'vscode';
import type { ScorePanelNavigationTarget } from '../analyzer/contracts/scorePanel';
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
  return revealNavigationTargetInPbirExplorer({
    kind: 'visual',
    pageName,
    visualId,
    label: `Open ${visualId}`,
    reason: 'Reveal visual',
    supportState: 'direct',
  });
}

async function revealItem(item: PbirTreeItem): Promise<boolean> {
  if (!explorerView) {
    return false;
  }

  await explorerView.reveal(item, {
    select: true,
    focus: false,
    expand: true,
  });

  if (item.resourceUri) {
    await vscode.commands.executeCommand('vscode.open', item.resourceUri);
  }

  return true;
}

export async function revealNavigationTargetInPbirExplorer(
  target: ScorePanelNavigationTarget,
): Promise<boolean> {
  if (!explorerProvider || !explorerView) {
    return false;
  }

  switch (target.kind) {
    case 'visual': {
      if (!target.pageName || !target.visualId) {
        return false;
      }

      const visualItem = await explorerProvider.findVisualItem(target.pageName, target.visualId);
      return visualItem ? revealItem(visualItem) : false;
    }
    case 'page': {
      if (!target.pageName) {
        return false;
      }

      const pageItem = await explorerProvider.findPageItem(target.pageName);
      return pageItem ? revealItem(pageItem) : false;
    }
    case 'report': {
      const reportItem = await explorerProvider.findReportItem();
      if (!reportItem) {
        return false;
      }

      if (target.reportElement === 'themeJson') {
        const reportChildren = await explorerProvider.getChildren(reportItem);
        const themeItem = reportChildren.find((item) => item.kind === 'theme');
        return themeItem ? revealItem(themeItem) : false;
      }

      if (target.reportElement === 'pageJson' && target.pageName) {
        const pageItem = await explorerProvider.findPageItem(target.pageName);
        return pageItem ? revealItem(pageItem) : false;
      }

      return revealItem(reportItem);
    }
  }
}
