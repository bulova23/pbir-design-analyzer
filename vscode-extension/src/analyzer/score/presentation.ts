import type { DesignAnalyzerConfig } from '../config/types';
import type { PageScore, ScoreResult } from '../contracts/scorePanel';

const FRAMEWORK_LABELS: Record<string, string> = {
  gestalt: 'Gestalt Principles',
  cognitiveLoad: 'Cognitive Load',
  dataInk: 'Data-Ink Ratio',
  graphicalPerception: 'Graphical Perception',
  accessibility: 'Accessibility',
  visualBestPractices: 'Visual Best Practices',
  governance: 'Enterprise Governance',
  stephenFew: 'Stephen Few',
  tufte: 'Tufte Minimalism',
  density: 'Dashboard Density',
  narrative: 'Narrative Design',
};

const SCORE_FIELD_MAP: Record<
  string,
  | 'gestaltScore'
  | 'cognitiveLoadScore'
  | 'dataInkScore'
  | 'accessibilityScore'
  | 'visualBestPracticesScore'
  | 'stephenFewScore'
  | 'enterpriseGovernanceScore'
  | 'tufteScore'
  | 'graphicalPerceptionScore'
  | 'densityScore'
  | 'narrativeScore'
> = {
  gestalt: 'gestaltScore',
  cognitiveLoad: 'cognitiveLoadScore',
  dataInk: 'dataInkScore',
  accessibility: 'accessibilityScore',
  visualBestPractices: 'visualBestPracticesScore',
  stephenFew: 'stephenFewScore',
  governance: 'enterpriseGovernanceScore',
  tufte: 'tufteScore',
  graphicalPerception: 'graphicalPerceptionScore',
  density: 'densityScore',
  narrative: 'narrativeScore',
};

export interface EnabledFramework {
  normalizedKey: string;
  label: string;
  weight: number;
  weightLabel: string;
}

export function normalizeFrameworkId(id: string): string {
  switch (id.toLowerCase()) {
    case 'gestalt':
      return 'gestalt';
    case 'cognitive':
    case 'cognitiveload':
      return 'cognitiveLoad';
    case 'dataink':
    case 'data-ink':
      return 'dataInk';
    case 'graphical':
    case 'graphicalperception':
      return 'graphicalPerception';
    case 'accessibility':
    case 'wcag':
      return 'accessibility';
    case 'visual':
    case 'visualbestpractices':
      return 'visualBestPractices';
    case 'governance':
    case 'enterprisegovernance':
      return 'governance';
    case 'stephen':
    case 'stephenfew':
      return 'stephenFew';
    case 'tufte':
    case 'tufeminimalism':
      return 'tufte';
    case 'density':
    case 'dashboarddensity':
      return 'density';
    case 'narrative':
    case 'narrativedesign':
      return 'narrative';
    default:
      return id;
  }
}

export function getResultScore(result: ScoreResult, normalizedKey: string): number {
  const field = SCORE_FIELD_MAP[normalizedKey];
  return field ? (result[field] as number) ?? 0 : 0;
}

export function getPageScore(page: PageScore, normalizedKey: string): number {
  const field = SCORE_FIELD_MAP[normalizedKey];
  return field ? (page[field] as number) ?? 0 : 0;
}

export function getEnabledFrameworks(
  config: DesignAnalyzerConfig | null,
  fallbackWeightMap: Record<string, number>,
): EnabledFramework[] {
  if (config?.frameworks?.length) {
    return config.frameworks
      .filter((framework) => framework.enabled)
      .map((framework) => {
        const normalizedKey = normalizeFrameworkId(framework.id);
        const weight = framework.weight ?? fallbackWeightMap[normalizedKey] ?? 0;

        return {
          normalizedKey,
          label: framework.name || FRAMEWORK_LABELS[normalizedKey] || normalizedKey,
          weight,
          weightLabel: `${weight}%`,
        };
      });
  }

  return Object.entries(fallbackWeightMap)
    .filter(([, weight]) => weight > 0)
    .map(([key, weight]) => ({
      normalizedKey: key,
      label: FRAMEWORK_LABELS[key] || key,
      weight,
      weightLabel: `${weight}%`,
    }));
}

export function groupRecommendations(
  recommendations: string[],
): Array<{ cls: string; text: string }> {
  return recommendations
    .map((recommendation) => {
      if (recommendation.startsWith('[High]')) {
        return { cls: 'rec-high', text: recommendation };
      }

      if (recommendation.startsWith('[Medium]')) {
        return { cls: 'rec-medium', text: recommendation };
      }

      return { cls: 'rec-low', text: recommendation };
    })
    .sort((left, right) => {
      const order: Record<string, number> = {
        'rec-high': 0,
        'rec-medium': 1,
        'rec-low': 2,
      };

      return (order[left.cls] ?? 3) - (order[right.cls] ?? 3);
    });
}

export function basename(filePath: string): string {
  return filePath.replace(/\\/g, '/').split('/').pop() ?? filePath;
}
