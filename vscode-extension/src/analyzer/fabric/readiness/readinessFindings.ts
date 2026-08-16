import type {
  FabricAppReadinessAssessment,
  NormalizedFinding,
  NormalizedFindingImpactArea,
  NormalizedFindingSeverity,
  ScoreResult,
} from '../../contracts/scorePanel';
import {
  getDefaultFabricScoringConfig,
  type FabricScoringConfig,
} from '../config/fabricScoringConfig';

function sanitizeIdPart(value: string): string {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
}

function inferImpactArea(text: string): NormalizedFindingImpactArea {
  const lowered = text.toLowerCase();
  if (lowered.includes('navigation')) {
    return 'navigation';
  }

  if (lowered.includes('accessibility')) {
    return 'accessibility';
  }

  if (lowered.includes('semantic')) {
    return 'metadata';
  }

  if (lowered.includes('narrative')) {
    return 'storytelling';
  }

  return 'layout';
}

function inferSeverity(text: string): NormalizedFindingSeverity {
  const lowered = text.toLowerCase();
  if (lowered.includes('accessibility') || lowered.includes('hidden-visual')) {
    return 'high';
  }

  return 'medium';
}

export function buildFabricReadinessFindings(
  result: ScoreResult,
  readiness: FabricAppReadinessAssessment,
  scoringConfig: FabricScoringConfig = getDefaultFabricScoringConfig(),
): NormalizedFinding[] {
  const findings: NormalizedFinding[] = [];
  const findingConfig = scoringConfig.readiness.findings;
  const thresholds = scoringConfig.readiness.thresholds;

  if (readiness.candidatePages.length > 0) {
    findings.push({
      id: 'fabric-readiness-good-candidate',
      title: 'Good Fabric App Candidate',
      summary: readiness.migrationSummary,
      severity: readiness.readinessBand === 'strongCandidate' ? 'low' : 'info',
      confidence: findingConfig.goodCandidateConfidence,
      scope: 'report',
      detectionType: 'deterministic',
      affectedPages: readiness.candidatePages,
      impactArea: 'layout',
      frameworkImpact: ['Fabric App Readiness'],
      recommendation: readiness.recommendedNextActions[0] ?? 'Start with the strongest migration-candidate pages first.',
      sourceKind: 'fabricAppReadiness',
      sourceSection: 'issues',
      evidence: readiness.evidence.slice(0, 2).map((item) => ({
        kind: 'readiness',
        label: item.label,
        pageName: item.pageName,
        detail: item.detail,
      })),
    });
  }

  for (const blocker of readiness.blockers) {
    findings.push({
      id: `fabric-readiness-blocker-${sanitizeIdPart(blocker)}`,
      title: 'Migration Blocker',
      summary: blocker,
      severity: inferSeverity(blocker),
      confidence: findingConfig.blockerConfidence,
      scope: 'report',
      detectionType: 'deterministic',
      affectedPages: readiness.pageAssessments
        .filter((page) => page.blockers.includes(blocker))
        .map((page) => page.pageName),
      impactArea: inferImpactArea(blocker),
      frameworkImpact: ['Fabric App Readiness'],
      recommendation: readiness.recommendedNextActions.find((action) => inferImpactArea(action) === inferImpactArea(blocker))
        ?? 'Reduce the portability blocker before migrating.',
      sourceKind: 'fabricAppReadiness',
      sourceSection: 'issues',
      evidence: readiness.evidence
        .filter((item) => readiness.pageAssessments.some((page) => page.pageName === item.pageName && page.blockers.includes(blocker)))
        .slice(0, 2)
        .map((item) => ({
          kind: 'readiness',
          label: item.label,
          pageName: item.pageName,
          detail: item.detail,
        })),
    });
  }

  for (const page of readiness.pageAssessments.filter((entry) => entry.candidateState === 'redesignRequired' || entry.candidateState === 'keepAsReport')) {
    findings.push({
      id: `fabric-readiness-redesign-${sanitizeIdPart(page.pageName)}`,
      title: 'Redesign Required',
      summary: `${page.pageName} needs redesign before it becomes a strong app migration candidate.`,
      severity: page.candidateState === 'keepAsReport' ? 'high' : 'medium',
      confidence: findingConfig.redesignConfidence,
      scope: 'page',
      detectionType: 'deterministic',
      affectedPages: [page.pageName],
      impactArea: inferImpactArea(page.redesignRequiredAreas[0] ?? 'layout'),
      frameworkImpact: ['Fabric App Readiness'],
      recommendation: page.migrationNotes[0] ?? 'Clarify the page structure before migration.',
      sourceKind: 'fabricAppReadiness',
      sourceSection: 'issues',
      evidence: page.evidence.slice(0, 2).map((item) => ({
        kind: 'readiness',
        label: item.label,
        pageName: item.pageName,
        detail: item.detail,
      })),
    });
  }

  for (const pattern of readiness.unsupportedPatterns) {
    findings.push({
      id: `fabric-readiness-unsupported-${sanitizeIdPart(pattern)}`,
      title: 'Unsupported Pattern',
      summary: pattern,
      severity: inferSeverity(pattern),
      confidence: findingConfig.unsupportedPatternConfidence,
      scope: 'report',
      detectionType: 'deterministic',
      affectedPages: readiness.pageAssessments
        .filter((page) => page.unsupportedPatterns.includes(pattern))
        .map((page) => page.pageName),
      impactArea: inferImpactArea(pattern),
      frameworkImpact: ['Fabric App Readiness'],
      recommendation: 'Reduce Power BI-only interaction dependencies before migration.',
      sourceKind: 'fabricAppReadiness',
      sourceSection: 'issues',
      evidence: [],
    });
  }

  for (const page of readiness.pageAssessments.filter((entry) => entry.readinessDimensions.visualizationAsCodeOpportunity >= thresholds.visualizationOpportunityScore)) {
    findings.push({
      id: `fabric-readiness-viz-opportunity-${sanitizeIdPart(page.pageName)}`,
      title: 'Visualization Opportunity',
      summary: `${page.pageName} has structure that should translate relatively well into a code-first app surface.`,
      severity: 'info',
      confidence: findingConfig.visualizationOpportunityConfidence,
      scope: 'page',
      detectionType: 'deterministic',
      affectedPages: [page.pageName],
      impactArea: 'kpiEffectiveness',
      frameworkImpact: ['Fabric App Readiness'],
      recommendation: 'Use this page as an early candidate when prototyping app-native visualization patterns.',
      sourceKind: 'fabricAppReadiness',
      sourceSection: 'issues',
      evidence: page.evidence
        .filter((item) => item.kind === 'semanticModel' || item.kind === 'portability')
        .map((item) => ({
          kind: 'readiness',
          label: item.label,
          pageName: item.pageName,
          detail: item.detail,
        })),
    });
  }

  return findings;
}
