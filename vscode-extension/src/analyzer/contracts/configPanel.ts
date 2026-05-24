import type { AudiencePreset, DesignAnalyzerConfig } from '../config/types';

export interface ConfigPanelStatus {
  level: 'success' | 'error';
  message: string;
}

export type AuditProviderChoice = 'anthropic' | 'openai';

export type ConfigPanelWebviewToHostMessage =
  | { type: 'webviewReady' }
  | { type: 'saveConfig'; config: DesignAnalyzerConfig }
  | { type: 'resetConfig' }
  | { type: 'openGovernanceJson' }
  | { type: 'saveAuditProvider'; provider: AuditProviderChoice; apiKey: string }
  | { type: 'deleteAuditProviderKey'; provider: AuditProviderChoice };

export type ConfigPanelHostToWebviewMessage =
  | {
      type: 'configState';
      config: DesignAnalyzerConfig;
      presets?: AudiencePreset[];
      status?: ConfigPanelStatus;
    }
  | {
      type: 'auditProviderState';
      activeProvider: AuditProviderChoice;
      anthropicConfigured: boolean;
      openaiConfigured: boolean;
      saveStatus?: ConfigPanelStatus;
    }
  | { type: 'error'; message: string };
