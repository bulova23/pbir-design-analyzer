import { createScorePanelAuditWorkflowService } from '../views/scorePanelAuditWorkflowService';

describe('scorePanelAuditWorkflowService', () => {
  it('adds uploaded screenshots using the current page names and persists the session', async () => {
    const session = { pages: [], unmatchedCaptures: [] } as never;
    const showOpenDialog = jest.fn().mockResolvedValue([{ fsPath: '/tmp/overview.png' }]);
    const addCaptures = jest.fn().mockResolvedValue(undefined);
    const saveSession = jest.fn().mockResolvedValue(undefined);
    const postAuditState = jest.fn().mockResolvedValue(undefined);
    const loadSession = jest.fn().mockResolvedValue(session);

    const service = createScorePanelAuditWorkflowService({
      showOpenDialog,
      loadSession,
      addCaptures,
      saveSession,
      removeCapture: jest.fn(),
      assignCapture: jest.fn(),
      postMessage: jest.fn(),
      showErrorMessage: jest.fn(),
      unlinkStoredPath: jest.fn(),
      getReportPath: () => '/Reports/Sales.Report',
      context: {} as never,
      getCurrentResult: () => ({
        pageScores: [{ pageName: 'Overview' }, { pageName: 'Details' }],
      } as never),
      getAuditProvider: () => ({
        providerName: 'test-provider',
        isConfigured: jest.fn().mockResolvedValue(true),
        analyzeCapture: jest.fn(),
      } as never),
      getAuditSession: () => undefined,
      setAuditSession: jest.fn(),
    });

    await service.uploadScreenshots();

    expect(showOpenDialog).toHaveBeenCalled();
    expect(addCaptures).toHaveBeenCalledWith(
      {} as never,
      session,
      ['/tmp/overview.png'],
      ['Overview', 'Details'],
    );
    expect(saveSession).toHaveBeenCalledWith({} as never, session);
    expect(postAuditState).not.toHaveBeenCalled();
  });
});
