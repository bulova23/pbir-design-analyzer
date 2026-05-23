import type {
  AudiencePreset,
  DesignAnalyzerConfig,
  NavigationScoringConfig,
} from './types';

/**
 * Overlays an audience preset onto an existing analyzer configuration, returning a new
 * configuration with the preset values applied. The preset id is stored on the returned
 * config as informational metadata so the UI can indicate which preset is currently active.
 *
 * Pure function: no I/O, safe to use from both the extension host and the webview.
 */
export function applyAudiencePreset(
  config: DesignAnalyzerConfig,
  preset: AudiencePreset,
): DesignAnalyzerConfig {
  const governance = config.governance.map((rule) => {
    const override = preset.governanceOverrides?.[rule.id];
    if (override === undefined) {
      return { ...rule };
    }
    return { ...rule, value: override };
  });

  const navigationScoring: NavigationScoringConfig = {
    enabled:
      preset.navigationScoring?.enabled !== undefined
        ? preset.navigationScoring.enabled
        : config.navigationScoring.enabled,
    weight:
      preset.navigationScoring?.weight !== undefined
        ? preset.navigationScoring.weight
        : config.navigationScoring.weight,
  };

  const frameworks = config.frameworks.map((framework) => {
    const weightOverride = preset.frameworkWeights?.[framework.id];
    if (weightOverride === undefined) {
      return { ...framework };
    }
    return { ...framework, weight: weightOverride };
  });

  return {
    ...config,
    frameworks,
    governance,
    navigationScoring,
    appliedAudiencePresetId: preset.id,
  };
}
