import fs from 'fs';
import path from 'path';

describe('pdfkit standard font asset packaging', () => {
  it('copies pdfkit standard font data into dist/data for bundled runtime use', () => {
    const dataDir = path.join(process.cwd(), 'dist', 'data');
    const helvetica = path.join(dataDir, 'Helvetica.afm');
    const helveticaBold = path.join(dataDir, 'Helvetica-Bold.afm');

    expect(fs.existsSync(dataDir)).toBe(true);
    expect(fs.existsSync(helvetica)).toBe(true);
    expect(fs.existsSync(helveticaBold)).toBe(true);
    expect(fs.readFileSync(helvetica, 'utf8')).toContain('StartFontMetrics');
  });
});
