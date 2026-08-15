import * as vscode from 'vscode';
import { PBIR_CONFIG_SECTIONS } from './extensionIds';

function hasConfiguredValue<T>(
  configuration: vscode.WorkspaceConfiguration,
  key: string,
  defaultValue: T,
): boolean {
  if (typeof configuration.inspect === 'function') {
    const inspected = configuration.inspect(key);
    if (inspected) {
      return [
        inspected.globalValue,
        inspected.workspaceValue,
        inspected.workspaceFolderValue,
      ].some((value) => value !== undefined);
    }
  }

  const value = configuration.get<T>(key, defaultValue);
  if (Array.isArray(value) && Array.isArray(defaultValue)) {
    return value.length > 0;
  }

  return value !== defaultValue;
}

export function getAnalyzerSetting<T>(key: string, defaultValue: T): T {
  const canonical = vscode.workspace.getConfiguration(PBIR_CONFIG_SECTIONS.canonical);
  if (hasConfiguredValue(canonical, key, defaultValue)) {
    return canonical.get<T>(key, defaultValue);
  }

  const legacy = vscode.workspace.getConfiguration(PBIR_CONFIG_SECTIONS.legacy);
  if (hasConfiguredValue(legacy, key, defaultValue)) {
    return legacy.get<T>(key, defaultValue);
  }

  return canonical.get<T>(key, defaultValue);
}

export interface EnhancedScoringSettings {
  enabled: boolean;
  provider: 'auto' | 'pbiLens';
  suggestPbiLens: boolean;
}

export interface EnhancedScoringConfiguration {
  get<T>(key: string, defaultValue: T): T;
}

export interface RenderedReviewSettings {
  enabled: boolean;
  suggestPbiLens: boolean;
  showChecklist: boolean;
}

export function getRenderedReviewSettings(
  configuration: EnhancedScoringConfiguration = vscode.workspace.getConfiguration(PBIR_CONFIG_SECTIONS.canonical),
): RenderedReviewSettings {
  return {
    enabled: configuration.get('renderedReview.enabled', true),
    suggestPbiLens: configuration.get('renderedReview.suggestPbiLens', true),
    showChecklist: configuration.get('renderedReview.showChecklist', true),
  };
}

export function getEnhancedScoringSettings(
  configuration: EnhancedScoringConfiguration = vscode.workspace.getConfiguration(PBIR_CONFIG_SECTIONS.canonical),
): EnhancedScoringSettings {
  return {
    enabled: configuration.get('enhancedScoring.enabled', false),
    provider: configuration.get('enhancedScoring.provider', 'auto'),
    suggestPbiLens: configuration.get('enhancedScoring.suggestPbiLens', true),
  };
}
