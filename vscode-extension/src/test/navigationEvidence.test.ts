import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { extractNavigationEvidence } from '../analyzer/fabric/review/navigationEvidence';

describe('extractNavigationEvidence', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'fabric-navigation-evidence-'));
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('extracts route hierarchy and executive-to-detail flow signals', () => {
    fs.mkdirSync(path.join(tempDir, 'src', 'routes'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'src', 'routes', 'index.tsx'), `
      export const routes = [
        { path: '/overview', label: 'Executive Overview' },
        { path: '/sales-detail', label: 'Sales Detail' },
        { path: '/inventory-detail', label: 'Inventory Detail' },
      ];
    `);

    const evidence = extractNavigationEvidence(tempDir);

    expect(evidence.routes).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ path: '/overview', label: 'Executive Overview' }),
        expect.objectContaining({ path: '/sales-detail', label: 'Sales Detail' }),
      ]),
    );
    expect(evidence.hasExecutiveToDetailFlow).toBe(true);
    expect(evidence.summary).toContain('overview');
  });
});
