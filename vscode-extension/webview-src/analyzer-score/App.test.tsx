import React from 'react';
import { act, fireEvent, render, screen, within } from '@testing-library/react';
import '@testing-library/jest-dom';
import App from './App';
import type {
  ScorePanelState,
  ScorePanelWebviewToHostMessagePayload,
} from '../../src/analyzer/contracts/scorePanel';
import { buildScorePanelState, withScorePanelEnvelope } from '../../src/views/scorePanelProtocol';

const postMessage = jest.fn();
let originalDispatchEvent: typeof window.dispatchEvent;

function expectLastPostedMessage(message: ScorePanelWebviewToHostMessagePayload): void {
  expect(postMessage).toHaveBeenLastCalledWith(withScorePanelEnvelope(message));
}

const scoreState: ScorePanelState = buildScorePanelState({
  config: {
    frameworks: [
      { id: 'gestalt', name: 'Gestalt Principles', enabled: true, weight: 60 },
      { id: 'cognitive', name: 'Cognitive Load', enabled: true, weight: 40 },
    ],
    navigationScoring: {
      enabled: true,
      weight: 25,
    },
    governance: [],
  },
  selectedPageIndex: 0,
  intentFeedback: [],
  fixSelection: {
    selectedOpportunityIds: [],
    compatibility: {
      isCompatible: true,
      compatibleOpportunityIds: [],
      blockingOpportunityIds: [],
      blockingReasons: [],
    },
    approvalState: 'NeedsPreview',
  },
  fixApplySessions: [],
  reviewPacketPreviewProfile: 'consultant',
  reviewPacketPreviewTemplateVariant: 'brandedConsultant',
  result: {
    gestaltScore: 84,
    cognitiveLoadScore: 72,
    dataInkScore: 80,
    accessibilityScore: 70,
    visualBestPracticesScore: 78,
    stephenFewScore: 66,
    enterpriseGovernanceScore: 74,
    tufteScore: 68,
    graphicalPerceptionScore: 70,
    densityScore: 64,
    narrativeScore: 69,
    compositeScore: 77,
    feedback: {
      gestalt: [
        { ok: true, text: 'Grid alignment: All visuals align to the grid.', findingType: 'strongHeuristic', earnedPoints: 35, possiblePoints: 35 },
        { ok: true, text: 'Figure/ground: KPI cards contrast with supporting charts.', findingType: 'strongHeuristic', earnedPoints: 30, possiblePoints: 30 },
        { ok: false, text: 'Similarity: 7 visual types may cause noise — aim for 2–5 distinct types.', findingType: 'strongHeuristic', earnedPoints: 0, possiblePoints: 20 },
        { ok: true, text: 'Visual presence: Report contains data visuals.', findingType: 'objective', earnedPoints: 15, possiblePoints: 15 },
        { ok: false, text: 'Surface treatment: Rounded cards and flat cards mix across repeated pages.', findingType: 'stylePreference' },
      ],
      cognitiveLoad: [
        {
          ok: false,
          text: 'Visual density: Several visuals compete for attention — simplify the page or split it into sub-pages.',
          findingType: 'strongHeuristic',
          earnedPoints: 72,
          possiblePoints: 100,
          affectedVisuals: [
            {
              pageName: 'Overview',
              visualId: 'd8427472eb598a9b5946',
              visualType: 'actionButton',
            },
          ],
        },
      ],
    },
    pageCount: 2,
    recommendations: ['[High] Layout: Snap visuals to grid'],
    reportPath: '/tmp/Sales.Report',
    scoredAt: '2026-05-02T20:00:00.000Z',
    dataVisualCount: 12,
    navigationVisualCount: 4,
    hiddenVisualCount: 1,
    inferredStorySummary: {
      intentProfile: 'executiveOverview',
      storyArchetype: 'executive overview + trend + comparison',
      inferredStory: 'This page appears to summarize Revenue performance over time, with region comparison as supporting evidence.',
      confidence: 'high',
      evidence: ['Visible title: Executive Overview', '2 KPI cards in the top scan path'],
    },
    pageIntentProfile: {
      inferredProfile: 'executive',
      actionabilityExpectation: 'high',
      reviewGuidance: ['Executive pages should expose the decision, target, and exception path quickly.'],
      evidence: ['2 KPI cards in the top band'],
    },
    actionabilityBreakdown: {
      score: 58,
      targetBenchmarkPresent: true,
      exceptionVisibility: false,
      urgencySignaling: false,
      priorPeriodContext: true,
      drillPathPresent: true,
      expectationLevel: 'high',
      strengths: ['Prior-period context is visible.'],
      gaps: ['Exception visibility is weak.'],
      summary: 'The page includes some decision context but still hides the main exception.',
    },
    benchmarkComparison: {
      archetype: 'executive scorecard',
      benchmarkLabel: 'Executive-ready benchmark',
      comparativePosition: 'mixed',
      beautifulButUseless: false,
      insight: 'The page is readable, but exception visibility is still weaker than the benchmark.',
      strengths: ['Clear KPI band'],
      gaps: ['Weak exception callout'],
    },
    pagePurposeAnalysis: {
      inferredPurpose: 'Executive',
      confidence: 'high',
      actionabilityScore: 58,
      benchmarkStatus: 'Mixed against expected',
      topGaps: ['Exception visibility is weak.'],
      whyThisMatters: 'This page appears intended for executive review but lacks the decision context expected for that audience. Decision makers may misinterpret KPI values without targets or prior-period comparison.',
    },
    reportConsistencySummary: {
      consistentTitleAnchors: false,
      consistentFilterBand: true,
      consistentMetricLabels: false,
      consistentSemanticColors: false,
      overallFinding: '3 cross-page consistency issue(s) detected across layout, navigation, semanticColors.',
      affectedPages: ['Overview', 'Details'],
      issueCount: 3,
      issues: [
        {
          category: 'layout',
          issueCategory: 'layoutPattern',
          overallFinding: 'Details breaks from the dominant layout pattern used on peer pages.',
          affectedPages: ['Details'],
          severity: 'medium',
          confidence: 'high',
          recommendedRemediation: 'Keep repeated pages on the dominant layout pattern.',
        },
        {
          category: 'navigation',
          issueCategory: 'navigationPattern',
          overallFinding: 'Navigation patterns differ across the report. Detection is partially detectable from PBIR metadata.',
          affectedPages: ['Details'],
          severity: 'medium',
          confidence: 'medium',
          recommendedRemediation: 'Keep navigation controls in one predictable zone.',
        },
        {
          category: 'semanticColors',
          issueCategory: 'semanticColorDrift',
          overallFinding: 'Semantic color consistency: status:on-track uses multiple colors across pages.',
          affectedPages: ['Overview', 'Details'],
          severity: 'medium',
          confidence: 'high',
          recommendedRemediation: 'Keep the same semantic roles on the same colors across pages.',
        },
      ],
      findings: [
        'Title anchors: title anchors shift between left and center alignment.',
        'Metric label consistency: labels shift between YTD Sales and Sales YTD.',
        'Semantic color consistency: status:on-track uses multiple colors across pages.',
      ],
    },
    normalizedFindings: [
      {
        id: 'overview-actionability',
        title: 'Actionability gap',
        summary: 'The page includes some decision context but still hides the main exception.',
        severity: 'high',
        confidence: 88,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'actionability',
        frameworkImpact: ['Narrative Design', 'Stephen Few'],
        recommendation: 'Exception visibility is weak.',
        sourceKind: 'actionability',
        sourceSection: 'issues',
        evidence: [
          {
            kind: 'actionability',
            label: 'Actionability score',
            pageName: 'Overview',
            detail: '58/100 · The page includes some decision context but still hides the main exception.',
          },
        ],
      },
      {
        id: 'details-benchmark',
        title: 'Beautiful but weakly actionable',
        summary: 'Beautiful but useless: the page looks polished, but the decision path is still weak.',
        severity: 'high',
        confidence: 84,
        scope: 'page',
        detectionType: 'deterministic',
        affectedPages: ['Details'],
        impactArea: 'benchmark',
        frameworkImpact: ['Narrative Design', 'Visual Best Practices'],
        recommendation: 'Decision support is weak',
        sourceKind: 'benchmark',
        sourceSection: 'issues',
        evidence: [
          {
            kind: 'benchmark',
            label: 'analytical deep dive',
            pageName: 'Details',
            detail: 'Beautiful but useless: the page looks polished, but the decision path is still weak.',
          },
        ],
      },
      {
        id: 'cross-page-navigation',
        title: 'navigation',
        summary: 'Navigation patterns differ across the report. Detection is partially detectable from PBIR metadata.',
        severity: 'medium',
        confidence: 74,
        scope: 'crossPage',
        detectionType: 'deterministic',
        affectedPages: ['Details'],
        impactArea: 'navigation',
        frameworkImpact: ['Enterprise Governance'],
        recommendation: 'Keep navigation controls in one predictable zone.',
        sourceKind: 'reportConsistency',
        sourceSection: 'issues',
        evidence: [
          {
            kind: 'consistency',
            label: 'navigationPattern',
            detail: 'Navigation patterns differ across the report. Detection is partially detectable from PBIR metadata.',
          },
        ],
      },
      {
        id: 'fabric-readiness-good-candidate',
        title: 'Good Fabric App Candidate',
        summary: 'The overview page is a promising migration candidate with relatively low redesign effort.',
        severity: 'info',
        confidence: 82,
        scope: 'report',
        detectionType: 'deterministic',
        affectedPages: ['Overview'],
        impactArea: 'layout',
        frameworkImpact: ['Fabric App Readiness'],
        recommendation: 'Start with the strongest migration-candidate pages first.',
        sourceKind: 'fabricAppReadiness',
        sourceSection: 'issues',
        evidence: [
          {
            kind: 'readiness',
            label: 'Migration rationale',
            pageName: 'Overview',
            detail: 'Overall readiness score 71/100.',
          },
        ],
      },
    ],
    overviewSummary: {
      overallScore: 77,
      maturityBand: 'Mature',
      riskBand: 'Elevated',
      benchmarkSummary: 'Executive-ready benchmark: The page is readable, but exception visibility is still weaker than the benchmark.',
      executiveSummary: 'This report is mature with elevated risk based on the current score and issue mix.',
      severityDistribution: {
        high: 2,
        medium: 1,
        low: 0,
        info: 0,
      },
      topStrengths: [
        {
          id: 'strength-1',
          title: 'Clear KPI band',
          detail: 'The overview opens with a strong KPI entry point.',
          affectedPages: ['Overview'],
          sourceFindingIds: [],
        },
      ],
      topWeaknesses: [
        {
          id: 'weakness-1',
          title: 'Actionability gap',
          detail: 'The page includes some decision context but still hides the main exception.',
          affectedPages: ['Overview'],
          severity: 'high',
          sourceFindingIds: ['overview-actionability'],
        },
      ],
      topIssues: [
        {
          id: 'issue-1',
          title: 'Actionability gap',
          detail: 'The page includes some decision context but still hides the main exception.',
          affectedPages: ['Overview'],
          severity: 'high',
          sourceFindingIds: ['overview-actionability'],
        },
      ],
      topActions: [
        {
          id: 'action-1',
          title: 'Actionability gap',
          detail: 'Exception visibility is weak.',
          severity: 'high',
          affectedPages: ['Overview'],
          sourceFindingIds: ['overview-actionability'],
        },
      ],
      crossPageSummary: {
        headline: '1 of 2 pages show stronger consistency signals.',
        details: ['Navigation patterns drift across detail pages.'],
        consistentPages: 1,
        totalPages: 2,
      },
      readinessSummary: {
        readinessScore: 71,
        readinessBand: 'possibleCandidate',
        candidatePageCount: 1,
        migrationBlockerCount: 2,
        estimatedRedesignEffort: 'medium',
      },
    },
    analysisContext: {
      surfaceType: 'pbirReport',
      analyzerType: 'fabricAppReadiness',
      analyzerProfile: 'migrationReadiness',
      surfaceDisplayName: 'Sales.Report',
      sourceLocation: '/tmp/Sales.Report',
      availableAnalyzerTypes: ['pbirDesignReview', 'fabricAppReadiness'],
      availableAnalyzerProfiles: ['default', 'migrationReadiness'],
    },
    readinessAssessment: {
      overallReadinessScore: 71,
      readinessBand: 'possibleCandidate',
      migrationSummary: 'This PBIR report has promising migration candidates, but navigation complexity should be reduced first.',
      candidatePages: ['Overview'],
      blockers: [
        'Navigation complexity is likely too Power BI-specific for direct migration.',
        'Low accessibility portability requires redesign before migration.',
      ],
      unsupportedPatterns: [
        'Hidden-visual state switching is difficult to translate directly.',
      ],
      redesignRequiredAreas: ['navigation portability', 'accessibility portability'],
      recommendedNextActions: [
        'Simplify navigation before treating the report as an app candidate.',
        'Reduce Power BI-only dependencies such as slicer-heavy interaction patterns.',
      ],
      estimatedRedesignEffort: 'medium',
      dimensionScores: {
        layoutPortability: 75,
        interactionPortability: 60,
        narrativePortability: 72,
        semanticModelSuitability: 74,
        navigationPortability: 54,
        governancePortability: 70,
        accessibilityPortability: 58,
        visualizationAsCodeOpportunity: 68,
      },
      pageAssessments: [
        {
          pageName: 'Overview',
          readinessScore: 80,
          readinessDimensions: {
            layoutPortability: 82,
            interactionPortability: 78,
            narrativePortability: 80,
            semanticModelSuitability: 76,
            navigationPortability: 72,
            governancePortability: 75,
            accessibilityPortability: 74,
            visualizationAsCodeOpportunity: 70,
          },
          candidateState: 'strongCandidate',
          positiveSignals: ['Focused visual scope is easier to recompose as an app surface.'],
          blockers: [],
          unsupportedPatterns: [],
          redesignRequiredAreas: [],
          migrationNotes: ['Keep the page purpose tight during migration.'],
          evidence: [
            {
              kind: 'portability',
              label: 'Migration rationale',
              pageName: 'Overview',
              detail: 'Overall readiness score 80/100.',
            },
          ],
        },
        {
          pageName: 'Details',
          readinessScore: 42,
          readinessDimensions: {
            layoutPortability: 48,
            interactionPortability: 38,
            narrativePortability: 46,
            semanticModelSuitability: 56,
            navigationPortability: 34,
            governancePortability: 52,
            accessibilityPortability: 40,
            visualizationAsCodeOpportunity: 44,
          },
          candidateState: 'redesignRequired',
          positiveSignals: ['The semantic model can still support a future app shell.'],
          blockers: ['Heavy slicer-driven interaction does not map cleanly to an app flow.'],
          unsupportedPatterns: ['Bookmark-style hidden-state switching is difficult to migrate directly.'],
          redesignRequiredAreas: ['interaction portability', 'navigation portability'],
          migrationNotes: ['Recompose this page into a simpler task-focused app experience.'],
          evidence: [
            {
              kind: 'portability',
              label: 'Migration rationale',
              pageName: 'Details',
              detail: 'Overall readiness score 42/100.',
            },
          ],
        },
      ],
      evidence: [
        {
          kind: 'portability',
          label: 'Migration rationale',
          pageName: 'Overview',
          detail: 'Overall readiness score 80/100.',
        },
        {
          kind: 'portability',
          label: 'Migration rationale',
          pageName: 'Details',
          detail: 'Overall readiness score 42/100.',
        },
      ],
      governanceSignals: [
        {
          category: 'navigation',
          severity: 'medium',
          summary: 'Navigation portability falls below the app-ready threshold.',
          pageName: 'Details',
        },
      ],
    },
    fixPlan: [
      {
        id: 'fix-overview-actionability',
        title: 'Add benchmarks and decision context',
        detail: 'Resolve 2 related findings through one remediation step.',
        severity: 'high',
        effort: 'low',
        impact: 'high',
        why: 'Reduces risk of KPI misinterpretation.',
        scope: 'page',
        affectedPages: ['Overview'],
        recommendedAction: 'Add target benchmarks, prior-period context, and urgency cues.',
        resolvedOutcomes: ['Benchmark gap', 'Actionability gap'],
        sourceFindingIds: ['overview-actionability', 'details-benchmark'],
      },
      {
        id: 'fix-cross-page-navigation',
        title: 'Standardize navigation cues',
        detail: 'Navigation patterns differ across the report. Detection is partially detectable from PBIR metadata.',
        severity: 'medium',
        effort: 'high',
        impact: 'medium',
        why: 'Makes navigation more predictable across related pages.',
        scope: 'crossPage',
        affectedPages: ['Details'],
        recommendedAction: 'Keep navigation controls in one predictable zone.',
        resolvedOutcomes: ['Navigation consistency'],
        sourceFindingIds: ['cross-page-navigation'],
      },
    ],
    fixOpportunities: [
      {
        id: 'fixopp-overview-title',
        remediationItemId: 'fix-overview-actionability',
        title: 'Standardize overview title anchor',
        category: 'title',
        summary: 'Normalize the existing title anchor for executive scanning.',
        confidence: 95,
        safetyClass: 'safe',
        affectedPages: ['Overview'],
        targetObjectIds: ['title-textbox-1'],
        sourceFindingIds: ['overview-actionability'],
        expectedResolutions: ['Benchmark gap', 'Actionability gap'],
        mutations: [
          {
            id: 'mutation-1',
            pageName: 'Overview',
            targetObjectId: 'title-textbox-1',
            targetFile: '/tmp/Sales.Report/definition/pages/Overview/visuals/title-textbox-1/visual.json',
            propertyPath: 'position.x',
            mutationType: 'setPosition',
            before: 42,
            after: 24,
          },
          {
            id: 'mutation-2',
            pageName: 'Overview',
            targetObjectId: 'title-textbox-1',
            targetFile: '/tmp/Sales.Report/definition/pages/Overview/visuals/title-textbox-1/visual.json',
            propertyPath: 'title.text',
            mutationType: 'setTitleText',
            before: 'Executive Overview',
            after: 'Overview',
          },
        ],
        previewRows: [
          {
            pageName: 'Overview',
            objectId: 'title-textbox-1',
            property: 'position.x',
            before: 42,
            after: 24,
          },
          {
            pageName: 'Overview',
            objectId: 'title-textbox-1',
            property: 'title.text',
            before: 'Executive Overview',
            after: 'Overview',
          },
        ],
        rollbackPlan: {
          id: 'rollback-fixopp-overview-title',
          fixOpportunityId: 'fixopp-overview-title',
          fileBackups: [
            {
              targetFile: '/tmp/Sales.Report/definition/pages/Overview/visuals/title-textbox-1/visual.json',
              beforeContent: '{"position":{"x":42},"title":{"text":"Executive Overview"}}',
            },
          ],
          reverseMutations: [],
        },
        state: 'Previewed',
      },
      {
        id: 'fixopp-overview-chart',
        remediationItemId: 'fix-overview-actionability',
        title: 'Normalize chart top spacing',
        category: 'alignment',
        summary: 'Align the lead chart with the title anchor.',
        confidence: 95,
        safetyClass: 'safe',
        affectedPages: ['Overview'],
        targetObjectIds: ['chart-hero-1'],
        sourceFindingIds: ['overview-actionability'],
        expectedResolutions: ['Actionability gap'],
        mutations: [
          {
            id: 'mutation-3',
            pageName: 'Overview',
            targetObjectId: 'chart-hero-1',
            targetFile: '/tmp/Sales.Report/definition/pages/Overview/visuals/chart-hero-1/visual.json',
            propertyPath: 'position.y',
            mutationType: 'setPosition',
            before: 120,
            after: 96,
          },
        ],
        previewRows: [
          {
            pageName: 'Overview',
            objectId: 'chart-hero-1',
            property: 'position.y',
            before: 120,
            after: 96,
          },
        ],
        rollbackPlan: {
          id: 'rollback-fixopp-overview-chart',
          fixOpportunityId: 'fixopp-overview-chart',
          fileBackups: [
            {
              targetFile: '/tmp/Sales.Report/definition/pages/Overview/visuals/chart-hero-1/visual.json',
              beforeContent: '{"position":{"y":120}}',
            },
          ],
          reverseMutations: [],
        },
        state: 'Previewed',
      },
    ],
    personaPresentation: {
      activePersona: 'default',
      availablePersonas: [
        {
          id: 'default',
          label: 'Default',
          description: 'Balanced prioritization across severity, confidence, and scope.',
          emphasizedImpactAreas: [],
          emphasizedScopes: [],
          defaultSeverityFilter: ['high', 'medium', 'low', 'info'],
          overviewEmphasis: ['issues', 'actions', 'weaknesses', 'benchmark', 'consistency'],
          fixPlanEmphasis: ['severity', 'scope'],
        },
        {
          id: 'executive',
          label: 'Executive',
          description: 'Emphasize decision support, KPI clarity, and narrative issues first.',
          emphasizedImpactAreas: ['actionability', 'kpiEffectiveness', 'storytelling', 'benchmark'],
          emphasizedScopes: ['crossPage', 'page'],
          defaultSeverityFilter: ['high', 'medium'],
          overviewEmphasis: ['issues', 'actions', 'benchmark', 'consistency'],
          fixPlanEmphasis: ['severity', 'scope', 'crossPage'],
        },
        {
          id: 'consultant',
          label: 'Consultant',
          description: 'Prioritize fix sequencing, remediation clarity, and evidence-backed issues.',
          emphasizedImpactAreas: ['actionability', 'storytelling', 'governance', 'navigation'],
          emphasizedScopes: ['crossPage', 'page'],
          defaultSeverityFilter: ['high', 'medium'],
          overviewEmphasis: ['issues', 'actions', 'weaknesses'],
          fixPlanEmphasis: ['severity', 'effort', 'evidence', 'scope', 'crossPage'],
        },
        {
          id: 'governance',
          label: 'Governance',
          description: 'Emphasize cross-page consistency, standards, and semantic drift.',
          emphasizedImpactAreas: ['governance', 'metadata', 'navigation', 'layout'],
          emphasizedScopes: ['crossPage', 'report'],
          defaultSeverityFilter: ['high', 'medium', 'low'],
          overviewEmphasis: ['issues', 'consistency', 'actions'],
          fixPlanEmphasis: ['crossPage', 'scope', 'severity'],
        },
        {
          id: 'accessibility',
          label: 'Accessibility',
          description: 'Emphasize accessibility, readability, and navigation usability.',
          emphasizedImpactAreas: ['accessibility', 'navigation', 'density'],
          emphasizedScopes: ['page', 'crossPage'],
          defaultSeverityFilter: ['high', 'medium', 'low'],
          overviewEmphasis: ['issues', 'actions', 'weaknesses'],
          fixPlanEmphasis: ['severity', 'scope', 'evidence'],
        },
      ],
    },
    crossPageMatrix: {
      dimensions: ['layout', 'story', 'accessibility', 'consistency', 'navigation', 'actionability'],
      rows: [
        {
          pageName: 'Overview',
          cells: [
            { pageName: 'Overview', dimension: 'layout', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped layout findings were generated for Overview.' },
            { pageName: 'Overview', dimension: 'story', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped story findings were generated for Overview.' },
            { pageName: 'Overview', dimension: 'accessibility', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped accessibility findings were generated for Overview.' },
            { pageName: 'Overview', dimension: 'consistency', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped consistency findings were generated for Overview.' },
            { pageName: 'Overview', dimension: 'navigation', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped navigation findings were generated for Overview.' },
            { pageName: 'Overview', dimension: 'actionability', severity: 'high', findingCount: 1, highSeverityCount: 1, confidenceAverage: 88, status: 'weak', relatedFindingIds: ['overview-actionability'], summary: 'The page includes some decision context but still hides the main exception.' },
          ],
        },
        {
          pageName: 'Details',
          cells: [
            { pageName: 'Details', dimension: 'layout', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped layout findings were generated for Details.' },
            { pageName: 'Details', dimension: 'story', severity: 'high', findingCount: 1, highSeverityCount: 1, confidenceAverage: 84, status: 'weak', relatedFindingIds: ['details-benchmark'], summary: 'Beautiful but useless: the page looks polished, but the decision path is still weak.' },
            { pageName: 'Details', dimension: 'accessibility', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped accessibility findings were generated for Details.' },
            { pageName: 'Details', dimension: 'consistency', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped consistency findings were generated for Details.' },
            { pageName: 'Details', dimension: 'navigation', severity: 'medium', findingCount: 1, highSeverityCount: 0, confidenceAverage: 74, status: 'weak', relatedFindingIds: ['cross-page-navigation'], summary: 'Navigation patterns differ across the report. Detection is partially detectable from PBIR metadata.' },
            { pageName: 'Details', dimension: 'actionability', severity: undefined, findingCount: 0, highSeverityCount: 0, confidenceAverage: undefined, status: 'unknown', relatedFindingIds: [], summary: 'No mapped actionability findings were generated for Details.' },
          ],
        },
      ],
    },
    pageScores: [
      {
        pageName: 'Overview',
        gestaltScore: 82,
        cognitiveLoadScore: 70,
        dataInkScore: 79,
        accessibilityScore: 70,
        visualBestPracticesScore: 77,
        stephenFewScore: 65,
        enterpriseGovernanceScore: 73,
        tufteScore: 68,
        graphicalPerceptionScore: 69,
        densityScore: 63,
        narrativeScore: 67,
        compositeScore: 75,
        feedback: {
          gestalt: [{ ok: true, text: 'Grid alignment: Overview grid is aligned.', findingType: 'strongHeuristic', earnedPoints: 35, possiblePoints: 35 }],
          cognitiveLoad: [
            {
              ok: false,
              text: 'Visual density: Overview is visually dense — simplify the page or split it into sub-pages.',
              findingType: 'strongHeuristic',
              earnedPoints: 70,
              possiblePoints: 100,
              affectedVisuals: [
                {
                  pageName: 'Overview',
                  visualId: 'd8427472eb598a9b5946',
                  visualType: 'actionButton',
                },
              ],
            },
          ],
        },
        recommendations: ['[High] Layout: Snap visuals to grid'],
        dataVisualCount: 7,
        navigationVisualCount: 2,
        hiddenVisualCount: 1,
        reportConsistencyNotes: [
          'Report consistency: Title anchor placement differs from other pages.',
          'Report consistency: Semantic color meaning differs from other pages.',
        ],
        inferredStorySummary: {
          intentProfile: 'executiveOverview',
          storyArchetype: 'executive overview + trend + comparison',
          inferredStory: 'This page appears to summarize Revenue performance over time, with region comparison as supporting evidence.',
          confidence: 'high',
          evidence: ['Visible title: Executive Overview', '2 KPI cards in the top scan path'],
        },
        pageIntentProfile: {
          inferredProfile: 'executive',
          actionabilityExpectation: 'high',
          reviewGuidance: ['Executive pages should expose the decision, target, and exception path quickly.'],
          evidence: ['2 KPI cards in the top band'],
        },
        actionabilityBreakdown: {
          score: 58,
          targetBenchmarkPresent: true,
          exceptionVisibility: false,
          urgencySignaling: false,
          priorPeriodContext: true,
          drillPathPresent: true,
          expectationLevel: 'high',
          strengths: ['Prior-period context is visible.'],
          gaps: ['Exception visibility is weak.'],
          summary: 'The page includes some decision context but still hides the main exception.',
        },
        benchmarkComparison: {
          archetype: 'executive scorecard',
          benchmarkLabel: 'Executive-ready benchmark',
          comparativePosition: 'mixed',
          beautifulButUseless: false,
          insight: 'The page is readable, but exception visibility is still weaker than the benchmark.',
          strengths: ['Clear KPI band'],
          gaps: ['Weak exception callout'],
        },
        pagePurposeAnalysis: {
          inferredPurpose: 'Executive',
          confidence: 'high',
          actionabilityScore: 58,
          benchmarkStatus: 'Mixed against expected',
          topGaps: ['Exception visibility is weak.'],
          whyThisMatters: 'This page appears intended for executive review but lacks the decision context expected for that audience. Decision makers may misinterpret KPI values without targets or prior-period comparison.',
        },
        visualMetadata: {
          pageName: 'Overview',
          visiblePageTitle: 'Executive Overview',
          strictVisiblePageTitle: 'Executive Overview',
          canvasWidth: 1280,
          canvasHeight: 720,
          semanticColorMap: [
            {
              semanticKey: 'status:on-track',
              displayLabel: 'On Track',
              color: '#00AA00',
              sourceVisualId: 'v1',
              sourcePageName: 'Overview',
            },
          ],
          chartIntentSummary: {
            intent: 'comparison',
            confidence: 'high',
            evidence: ['Revenue by region', 'Top-row KPI cards'],
            fitStatus: 'good',
            recommendedAlternatives: [],
          },
          visualCount: 2,
          visibleTitleVisualCount: 1,
          textVisualCount: 0,
          slicerCount: 0,
          legendVisualCount: 1,
          axisLabelVisualCount: 1,
          dataLabelVisualCount: 0,
          formattedVisualCount: 1,
          visuals: [
            {
              visualId: 'v1',
              visualType: 'barChart',
              x: 0,
              y: 0,
              width: 320,
              height: 180,
              isHidden: false,
              isNavigationElement: false,
              isDecorative: false,
              isSlicer: false,
              visibleTitleText: 'Executive Overview',
              bestVisibleText: 'Executive Overview',
              hasVisibleTitleIntent: true,
              hasLegend: true,
              hasAxisLabels: true,
              hasDataLabels: false,
              categoryHints: ['Region'],
              valueHints: ['Revenue'],
              seriesHints: [],
              measureHints: ['Revenue'],
              backgroundFillColor: '#FFFFFF',
              fontColor: '#111111',
              hasBorder: true,
              cornerRadius: 8,
              hasShadow: false,
              semanticColors: [
                {
                  semanticKey: 'status:on-track',
                  displayLabel: 'On Track',
                  color: '#00AA00',
                  sourceVisualId: 'v1',
                  sourcePageName: 'Overview',
                },
              ],
              chartIntent: {
                intent: 'comparison',
                confidence: 'high',
                evidence: ['Region categories', 'Revenue measure'],
                fitStatus: 'good',
                recommendedAlternatives: [],
              },
            },
          ],
        },
      },
      {
        pageName: 'Details',
        gestaltScore: 86,
        cognitiveLoadScore: 74,
        dataInkScore: 81,
        accessibilityScore: 71,
        visualBestPracticesScore: 79,
        stephenFewScore: 67,
        enterpriseGovernanceScore: 75,
        tufteScore: 69,
        graphicalPerceptionScore: 71,
        densityScore: 65,
        narrativeScore: 70,
        compositeScore: 79,
        feedback: {
          gestalt: [{ ok: true, text: 'Grid alignment: Details grid is aligned.', findingType: 'strongHeuristic', earnedPoints: 35, possiblePoints: 35 }],
          cognitiveLoad: [{ ok: true, text: 'Visual density: Details density is acceptable.', findingType: 'strongHeuristic', earnedPoints: 74, possiblePoints: 100 }],
        },
        recommendations: [],
        dataVisualCount: 5,
        navigationVisualCount: 2,
        hiddenVisualCount: 0,
        reportConsistencyNotes: [
          'Report consistency: Metric label naming differs from other pages.',
        ],
        inferredStorySummary: {
          intentProfile: 'analyticalDeepDive',
          storyArchetype: 'trend',
          inferredStory: 'This page appears to support exploratory analysis of performance, using multiple views and filters to explain differences.',
          confidence: 'medium',
          evidence: ['Visible title: Detail Comparison', 'Lead visual: lineChart using Revenue / Month'],
        },
        pageIntentProfile: {
          inferredProfile: 'analytical',
          actionabilityExpectation: 'medium',
          reviewGuidance: ['Analytical pages should support exploration without hiding the main analytical question.'],
          evidence: ['Interactive slicers suggest exploratory use'],
        },
        actionabilityBreakdown: {
          score: 32,
          targetBenchmarkPresent: false,
          exceptionVisibility: false,
          urgencySignaling: false,
          priorPeriodContext: false,
          drillPathPresent: true,
          expectationLevel: 'medium',
          strengths: ['A supporting evidence path exists.'],
          gaps: ['Add a target or benchmark next to the KPI.', 'Call out the exception that needs action now.'],
          summary: 'The page looks polished but still does not tell a reviewer what action to take.',
        },
        benchmarkComparison: {
          archetype: 'analytical deep dive',
          benchmarkLabel: 'Analytical deep-dive benchmark',
          comparativePosition: 'below',
          beautifulButUseless: true,
          insight: 'Beautiful but useless: the page looks polished, but the decision path is still weak.',
          strengths: ['Clean trend visual'],
          gaps: ['Decision support is weak'],
        },
        pagePurposeAnalysis: {
          inferredPurpose: 'Analytical',
          confidence: 'medium',
          actionabilityScore: 32,
          benchmarkStatus: 'Below expected',
          topGaps: ['Add a target or benchmark next to the KPI.', 'Call out the exception that needs action now.', 'Decision support is weak'],
          whyThisMatters: 'This page appears intended for analytical use but lacks the decision context expected for that audience. Readers may over-trust visual polish even when the decision path is still weaker than expected.',
        },
        visualMetadata: {
          pageName: 'Details',
          visiblePageTitle: 'Detail Comparison',
          strictVisiblePageTitle: 'Detail Comparison',
          semanticColorMap: [],
          chartIntentSummary: {
            intent: 'trend',
            confidence: 'medium',
            evidence: ['Axis labels indicate sequence'],
            fitStatus: 'watch',
            recommendedAlternatives: ['clusteredColumnChart'],
          },
          visualCount: 1,
          visibleTitleVisualCount: 1,
          textVisualCount: 0,
          slicerCount: 1,
          legendVisualCount: 0,
          axisLabelVisualCount: 1,
          dataLabelVisualCount: 1,
          formattedVisualCount: 0,
          visuals: [],
        },
      },
    ],
  },
});

const reviewPacketPreview = {
  reportPath: '/tmp/Sales.Report',
  scoredAt: '2026-05-02T20:00:00.000Z',
  exportedAt: '2026-05-28T10:00:00.000Z',
  compositeScore: 77,
  pageCount: 2,
  reviewSummary: {
    totalPages: 2,
    reviewedPages: 1,
    confirmedPages: 1,
    partialPages: 0,
    mismatchPages: 0,
    unreviewedPages: 1,
  },
  executiveSummary: {
    overallStatus: 'In progress',
    headline: 'Intent validation is underway, but some pages are still unreviewed.',
    reviewCoveragePercent: 50,
  },
  intentValidationSummary: {
    confirmedPages: [
      {
        pageName: 'Overview',
        reviewStatus: 'confirmed',
        inferredIntent: 'executiveOverview',
        storyArchetype: 'executive overview + trend + comparison',
      },
    ],
    partialPages: [],
    mismatchPages: [],
    unreviewedPages: [
      {
        pageName: 'Details',
        reviewStatus: 'unreviewed',
        inferredIntent: 'analyticalDeepDive',
        storyArchetype: 'trend',
      },
    ],
    pagesNeedingReview: [],
  },
  remediationQueue: [
    {
      pageName: 'Details',
      reviewStatus: 'partial',
      reason: 'Needs a clearer variance takeaway.',
      suggestedAction: 'Clarify the intended takeaway with tighter titles, KPI context, or supporting visual evidence.',
    },
  ],
  topRecommendations: [
    'Standardize revenue terminology across pages.',
    'Keep navigation controls in one predictable zone.',
  ],
  crossPageConsistencyRollup: {
    overallFinding: '3 cross-page consistency issue(s) detected across layout, navigation, semanticColors.',
    issueCount: 3,
    affectedPages: ['Overview', 'Details'],
    issuesByCategory: [
      ['Layout', 1],
      ['Navigation', 1],
      ['Semantic Colors', 1],
    ],
    highestSeverity: 'medium',
    remediation: [
      'Keep repeated pages on the dominant layout pattern.',
      'Keep navigation controls in one predictable zone.',
    ],
  },
};

const reviewPacketPreviewHtml = `<!DOCTYPE html>
<html lang="en">
<head>
  <title>FY26 Sales Review Packet Preview</title>
</head>
<body>
  <main>
    <section class="cover">
      <h1>FY26 Sales Review Packet Preview</h1>
      <p>Prepared for: FY26 Sales</p>
    </section>
    <section>
      <h2>Executive Summary</h2>
      <p>Intent validation is underway, but some pages are still unreviewed.</p>
    </section>
  </main>
</body>
</html>`;

describe('Analyzer Score App', () => {
  beforeEach(() => {
    postMessage.mockReset();
    HTMLElement.prototype.scrollIntoView = jest.fn();
    originalDispatchEvent = window.dispatchEvent.bind(window);
    jest.spyOn(window, 'dispatchEvent').mockImplementation((event: Event) => {
      if (event instanceof MessageEvent && event.type === 'message' && event.data && typeof event.data === 'object') {
        const message = event.data as Record<string, unknown>;
        const normalizedMessage = 'protocolVersion' in message
          ? message
          : withScorePanelEnvelope({
              ...(message as { type: string }),
              state: message.type === 'scoreState' && message.state && typeof message.state === 'object'
                && !('protocolVersion' in (message.state as Record<string, unknown>))
                ? buildScorePanelState(message.state as Omit<ScorePanelState, 'protocolVersion' | 'schemaVersion'>)
                : message.state,
            });
        return originalDispatchEvent(new MessageEvent('message', { data: normalizedMessage }));
      }

      return originalDispatchEvent(event);
    });
    (globalThis as unknown as { acquireVsCodeApi: () => { postMessage: typeof postMessage } }).acquireVsCodeApi =
      () => ({
        postMessage,
      });
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('renders the score state and posts tab selection back to the host', async () => {
    render(<App />);

    expect(postMessage).toHaveBeenCalledWith(withScorePanelEnvelope({ type: 'webviewReady' }));

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              reviewPacketPreview,
              reviewPacketPreviewHtml,
            },
          },
        }),
      );
    });

    expect(screen.getByText('Optimization Report')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /review fix plan/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Overview' })).toBeInTheDocument();
    expect(screen.getByText(/Visual mix: 12 data, 4 navigation, 1 hidden/i)).toBeInTheDocument();
    expect(screen.getByText(/Maturity: Mature/i)).toBeInTheDocument();
    expect(screen.getByText(/Risk: Elevated/i)).toBeInTheDocument();
    const readinessOverview = screen.getByLabelText('Fabric App migration readiness');
    expect(within(readinessOverview).getByText('Fabric App migration readiness')).toBeInTheDocument();
    expect(within(readinessOverview).getByText(/Possible Candidate/i)).toBeInTheDocument();
    expect(within(readinessOverview).getByText(/^Readiness score$/i)).toBeInTheDocument();
    expect(within(readinessOverview).getByText(/^Candidate pages$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/workspace review mode/i)).toHaveValue('default');
    expect(screen.getByText(/Review modes change how findings are prioritized and explained/i)).toBeInTheDocument();
    expect(screen.getByText('Cross-page matrix')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Issues' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Fix Plan' })).toBeInTheDocument();
    expect(screen.getAllByText(/The page includes some decision context but still hides the main exception\./i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Fix first:/i).length).toBeGreaterThan(0);
    expect(screen.getByText('Evidence')).toBeInTheDocument();
    expect(screen.getByLabelText(/issue fabric app readiness filter/i)).toBeInTheDocument();
    expect(screen.getByText('Export')).toBeInTheDocument();
    expect(screen.getByText('Review Summary')).toBeInTheDocument();
    expect(screen.getAllByText('Review Packet Preview').length).toBeGreaterThan(0);
    expect(screen.getByText(/Intent validation is underway/i)).toBeInTheDocument();
    expect(screen.getByText(/Review coverage: 50%/i)).toBeInTheDocument();
    const previewFrame = screen.getByTitle(/review packet html preview/i) as HTMLIFrameElement;
    expect(previewFrame.srcdoc).toContain('FY26 Sales Review Packet Preview');
    expect(screen.getByLabelText(/preview profile/i)).toHaveValue('consultant');
    expect(screen.getByLabelText(/consultant template/i)).toHaveValue('brandedConsultant');
    expect(screen.getByText(/Branded consultant packet preview/i)).toBeInTheDocument();
    expect(screen.getByText(/Read-only HTML renderer/i)).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/preview profile/i), {
      target: { value: 'executive' },
    });
    expectLastPostedMessage({
      type: 'setReviewPacketPreviewProfile',
      profile: 'executive',
    });
    fireEvent.change(screen.getByLabelText(/consultant template/i), {
      target: { value: 'standard' },
    });
    expectLastPostedMessage({
      type: 'setReviewPacketPreviewTemplateVariant',
      templateVariant: 'standard',
    });
    fireEvent.click(screen.getByRole('button', { name: /open full packet/i }));
    expectLastPostedMessage({ type: 'openReviewPacketPreview' });
    expect(screen.getByText('Unreviewed')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Not reviewed/i })).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: /export review summary/i }).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Cross-Page Consistency').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/3 cross-page consistency issue\(s\) detected/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Layout').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Navigation').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Semantic Colors').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Affected pages: Details/i).length).toBeGreaterThan(0);
    expect(screen.getByText(/Confidence: medium/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Keep navigation controls in one predictable zone\./i).length).toBeGreaterThan(0);
    expect(screen.getByText(/status:on-track uses multiple colors across pages/i)).toBeInTheDocument();
    expect(screen.getByText('Metadata Overview')).toBeInTheDocument();
    const frameworkEvidenceDetails = screen.getByText('Design Framework Analysis').closest('details') as HTMLDetailsElement;
    expect(frameworkEvidenceDetails).toBeTruthy();
    const overallMetadataDetails = screen.getByText('Metadata Overview').closest('details') as HTMLDetailsElement;
    overallMetadataDetails.open = true;
    expect(screen.getAllByText((_, node) => node?.textContent?.includes('Page intent: comparison') ?? false).length).toBeGreaterThan(0);
    expect(screen.getByText(/status:on-track/i)).toBeInTheDocument();
    expect(screen.getAllByText(/Executive Overview/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Detail Comparison/i).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: 'Overview' }));
    expect(screen.getAllByText(/Affected pages/i).length).toBeGreaterThan(0);
    expect(screen.getByText('Story Assessment')).toBeInTheDocument();
    expect(screen.getByText('Why This Matters')).toBeInTheDocument();
    expect(screen.getAllByText(/Decision makers may misinterpret KPI values/i).length).toBeGreaterThan(0);
    expect(screen.getByText('Detected Story')).toBeInTheDocument();
    expect(screen.getByText(/This page appears to summarize Revenue performance over time/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /show full reasoning/i }));
    expect(screen.getByText('Inferred Page Story')).toBeInTheDocument();
    expect(screen.getByText((_, node) => node?.textContent === 'Intent profile: executiveOverview')).toBeInTheDocument();
    expect(screen.getByText((_, node) => node?.textContent === 'Story archetype: executive overview + trend + comparison')).toBeInTheDocument();
    expect(screen.getByText('Page Intent Profile')).toBeInTheDocument();
    expect(screen.getByText(/Actionability score:/i)).toBeInTheDocument();
    expect(screen.getByText(/Benchmark and Archetype/i)).toBeInTheDocument();
    expect(screen.getByText(/Review status:/i)).toBeInTheDocument();
    expect(screen.getByText(/Not reviewed yet\./i)).toBeInTheDocument();
    expect(screen.getAllByText(/exception visibility is still weaker than the benchmark/i).length).toBeGreaterThan(0);
    fireEvent.change(screen.getByLabelText(/page intent profile override/i), {
      target: { value: 'operational' },
    });
    expect(screen.getByText((_, node) => node?.textContent === 'Selected profile: Operational')).toBeInTheDocument();
    expect(screen.getByText('Intent Feedback')).toBeInTheDocument();
    expect(screen.getByText(/Does this match your intent\?/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'No' }));
    expectLastPostedMessage({
      type: 'setIntentFeedback',
      pageName: 'Overview',
      inferredIntent: 'executiveOverview',
      storyArchetype: 'executive overview + trend + comparison',
      userConfirmation: 'no',
      inferenceConfidence: 'high',
    });
    expect(screen.getByText(/Mismatch \/ Needs review/i)).toBeInTheDocument();
    expect(screen.getByText(/Intent mismatch detected/i)).toBeInTheDocument();
    expect(screen.getByText(/The page currently reads as executiveOverview/i)).toBeInTheDocument();
    expect(screen.getByText(/Consider tightening the title, lead KPI band, or supporting visuals/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Yes' }));
    expectLastPostedMessage({
      type: 'setIntentFeedback',
      pageName: 'Overview',
      inferredIntent: 'executiveOverview',
      storyArchetype: 'executive overview + trend + comparison',
      userConfirmation: 'yes',
      inferenceConfidence: 'high',
    });
    expect(screen.queryByText(/Intent mismatch detected/i)).not.toBeInTheDocument();
    expect(screen.getByText(/Confirmed by you during this session\./i)).toBeInTheDocument();
    // Framework labels appear in both the summary mini-list and the framework cards; click the card title
    fireEvent.click(screen.getAllByText('Gestalt Principles').find((el) => el.classList.contains('framework-title'))!);
    expect(screen.getAllByText('35/35').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Score Breakdown/i).length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: 'Details' }));

    expectLastPostedMessage({
      type: 'selectTab',
      pageIndex: 2,
    });
    expect(screen.getAllByText(/Detail Comparison/i).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: /show full reasoning/i }));
    expect(screen.getByText((_, node) => node?.textContent === 'Intent profile: analyticalDeepDive')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /partially/i }));
    expectLastPostedMessage({
      type: 'setIntentFeedback',
      pageName: 'Details',
      inferredIntent: 'analyticalDeepDive',
      storyArchetype: 'trend',
      userConfirmation: 'partial',
      inferenceConfidence: 'medium',
    });
    expect(screen.getByText(/Partially aligned with your intent\./i)).toBeInTheDocument();
    expect(screen.getByText(/Partial \/ Needs clarification/i)).toBeInTheDocument();
    const noteField = screen.getByLabelText(/reviewer note/i);
    fireEvent.change(noteField, {
      target: { value: 'Needs a clearer variance narrative before this can be review-ready.' },
    });
    fireEvent.click(screen.getByRole('button', { name: /save note/i }));
    expectLastPostedMessage({
      type: 'setIntentFeedback',
      pageName: 'Details',
      inferredIntent: 'analyticalDeepDive',
      storyArchetype: 'trend',
      userConfirmation: 'partial',
      inferenceConfidence: 'medium',
      note: 'Needs a clearer variance narrative before this can be review-ready.',
    });
    expect(screen.getByText(/Reviewer note saved for this page review\./i)).toBeInTheDocument();
    expect(screen.queryByText('Reviewer Comment Generator')).not.toBeInTheDocument();
    const evidenceSection = screen.getByText('Evidence').closest('details') as HTMLDetailsElement;
    evidenceSection.open = true;
    const reviewCommentarySection = screen.getByText('Review Commentary').closest('details') as HTMLDetailsElement;
    expect(reviewCommentarySection.open).toBe(false);
    reviewCommentarySection.open = true;
    fireEvent.change(within(reviewCommentarySection).getByLabelText(/reviewer persona/i), {
      target: { value: 'strictDesignCritic' },
    });
    expect(within(reviewCommentarySection).getAllByText(/beautiful but useless/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Page Consistency Notes').length).toBeGreaterThan(0);
    expect(screen.getByText(/Metric label naming differs from other pages/i)).toBeInTheDocument();
    const detailMetadataDetails = screen.getByText('Parsed Metadata').closest('details') as HTMLDetailsElement;
    detailMetadataDetails.open = true;
    // Framework labels appear in both the summary mini-list and the framework cards; click the card title
    fireEvent.click(screen.getAllByText('Cognitive Load').find((el) => el.classList.contains('framework-title'))!);
    expect(screen.getAllByText('74/100').length).toBeGreaterThan(0);
    expect(screen.getByText(/Details density is acceptable\./i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Overall' }));
    expect(screen.getByRole('button', { name: /Confirmed/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Partial \/ Needs clarification/i })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Partial \/ Needs clarification/i }));
    expect(screen.getByRole('button', { name: 'Review page Details' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Review page Overview' })).not.toBeInTheDocument();
    fireEvent.click(screen.getAllByRole('button', { name: /export review summary/i })[0]);
    expectLastPostedMessage({ type: 'exportReviewWorkflow' });
  });

  it('updates preview controls from host-owned state and hides consultant template outside consultant mode', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              reviewPacketPreview,
              reviewPacketPreviewHtml,
              reviewPacketPreviewProfile: 'executive',
              reviewPacketPreviewTemplateVariant: 'standard',
            },
          },
        }),
      );
    });

    expect(screen.getByLabelText(/preview profile/i)).toHaveValue('executive');
    expect(screen.queryByLabelText(/consultant template/i)).not.toBeInTheDocument();

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              reviewPacketPreview,
              reviewPacketPreviewHtml: reviewPacketPreviewHtml.split('FY26 Sales Review Packet Preview').join('FY26 Sales Executive Brief'),
              reviewPacketPreviewProfile: 'consultant',
              reviewPacketPreviewTemplateVariant: 'standard',
            },
          },
        }),
      );
    });

    expect(screen.getByLabelText(/preview profile/i)).toHaveValue('consultant');
    expect(screen.getByLabelText(/consultant template/i)).toHaveValue('standard');
    const previewFrame = screen.getByTitle(/review packet html preview/i) as HTMLIFrameElement;
    expect(previewFrame.srcdoc).toContain('FY26 Sales Executive Brief');
  });

  it('posts a reveal message when an affected visual is selected', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    // Framework labels appear in both the summary mini-list and the framework cards; click the card title
    fireEvent.click(screen.getAllByText('Cognitive Load').find((el) => el.classList.contains('framework-title'))!);
    fireEvent.click(screen.getByText(/show affected visuals/i));
    fireEvent.click(screen.getByRole('button', { name: /actionbutton/i }));

    expectLastPostedMessage({
      type: 'revealVisual',
      pageName: 'Overview',
      visualId: 'd8427472eb598a9b5946',
    });
  });

  it('shows error state and retries through the host', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'error',
            message: 'Backend unavailable',
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: /retry/i }));

    expectLastPostedMessage({ type: 'refresh' });
  });

  it('fails clearly when the host protocol version diverges', async () => {
    render(<App />);

    await act(async () => {
      originalDispatchEvent(new MessageEvent('message', {
        data: {
          type: 'scoreState',
          protocolVersion: 999,
          schemaVersion: 1,
          state: scoreState,
        },
      }));
    });

    expect(await screen.findByText(/score panel protocol mismatch/i)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Scoring failed' })).toBeInTheDocument();
  });

  it('restores persisted intent feedback without changing displayed score', async () => {
    render(<App />);

    expect(postMessage).toHaveBeenCalledWith(withScorePanelEnvelope({ type: 'webviewReady' }));

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              reviewPacketPreview,
              reviewPacketPreviewHtml,
              reviewPacketPreviewProfile: 'consultant',
              reviewPacketPreviewTemplateVariant: 'brandedConsultant',
              intentFeedback: [
                {
                  pageName: 'Overview',
                  inferredIntent: 'executiveOverview',
                  storyArchetype: 'executive overview + trend + comparison',
                  userConfirmation: 'yes',
                  note: 'Title works, but the supporting chart still needs a clearer takeaway.',
                  timestamp: '2026-05-27T16:02:08.000Z',
                  analyzerVersion: '1.2.3',
                  reportSessionId: 'abc123:2026-05-27T16:00:00.000Z',
                  inferenceConfidence: 'high',
                },
              ],
            },
          },
        }),
      );
    });

    expect(screen.getByText('Review Summary')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Confirmed/i })).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Overview' }));
    fireEvent.click(screen.getByRole('button', { name: /show full reasoning/i }));
    expect(screen.getByText(/Review status:/i)).toBeInTheDocument();
    expect(screen.getByText(/Confirmed by you during this session\./i)).toBeInTheDocument();
    expect(screen.getByDisplayValue('Title works, but the supporting chart still needs a clearer takeaway.')).toBeInTheDocument();
    expect(screen.getByText('75')).toBeInTheDocument();
    expect(screen.getByText('/100')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Overall' }));
    const previewFrame = screen.getByTitle(/review packet html preview/i) as HTMLIFrameElement;
    expect(previewFrame.srcdoc).toContain('FY26 Sales Review Packet Preview');
    expect(screen.getByLabelText(/preview profile/i)).toHaveValue('consultant');
  });

  it('shows screenshot issue source distinctions in the visual audit output', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              reviewPacketPreview,
              reviewPacketPreviewHtml,
            },
          },
        }),
      );
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'auditState',
            audit: {
              coverage: {
                totalPages: 2,
                pagesWithCaptures: 1,
                unmatchedCaptures: 0,
                pagesWithFindings: 1,
              },
              pages: [
                {
                  pageName: 'Overview',
                  captures: [
                    {
                      captureId: 'c1',
                      pageName: 'Overview',
                      fileName: 'overview.png',
                      storedPath: '/tmp/overview.png',
                      findingCount: 2,
                    },
                  ],
                  findings: [
                    {
                      findingId: 'f1',
                      captureId: 'c1',
                      findingType: 'objective',
                      severity: 'warning',
                      confidence: 'high',
                      issueSource: 'renderedLayout',
                      text: 'Labels are clipped in the upper-right chart.',
                    },
                    {
                      findingId: 'f2',
                      captureId: 'c1',
                      findingType: 'strongHeuristic',
                      severity: 'warning',
                      confidence: 'medium',
                      issueSource: 'metadataModel',
                      text: 'The page still lacks clear decision context.',
                    },
                  ],
                },
              ],
              unmatchedCaptures: [],
              isAnalyzing: false,
              providerName: 'OpenAI GPT-4o Vision',
              providerConfigured: true,
            },
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Overview' }));
    expect(screen.getByText('AI Screenshot Audit')).toBeInTheDocument();
    expect(screen.getAllByText(/Issue source:/i).length).toBeGreaterThan(0);
    expect(screen.getByText(/Rendered \/ layout/i)).toBeInTheDocument();
    expect(screen.getByText(/Metadata \/ model/i)).toBeInTheDocument();
  });

  it('filters and regroups normalized findings in the issues workspace', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.change(screen.getByLabelText(/issue severity filter/i), {
      target: { value: 'medium' },
    });
    expect(screen.getByText(/Showing 1 of 4 finding\(s\)\./i)).toBeInTheDocument();
    const issuesWorkspace = screen.getByRole('heading', { name: 'Issues' }).closest('section') as HTMLElement;
    expect(within(issuesWorkspace).queryByText('Actionability gap')).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText(/issue grouping mode/i), {
      target: { value: 'impactArea' },
    });
    expect(within(issuesWorkspace).getAllByText('Navigation').length).toBeGreaterThan(0);
  });

  it('filters and hides Fabric readiness findings without overloading scope', async () => {
    render(<App />);

    const readinessFilterState = {
      ...scoreState,
      result: {
        ...scoreState.result,
        normalizedFindings: [
          ...(scoreState.result.normalizedFindings ?? []),
          {
            id: 'fabric-readiness-blocker-navigation',
            title: 'Migration Blocker',
            summary: 'Navigation complexity is likely too Power BI-specific for direct migration.',
            severity: 'medium',
            confidence: 88,
            scope: 'report',
            detectionType: 'deterministic',
            affectedPages: ['Details'],
            impactArea: 'navigation',
            frameworkImpact: ['Fabric App Readiness'],
            recommendation: 'Simplify navigation before treating the report as an app candidate.',
            sourceKind: 'fabricAppReadiness',
            sourceSection: 'issues',
            evidence: [],
          },
          {
            id: 'fabric-readiness-redesign-details',
            title: 'Redesign Required',
            summary: 'Details needs redesign before it becomes a strong app migration candidate.',
            severity: 'medium',
            confidence: 84,
            scope: 'page',
            detectionType: 'deterministic',
            affectedPages: ['Details'],
            impactArea: 'layout',
            frameworkImpact: ['Fabric App Readiness'],
            recommendation: 'Recompose this page into a simpler task-focused app experience.',
            sourceKind: 'fabricAppReadiness',
            sourceSection: 'issues',
            evidence: [],
          },
        ],
      },
    } as ScorePanelState;

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: readinessFilterState,
          },
        }),
      );
    });

    fireEvent.change(screen.getByLabelText(/issue severity filter/i), {
      target: { value: 'all' },
    });
    fireEvent.change(screen.getByLabelText(/issue fabric app readiness filter/i), {
      target: { value: 'blocker' },
    });

    const issuesWorkspace = screen.getByRole('heading', { name: 'Issues' }).closest('section') as HTMLElement;
    expect(screen.getByText(/Fabric App Readiness: Blocker/i)).toBeInTheDocument();
    expect(within(issuesWorkspace).getAllByText('Migration Blocker').length).toBeGreaterThan(0);
    expect(within(issuesWorkspace).queryByText('Redesign Required')).not.toBeInTheDocument();
    expect(screen.queryByText(/Scope: Report/i)).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText(/issue fabric app readiness filter/i), {
      target: { value: 'hidden' },
    });
    expect(screen.getByText(/Fabric App Readiness: Hidden/i)).toBeInTheDocument();
    expect(within(issuesWorkspace).queryByText('Migration Blocker')).not.toBeInTheDocument();
    expect(within(issuesWorkspace).queryByText('Good Fabric App Candidate')).not.toBeInTheDocument();
  });

  it('syncs the issue page filter to the selected page tab', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    expect(screen.getByLabelText(/issue page filter/i)).toHaveValue('all');

    fireEvent.click(screen.getByRole('button', { name: 'Details' }));
    expect(screen.getByLabelText(/issue page filter/i)).toHaveValue('Details');

    fireEvent.click(screen.getByRole('button', { name: 'Overall' }));
    expect(screen.getByLabelText(/issue page filter/i)).toHaveValue('all');
  });

  it('applies workspace persona ordering and matrix-driven issue filters without changing score', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    expect(screen.getByText('77')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/workspace review mode/i), {
      target: { value: 'governance' },
    });
    expect(screen.getByLabelText(/workspace review mode/i)).toHaveValue('governance');
    expect(screen.getByText('77')).toBeInTheDocument();

    const topIssuesCard = screen.getByRole('heading', { name: 'Top issues' }).closest('div') as HTMLElement;
    expect(within(topIssuesCard).getAllByText(/navigation/i).length).toBeGreaterThan(0);
    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getByText('Remediation Focus')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /filter issues for details navigation/i }));
    expect(screen.getByLabelText(/issue page filter/i)).toHaveValue('Details');
    expect(screen.getByLabelText(/issue dimension filter/i)).toHaveValue('navigation');
    expect(screen.getByLabelText(/workspace review mode/i)).toHaveValue('governance');
    expect(screen.getByText(/Page: Details/i)).toBeInTheDocument();
    expect(screen.getByText(/Dimension: Navigation/i)).toBeInTheDocument();
    expect(within(fixPlan).getByText('Details · Navigation')).toBeInTheDocument();
    expect(within(fixPlan).getByText('Standardize navigation cues')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /clear filters/i }));
    expect(screen.getByLabelText(/issue page filter/i)).toHaveValue('all');
    expect(screen.getByLabelText(/issue dimension filter/i)).toHaveValue('all');
  });

  it('uses smart-collapse defaults for issue groups and evidence', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              reviewPacketPreview,
              reviewPacketPreviewHtml,
            },
          },
        }),
      );
    });

    const highSeverityGroup = screen.getByText('High severity').closest('details') as HTMLDetailsElement;
    const evidenceSection = screen.getByText('Evidence').closest('details') as HTMLDetailsElement;

    expect(highSeverityGroup.open).toBe(true);
    expect(screen.queryByText('Medium severity')).not.toBeInTheDocument();
    expect(evidenceSection.open).toBe(false);
  });

  it('renders Fabric readiness as a dedicated overview callout with human-readable labels', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    expect(screen.getByText('Fabric App migration readiness')).toBeInTheDocument();
    expect(screen.getAllByText('Possible Candidate').length).toBeGreaterThan(0);
    expect(screen.queryByText(/Readiness: possibleCandidate/i)).not.toBeInTheDocument();
  });

  it('filters the overview Fabric readiness callout to the selected page', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Details' }));

    const readinessOverview = screen.getByLabelText('Fabric App migration readiness');
    expect(within(readinessOverview).getByText('Fabric App readiness for Details')).toBeInTheDocument();
    expect(within(readinessOverview).getByText('Redesign Required')).toBeInTheDocument();
    expect(within(readinessOverview).getByText('42')).toBeInTheDocument();
    expect(within(readinessOverview).getByText(/^Blockers$/i)).toBeInTheDocument();
    expect(within(readinessOverview).getByText(/^Unsupported patterns$/i)).toBeInTheDocument();
    expect(within(readinessOverview).queryByText(/^Candidate pages$/i)).not.toBeInTheDocument();
  });

  it('filters overview summary cards to the selected page context', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Details' }));

    const topStrengthsCard = screen.getByRole('heading', { name: 'Top strengths' }).closest('div') as HTMLElement;
    const topIssuesCard = screen.getByRole('heading', { name: 'Top issues' }).closest('div') as HTMLElement;
    const topActionsCard = screen.getByRole('heading', { name: 'Top actions' }).closest('div') as HTMLElement;

    expect(within(topStrengthsCard).getByText(/Clean trend visual/i)).toBeInTheDocument();
    expect(within(topStrengthsCard).queryByText(/Overall design quality is above the working threshold/i)).not.toBeInTheDocument();
    expect(within(topIssuesCard).getByText(/Beautiful but weakly actionable/i)).toBeInTheDocument();
    expect(within(topIssuesCard).queryByText(/Actionability gap/i)).not.toBeInTheDocument();
    expect(within(topActionsCard).getByText(/Decision support is weak/i)).toBeInTheDocument();
  });

  it('filters Fabric readiness evidence to the selected page', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Details' }));

    const evidenceSection = screen.getByText('Evidence').closest('details') as HTMLDetailsElement;
    evidenceSection.open = true;

    const readinessSection = screen.getAllByText('Fabric App Readiness')
      .map((node) => node.closest('details'))
      .find((node): node is HTMLDetailsElement => Boolean(node)) as HTMLDetailsElement;
    readinessSection.open = true;

    const readinessPanel = within(readinessSection);

    expect(readinessPanel.getByText('Fabric App readiness for Details')).toBeInTheDocument();
    expect(readinessPanel.getByText('Redesign Required')).toBeInTheDocument();
    expect(readinessPanel.getByText(/Overall readiness score 42\/100\./i)).toBeInTheDocument();
    expect(readinessPanel.queryByText(/Overall readiness score 80\/100\./i)).not.toBeInTheDocument();
    expect(readinessPanel.queryByText(/^Candidate pages$/i)).not.toBeInTheDocument();
  });

  it('moves reviewer commentary under Evidence as collapsed supporting material while preserving persona-aware output', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Details' }));

    expect(screen.queryByText('Reviewer Comment Generator')).not.toBeInTheDocument();

    const evidenceSection = screen.getByText('Evidence').closest('details') as HTMLDetailsElement;
    evidenceSection.open = true;

    const reviewCommentarySection = screen.getByText('Review Commentary').closest('details') as HTMLDetailsElement;
    expect(reviewCommentarySection.open).toBe(false);
    reviewCommentarySection.open = true;

    const commentaryPanel = within(reviewCommentarySection);
    expect(commentaryPanel.getByLabelText(/reviewer persona/i)).toHaveValue('consultant');
    expect(commentaryPanel.getByText(/Consultant review for Details \(analytical\)/i)).toBeInTheDocument();
    expect(commentaryPanel.getAllByText(/beautiful but useless/i).length).toBeGreaterThan(0);

    fireEvent.change(commentaryPanel.getByLabelText(/reviewer persona/i), {
      target: { value: 'strictDesignCritic' },
    });

    expect(commentaryPanel.getAllByText(/beautiful but useless/i).length).toBeGreaterThan(0);
  });

  it('renders Story Assessment as a story-first workflow and preserves full reasoning on demand', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Overview' }));

    expect(screen.getByText('Story Assessment')).toBeInTheDocument();
    expect(screen.getByText('Detected Story')).toBeInTheDocument();
    expect(screen.getByText(/This page appears to summarize Revenue performance over time, with region comparison as supporting evidence\./i)).toBeInTheDocument();
    expect(screen.getByText('Supported Decision')).toBeInTheDocument();
    expect(screen.getByText(/This page should help leaders judge performance against expectations and decide whether intervention is needed\./i)).toBeInTheDocument();
    expect(screen.getByText('Why This Matters')).toBeInTheDocument();
    expect(screen.getAllByText(/Decision makers may misinterpret KPI values/i).length).toBeGreaterThan(0);
    const detectedStoryHeading = screen.getByText('Detected Story');
    const whyThisMattersHeading = screen.getByText('Why This Matters');
    const storyGapsHeading = screen.getByText('Story Gaps');
    const storyGapsBlock = storyGapsHeading.closest('div') as HTMLElement;
    const supportedDecisionHeading = screen.getByText('Supported Decision');
    expect(detectedStoryHeading.compareDocumentPosition(supportedDecisionHeading) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(supportedDecisionHeading.compareDocumentPosition(whyThisMattersHeading) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(whyThisMattersHeading.compareDocumentPosition(storyGapsHeading) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(screen.getByText('Story Gaps')).toBeInTheDocument();
    expect(within(storyGapsBlock).getByText(/Exception visibility is weak\./i)).toBeInTheDocument();
    expect(screen.getByText(/Story Confidence: high/i)).toBeInTheDocument();
    expect(screen.getByText(/Decision Support: 58\/100/i)).toBeInTheDocument();
    expect(screen.getByText(/Benchmark: Mixed against expected/i)).toBeInTheDocument();
    expect(screen.queryByText('Inferred Page Story')).not.toBeInTheDocument();
    expect(screen.queryByText('Page Intent Profile')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /show full reasoning/i }));
    expect(screen.getByText('Inferred Page Story')).toBeInTheDocument();
    expect(screen.getByText('Page Intent Profile')).toBeInTheDocument();
    expect(screen.getByText('Intent Feedback')).toBeInTheDocument();
  });

  it('derives decision risk from existing reasoning when applicable', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Overview' }));

    expect(screen.getByText('Decision Risk')).toBeInTheDocument();
    expect(screen.getAllByText(/Decision makers may misinterpret KPI values without targets or prior-period comparison\./i).length).toBeGreaterThan(0);
  });

  it('renders Fix Plan as a differentiated remediation queue with action-specific rationale', async () => {
    render(<App />);

    const remediationState = {
      ...scoreState,
      result: {
        ...scoreState.result,
        fixPlan: [
          {
            id: 'fix-1',
            title: 'Add benchmarks and decision context',
            detail: 'Add missing target and prior-period context to the KPI band.',
            severity: 'high',
            effort: 'low',
            impact: 'high',
            why: 'Reduces risk of KPI misinterpretation.',
            scope: 'page',
            affectedPages: ['Overview'],
            recommendedAction: 'Add target benchmarks, prior-period context, and urgency cues.',
            resolvedOutcomes: ['Benchmark gap', 'Actionability gap'],
            sourceFindingIds: ['overview-actionability', 'details-benchmark'],
          },
        ],
      },
    } as unknown as ScorePanelState;

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: remediationState,
          },
        }),
      );
    });

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getAllByText(/^Impact$/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/^Why:$/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/^Resolves$/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/Reduces risk of KPI misinterpretation/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getByText('Benchmark gap')).toBeInTheDocument();
    expect(within(fixPlan).getAllByText('Actionability gap').length).toBeGreaterThan(0);
  });

  it('explains remediation focus and keeps the queue broader than severity-only issue filters', async () => {
    render(<App />);

    const remediationState = {
      ...scoreState,
      result: {
        ...scoreState.result,
        normalizedFindings: [
          {
            id: 'overview-layout-high',
            title: 'Grid alignment',
            summary: 'Top-row KPI cards overlap instead of holding a clean band.',
            severity: 'high',
            confidence: 82,
            scope: 'page',
            detectionType: 'deterministic',
            affectedPages: ['Overview'],
            impactArea: 'layout',
            frameworkImpact: ['Gestalt Principles'],
            recommendation: 'Tighten the layout so peer visuals read as a deliberate system.',
            sourceKind: 'framework',
            sourceSection: 'issues',
            evidence: [],
          },
          {
            id: 'overview-density-medium',
            title: 'Visual density',
            summary: 'The page is too crowded for fast executive scanning.',
            severity: 'medium',
            confidence: 80,
            scope: 'page',
            detectionType: 'deterministic',
            affectedPages: ['Overview'],
            impactArea: 'density',
            frameworkImpact: ['Cognitive Load'],
            recommendation: 'Split dense sections into a smaller number of focal visuals.',
            sourceKind: 'framework',
            sourceSection: 'issues',
            evidence: [],
          },
          ...(scoreState.result.normalizedFindings ?? []),
        ],
      },
    } as unknown as ScorePanelState;

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: remediationState,
          },
        }),
      );
    });

    fireEvent.change(screen.getByLabelText(/issue page filter/i), {
      target: { value: 'Overview' },
    });
    fireEvent.change(screen.getByLabelText(/issue dimension filter/i), {
      target: { value: 'layout' },
    });
    fireEvent.change(screen.getByLabelText(/issue severity filter/i), {
      target: { value: 'high' },
    });

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getByText('Remediation Focus')).toBeInTheDocument();
    expect(within(fixPlan).getByText('Overview · Layout')).toBeInTheDocument();
    expect(within(fixPlan).getByText(/Actions are grouped by problem area rather than individual findings/i)).toBeInTheDocument();
    expect(within(fixPlan).getByText('Reduce visual density and align layout')).toBeInTheDocument();
    expect(within(fixPlan).getByText('1 High · 1 Medium')).toBeInTheDocument();
  });

  it('updates remediation focus from matrix-driven page and dimension context', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: /filter issues for details navigation/i }));

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getByText('Remediation Focus')).toBeInTheDocument();
    expect(within(fixPlan).getByText('Details · Navigation')).toBeInTheDocument();
    expect(within(fixPlan).getByText('Standardize navigation cues')).toBeInTheDocument();
  });

  it('renders deterministic fix opportunities under remediation items and keeps unsupported remediation advisory', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getAllByText('Fix opportunities').length).toBeGreaterThan(0);
    expect(within(fixPlan).getByText('Standardize overview title anchor')).toBeInTheDocument();
    expect(within(fixPlan).getAllByText('Previewed').length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText('Rollback:').length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText('Advisory only: no safe metadata-only fix is currently available for this remediation.').length).toBeGreaterThan(0);
  });

  it('shows the batch workflow block when deterministic opportunities are available in the current context', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getByText('Batch workflow')).toBeInTheDocument();
    expect(within(fixPlan).getByRole('button', { name: 'Preview selected' })).toBeInTheDocument();
    expect(within(fixPlan).getByRole('button', { name: 'Approve selected' })).toBeInTheDocument();
    expect(within(fixPlan).getByRole('button', { name: 'Apply selected' })).toBeInTheDocument();
  });

  it('hides the batch workflow block when only advisory recommendations are available', async () => {
    render(<App />);

    const advisoryOnlyState = {
      ...scoreState,
      result: {
        ...scoreState.result,
        fixOpportunities: [],
      },
      fixSelection: {
        selectedOpportunityIds: [],
        compatibility: {
          isCompatible: true,
          compatibleOpportunityIds: [],
          blockingOpportunityIds: [],
          blockingReasons: [],
        },
        approvalState: 'NeedsPreview',
      },
    } as ScorePanelState;

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: advisoryOnlyState,
          },
        }),
      );
    });

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).queryByText('Batch workflow')).not.toBeInTheDocument();
    expect(within(fixPlan).queryByRole('button', { name: 'Preview selected' })).not.toBeInTheDocument();
    expect(within(fixPlan).queryByRole('button', { name: 'Approve selected' })).not.toBeInTheDocument();
    expect(within(fixPlan).queryByRole('button', { name: 'Apply selected' })).not.toBeInTheDocument();
  });

  it('shows advisory-only messaging when no deterministic opportunities are available', async () => {
    render(<App />);

    const advisoryOnlyState = {
      ...scoreState,
      result: {
        ...scoreState.result,
        fixOpportunities: [],
      },
    } as ScorePanelState;

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: advisoryOnlyState,
          },
        }),
      );
    });

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getByText('Advisory Recommendations Only')).toBeInTheDocument();
    expect(within(fixPlan).getByText(/No deterministic opportunities are available in the current context\./i)).toBeInTheDocument();
  });

  it('renders advisory proposal enrichment separately from deterministic fix execution details', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              result: {
                ...scoreState.result,
                proposalEnrichments: [
                  {
                    remediationItemId: 'fix-overview-actionability',
                    status: 'available',
                    source: 'provider',
                    enrichersApplied: ['storytelling', 'executiveReadability'],
                    titleSuggestions: [
                      {
                        title: 'Executive Sales Overview',
                        confidence: 0.82,
                        rationale: 'Matches the KPI-led page purpose.',
                      },
                    ],
                    explanation: {
                      shortText: 'Adding benchmark context helps readers interpret the KPI quickly.',
                      expandedText: 'Without a benchmark, the KPI communicates performance but not whether the result is good or bad.',
                    },
                    whyThisMatters: {
                      text: 'Without a benchmark, users cannot quickly tell whether performance is on target.',
                    },
                    advisoryPriority: {
                      tier: 'highLeverage',
                      rationale: 'This improves decision context on a high-visibility page.',
                    },
                    expectedOutcome: {
                      text: 'If applied, this change is expected to improve readability and decision context.',
                      areas: ['readability', 'decision context'],
                    },
                    advisoryAlternatives: [
                      {
                        title: 'Consolidate the KPI section',
                        description: 'Instead of adding another KPI card, consider consolidating the KPI section around one benchmarked summary.',
                      },
                    ],
                    validation: {
                      status: 'passed',
                      issues: [],
                    },
                    provenance: {
                      providerName: 'Test Provider',
                      usedFallback: false,
                      enrichedAt: '2026-06-02T20:00:00.000Z',
                      sourceFindingIds: ['finding-overview-actionability', 'finding-overview-benchmark'],
                    },
                  },
                ],
              },
            },
          },
        }),
      );
    });

    const fixPlan = screen.getByRole('heading', { name: 'Fix Plan' }).closest('section') as HTMLElement;
    expect(within(fixPlan).getAllByText('AI-enriched guidance').length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText('Executive Sales Overview').length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/Adding benchmark context helps readers interpret the KPI quickly/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/Without a benchmark, users cannot quickly tell whether performance is on target/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/High leverage/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/If applied, this change is expected to improve readability and decision context/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/Consolidate the KPI section/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText(/AI-enriched/i).length).toBeGreaterThan(0);
    expect(within(fixPlan).getAllByText('Expected resolutions:').length).toBeGreaterThan(0);
  });

  it('supports grouped preview approval apply and session rollback for compatible opportunities', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    fireEvent.click(screen.getByLabelText('Select Standardize overview title anchor'));
    expectLastPostedMessage({
      type: 'toggleFixOpportunitySelection',
      opportunityId: 'fixopp-overview-title',
    });

    fireEvent.click(screen.getByLabelText('Select Normalize chart top spacing'));
    expectLastPostedMessage({
      type: 'toggleFixOpportunitySelection',
      opportunityId: 'fixopp-overview-chart',
    });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              fixSelection: {
                selectedOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                compatibility: {
                  isCompatible: true,
                  compatibleOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                  blockingOpportunityIds: [],
                  blockingReasons: [],
                },
                approvalState: 'NeedsPreview',
              },
            },
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Preview selected' }));
    expectLastPostedMessage({ type: 'previewSelectedFixOpportunities' });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              fixSelection: {
                selectedOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                compatibility: {
                  isCompatible: true,
                  compatibleOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                  blockingOpportunityIds: [],
                  blockingReasons: [],
                },
                approvalState: 'Previewed',
                groupedPreview: {
                  opportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                  summary: {
                    changedFileCount: 2,
                    changedObjectCount: 2,
                    expectedOutcomeCount: 2,
                    touchedFiles: ['a', 'b'],
                    changedObjects: ['title-textbox-1', 'chart-hero-1'],
                  },
                  pageGroups: [
                    {
                      pageName: 'Overview',
                      objectGroups: [
                        {
                          objectId: 'title-textbox-1',
                          pageName: 'Overview',
                          propertyChanges: [
                            {
                              opportunityId: 'fixopp-overview-title',
                              property: 'position.x',
                              before: 42,
                              after: 24,
                            },
                          ],
                        },
                      ],
                    },
                  ],
                  mutationFacts: [
                    {
                      pageName: 'Overview',
                      objectId: 'title-textbox-1',
                      property: 'position.x',
                      before: 42,
                      after: 24,
                    },
                    {
                      pageName: 'Overview',
                      objectId: 'chart-hero-1',
                      property: 'position.y',
                      before: 120,
                      after: 96,
                    },
                  ],
                  expectedOutcomes: ['Actionability gap', 'Benchmark gap'],
                },
              },
            },
          },
        }),
      );
    });

    expect(screen.getByText('Grouped preview')).toBeInTheDocument();
    expect(screen.getByText('Mutation facts')).toBeInTheDocument();
    expect(screen.getByText('Grouped by page / object / property')).toBeInTheDocument();
    expect(screen.getByText('chart-hero-1')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Approve selected' }));
    expectLastPostedMessage({ type: 'approveSelectedFixOpportunities' });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              fixSelection: {
                ...(scoreState.fixSelection as NonNullable<typeof scoreState.fixSelection>),
                selectedOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                compatibility: {
                  isCompatible: true,
                  compatibleOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                  blockingOpportunityIds: [],
                  blockingReasons: [],
                },
                approvalState: 'Approved',
                groupedPreview: {
                  opportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                  summary: {
                    changedFileCount: 2,
                    changedObjectCount: 2,
                    expectedOutcomeCount: 2,
                    touchedFiles: ['a', 'b'],
                    changedObjects: ['title-textbox-1', 'chart-hero-1'],
                  },
                  pageGroups: [],
                  mutationFacts: [],
                  expectedOutcomes: ['Actionability gap', 'Benchmark gap'],
                },
              },
            },
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Apply selected' }));
    expectLastPostedMessage({ type: 'applySelectedFixOpportunities' });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              fixSelection: {
                ...(scoreState.fixSelection as NonNullable<typeof scoreState.fixSelection>),
                selectedOpportunityIds: [],
                compatibility: {
                  isCompatible: true,
                  compatibleOpportunityIds: [],
                  blockingOpportunityIds: [],
                  blockingReasons: [],
                },
                approvalState: 'NeedsPreview',
              },
              fixApplySessions: [
                {
                  id: 'session-1',
                  appliedAt: '2026-06-01T22:40:00.000Z',
                  opportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                  opportunityTitles: ['Standardize overview title anchor', 'Normalize chart top spacing'],
                  rollbackAvailable: true,
                  rollbackHistory: [],
                  groupedOutcomeSummary: {
                    totalEntries: 2,
                    statuses: [
                      { status: 'Resolved', count: 1, opportunityIds: ['fixopp-overview-title'] },
                      { status: 'Unexpected', count: 1, opportunityIds: ['fixopp-overview-chart'] },
                    ],
                    appliedWithUnexpectedOutcomeOpportunityIds: ['fixopp-overview-chart'],
                  },
                },
              ],
            },
          },
        }),
      );
    });

    expect(screen.getByText('Session history')).toBeInTheDocument();
    expect(screen.getByText(/Resolved 1/i)).toBeInTheDocument();
    expect(screen.getByText(/Unexpected 1/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Roll back session' }));
    expectLastPostedMessage({
      type: 'rollbackFixSession',
      sessionId: 'session-1',
    });
  });

  it('blocks incompatible selections with clear conflict messaging and supports regeneration messaging', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              fixSelection: {
                selectedOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                compatibility: {
                  isCompatible: false,
                  compatibleOpportunityIds: [],
                  blockingOpportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                  blockingReasons: [
                    {
                      code: 'overlappingMutation',
                      message: 'Selected opportunities both change title-textbox-1 at position.x.',
                      opportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
                      targetObjectId: 'title-textbox-1',
                      propertyPath: 'position.x',
                    },
                  ],
                },
                approvalState: 'NeedsPreview',
                message: 'Selected opportunities are stale or drifted. Regenerate them before retrying.',
              },
            },
          },
        }),
      );
    });

    expect(screen.getByText('Compatibility')).toBeInTheDocument();
    expect(screen.getByText(/Selected opportunities both change title-textbox-1/i)).toBeInTheDocument();
    expect(screen.getByText(/overlappingMutation/i)).toBeInTheDocument();
    expect(screen.getByText(/Regenerate them before retrying/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Regenerate stale' }));
    expectLastPostedMessage({
      type: 'regenerateFixOpportunities',
      opportunityIds: ['fixopp-overview-title', 'fixopp-overview-chart'],
    });

    fireEvent.click(screen.getAllByRole('button', { name: 'Show Preview' })[0]);
    expect(screen.getByRole('columnheader', { name: 'Object' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Property' })).toBeInTheDocument();
    expect(screen.getAllByText('Overview · title-textbox-1').length).toBeGreaterThan(0);
    expect(screen.getByText('position.x')).toBeInTheDocument();
    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('24')).toBeInTheDocument();
  });

  it('adapts the matrix between report and page review contexts using qualitative statuses', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: scoreState,
          },
        }),
      );
    });

    const overallMatrix = screen.getByText('Cross-page matrix').closest('.overview-matrix') as HTMLElement;
    expect(within(overallMatrix).getByText('Overview')).toBeInTheDocument();
    expect(within(overallMatrix).getByText('Details')).toBeInTheDocument();
    expect(within(overallMatrix).getAllByText(/Weak|Watch|Strong|Unknown/i).length).toBeGreaterThan(0);

    fireEvent.click(screen.getByRole('button', { name: 'Overview' }));

    const pageMatrix = screen.getByText('Cross-page matrix').closest('.overview-matrix') as HTMLElement;
    expect(within(pageMatrix).getByText('Overview')).toBeInTheDocument();
    expect(within(pageMatrix).queryByText('Details')).not.toBeInTheDocument();
    expect(within(pageMatrix).getAllByText(/Weak|Watch|Strong|Unknown/i).length).toBeGreaterThan(0);
    expect(within(pageMatrix).queryByRole('button', { name: /filter issues for details navigation/i })).not.toBeInTheDocument();
  });

  it('preserves the active tab across loading and refresh-driven score state updates', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              selectedPageIndex: 2,
            },
          },
        }),
      );
    });

    expect(screen.getByRole('button', { name: 'Details' }).className).toContain('tab-button-active');

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'loading',
          },
        }),
      );
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              selectedPageIndex: 2,
            },
          },
        }),
      );
    });

    expect(screen.getByRole('button', { name: 'Details' }).className).toContain('tab-button-active');
  });

  it('renders Fabric App review inside the existing workspace without deterministic mutation controls', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              result: {
                ...scoreState.result,
                reportPath: '/tmp/executive-fabric-app',
                pageCount: 2,
                pageScores: undefined,
                readinessAssessment: undefined,
                feedback: {},
                reportConsistencySummary: undefined,
                inferredStorySummary: undefined,
                pageIntentProfile: undefined,
                actionabilityBreakdown: undefined,
                benchmarkComparison: undefined,
                pagePurposeAnalysis: undefined,
                analysisContext: {
                  surfaceType: 'fabricApp',
                  analyzerType: 'fabricAppReview',
                  analyzerProfile: 'fabricAppQuality',
                  surfaceDisplayName: 'Executive Fabric App',
                  sourceLocation: '/tmp/executive-fabric-app',
                  availableAnalyzerTypes: ['fabricAppReview'],
                  availableAnalyzerProfiles: ['default', 'fabricAppQuality'],
                },
                fabricAppReview: {
                  qualityScore: 72,
                  summary: 'Fabric App review produced bounded findings from TypeScript, navigation, tokens, screenshots, and semantic-model evidence.',
                  remediationGuidance: ['Rename generic routes.', 'Standardize token usage.'],
                  evidence: [
                    {
                      kind: 'navigation',
                      label: 'Navigation evidence',
                      summary: 'Executive Overview -> /overview',
                      filePath: 'src/routes/index.tsx',
                    },
                    {
                      kind: 'designToken',
                      label: 'Token bypass evidence',
                      summary: 'Hard-coded color bypass detected: #ff0000.',
                      filePath: 'src/ExecutiveCard.tsx',
                    },
                    {
                      kind: 'screenshot',
                      label: 'Screenshot evidence',
                      summary: 'Executive Overview capture shows a KPI-first landing state.',
                      filePath: 'screenshots/01 Executive Overview - Default.png',
                    },
                    {
                      kind: 'semanticModel',
                      label: 'Semantic model evidence',
                      summary: 'SalesModel query usage anchors Revenue and Margin interactions.',
                      filePath: 'src/data/queries.ts',
                    },
                  ],
                },
                fixOpportunities: [],
                proposalEnrichments: [],
                normalizedFindings: [
                  {
                    id: 'fabric-route-clarity',
                    title: 'Route labeling is too generic for analytical navigation',
                    summary: 'Generic labels weaken evidence flow.',
                    severity: 'medium',
                    confidence: 82,
                    scope: 'report',
                    detectionType: 'deterministic',
                    affectedPages: [],
                    impactArea: 'navigation',
                    frameworkImpact: ['Fabric App Review'],
                    recommendation: 'Rename generic routes.',
                    sourceKind: 'fabricAppReview',
                    sourceSection: 'issues',
                    evidence: [
                      {
                        kind: 'navigation',
                        label: 'Navigation evidence',
                        detail: 'src/routes/index.tsx — Detail -> /detail',
                        filePath: 'src/routes/index.tsx',
                      },
                      {
                        kind: 'screenshot',
                        label: 'Screenshot evidence',
                        detail: 'screenshots/01 Executive Overview - Default.png — KPI-first landing state is visible.',
                        filePath: 'screenshots/01 Executive Overview - Default.png',
                      },
                    ],
                  },
                  {
                    id: 'fabric-token-bypass',
                    title: 'Token inconsistencies were detected',
                    summary: 'Hard-coded styling bypasses the shared token layer.',
                    severity: 'medium',
                    confidence: 82,
                    scope: 'report',
                    detectionType: 'deterministic',
                    affectedPages: [],
                    impactArea: 'layout',
                    frameworkImpact: ['Fabric App Review'],
                    recommendation: 'Standardize token usage.',
                    sourceKind: 'fabricAppReview',
                    sourceSection: 'issues',
                    evidence: [
                      {
                        kind: 'designToken',
                        label: 'Token bypass evidence',
                        detail: 'src/ExecutiveCard.tsx — Hard-coded color bypass detected: #ff0000.',
                        filePath: 'src/ExecutiveCard.tsx',
                      },
                      {
                        kind: 'semanticModel',
                        label: 'Semantic model evidence',
                        detail: 'src/data/queries.ts — SalesModel query usage anchors Revenue interactions.',
                        filePath: 'src/data/queries.ts',
                      },
                    ],
                  },
                ],
                fixPlan: [
                  {
                    id: 'fix-fabric-navigation',
                    title: 'Improve navigation clarity',
                    detail: 'Resolve 1 related finding through one remediation step.',
                    severity: 'medium',
                    effort: 'medium',
                    impact: 'medium',
                    why: 'Clarifies the executive-to-detail scan path.',
                    scope: 'report',
                    affectedPages: [],
                    recommendedAction: 'Rename generic routes.',
                    resolvedOutcomes: ['Navigation consistency'],
                    sourceFindingIds: ['fabric-route-clarity'],
                  },
                ],
                overviewSummary: {
                  ...scoreState.result.overviewSummary!,
                  overallScore: 72,
                  executiveSummary: 'This Fabric App has usable analytical structure, but route clarity and token discipline need improvement.',
                },
              },
            },
          },
        }),
      );
    });

    expect(screen.getByText('Optimization Report')).toBeInTheDocument();
    expect(screen.getByText(/This Fabric App has usable analytical structure/i)).toBeInTheDocument();
    expect(screen.getAllByText('Route labeling is too generic for analytical navigation').length).toBeGreaterThan(0);
    expect(screen.getByText('Improve navigation clarity')).toBeInTheDocument();
    fireEvent.click(screen.getByText('Evidence'));
    const fabricEvidenceSummary = screen.getAllByText('Fabric App Review Evidence')[0].closest('summary') as HTMLElement;
    expect(fabricEvidenceSummary).toBeInTheDocument();
    fireEvent.click(fabricEvidenceSummary);
    expect(screen.getAllByText((_, element) => element?.textContent?.includes('Executive Overview -> /overview') ?? false).length).toBeGreaterThan(0);
    expect(screen.getAllByText((_, element) => element?.textContent?.includes('src/ExecutiveCard.tsx') ?? false).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Screenshot Evidence').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Semantic Model Evidence').length).toBeGreaterThan(0);
    expect(screen.getAllByText((_, element) => element?.textContent?.includes('screenshots/01 Executive Overview - Default.png') ?? false).length).toBeGreaterThan(0);
    expect(screen.getAllByText((_, element) => element?.textContent?.includes('src/data/queries.ts') ?? false).length).toBeGreaterThan(0);
    expect(screen.queryByText('Batch workflow')).not.toBeInTheDocument();
    expect(screen.getByText('Advisory Recommendations Only')).toBeInTheDocument();
  });

  it('shows graceful missing-state messaging when screenshot and semantic model evidence are unavailable', async () => {
    render(<App />);

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              result: {
                ...scoreState.result,
                reportPath: '/tmp/fabric-without-extra-evidence',
                pageCount: 1,
                pageScores: undefined,
                readinessAssessment: undefined,
                feedback: {},
                reportConsistencySummary: undefined,
                inferredStorySummary: undefined,
                pageIntentProfile: undefined,
                actionabilityBreakdown: undefined,
                benchmarkComparison: undefined,
                pagePurposeAnalysis: undefined,
                analysisContext: {
                  surfaceType: 'fabricApp',
                  analyzerType: 'fabricAppReview',
                  analyzerProfile: 'fabricAppQuality',
                  surfaceDisplayName: 'Minimal Fabric App',
                  sourceLocation: '/tmp/fabric-without-extra-evidence',
                  availableAnalyzerTypes: ['fabricAppReview'],
                  availableAnalyzerProfiles: ['default', 'fabricAppQuality'],
                },
                fabricAppReview: {
                  qualityScore: 79,
                  summary: 'Fabric App review produced bounded findings from TypeScript, navigation, and design-token evidence.',
                  remediationGuidance: ['Clarify route naming.'],
                  evidence: [
                    {
                      kind: 'navigation',
                      label: 'Navigation evidence',
                      summary: 'Overview -> /overview',
                      filePath: 'src/routes/index.tsx',
                    },
                  ],
                },
                fixOpportunities: [],
                proposalEnrichments: [],
                normalizedFindings: [],
                fixPlan: [],
                overviewSummary: {
                  ...scoreState.result.overviewSummary!,
                  overallScore: 79,
                  executiveSummary: 'Core app structure is present, but richer evidence is unavailable for this surface.',
                },
              },
            },
          },
        }),
      );
    });

    fireEvent.click(screen.getByText('Evidence'));
    const fabricEvidenceSummary = screen.getAllByText('Fabric App Review Evidence')[0].closest('summary') as HTMLElement;
    fireEvent.click(fabricEvidenceSummary);

    expect(screen.getByText('No screenshot evidence is available for this Fabric App review.')).toBeInTheDocument();
    expect(screen.getByText('No semantic model evidence is available for this Fabric App review.')).toBeInTheDocument();
  });
});
