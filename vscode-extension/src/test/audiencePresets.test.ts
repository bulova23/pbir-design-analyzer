import path from 'path';
import type * as vscode from 'vscode';
import { applyAudiencePreset } from '../analyzer/config/presets';
import {
  loadAudiencePresets,
  loadDesignAnalyzerConfig,
} from '../analyzer/config/store';
import type { AudiencePreset, DesignAnalyzerConfig } from '../analyzer/config/types';

function createContext(initialValue?: unknown): vscode.ExtensionContext {
  let storedValue = initialValue;

  return {
    extensionPath: path.resolve(__dirname, '../..'),
    globalState: {
      get: jest.fn(() => storedValue),
      update: jest.fn(async (_key: string, value: unknown) => {
        storedValue = value;
      }),
    },
  } as unknown as vscode.ExtensionContext;
}

describe('audience presets', () => {
  it('loads bundled presets from the extension config folder', () => {
    const context = createContext();

    const presets = loadAudiencePresets(context);

    expect(presets.length).toBeGreaterThanOrEqual(3);
    const ids = presets.map((preset) => preset.id);
    expect(ids).toEqual(expect.arrayContaining(['executive', 'operational', 'analyst']));
    const executive = presets.find((preset) => preset.id === 'executive');
    expect(executive?.governanceOverrides?.maxVisualsPerPage).toBeDefined();
  });

  it('applyAudiencePreset overlays governance rule values without mutating the input config', async () => {
    const context = createContext();
    const baseConfig = await loadDesignAnalyzerConfig(context);
    const preset: AudiencePreset = {
      id: 'test',
      name: 'Test',
      governanceOverrides: {
        maxVisualsPerPage: 4,
      },
    };
    const originalMaxVisuals = baseConfig.governance.find(
      (rule) => rule.id === 'maxVisualsPerPage',
    )?.value;

    const next = applyAudiencePreset(baseConfig, preset);

    expect(next.governance.find((rule) => rule.id === 'maxVisualsPerPage')?.value).toBe(4);
    expect(next.appliedAudiencePresetId).toBe('test');
    // Input config must be untouched (pure function).
    expect(baseConfig.governance.find((rule) => rule.id === 'maxVisualsPerPage')?.value).toBe(
      originalMaxVisuals,
    );
    expect(baseConfig.appliedAudiencePresetId).toBeUndefined();
  });

  it('applyAudiencePreset overlays navigation scoring weight while keeping enabled flag', async () => {
    const context = createContext();
    const baseConfig = await loadDesignAnalyzerConfig(context);
    const preset: AudiencePreset = {
      id: 'low-nav',
      name: 'Low nav',
      navigationScoring: { weight: 12 },
    };

    const next = applyAudiencePreset(baseConfig, preset);

    expect(next.navigationScoring.weight).toBe(12);
    expect(next.navigationScoring.enabled).toBe(baseConfig.navigationScoring.enabled);
  });

  it('applyAudiencePreset overlays framework weights only for ids it specifies', async () => {
    const context = createContext();
    const baseConfig = await loadDesignAnalyzerConfig(context);
    const preset: AudiencePreset = {
      id: 'tilt-gestalt',
      name: 'Tilt Gestalt',
      frameworkWeights: { gestalt: 50 },
    };
    const dataInkOriginal = baseConfig.frameworks.find(
      (framework) => framework.id === 'dataink',
    )?.weight;

    const next = applyAudiencePreset(baseConfig, preset);

    expect(next.frameworks.find((framework) => framework.id === 'gestalt')?.weight).toBe(50);
    // Frameworks the preset doesn't mention must retain their previous weights.
    expect(next.frameworks.find((framework) => framework.id === 'dataink')?.weight).toBe(
      dataInkOriginal,
    );
  });

  it('persists appliedAudiencePresetId through the migration cycle', async () => {
    const seed: DesignAnalyzerConfig = {
      frameworks: [],
      governance: [],
      navigationScoring: { enabled: true, weight: 25 },
      appliedAudiencePresetId: 'executive',
    } as DesignAnalyzerConfig;
    const context = createContext(seed);

    const loaded = await loadDesignAnalyzerConfig(context);

    expect(loaded.appliedAudiencePresetId).toBe('executive');
  });
});
