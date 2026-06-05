import type { AnalyzerProfileId } from '../../analyzers/types';
import type { NormalizedFinding } from '../../contracts/scorePanel';
import type { AnalyzableSurface } from '../../surfaces/types';
import { extractDesignTokenEvidence } from './designTokenEvidence';
import { extractNavigationEvidence } from './navigationEvidence';
import { extractTypeScriptEvidence } from './typescriptEvidence';
import type {
  FabricAppEvidenceItem,
  FabricAppReviewResult,
  NavigationEvidenceReport,
  TypeScriptEvidenceReport,
  DesignTokenEvidenceReport,
} from './reviewTypes';

function evidenceItemsFromTypeScript(report: TypeScriptEvidenceReport): FabricAppEvidenceItem[] {
  return [
    ...report.layoutPatterns.map((item) => ({
      kind: 'typescriptLayout' as const,
      label: 'TypeScript layout evidence',
      summary: item.summary,
      filePath: item.filePath,
    })),
    ...report.kpiPatterns.map((item) => ({
      kind: 'typescriptLayout' as const,
      label: 'KPI structure evidence',
      summary: item.summary,
      filePath: item.filePath,
    })),
    ...report.compositionSignals.map((item) => ({
      kind: 'typescriptLayout' as const,
      label: 'Dashboard composition evidence',
      summary: item.summary,
      filePath: item.filePath,
    })),
  ];
}

function evidenceItemsFromNavigation(report: NavigationEvidenceReport): FabricAppEvidenceItem[] {
  return report.routes.map((route) => ({
    kind: 'navigation' as const,
    label: 'Navigation evidence',
    summary: `${route.label} -> ${route.path}`,
    filePath: route.filePath,
  }));
}

function evidenceItemsFromTokens(report: DesignTokenEvidenceReport): FabricAppEvidenceItem[] {
  return [
    ...report.tokens.map((item) => ({
      kind: 'designToken' as const,
      label: 'Design token evidence',
      summary: item.summary,
      filePath: item.filePath,
    })),
    ...report.bypasses.map((item) => ({
      kind: 'designToken' as const,
      label: 'Token bypass evidence',
      summary: item.summary,
      filePath: item.filePath,
    })),
  ];
}

function buildFinding(params: {
  id: string;
  title: string;
  summary: string;
  severity: NormalizedFinding['severity'];
  impactArea: NormalizedFinding['impactArea'];
  recommendation: string;
  evidence: FabricAppEvidenceItem[];
}): NormalizedFinding {
  return {
    id: params.id,
    title: params.title,
    summary: params.summary,
    severity: params.severity,
    confidence: 82,
    scope: 'report',
    detectionType: 'deterministic',
    affectedPages: [],
    impactArea: params.impactArea,
    frameworkImpact: ['Fabric App Review'],
    recommendation: params.recommendation,
    sourceKind: 'fabricAppReview',
    sourceSection: 'issues',
    evidence: params.evidence.map((item) => ({
      kind: item.kind,
      label: item.label,
      detail: `${item.filePath} — ${item.summary}`,
      filePath: item.filePath,
    })),
  };
}

function buildFindings(
  typeScriptEvidence: TypeScriptEvidenceReport,
  navigationEvidence: NavigationEvidenceReport,
  tokenEvidence: DesignTokenEvidenceReport,
): NormalizedFinding[] {
  const findings: NormalizedFinding[] = [];

  if (tokenEvidence.bypasses.length > 0) {
    findings.push(buildFinding({
      id: 'fabric-token-bypass',
      title: 'Token inconsistencies were detected',
      summary: 'Hard-coded color or spacing values bypass the shared token layer.',
      severity: 'medium',
      impactArea: 'layout',
      recommendation: 'Standardize token usage so color, spacing, and typography stay consistent across the app.',
      evidence: evidenceItemsFromTokens({ tokens: [], bypasses: tokenEvidence.bypasses }),
    }));
  }

  if (!navigationEvidence.hasExecutiveToDetailFlow) {
    findings.push(buildFinding({
      id: 'fabric-navigation-flow',
      title: 'Executive-to-detail navigation flow is unclear',
      summary: 'The route hierarchy does not clearly establish a summary-to-detail analytical path.',
      severity: 'high',
      impactArea: 'navigation',
      recommendation: 'Create a clearer overview-to-detail route structure so readers understand where to start and where to drill deeper.',
      evidence: evidenceItemsFromNavigation(navigationEvidence),
    }));
  }

  if (navigationEvidence.routes.some((route) => route.label.toLowerCase() === 'detail')) {
    findings.push(buildFinding({
      id: 'fabric-route-clarity',
      title: 'Route labeling is too generic for analytical navigation',
      summary: 'Generic labels such as "Detail" weaken evidence flow and executive readability.',
      severity: 'medium',
      impactArea: 'storytelling',
      recommendation: 'Rename generic routes so the analytical purpose of each destination is obvious before navigation.',
      evidence: evidenceItemsFromNavigation(navigationEvidence).filter((item) => item.summary.toLowerCase().includes('detail')),
    }));
  }

  if (typeScriptEvidence.layoutPatterns.length > 0 && typeScriptEvidence.kpiPatterns.length === 0) {
    findings.push(buildFinding({
      id: 'fabric-kpi-hierarchy',
      title: 'Dashboard hierarchy does not clearly establish KPI emphasis',
      summary: 'Layout structure exists, but KPI organization is not obvious from the app shell evidence.',
      severity: 'medium',
      impactArea: 'storytelling',
      recommendation: 'Strengthen KPI grouping and executive scan order in the landing experience.',
      evidence: evidenceItemsFromTypeScript(typeScriptEvidence),
    }));
  }

  return findings;
}

function buildQualityScore(findings: NormalizedFinding[]): number {
  let score = 82;

  for (const finding of findings) {
    if (finding.severity === 'high') {
      score -= 18;
    } else if (finding.severity === 'medium') {
      score -= 10;
    } else {
      score -= 5;
    }
  }

  return Math.max(25, score);
}

export function reviewFabricAppSurface(
  surface: AnalyzableSurface,
  _profile: AnalyzerProfileId = 'fabricAppQuality',
): FabricAppReviewResult {
  if (surface.surfaceType !== 'fabricApp') {
    throw new Error('FabricAppReviewAnalyzer accepts only Fabric App surfaces.');
  }

  const typeScriptEvidence = extractTypeScriptEvidence(surface.sourceLocation);
  const navigationEvidence = extractNavigationEvidence(surface.sourceLocation);
  const tokenEvidence = extractDesignTokenEvidence(surface.sourceLocation);
  const normalizedFindings = buildFindings(typeScriptEvidence, navigationEvidence, tokenEvidence);
  const evidence = [
    ...evidenceItemsFromTypeScript(typeScriptEvidence),
    ...evidenceItemsFromNavigation(navigationEvidence),
    ...evidenceItemsFromTokens(tokenEvidence),
  ];

  return {
    qualityScore: buildQualityScore(normalizedFindings),
    summary: `Fabric App review produced ${normalizedFindings.length} finding(s) from TypeScript layout, navigation, and design-token evidence.`,
    remediationGuidance: [...new Set(normalizedFindings.map((finding) => finding.recommendation))],
    evidence,
    normalizedFindings,
  };
}
