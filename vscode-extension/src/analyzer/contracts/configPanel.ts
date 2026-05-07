import type { DesignAnalyzerConfig } from '../config/types';

export interface ConfigPanelStatus {
  level: 'success' | 'error';
  message: string;
}

export type ConfigPanelWebviewToHostMessage =
  | { type: 'webviewReady' }
  | { type: 'saveConfig'; config: DesignAnalyzerConfig }
  | { type: 'resetConfig' }
  | { type: 'openGovernanceJson' };

export type ConfigPanelHostToWebviewMessage =
  | { type: 'configState'; config: DesignAnalyzerConfig; status?: ConfigPanelStatus }
  | { type: 'error'; message: string };
