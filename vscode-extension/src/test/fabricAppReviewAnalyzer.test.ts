import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { AnalyzableSurface } from '../analyzer/surfaces/types';
import { reviewFabricAppSurface } from '../analyzer/fabric/review/fabricAppReviewAnalyzer';

function buildFabricSurface(repoPath: string): AnalyzableSurface {
  return {
    surfaceType: 'fabricApp',
    displayName: 'Executive Fabric App',
    sourceLocation: repoPath,
    availableEvidenceKinds: ['typescriptLayout', 'navigation', 'designToken', 'screenshot', 'semanticModel'],
    availableAnalyzerTypes: ['fabricAppReview'],
    availableAnalyzerProfiles: ['default', 'fabricAppQuality'],
    analysisCapabilities: ['findings', 'evidence', 'remediation', 'governanceSignals'],
    governanceCapabilities: ['analytics'],
  };
}

describe('reviewFabricAppSurface', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'fabric-review-analyzer-'));
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('orchestrates evidence extraction and returns advisory findings with remediation guidance', () => {
    fs.mkdirSync(path.join(tempDir, 'src', 'routes'), { recursive: true });
    fs.mkdirSync(path.join(tempDir, 'src', 'data'), { recursive: true });
    fs.mkdirSync(path.join(tempDir, 'src', 'theme'), { recursive: true });
    fs.mkdirSync(path.join(tempDir, 'screenshots'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'package.json'), JSON.stringify({ name: 'executive-fabric-app' }));
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
    fs.writeFileSync(path.join(tempDir, 'src', 'routes', 'index.tsx'), `
      export const routes = [
        { path: '/overview', label: 'Executive Overview' },
        { path: '/detail', label: 'Detail' },
      ];
    `);
    fs.writeFileSync(path.join(tempDir, 'src', 'theme', 'tokens.css'), `
      :root {
        --color-brand: #0055AA;
        --space-md: 16px;
      }
    `);
    fs.writeFileSync(path.join(tempDir, 'src', 'data', 'queries.ts'), `
      export const revenueQuery = {
        semanticModel: 'SalesModel',
        measure: 'Revenue',
        dimension: 'Region',
      };
    `);
    fs.writeFileSync(path.join(tempDir, 'src', 'ExecutiveCard.tsx'), `
      export function ExecutiveCard() {
        return <div style={{ color: '#ff0000', padding: '24px' }}>Revenue</div>;
      }
    `);
    fs.writeFileSync(path.join(tempDir, 'screenshots', '01 Executive Overview - Default.png'), 'fake-image');

    const result = reviewFabricAppSurface(buildFabricSurface(tempDir), 'fabricAppQuality');

    expect(result.qualityScore).toBeGreaterThan(0);
    expect(result.summary).toContain('Fabric App');
    expect(result.remediationGuidance.length).toBeGreaterThan(0);
    expect(result.evidence.length).toBeGreaterThan(0);
    expect(result.evidence).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ kind: 'screenshot', pageName: 'Executive Overview' }),
        expect.objectContaining({ kind: 'semanticModel' }),
      ]),
    );
    expect(result.normalizedFindings).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          sourceKind: 'fabricAppReview',
          recommendation: expect.any(String),
          evidence: expect.arrayContaining([
            expect.objectContaining({ kind: 'screenshot' }),
            expect.objectContaining({ kind: 'semanticModel' }),
          ]),
        }),
      ]),
    );
  });
});
