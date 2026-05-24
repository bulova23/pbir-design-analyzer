export type GovernanceRuleSeverity = 'error' | 'warning' | 'info';

export type GovernanceRuleValue = string | number | boolean;

export interface ScoringFramework {
  id: string;
  name: string;
  enabled: boolean;
  weight: number;
  optional?: boolean;
  description?: string;
  reference?: string;
}

export interface GovernanceRule {
  id: string;
  name: string;
  value: GovernanceRuleValue;
  adminOnly: boolean;
  description?: string;
  severity?: GovernanceRuleSeverity;
}

export interface NavigationScoringConfig {
  enabled: boolean;
  weight: number;
}

export interface AudiencePreset {
  id: string;
  name: string;
  description?: string;
  governanceOverrides?: Record<string, GovernanceRuleValue>;
  navigationScoring?: Partial<NavigationScoringConfig>;
  frameworkWeights?: Record<string, number>;
}

export interface DesignAnalyzerConfig {
  frameworks: ScoringFramework[];
  governance: GovernanceRule[];
  navigationScoring: NavigationScoringConfig;
  appliedAudiencePresetId?: string;
  lastUpdated?: string;
}

export interface DesignAnalyzerConfigValidation {
  isValid: boolean;
  totalWeight: number;
  message: string;
}
