import * as vscode from 'vscode';

jest.mock('../views/pbirExplorerReveal', () => ({
  revealNavigationTargetInPbirExplorer: jest.fn(),
  revealVisualInPbirExplorer: jest.fn(),
}));

import { createScorePanelMessageRouter } from '../views/scorePanelMessageRouter';
import { withScorePanelEnvelope } from '../views/scorePanelProtocol';
import { revealNavigationTargetInPbirExplorer } from '../views/pbirExplorerReveal';

describe('scorePanelMessageRouter', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('routes navigateToTarget messages through the shared target reveal helper', async () => {
    (revealNavigationTargetInPbirExplorer as jest.Mock).mockResolvedValue(true);
    const onSelectTab = jest.fn();
    const onRefresh = jest.fn();
    const onReady = jest.fn();

    const router = createScorePanelMessageRouter({
      getPageCount: () => 3,
      onReady,
      onRefresh,
      onSelectTab,
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
    expect(onReady).not.toHaveBeenCalled();
    expect(onRefresh).not.toHaveBeenCalled();
    expect(onSelectTab).not.toHaveBeenCalled();
  });

  it('clamps selectTab messages against the current page count before dispatching', async () => {
    const onSelectTab = jest.fn();
    const router = createScorePanelMessageRouter({
      getPageCount: () => 2,
      onReady: jest.fn(),
      onRefresh: jest.fn(),
      onSelectTab,
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
      type: 'selectTab',
      pageIndex: 8,
    }));

    expect(onSelectTab).toHaveBeenCalledWith(2);
  });

  it('builds the existing handoff warning message without changing the contract', () => {
    expect(createScorePanelMessageRouter.buildHandoffMessage({
      candidateId: 'candidate-42',
      analyzerId: 'pbirAnalyzer',
      analyzerProfileId: 'fabricAppQuality',
    })).toBe(
      'Analyzer Workspace opened from Design Studio handoff for candidate candidate-42. Analysis has not started. Run Retry when you are ready to start pbirAnalyzer with profile fabricAppQuality.',
    );
  });
});
