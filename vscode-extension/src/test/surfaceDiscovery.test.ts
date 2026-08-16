import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { detectAnalyzableSurface } from '../analyzer/surfaces/discovery';

describe('detectAnalyzableSurface', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'surface-discovery-'));
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('resolves a PBIR report folder to the pbir surface with readiness analyzer support', () => {
    const reportRoot = path.join(tempDir, 'Sales & Production.Report');
    fs.mkdirSync(path.join(reportRoot, 'definition'), { recursive: true });
    fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
    fs.writeFileSync(path.join(reportRoot, 'definition', 'report.json'), '{}');

    const result = detectAnalyzableSurface(reportRoot);

    expect(result.status).toBe('supported');
    if (result.status !== 'supported') {
      throw new Error('expected supported surface');
    }

    expect(result.surface).toMatchObject({
      surfaceType: 'pbirReport',
      sourceLocation: reportRoot,
      availableAnalyzerProfiles: ['default', 'migrationReadiness'],
      availableAnalyzerTypes: ['pbirDesignReview', 'fabricAppReadiness'],
    });
  });

  it('resolves a PBIP file to the pbir surface', () => {
    const pbipPath = path.join(tempDir, 'Sales & Production.pbip');
    fs.writeFileSync(pbipPath, '{}');

    const result = detectAnalyzableSurface(pbipPath);

    expect(result.status).toBe('supported');
    if (result.status !== 'supported') {
      throw new Error('expected supported surface');
    }

    expect(result.surface.surfaceType).toBe('pbirReport');
    expect(result.surface.sourceLocation).toBe(pbipPath);
  });

  it('returns unsupported for folders that are not analyzable surfaces yet', () => {
    const workspaceRoot = path.join(tempDir, 'unrelated');
    fs.mkdirSync(workspaceRoot, { recursive: true });
    fs.writeFileSync(path.join(workspaceRoot, 'README.md'), '# not a report');

    const result = detectAnalyzableSurface(workspaceRoot);

    expect(result).toMatchObject({
      status: 'unsupported',
      reasonCode: 'missingFabricAppIndicators',
      reason: expect.stringContaining('minimum Fabric App repo indicators'),
    });
  });

  it('resolves a supported Fabric App repo when repo indicators, analytics TypeScript, and navigation artifacts exist', () => {
    const appRoot = path.join(tempDir, 'fabric-app');
    fs.mkdirSync(path.join(appRoot, 'src', 'routes'), { recursive: true });
    fs.writeFileSync(path.join(appRoot, 'package.json'), JSON.stringify({
      name: 'executive-fabric-app',
      scripts: { start: 'vite' },
    }));
    fs.writeFileSync(path.join(appRoot, 'src', 'appShell.tsx'), `
      export function AppShell() {
        return (
          <DashboardLayout>
            <KpiCard label="Revenue" />
            <ExecutiveSummary />
          </DashboardLayout>
        );
      }
    `);
    fs.writeFileSync(path.join(appRoot, 'src', 'routes', 'index.tsx'), `
      export const routes = [
        { path: '/overview', label: 'Overview' },
        { path: '/details', label: 'Details' },
      ];
    `);

    const result = detectAnalyzableSurface(appRoot);

    expect(result.status).toBe('supported');
    if (result.status !== 'supported') {
      throw new Error('expected supported surface');
    }

    expect(result.surface).toMatchObject({
      surfaceType: 'fabricApp',
      sourceLocation: appRoot,
      availableAnalyzerTypes: ['fabricAppReview'],
      availableAnalyzerProfiles: ['default', 'fabricAppQuality'],
    });
  });

  it('returns unsupported with an explicit reason code when analytics TypeScript is missing', () => {
    const appRoot = path.join(tempDir, 'unsupported-fabric-app');
    fs.mkdirSync(path.join(appRoot, 'src', 'routes'), { recursive: true });
    fs.writeFileSync(path.join(appRoot, 'package.json'), JSON.stringify({ name: 'generic-shell' }));
    fs.writeFileSync(path.join(appRoot, 'src', 'routes', 'index.tsx'), 'export const routes = [{ path: "/" }];');

    const result = detectAnalyzableSurface(appRoot);

    expect(result).toMatchObject({
      status: 'unsupported',
      reasonCode: 'missingAnalyticsTypescript',
      reason: expect.stringContaining('analytics-facing TypeScript'),
    });
  });

  it('returns ambiguous with an explicit reason code when route structure exists but the repo does not clearly look analytical', () => {
    const appRoot = path.join(tempDir, 'ambiguous-fabric-app');
    fs.mkdirSync(path.join(appRoot, 'src', 'routes'), { recursive: true });
    fs.writeFileSync(path.join(appRoot, 'package.json'), JSON.stringify({ name: 'maybe-app' }));
    fs.writeFileSync(path.join(appRoot, 'src', 'workflowView.tsx'), `
      export function WorkflowView() {
        return <MainPanel><FormWizard /></MainPanel>;
      }
    `);
    fs.writeFileSync(path.join(appRoot, 'src', 'routes', 'index.tsx'), `
      export const routes = [
        { path: '/step-1', label: 'Step 1' },
        { path: '/step-2', label: 'Step 2' },
      ];
    `);

    const result = detectAnalyzableSurface(appRoot);

    expect(result).toMatchObject({
      status: 'ambiguous',
      reasonCode: 'ambiguousAnalyticsSurface',
      reason: expect.stringContaining('analytical'),
    });
  });
});
