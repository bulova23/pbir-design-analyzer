import * as vscode from 'vscode';

jest.mock('../views/pbirExplorerReveal', () => ({
  revealNavigationTargetInPbirExplorer: jest.fn(),
  revealVisualInPbirExplorer: jest.fn(),
}));

import { createScorePanelMessageRouter } from '../views/scorePanelMessageRouter';
import { withScorePanelEnvelope } from '../views/scorePanelProtocol';
import { revealNavigationTargetInPbirExplorer } from '../views/pbirExplorerReveal';

describe('PbirScorePanel navigation messages', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('routes navigateToTarget messages through the shared target reveal helper', async () => {
    (revealNavigationTargetInPbirExplorer as jest.Mock).mockResolvedValue(true);
    const router = createScorePanelMessageRouter({
      getPageCount: () => 0,
      onReady: jest.fn(),
      onRefresh: jest.fn(),
      onSelectTab: jest.fn(),
      onSetIntentFeedback: jest.fn(),
      onUploadScreenshots: jest.fn(),
      onAttachScreenshot: jest.fn(),
      onRemoveScreenshot: jest.fn(),
      onAssignCapture: jest.fn(),
      onAnalyzeCapture: jest.fn(),
      onExportReviewWorkflow: jest.fn(),
      onSetReviewPacketPreviewProfile: jest.fn(),
      onSetReviewPacketPreviewTemplateVariant: jest.fn(),
      onOpenReviewPacketPreview: jest.fn(),
      onToggleFixOpportunitySelection: jest.fn(),
      onPreviewSelectedFixOpportunities: jest.fn(),
      onApproveSelectedFixOpportunities: jest.fn(),
      onApplySelectedFixOpportunities: jest.fn(),
      onRollbackFixSession: jest.fn(),
      onRegenerateFixOpportunities: jest.fn(),
      onApproveFixOpportunity: jest.fn(),
      onApplyFixOpportunity: jest.fn(),
      onRollbackFixOpportunity: jest.fn(),
      onOpenSettings: jest.fn(),
    });

    await router.route(withScorePanelEnvelope({
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
    const router = createScorePanelMessageRouter({
      getPageCount: () => 0,
      onReady: jest.fn(),
      onRefresh: jest.fn(),
      onSelectTab: jest.fn(),
      onSetIntentFeedback: jest.fn(),
      onUploadScreenshots: jest.fn(),
      onAttachScreenshot: jest.fn(),
      onRemoveScreenshot: jest.fn(),
      onAssignCapture: jest.fn(),
      onAnalyzeCapture: jest.fn(),
      onExportReviewWorkflow: jest.fn(),
      onSetReviewPacketPreviewProfile: jest.fn(),
      onSetReviewPacketPreviewTemplateVariant: jest.fn(),
      onOpenReviewPacketPreview: jest.fn(),
      onToggleFixOpportunitySelection: jest.fn(),
      onPreviewSelectedFixOpportunities: jest.fn(),
      onApproveSelectedFixOpportunities: jest.fn(),
      onApplySelectedFixOpportunities: jest.fn(),
      onRollbackFixSession: jest.fn(),
      onRegenerateFixOpportunities: jest.fn(),
      onApproveFixOpportunity: jest.fn(),
      onApplyFixOpportunity: jest.fn(),
      onRollbackFixOpportunity: jest.fn(),
      onOpenSettings: jest.fn(),
    });

    await router.route(withScorePanelEnvelope({
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

  it('propagates a reveal-implementation exception out of route() instead of swallowing it', async () => {
    // The router itself has no try/catch around handler dispatch — that safety net lives in
    // PbirScorePanel.handleMessage, which is fire-and-forget from VS Code's own
    // webview.onDidReceiveMessage callback. If route() ever silently absorbed a handler exception
    // here, that safety net would have nothing to catch, and a real failure (RPC timeout, a moved
    // file, a stale tree) would go back to being an invisible no-op click for the user.
    (revealNavigationTargetInPbirExplorer as jest.Mock).mockRejectedValue(new Error('tree provider RPC timed out'));
    const router = createScorePanelMessageRouter({
      getPageCount: () => 0,
      onReady: jest.fn(),
      onRefresh: jest.fn(),
      onSelectTab: jest.fn(),
      onSetIntentFeedback: jest.fn(),
      onUploadScreenshots: jest.fn(),
      onAttachScreenshot: jest.fn(),
      onRemoveScreenshot: jest.fn(),
      onAssignCapture: jest.fn(),
      onAnalyzeCapture: jest.fn(),
      onExportReviewWorkflow: jest.fn(),
      onSetReviewPacketPreviewProfile: jest.fn(),
      onSetReviewPacketPreviewTemplateVariant: jest.fn(),
      onOpenReviewPacketPreview: jest.fn(),
      onToggleFixOpportunitySelection: jest.fn(),
      onPreviewSelectedFixOpportunities: jest.fn(),
      onApproveSelectedFixOpportunities: jest.fn(),
      onApplySelectedFixOpportunities: jest.fn(),
      onRollbackFixSession: jest.fn(),
      onRegenerateFixOpportunities: jest.fn(),
      onApproveFixOpportunity: jest.fn(),
      onApplyFixOpportunity: jest.fn(),
      onRollbackFixOpportunity: jest.fn(),
      onOpenSettings: jest.fn(),
    });

    await expect(router.route(withScorePanelEnvelope({
      type: 'navigateToTarget',
      target: {
        kind: 'page',
        pageName: 'Overview',
        label: 'Open Overview page',
        reason: 'This recommendation affects page framing.',
        supportState: 'direct',
      },
    }))).rejects.toThrow('tree provider RPC timed out');
  });
});
