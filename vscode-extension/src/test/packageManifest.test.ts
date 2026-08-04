import * as fs from 'fs';
import * as path from 'path';

describe('extension manifest 0.5.2 runtime posture', () => {
  const packageJsonPath = path.resolve(__dirname, '../../package.json');
  type PackageJson = {
    capabilities?: Record<string, unknown>;
    contributes?: {
      commands?: Array<{ command?: string }>;
      views?: Record<string, Array<{ id?: string }>>;
      configuration?: { properties?: Record<string, unknown> };
      menus?: Record<string, Array<{ command?: string }>>;
    };
    scripts?: Record<string, string>;
  };
  const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8')) as PackageJson;

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
    const views = packageJson.contributes?.views?.['pbir-analyzer-container'] ?? [];
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

  it('declares the Design Studio command and explorer entry', () => {
    const commands = packageJson.contributes?.commands ?? [];
    expect(commands).toEqual(expect.arrayContaining([
      expect.objectContaining({
        command: 'pbirAnalyzer.openDesignStudio',
      }),
      expect.objectContaining({
        command: 'pbirAnalyzer.openLocalPbirMaterialization',
      }),
    ]));

    const contextMenus = packageJson.contributes?.menus?.['view/item/context'] ?? [];
    expect(contextMenus).toEqual(expect.arrayContaining([
      expect.objectContaining({
        command: 'pbirAnalyzer.openDesignStudio',
      }),
    ]));
  });

  it('declares explicit backend target maintenance scripts for packaged runtime assets', () => {
    expect(packageJson.scripts).toEqual(expect.objectContaining({
      'build:backend': 'node scripts/build-backend.mjs',
      'clean:backend': 'node scripts/clean-paths.mjs backend/rpc',
      'package:all': 'node scripts/package-vsix.mjs --all',
      'clean:backend:targets': 'node scripts/clean-backend-targets.mjs',
      'verify:backend:targets': 'node scripts/verify-backend-targets.mjs',
    }));
  });
});
