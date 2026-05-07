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

export interface DesignAnalyzerConfig {
  frameworks: ScoringFramework[];
  governance: GovernanceRule[];
  navigationScoring: NavigationScoringConfig;
  lastUpdated?: string;
}

export interface DesignAnalyzerConfigValidation {
  isValid: boolean;
  totalWeight: number;
  message: string;
}
