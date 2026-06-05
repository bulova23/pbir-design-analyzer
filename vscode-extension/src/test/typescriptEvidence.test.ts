import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { extractTypeScriptEvidence } from '../analyzer/fabric/review/typescriptEvidence';

describe('extractTypeScriptEvidence', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'fabric-typescript-evidence-'));
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('extracts layout, KPI, and dashboard composition evidence from analytics-facing TypeScript', () => {
    fs.mkdirSync(path.join(tempDir, 'src'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'src', 'ExecutiveDashboard.tsx'), `
      export function ExecutiveDashboard() {
        return (
          <DashboardLayout>
            <HeroSection>
              <KpiCard label="Revenue" />
              <KpiCard label="Margin" />
            </HeroSection>
            <TrendChart />
            <DetailGrid />
          </DashboardLayout>
        );
      }
    `);

    const evidence = extractTypeScriptEvidence(tempDir);

    expect(evidence.layoutPatterns).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          filePath: expect.stringContaining('ExecutiveDashboard.tsx'),
          summary: expect.stringContaining('DashboardLayout'),
        }),
      ]),
    );
    expect(evidence.kpiPatterns).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          summary: expect.stringContaining('KPI'),
        }),
      ]),
    );
    expect(evidence.compositionSignals).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          summary: expect.stringContaining('HeroSection'),
        }),
      ]),
    );
  });
});
