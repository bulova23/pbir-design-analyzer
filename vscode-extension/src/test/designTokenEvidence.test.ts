import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { extractDesignTokenEvidence } from '../analyzer/fabric/review/designTokenEvidence';

describe('extractDesignTokenEvidence', () => {
  let tempDir: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'fabric-design-token-evidence-'));
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('extracts token definitions and detects hard-coded bypasses', async () => {
    fs.mkdirSync(path.join(tempDir, 'src', 'theme'), { recursive: true });
    fs.writeFileSync(path.join(tempDir, 'src', 'theme', 'tokens.css'), `
      :root {
        --color-brand: #0055AA;
        --space-md: 16px;
        --font-display: "Segoe UI";
      }
    `);
    fs.writeFileSync(path.join(tempDir, 'src', 'ExecutiveCard.tsx'), `
      export function ExecutiveCard() {
        return <div style={{ color: '#ff0000', padding: '24px' }}>Revenue</div>;
      }
    `);

    const evidence = await extractDesignTokenEvidence(tempDir);

    expect(evidence.tokens).toEqual(
      expect.arrayContaining([
        expect.objectContaining({ token: '--color-brand' }),
        expect.objectContaining({ token: '--space-md' }),
      ]),
    );
    expect(evidence.bypasses).toEqual(
      expect.arrayContaining([
        expect.objectContaining({
          filePath: expect.stringContaining('ExecutiveCard.tsx'),
          summary: expect.stringContaining('#ff0000'),
        }),
      ]),
    );
  });
});
