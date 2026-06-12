import * as vscode from 'vscode';

jest.mock('../views/pbirExplorerReveal', () => ({
  revealNavigationTargetInPbirExplorer: jest.fn(),
  revealVisualInPbirExplorer: jest.fn(),
}));

import { PbirScorePanel } from '../views/PbirScorePanel';
import { withScorePanelEnvelope } from '../views/scorePanelProtocol';
import { revealNavigationTargetInPbirExplorer } from '../views/pbirExplorerReveal';

describe('PbirScorePanel navigation messages', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('routes navigateToTarget messages through the shared target reveal helper', async () => {
    (revealNavigationTargetInPbirExplorer as jest.Mock).mockResolvedValue(true);
    const panel = Object.create(PbirScorePanel.prototype) as any;

    await panel.handleMessage(withScorePanelEnvelope({
      type: 'navigateToTarget',
      target: {
        kind: 'page',
        pageName: 'Overview',
        label: 'Open Overview page',
        reason: 'This recommendation affects page framing.',
        supportState: 'direct',
      },
    }));

    expect(revealNavigationTargetInPbirExplorer).toHaveBeenCalledWith({
      kind: 'page',
      pageName: 'Overview',
      label: 'Open Overview page',
      reason: 'This recommendation affects page framing.',
      supportState: 'direct',
    });
    expect(vscode.window.showWarningMessage).not.toHaveBeenCalled();
  });

  it('shows a non-blocking warning when a navigation target cannot be resolved', async () => {
    (revealNavigationTargetInPbirExplorer as jest.Mock).mockResolvedValue(false);
    const panel = Object.create(PbirScorePanel.prototype) as any;

    await panel.handleMessage(withScorePanelEnvelope({
      type: 'navigateToTarget',
      target: {
        kind: 'visual',
        pageName: 'Overview',
        visualId: 'hero-kpi',
        label: 'Open hero KPI visual',
        reason: 'This recommendation is tied to the lead KPI.',
        supportState: 'direct',
      },
    }));

    expect(vscode.window.showWarningMessage).toHaveBeenCalledWith(
      "Could not navigate to 'Open hero KPI visual'. This recommendation is tied to the lead KPI.",
    );
  });
});
