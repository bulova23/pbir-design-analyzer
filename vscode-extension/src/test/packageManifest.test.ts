import * as fs from 'fs';
import * as path from 'path';

describe('extension manifest 0.5.2 runtime posture', () => {
  const packageJsonPath = path.resolve(__dirname, '../../package.json');
  const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8')) as Record<string, any>;

  it('declares explicit unsupported workspace capabilities', () => {
    expect(packageJson.capabilities).toEqual({
      untrustedWorkspaces: {
        supported: false,
        description: expect.any(String),
      },
      virtualWorkspaces: {
        supported: false,
        description: expect.any(String),
      },
    });
  });

  it('uses the canonical pbirAnalyzer explorer view id', () => {
    const views = packageJson.contributes?.views?.['pbir-analyzer-container'];
    expect(Array.isArray(views)).toBe(true);
    expect(views[0]?.id).toBe('pbirAnalyzer.explorer');
  });

  it('contributes only canonical pbirAnalyzer governance settings in release metadata', () => {
    const properties = packageJson.contributes?.configuration?.properties ?? {};

    expect(properties['pbirAnalyzer.governance.enabled']).toBeDefined();
    expect(properties['pbirAnalyzer.governance.minimumCompositeScore']).toBeDefined();
    expect(properties['pbirAnalyzer.governance.approvedThemeIds']).toBeDefined();
    expect(properties['powerbi-modeling.governance.enabled']).toBeUndefined();
    expect(properties['powerbi-modeling.governance.minimumCompositeScore']).toBeUndefined();
    expect(properties['powerbi-modeling.governance.approvedThemeIds']).toBeUndefined();
  });
});
