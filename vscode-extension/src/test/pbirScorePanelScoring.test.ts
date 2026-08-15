import { buildPbirScoreRequest } from '../views/PbirScorePanel';
import type { DesignAnalyzerConfig } from '../analyzer/config/types';

describe('PBIR Optimization Report scoring boundary', () => {
  it('builds the legacy analyzer score request without entering the authoring RPC', () => {
    const config = { frameworks: [], governance: [], navigationScoring: { enabled: false, weight: 0 } } as DesignAnalyzerConfig;

    expect(buildPbirScoreRequest('/reports/Sales.Report', config, 'Overview')).toEqual({
      reportPath: '/reports/Sales.Report',
      config,
      pageName: 'Overview',
    });
  });
});
