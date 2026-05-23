import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import type {
  AudiencePreset,
  DesignAnalyzerConfig,
  DesignAnalyzerConfigValidation,
  GovernanceRule,
  GovernanceRuleSeverity,
  GovernanceRuleValue,
  NavigationScoringConfig,
  ScoringFramework,
} from './types';

export const DESIGN_ANALYZER_CONFIG_KEY = 'designAnalyzerConfig';

const DEFAULT_FRAMEWORKS: ScoringFramework[] = [
  {
    id: 'gestalt',
    name: 'Gestalt Principles',
    enabled: true,
    weight: 30,
    description: 'Evaluates grouping, alignment, proximity, similarity, and continuity across report layouts.',
    reference: 'https://www.interaction-design.org/literature/topics/gestalt-principles',
  },
  {
    id: 'cognitive',
    name: 'Cognitive Load',
    enabled: true,
    weight: 20,
    description: 'Measures visual density, competing signals, and the mental effort required to interpret a page.',
    reference: 'https://www.nngroup.com/articles/minimize-cognitive-load/',
  },
  {
    id: 'dataink',
    name: 'Data-Ink Ratio',
    enabled: true,
    weight: 15,
    description: 'Rewards visuals that maximize data signal and minimize decorative or redundant ink.',
    reference: 'https://www.edwardtufte.com/tufte/books_vdqi',
  },
  {
    id: 'graphical',
    name: 'Graphical Perception',
    enabled: false,
    weight: 0,
    optional: true,
    description: 'Evaluates whether chart encodings match how accurately people compare quantitative values.',
    reference: 'https://www.jstor.org/stable/2288400',
  },
  {
    id: 'accessibility',
    name: 'Accessibility (WCAG)',
    enabled: true,
    weight: 15,
    description: 'Checks contrast, readability, and reporting choices that improve accessibility coverage.',
    reference: 'https://www.w3.org/WAI/standards-guidelines/wcag/',
  },
  {
    id: 'visual',
    name: 'Visual Best Practices',
    enabled: true,
    weight: 20,
    description: 'Applies dashboard design guidance around chart choice, labeling, and consistency.',
    reference: 'https://www.perceptualedge.com/articles/whitepapers/good_charts.pdf',
  },
  {
    id: 'governance',
    name: 'Enterprise Governance',
    enabled: false,
    weight: 0,
    optional: true,
    description: 'Optionally scores reports against team or enterprise design standards configured in the local analyzer profile.',
    reference: 'Internal governance configuration',
  },
  {
    id: 'stephen',
    name: 'Stephen Few Principles',
    enabled: false,
    weight: 0,
    optional: true,
    description: 'Applies Stephen Few dashboard heuristics such as KPI prominence and one-screen density.',
    reference: 'https://www.perceptualedge.com/',
  },
  {
    id: 'tufte',
    name: 'Tufte Minimalism',
    enabled: false,
    weight: 0,
    optional: true,
    description: 'Emphasizes clarity, precision, and minimal chart junk in report presentation.',
    reference: 'https://www.edwardtufte.com/',
  },
  {
    id: 'density',
    name: 'Dashboard Density',
    enabled: false,
    weight: 0,
    optional: true,
    description: 'Evaluates balance between information richness and crowding on each report page.',
    reference: 'Internal guidelines',
  },
  {
    id: 'narrative',
    name: 'Narrative Design',
    enabled: false,
    weight: 0,
    optional: true,
    description: 'Evaluates how well page sequencing and layout guide a user through the report story.',
    reference: 'Internal guidelines',
  },
];

const FALLBACK_GOVERNANCE_RULES: GovernanceRule[] = [
  {
    id: 'maxVisualsPerPage',
    name: 'Max Visuals Per Page',
    value: 15,
    description: 'Maximum number of visuals allowed on a single page/state.',
    severity: 'warning',
    adminOnly: true,
  },
  {
    id: 'maxBookmarksPerPage',
    name: 'Max Bookmarks Per Page',
    value: 10,
    description: 'Maximum number of bookmark states allowed per page.',
    severity: 'warning',
    adminOnly: true,
  },
  {
    id: 'maxLayoutStatesPerPage',
    name: 'Max Layout States Per Page',
    value: 8,
    description: 'Maximum number of distinct visual layout states allowed per page.',
    severity: 'warning',
    adminOnly: true,
  },
  {
    id: 'maxHiddenVisuals',
    name: 'Max Hidden Visuals',
    value: 10,
    description: 'Maximum number of hidden visuals allowed in bookmark-driven layouts.',
    severity: 'info',
    adminOnly: true,
  },
  {
    id: 'minWhiteSpaceRatio',
    name: 'Minimum White Space Ratio',
    value: 0.15,
    description: 'Minimum ratio of white space to total page area.',
    severity: 'warning',
    adminOnly: true,
  },
  {
    id: 'allowPieCharts',
    name: 'Allow Pie Charts',
    value: true,
    description: 'Whether pie or donut charts are allowed in reports.',
    severity: 'warning',
    adminOnly: true,
  },
  {
    id: 'allowCustomVisuals',
    name: 'Allow Custom Visuals',
    value: true,
    description: 'Whether third-party custom visuals are allowed in reports.',
    severity: 'warning',
    adminOnly: true,
  },
  {
    id: 'requirePageTitle',
    name: 'Require Page Title',
    value: true,
    description: 'Whether all report pages must include a title.',
    severity: 'error',
    adminOnly: true,
  },
  {
    id: 'requireFilterPanel',
    name: 'Require Filter Panel',
    value: false,
    description: 'Whether every report page must expose a filter panel.',
    severity: 'warning',
    adminOnly: true,
  },
  {
    id: 'themeStandard',
    name: 'Standard Theme',
    value: 'default',
    description: 'Approved report theme identifier.',
    severity: 'warning',
    adminOnly: true,
  },
];

interface MigrationResult {
  config: DesignAnalyzerConfig;
  changed: boolean;
}

const DEFAULT_NAVIGATION_SCORING: NavigationScoringConfig = {
  enabled: true,
  weight: 25,
};

function cloneFrameworks(): ScoringFramework[] {
  return DEFAULT_FRAMEWORKS.map((framework) => ({ ...framework }));
}

function cloneGovernanceRules(rules: GovernanceRule[]): GovernanceRule[] {
  return rules.map((rule) => ({ ...rule }));
}

function cloneNavigationScoring(): NavigationScoringConfig {
  return { ...DEFAULT_NAVIGATION_SCORING };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function normalizeFrameworkId(id: string): string {
  switch (id.trim().toLowerCase()) {
    case 'cognitiveload':
    case 'cogload':
      return 'cognitive';
    case 'dataink':
      return 'dataink';
    case 'graphicalperception':
    case 'perception':
      return 'graphical';
    case 'a11y':
    case 'wcag':
      return 'accessibility';
    case 'visualbestpractices':
    case 'vbp':
      return 'visual';
    case 'enterprisegovernance':
      return 'governance';
    case 'stephenfew':
    case 'few':
      return 'stephen';
    case 'tufeminimalism':
      return 'tufte';
    case 'dashboarddensity':
      return 'density';
    case 'narrativedesign':
      return 'narrative';
    default:
      return id.trim().toLowerCase();
  }
}

function normalizeGovernanceRuleId(id: string): string {
  switch (id.trim()) {
    case 'maxVisuals':
      return 'maxVisualsPerPage';
    case 'allowPie':
      return 'allowPieCharts';
    case 'requireTitle':
      return 'requirePageTitle';
    default:
      return id.trim();
  }
}

function coerceGovernanceValue(value: unknown): GovernanceRuleValue {
  if (typeof value === 'boolean' || typeof value === 'number' || typeof value === 'string') {
    return value;
  }

  if (value === null || typeof value === 'undefined') {
    return '';
  }

  return String(value);
}

function coerceSeverity(value: unknown): GovernanceRuleSeverity | undefined {
  return value === 'error' || value === 'warning' || value === 'info' ? value : undefined;
}

function getGovernanceDefaultsFromFile(context: vscode.ExtensionContext): GovernanceRule[] {
  const configPath = getGovernanceDefaultsPath(context);
  if (!fs.existsSync(configPath)) {
    return cloneGovernanceRules(FALLBACK_GOVERNANCE_RULES);
  }

  try {
    const raw = fs.readFileSync(configPath, 'utf8');
    const parsed = JSON.parse(raw) as { rules?: Record<string, Record<string, unknown>> };
    if (!parsed.rules || !isRecord(parsed.rules)) {
      return cloneGovernanceRules(FALLBACK_GOVERNANCE_RULES);
    }

    const rules = Object.entries(parsed.rules)
      .filter(([, rule]) => isRecord(rule))
      .map(([id, rule]) => ({
        id,
        name: typeof rule.name === 'string' ? rule.name : id,
        value: coerceGovernanceValue(rule.value),
        adminOnly: typeof rule.adminOnly === 'boolean' ? rule.adminOnly : true,
        description: typeof rule.description === 'string' ? rule.description : undefined,
        severity: coerceSeverity(rule.severity),
      }));

    return rules.length > 0 ? rules : cloneGovernanceRules(FALLBACK_GOVERNANCE_RULES);
  } catch {
    return cloneGovernanceRules(FALLBACK_GOVERNANCE_RULES);
  }
}

function getDefaultConfig(context: vscode.ExtensionContext): DesignAnalyzerConfig {
  return {
    frameworks: cloneFrameworks(),
    governance: getGovernanceDefaultsFromFile(context),
    navigationScoring: cloneNavigationScoring(),
  };
}

function normalizeNavigationScoring(
  rawNavigationScoring: unknown,
): NavigationScoringConfig {
  if (!isRecord(rawNavigationScoring)) {
    return cloneNavigationScoring();
  }

  const enabled =
    typeof rawNavigationScoring.enabled === 'boolean'
      ? rawNavigationScoring.enabled
      : DEFAULT_NAVIGATION_SCORING.enabled;
  const weight =
    typeof rawNavigationScoring.weight === 'number'
      ? Math.max(0, Math.min(100, rawNavigationScoring.weight))
      : DEFAULT_NAVIGATION_SCORING.weight;

  return {
    enabled,
    weight,
  };
}

function normalizeFrameworks(rawFrameworks: unknown): ScoringFramework[] {
  const defaults = cloneFrameworks();
  if (!Array.isArray(rawFrameworks)) {
    return defaults;
  }

  const defaultsById = new Map(defaults.map((framework) => [framework.id, framework]));
  const normalizedById = new Map<string, ScoringFramework>();

  rawFrameworks.forEach((entry) => {
    if (!isRecord(entry) || typeof entry.id !== 'string') {
      return;
    }

    const id = normalizeFrameworkId(entry.id);
    const defaultFramework = defaultsById.get(id);
    if (!defaultFramework) {
      return;
    }

    normalizedById.set(id, {
      ...defaultFramework,
      ...entry,
      id,
      name: typeof entry.name === 'string' ? entry.name : defaultFramework.name,
      enabled: typeof entry.enabled === 'boolean' ? entry.enabled : defaultFramework.enabled,
      weight: typeof entry.weight === 'number' ? entry.weight : defaultFramework.weight,
      optional: typeof entry.optional === 'boolean' ? entry.optional : defaultFramework.optional,
      description:
        typeof entry.description === 'string' ? entry.description : defaultFramework.description,
      reference: typeof entry.reference === 'string' ? entry.reference : defaultFramework.reference,
    });
  });

  return defaults.map((framework) => normalizedById.get(framework.id) ?? framework);
}

function normalizeGovernanceRules(
  rawGovernance: unknown,
  defaultRules: GovernanceRule[],
): GovernanceRule[] {
  const defaultsById = new Map(defaultRules.map((rule) => [rule.id, rule]));
  const normalizedById = new Map<string, GovernanceRule>();

  const upsertRule = (candidateId: string, candidate: unknown): void => {
    if (!candidateId) {
      return;
    }

    const id = normalizeGovernanceRuleId(candidateId);
    const defaultRule = defaultsById.get(id);
    const candidateRecord = isRecord(candidate) ? candidate : undefined;
    const candidateValue = candidateRecord ? candidateRecord.value : candidate;

    normalizedById.set(id, {
      ...(defaultRule ?? {
        id,
        name: id,
        value: '',
        adminOnly: true,
      }),
      ...(candidateRecord ?? {}),
      id,
      name:
        candidateRecord && typeof candidateRecord.name === 'string'
          ? candidateRecord.name
          : defaultRule?.name ?? id,
      value: coerceGovernanceValue(candidateValue),
      adminOnly:
        candidateRecord && typeof candidateRecord.adminOnly === 'boolean'
          ? candidateRecord.adminOnly
          : defaultRule?.adminOnly ?? true,
      description:
        candidateRecord && typeof candidateRecord.description === 'string'
          ? candidateRecord.description
          : defaultRule?.description,
      severity: candidateRecord ? coerceSeverity(candidateRecord.severity) ?? defaultRule?.severity : defaultRule?.severity,
    });
  };

  if (Array.isArray(rawGovernance)) {
    rawGovernance.forEach((rule) => {
      if (isRecord(rule) && typeof rule.id === 'string') {
        upsertRule(rule.id, rule);
      }
    });
  } else if (isRecord(rawGovernance)) {
    Object.entries(rawGovernance).forEach(([id, value]) => upsertRule(id, value));
  }

  const defaultsInOrder = defaultRules.map((rule) => normalizedById.get(rule.id) ?? rule);
  const customRules = Array.from(normalizedById.entries())
    .filter(([id]) => !defaultsById.has(id))
    .map(([, rule]) => rule);

  return [...defaultsInOrder, ...customRules];
}

function parseStoredConfig(raw: unknown): unknown {
  if (typeof raw !== 'string') {
    return raw;
  }

  try {
    return JSON.parse(raw);
  } catch {
    return undefined;
  }
}

function migrateConfig(
  rawConfig: unknown,
  context: vscode.ExtensionContext,
): MigrationResult {
  const parsed = parseStoredConfig(rawConfig);
  const defaults = getDefaultConfig(context);

  if (!isRecord(parsed)) {
    return {
      config: defaults,
      changed: true,
    };
  }

  const migrated: DesignAnalyzerConfig = {
    frameworks: normalizeFrameworks(parsed.frameworks),
    governance: normalizeGovernanceRules(parsed.governance, defaults.governance),
    navigationScoring: normalizeNavigationScoring(parsed.navigationScoring),
    appliedAudiencePresetId:
      typeof parsed.appliedAudiencePresetId === 'string' ? parsed.appliedAudiencePresetId : undefined,
    lastUpdated: typeof parsed.lastUpdated === 'string' ? parsed.lastUpdated : undefined,
  };

  return {
    config: migrated,
    changed: JSON.stringify(parsed) !== JSON.stringify(migrated),
  };
}

async function persistConfig(
  context: vscode.ExtensionContext,
  config: DesignAnalyzerConfig,
): Promise<void> {
  await context.globalState.update(DESIGN_ANALYZER_CONFIG_KEY, config);
}

export function getGovernanceDefaultsPath(context: vscode.ExtensionContext): string {
  return path.join(context.extensionPath, 'config', 'governance-defaults.json');
}

export function getAudiencePresetsPath(context: vscode.ExtensionContext): string {
  return path.join(context.extensionPath, 'config', 'audience-presets.json');
}

function isGovernanceRuleValue(value: unknown): value is GovernanceRuleValue {
  return typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean';
}

function coerceAudiencePreset(raw: unknown): AudiencePreset | undefined {
  if (!isRecord(raw)) {
    return undefined;
  }

  const id = typeof raw.id === 'string' && raw.id.trim().length > 0 ? raw.id.trim() : undefined;
  const name = typeof raw.name === 'string' && raw.name.trim().length > 0 ? raw.name.trim() : undefined;
  if (!id || !name) {
    return undefined;
  }

  const preset: AudiencePreset = { id, name };

  if (typeof raw.description === 'string') {
    preset.description = raw.description;
  }

  if (isRecord(raw.governanceOverrides)) {
    const overrides: Record<string, GovernanceRuleValue> = {};
    for (const [ruleId, value] of Object.entries(raw.governanceOverrides)) {
      if (isGovernanceRuleValue(value)) {
        overrides[normalizeGovernanceRuleId(ruleId)] = value;
      }
    }
    if (Object.keys(overrides).length > 0) {
      preset.governanceOverrides = overrides;
    }
  }

  if (isRecord(raw.navigationScoring)) {
    const ns: Partial<NavigationScoringConfig> = {};
    if (typeof raw.navigationScoring.enabled === 'boolean') {
      ns.enabled = raw.navigationScoring.enabled;
    }
    if (typeof raw.navigationScoring.weight === 'number') {
      ns.weight = Math.max(0, Math.min(100, raw.navigationScoring.weight));
    }
    if (Object.keys(ns).length > 0) {
      preset.navigationScoring = ns;
    }
  }

  if (isRecord(raw.frameworkWeights)) {
    const weights: Record<string, number> = {};
    for (const [frameworkId, value] of Object.entries(raw.frameworkWeights)) {
      if (typeof value === 'number' && Number.isFinite(value)) {
        weights[normalizeFrameworkId(frameworkId)] = Math.max(0, Math.min(100, value));
      }
    }
    if (Object.keys(weights).length > 0) {
      preset.frameworkWeights = weights;
    }
  }

  return preset;
}

export function loadAudiencePresets(context: vscode.ExtensionContext): AudiencePreset[] {
  const configPath = getAudiencePresetsPath(context);
  if (!fs.existsSync(configPath)) {
    return [];
  }

  try {
    const raw = fs.readFileSync(configPath, 'utf8');
    const parsed = JSON.parse(raw) as { presets?: unknown };
    if (!Array.isArray(parsed.presets)) {
      return [];
    }

    return parsed.presets
      .map((entry) => coerceAudiencePreset(entry))
      .filter((preset): preset is AudiencePreset => preset !== undefined);
  } catch {
    return [];
  }
}

export { applyAudiencePreset } from './presets';

export function validateDesignAnalyzerConfig(
  config: DesignAnalyzerConfig,
): DesignAnalyzerConfigValidation {
  if (!Number.isFinite(config.navigationScoring.weight) ||
      config.navigationScoring.weight < 0 ||
      config.navigationScoring.weight > 100) {
    return {
      isValid: false,
      totalWeight: 0,
      message: 'Navigation scoring weight must be between 0% and 100%.',
    };
  }

  const totalWeight = config.frameworks
    .filter((framework) => framework.enabled)
    .reduce((sum, framework) => sum + framework.weight, 0);

  if (totalWeight !== 100) {
    return {
      isValid: false,
      totalWeight,
      message: `Enabled frameworks must total 100%. Current total: ${totalWeight}%.`,
    };
  }

  return {
    isValid: true,
    totalWeight,
    message: 'Enabled frameworks total 100%.',
  };
}

export async function initializeDesignAnalyzerConfig(
  context: vscode.ExtensionContext,
): Promise<DesignAnalyzerConfig> {
  return loadDesignAnalyzerConfig(context);
}

export async function loadDesignAnalyzerConfig(
  context: vscode.ExtensionContext,
): Promise<DesignAnalyzerConfig> {
  const raw = context.globalState.get<unknown>(DESIGN_ANALYZER_CONFIG_KEY);
  const { config, changed } = migrateConfig(raw, context);
  if (changed) {
    await persistConfig(context, config);
  }

  return config;
}

export async function saveDesignAnalyzerConfig(
  context: vscode.ExtensionContext,
  config: DesignAnalyzerConfig,
): Promise<DesignAnalyzerConfig> {
  const validation = validateDesignAnalyzerConfig(config);
  if (!validation.isValid) {
    throw new Error(validation.message);
  }

  const stampedConfig: DesignAnalyzerConfig = {
    ...config,
    frameworks: config.frameworks.map((framework) => ({ ...framework })),
    governance: config.governance.map((rule) => ({ ...rule })),
    navigationScoring: {
      enabled: config.navigationScoring.enabled,
      weight: config.navigationScoring.weight,
    },
    appliedAudiencePresetId: config.appliedAudiencePresetId,
    lastUpdated: new Date().toISOString(),
  };

  await persistConfig(context, stampedConfig);
  return stampedConfig;
}

export async function resetDesignAnalyzerConfig(
  context: vscode.ExtensionContext,
): Promise<DesignAnalyzerConfig> {
  const defaultConfig = getDefaultConfig(context);
  return saveDesignAnalyzerConfig(context, defaultConfig);
}
