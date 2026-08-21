import { createScorePanelStateService } from '../views/scorePanelStateService';

describe('scorePanelStateService', () => {
  it('clamps the selected page index against the current result page count', () => {
    const service = createScorePanelStateService();
    service.setSelectedPageIndex(9, 2);

    expect(service.getSelectedPageIndex()).toBe(2);
  });

  it('resets score-bound state for a Design Studio handoff shell', () => {
    const service = createScorePanelStateService();
    service.setSelectedPageIndex(2, 3);
    service.setCurrentResult({ compositeScore: 85 } as never);
    service.setSavedConfig({ includeInsights: true } as never);
    service.setPendingMessages([{ type: 'loading' } as never]);

    service.resetForHandoff();

    expect(service.getSelectedPageIndex()).toBe(0);
    expect(service.getCurrentResult()).toBeUndefined();
    expect(service.getSavedConfig()).toBeNull();
    expect(service.getPendingMessages()).toEqual([]);
  });
});
