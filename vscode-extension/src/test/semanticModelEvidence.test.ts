import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { extractSemanticModelEvidence } from '../analyzer/fabric/review/semanticModelEvidence';

describe('extractSemanticModelEvidence', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'fabric-semantic-model-evidence-'));
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('extracts bounded analytics-facing semantic model usage evidence', async () => {
    fs.mkdirSync(path.join(tempDir, 'src', 'data'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'src', 'data', 'queries.ts'), `
      export const revenueQuery = {
        semanticModel: 'SalesModel',
        measure: 'Revenue',
        dimension: 'Region',
      };

      export const marginQuery = {
        dataset: 'FinanceDataset',
        metric: 'MarginPct',
        dimension: 'Segment',
      };
    `);

    const evidence = await extractSemanticModelEvidence(tempDir);

    expect(evidence.signals).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          filePath: expect.stringContaining('queries.ts'),
          summary: expect.stringContaining('SalesModel'),
        }),
        expect.objectContaining({
          summary: expect.stringContaining('FinanceDataset'),
        }),
      ]),
    );
  });

  it('returns no evidence when no semantic model artifacts are present', async () => {
    fs.mkdirSync(path.join(tempDir, 'src'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'src', 'app.tsx'), 'export const appShell = true;');

    const evidence = await extractSemanticModelEvidence(tempDir);

    expect(evidence.signals).toEqual([]);
  });

  it('caps extracted evidence to a bounded number of signals', async () => {
    fs.mkdirSync(path.join(tempDir, 'src', 'data'), { recursive: true });
    fs.writeFileSync(
      path.join(tempDir, 'src', 'data', 'queries.ts'),
      Array.from({ length: 12 }, (_, index) => `export const query${index} = { semanticModel: 'Model${index}', measure: 'Metric${index}' };`).join('\n'),
    );

    const evidence = await extractSemanticModelEvidence(tempDir);

    expect(evidence.signals.length).toBeLessThanOrEqual(6);
  });
});
