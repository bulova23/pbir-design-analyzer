import {
  buildPbirOptimizationAnalyzeRequest,
  executePbirOptimizationScore,
} from '../views/PbirScorePanel';
import type { DesignAnalyzerConfig } from '../analyzer/config/types';

describe('PBIR Optimization Report scoring boundary', () => {
  it('builds a direct Analyze(reportDirectory) request without Import or a legacy score route', () => {
    const config = { frameworks: [], governance: [], navigationScoring: { enabled: false, weight: 0 } } as DesignAnalyzerConfig;

    expect(buildPbirOptimizationAnalyzeRequest('/reports/Sales.Report', config, 'Overview')).toEqual({
      schemaVersion: 'pbir-authoring-rpc/v1',
      operation: 'analyze',
      analyze: { reportDirectory: '/reports/Sales.Report', config, pageName: 'Overview' },
    });
  });

  it('executes the user-visible scoring workflow as a single Analyze(reportDirectory) call', async () => {
    const config = { frameworks: [], governance: [], navigationScoring: { enabled: false, weight: 0 } } as DesignAnalyzerConfig;
    const score = { CompositeScore: 88 } as never;
    const executeAuthoringRequest = jest.fn()
      .mockResolvedValueOnce({ succeeded: true, analyzer: { result: score } });
    const diagnostics: string[] = [];

    const response = await executePbirOptimizationScore(
      { executeAuthoringRequest },
      '/reports/Sales.Report',
      config,
      'Overview',
      (message) => diagnostics.push(message),
    );

    expect(response.analyzer?.result).toBe(score);
    expect(executeAuthoringRequest).toHaveBeenCalledTimes(1);
    expect(executeAuthoringRequest).toHaveBeenNthCalledWith(1, buildPbirOptimizationAnalyzeRequest('/reports/Sales.Report', config, 'Overview'));
    expect(diagnostics).toEqual([
      expect.stringContaining('rpcRoute=pbir/authoring schemaVersion=pbir-authoring-rpc/v1 operation=Analyze sourceKind=ReportReference reportPathPresent=true snapshotHandlePresent=false artifactHandlePresent=false authoringRequestPresent=false'),
    ]);
  });

  it('surfaces a failed Analyze response without attempting a second request', async () => {
    const config = { frameworks: [], governance: [], navigationScoring: { enabled: false, weight: 0 } } as DesignAnalyzerConfig;
    const executeAuthoringRequest = jest.fn()
      .mockResolvedValueOnce({ succeeded: false, error: { category: 'importFailed', code: 'PBIR-RPC-ANALYZE-001', summary: 'The report could not be resolved for analysis.' } });

    const response = await executePbirOptimizationScore(
      { executeAuthoringRequest },
      '/reports/Sales.Report',
      config,
    );

    expect(response.succeeded).toBe(false);
    expect(response.error?.summary).toBe('The report could not be resolved for analysis.');
    expect(executeAuthoringRequest).toHaveBeenCalledTimes(1);
  });
});
