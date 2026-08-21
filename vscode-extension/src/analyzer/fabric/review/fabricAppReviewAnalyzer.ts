import type { AnalyzerProfileId } from '../../analyzers/types';
import type { NormalizedFinding } from '../../contracts/scorePanel';
import {
  getDefaultFabricScoringConfig,
  type FabricScoringConfig,
} from '../config/fabricScoringConfig';
import { createRepositorySnapshot } from '../../project/repoSnapshot';
import type { AnalyzableSurface } from '../../surfaces/types';
import { extractDesignTokenEvidence } from './designTokenEvidence';
import { extractNavigationEvidence } from './navigationEvidence';
import { extractScreenshotEvidence } from './screenshotEvidence';
import { extractSemanticModelEvidence } from './semanticModelEvidence';
import { extractTypeScriptEvidence } from './typescriptEvidence';
import type {
  FabricAppEvidenceItem,
  FabricAppReviewResult,
  NavigationEvidenceReport,
  ScreenshotEvidenceReport,
  SemanticModelEvidenceReport,
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

function evidenceItemsFromScreenshots(report: ScreenshotEvidenceReport): FabricAppEvidenceItem[] {
  return report.captures.map((capture) => ({
    kind: 'screenshot' as const,
    label: 'Screenshot evidence',
    summary: capture.stateName
      ? `${capture.pageName} capture shows state "${capture.stateName}".`
      : `${capture.pageName} capture is available for review.`,
    filePath: capture.filePath,
    pageName: capture.pageName,
    stateName: capture.stateName,
  }));
}

function evidenceItemsFromSemanticModel(report: SemanticModelEvidenceReport): FabricAppEvidenceItem[] {
  return report.signals.map((signal) => ({
    kind: 'semanticModel' as const,
    label: 'Semantic model evidence',
    summary: signal.summary,
    filePath: signal.filePath,
  }));
}

function buildFinding(params: {
  id: string;
  title: string;
  summary: string;
  severity: NormalizedFinding['severity'];
  impactArea: NormalizedFinding['impactArea'];
  recommendation: string;
  evidence: FabricAppEvidenceItem[];
  confidence: number;
}): NormalizedFinding {
  return {
    id: params.id,
    title: params.title,
    summary: params.summary,
    severity: params.severity,
    confidence: params.confidence,
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
  screenshotEvidence: ScreenshotEvidenceReport,
  semanticModelEvidence: SemanticModelEvidenceReport,
  scoringConfig: FabricScoringConfig,
): NormalizedFinding[] {
  const findings: NormalizedFinding[] = [];
  const screenshotItems = evidenceItemsFromScreenshots(screenshotEvidence);
  const semanticItems = evidenceItemsFromSemanticModel(semanticModelEvidence);
  const confidence = scoringConfig.review.findingConfidence;

  if (tokenEvidence.bypasses.length > 0) {
    findings.push(buildFinding({
      id: 'fabric-token-bypass',
      title: 'Token inconsistencies were detected',
      summary: 'Hard-coded color or spacing values bypass the shared token layer.',
      severity: 'medium',
      impactArea: 'layout',
      recommendation: 'Standardize token usage so color, spacing, and typography stay consistent across the app.',
      evidence: [
        ...evidenceItemsFromTokens({ tokens: [], bypasses: tokenEvidence.bypasses }),
        ...semanticItems.slice(0, 1),
      ],
      confidence,
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
      evidence: [
        ...evidenceItemsFromNavigation(navigationEvidence),
        ...screenshotItems.slice(0, 2),
        ...semanticItems.slice(0, 1),
      ],
      confidence,
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
      evidence: [
        ...evidenceItemsFromNavigation(navigationEvidence).filter((item) => item.summary.toLowerCase().includes('detail')),
        ...screenshotItems.filter((item) => item.pageName?.toLowerCase().includes('overview')).slice(0, 1),
        ...semanticItems.slice(0, 1),
      ],
      confidence,
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
      evidence: [
        ...evidenceItemsFromTypeScript(typeScriptEvidence),
        ...screenshotItems.slice(0, 1),
        ...semanticItems.slice(0, 2),
      ],
      confidence,
    }));
  }

  return findings;
}

function buildQualityScore(
  findings: NormalizedFinding[],
  scoringConfig: FabricScoringConfig,
): number {
  let score = scoringConfig.review.qualityScore.base;
  const penalties = scoringConfig.review.qualityScore.penalties;

  for (const finding of findings) {
    if (finding.severity === 'high') {
      score -= penalties.high;
    } else if (finding.severity === 'medium') {
      score -= penalties.medium;
    } else if (finding.severity === 'info') {
      score -= penalties.info;
    } else {
      score -= penalties.low;
    }
  }

  return Math.max(scoringConfig.review.qualityScore.minimum, score);
}

export async function reviewFabricAppSurface(
  surface: AnalyzableSurface,
  _profile: AnalyzerProfileId = 'fabricAppQuality',
  scoringConfig: FabricScoringConfig = getDefaultFabricScoringConfig(),
): Promise<FabricAppReviewResult> {
  void _profile;
  if (surface.surfaceType !== 'fabricApp') {
    throw new Error('FabricAppReviewAnalyzer accepts only Fabric App surfaces.');
  }

  const snapshot = await createRepositorySnapshot(surface.sourceLocation);

  try {
    const typeScriptEvidence = await extractTypeScriptEvidence(snapshot);
    const navigationEvidence = await extractNavigationEvidence(snapshot);
    const tokenEvidence = await extractDesignTokenEvidence(snapshot);
    const screenshotEvidence = await extractScreenshotEvidence(
      snapshot,
      navigationEvidence.routes.map((route) => route.label),
    );
    const semanticModelEvidence = await extractSemanticModelEvidence(snapshot);
    const normalizedFindings = buildFindings(
      typeScriptEvidence,
      navigationEvidence,
      tokenEvidence,
      screenshotEvidence,
      semanticModelEvidence,
      scoringConfig,
    );
    const evidence = [
      ...evidenceItemsFromTypeScript(typeScriptEvidence),
      ...evidenceItemsFromNavigation(navigationEvidence),
      ...evidenceItemsFromTokens(tokenEvidence),
      ...evidenceItemsFromScreenshots(screenshotEvidence),
      ...evidenceItemsFromSemanticModel(semanticModelEvidence),
    ];

    return {
      qualityScore: buildQualityScore(normalizedFindings, scoringConfig),
      summary: `Fabric App review produced ${normalizedFindings.length} finding(s) from TypeScript layout, navigation, design-token, screenshot, and semantic-model evidence.`,
      remediationGuidance: [...new Set(normalizedFindings.map((finding) => finding.recommendation))],
      evidence,
      normalizedFindings,
    };
  } finally {
    snapshot.dispose();
  }
}
