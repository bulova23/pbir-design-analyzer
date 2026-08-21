import * as vscode from 'vscode';
import { PbirTreeItem } from '../providers/PbirTreeProvider';
import { registerPbirExplorerReveal, revealNavigationTargetInPbirExplorer } from '../views/pbirExplorerReveal';

describe('pbirExplorerReveal', () => {
  const reveal = jest.fn();
  const pageItem = new PbirTreeItem(
    'Overview',
    'page',
    vscode.TreeItemCollapsibleState.None,
    '/tmp/definition/pages/OverviewPage/page.json',
  );
  const visualItem = new PbirTreeItem(
    'Hero KPI (card)',
    'visual',
    vscode.TreeItemCollapsibleState.None,
    '/tmp/definition/pages/OverviewPage/visuals/HeroKpi/visual.json',
  );

  const provider = {
    findPageItem: jest.fn(),
    findVisualItem: jest.fn(),
    findReportItem: jest.fn(),
    getChildren: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
    registerPbirExplorerReveal(provider as never, { reveal } as never);
  });

  it('reveals and opens the resolved page target file', async () => {
    provider.findPageItem.mockResolvedValue(pageItem);

    const resolved = await revealNavigationTargetInPbirExplorer({
      kind: 'page',
      pageName: 'Overview',
      label: 'Open Overview page',
      reason: 'This recommendation affects page framing.',
      supportState: 'direct',
    });

    expect(resolved).toBe(true);
    expect(reveal).toHaveBeenCalledWith(pageItem, {
      select: true,
      focus: false,
      expand: true,
    });
    expect(vscode.commands.executeCommand).toHaveBeenCalledWith('vscode.open', pageItem.resourceUri);
  });

  it('reveals and opens the resolved visual target file', async () => {
    provider.findVisualItem.mockResolvedValue(visualItem);

    const resolved = await revealNavigationTargetInPbirExplorer({
      kind: 'visual',
      pageName: 'Overview',
      visualId: 'HeroKpi',
      label: 'Open Hero KPI visual',
      reason: 'This recommendation is tied to the lead KPI.',
      supportState: 'direct',
    });

    expect(resolved).toBe(true);
    expect(reveal).toHaveBeenCalledWith(visualItem, {
      select: true,
      focus: false,
      expand: true,
    });
    expect(vscode.commands.executeCommand).toHaveBeenCalledWith('vscode.open', visualItem.resourceUri);
  });
});
