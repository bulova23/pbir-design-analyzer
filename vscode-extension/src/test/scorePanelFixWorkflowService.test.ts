import { createScorePanelFixWorkflowService } from '../views/scorePanelFixWorkflowService';

describe('scorePanelFixWorkflowService', () => {
  it('reports the existing preview warning when no fix opportunities are selected', async () => {
    const postCurrentScoreState = jest.fn().mockResolvedValue(undefined);
    const setFixWorkflowMessage = jest.fn();
    const service = createScorePanelFixWorkflowService({
      evaluateFixOpportunityCompatibility: jest.fn(),
      applyFixOpportunityBatch: jest.fn(),
      applyFixOpportunity: jest.fn(),
      rollbackFixOpportunity: jest.fn(),
      rollbackFixSession: jest.fn(),
      evaluateFixOutcome: jest.fn(),
      summarizeBatchFixOutcomes: jest.fn(),
      createFixApplySessionRecord: jest.fn(),
      recordFixSessionRollback: jest.fn(),
      markFixSessionRegenerated: jest.fn(),
      refresh: jest.fn(),
      postCurrentScoreState,
      showWarningMessage: jest.fn(),
      getCurrentResult: () => ({ fixOpportunities: [] } as any),
      getFixOpportunityHistory: () => new Map(),
      getSelectedFixOpportunityIds: () => [],
      setSelectedFixOpportunityIds: jest.fn(),
      getFixSelectionApprovalState: () => 'NeedsPreview',
      setFixSelectionApprovalState: jest.fn(),
      getFixApplySessions: () => [],
      setFixApplySessions: jest.fn(),
      getFixWorkflowMessage: () => undefined,
      setFixWorkflowMessage,
    });

    await service.previewSelectedFixOpportunities();

    expect(setFixWorkflowMessage).toHaveBeenCalledWith(
      'Select one or more opportunities before previewing fixes.',
    );
    expect(postCurrentScoreState).toHaveBeenCalled();
  });
});
