import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { extractScreenshotEvidence } from '../analyzer/fabric/review/screenshotEvidence';

describe('extractScreenshotEvidence', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'fabric-screenshot-evidence-'));
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('extracts matched screenshot evidence and preserves capture state details', async () => {
    fs.mkdirSync(path.join(tempDir, 'screenshots'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'screenshots', '01 Executive Overview - Default.png'), 'fake-image');
    fs.writeFileSync(path.join(tempDir, 'screenshots', '02 Sales Detail - Focus State.png'), 'fake-image');

    const evidence = await extractScreenshotEvidence(tempDir, ['Executive Overview', 'Sales Detail']);

    expect(evidence.captures).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          pageName: 'Executive Overview',
          fileName: '01 Executive Overview - Default.png',
          stateName: 'Default',
        }),
        expect.objectContaining({
          pageName: 'Sales Detail',
          stateName: 'Focus State',
        }),
      ]),
    );
    expect(evidence.unmatchedCaptures).toHaveLength(0);
  });

  it('degrades gracefully when screenshots are missing', async () => {
    const evidence = await extractScreenshotEvidence(tempDir, ['Executive Overview']);

    expect(evidence.captures).toEqual([]);
    expect(evidence.unmatchedCaptures).toEqual([]);
  });
});
