import React from 'react';
import { act, fireEvent, render, screen, within } from '@testing-library/react';
import '@testing-library/jest-dom';
import App from './App';
import type { ScorePanelState } from '../../src/analyzer/contracts/scorePanel';

const postMessage = jest.fn();

const scoreState: ScorePanelState = {
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
};

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
    (globalThis as unknown as { acquireVsCodeApi: () => { postMessage: typeof postMessage } }).acquireVsCodeApi =
      () => ({
        postMessage,
      });
  });

  it('renders the score state and posts tab selection back to the host', async () => {
    render(<App />);

    expect(postMessage).toHaveBeenCalledWith({ type: 'webviewReady' });

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
    expect(screen.getByLabelText(/workspace review mode/i)).toHaveValue('default');
    expect(screen.getByText(/Review modes change how findings are prioritized and explained/i)).toBeInTheDocument();
    expect(screen.getByText('Cross-page matrix')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Issues' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Fix Plan' })).toBeInTheDocument();
    expect(screen.getAllByText(/The page includes some decision context but still hides the main exception\./i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Fix first:/i).length).toBeGreaterThan(0);
    expect(screen.getByText('Evidence')).toBeInTheDocument();
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
    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'setReviewPacketPreviewProfile',
      profile: 'executive',
    });
    fireEvent.change(screen.getByLabelText(/consultant template/i), {
      target: { value: 'standard' },
    });
    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'setReviewPacketPreviewTemplateVariant',
      templateVariant: 'standard',
    });
    fireEvent.click(screen.getByRole('button', { name: /open full packet/i }));
    expect(postMessage).toHaveBeenLastCalledWith({ type: 'openReviewPacketPreview' });
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
    expect(screen.getByText('Page Purpose Analysis')).toBeInTheDocument();
    expect(screen.getByText('Why This Matters')).toBeInTheDocument();
    expect(screen.getByText(/Decision makers may misinterpret KPI values/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /show full reasoning/i }));
    expect(screen.getByText(/This page appears to summarize Revenue performance over time/i)).toBeInTheDocument();
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
    expect(postMessage).toHaveBeenLastCalledWith({
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
    expect(postMessage).toHaveBeenLastCalledWith({
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

    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'selectTab',
      pageIndex: 2,
    });
    expect(screen.getAllByText(/Detail Comparison/i).length).toBeGreaterThan(0);
    fireEvent.click(screen.getByRole('button', { name: /show full reasoning/i }));
    expect(screen.getByText((_, node) => node?.textContent === 'Intent profile: analyticalDeepDive')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /partially/i }));
    expect(postMessage).toHaveBeenLastCalledWith({
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
    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'setIntentFeedback',
      pageName: 'Details',
      inferredIntent: 'analyticalDeepDive',
      storyArchetype: 'trend',
      userConfirmation: 'partial',
      inferenceConfidence: 'medium',
      note: 'Needs a clearer variance narrative before this can be review-ready.',
    });
    expect(screen.getByText(/Reviewer note saved for this page review\./i)).toBeInTheDocument();
    expect(screen.getByText('Reviewer Comment Generator')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText(/reviewer persona/i), {
      target: { value: 'strictDesignCritic' },
    });
    expect(screen.getAllByText(/beautiful but useless/i).length).toBeGreaterThan(0);
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
    expect(postMessage).toHaveBeenLastCalledWith({ type: 'exportReviewWorkflow' });
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

    expect(postMessage).toHaveBeenLastCalledWith({
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

    expect(postMessage).toHaveBeenLastCalledWith({ type: 'refresh' });
  });

  it('restores persisted intent feedback without changing displayed score', async () => {
    render(<App />);

    expect(postMessage).toHaveBeenCalledWith({ type: 'webviewReady' });

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
    expect(screen.getByText(/Showing 1 of 3 finding\(s\)\./i)).toBeInTheDocument();
    const issuesWorkspace = screen.getByRole('heading', { name: 'Issues' }).closest('section') as HTMLElement;
    expect(within(issuesWorkspace).queryByText('Actionability gap')).not.toBeInTheDocument();

    fireEvent.change(screen.getByLabelText(/issue grouping mode/i), {
      target: { value: 'impactArea' },
    });
    expect(within(issuesWorkspace).getAllByText('Navigation').length).toBeGreaterThan(0);
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

  it('renders Page Purpose Analysis as a summary-first workflow and preserves full reasoning on demand', async () => {
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

    expect(screen.getByText('Page Purpose Analysis')).toBeInTheDocument();
    expect(screen.getByText('Why This Matters')).toBeInTheDocument();
    expect(screen.getByText(/Decision makers may misinterpret KPI values/i)).toBeInTheDocument();
    expect(screen.queryByText('Inferred Page Story')).not.toBeInTheDocument();
    expect(screen.queryByText('Page Intent Profile')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /show full reasoning/i }));
    expect(screen.getByText('Inferred Page Story')).toBeInTheDocument();
    expect(screen.getByText('Page Intent Profile')).toBeInTheDocument();
    expect(screen.getByText('Intent Feedback')).toBeInTheDocument();
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
    expect(within(fixPlan).getByText('Actionability gap')).toBeInTheDocument();
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
    expect(within(fixPlan).getByText('Previewed')).toBeInTheDocument();
    expect(within(fixPlan).getByText('Rollback:')).toBeInTheDocument();
    expect(within(fixPlan).getAllByText('Advisory only: no safe metadata-only fix is currently available for this remediation.').length).toBeGreaterThan(0);
  });

  it('shows structured mutation preview and posts approve/apply/rollback messages for fix opportunities', async () => {
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

    fireEvent.click(screen.getByRole('button', { name: 'Show Preview' }));
    expect(screen.getByRole('columnheader', { name: 'Object' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Property' })).toBeInTheDocument();
    expect(screen.getAllByText('Overview · title-textbox-1').length).toBeGreaterThan(0);
    expect(screen.getByText('position.x')).toBeInTheDocument();
    expect(screen.getByText('42')).toBeInTheDocument();
    expect(screen.getByText('24')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Approve' }));
    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'approveFixOpportunity',
      opportunityId: 'fixopp-overview-title',
    });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              result: {
                ...scoreState.result,
                fixOpportunities: [
                  {
                    ...scoreState.result.fixOpportunities?.[0],
                    state: 'Approved',
                  },
                ],
              },
            },
          },
        }),
      );
    });

    fireEvent.click(screen.getByRole('button', { name: 'Apply' }));
    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'applyFixOpportunity',
      opportunityId: 'fixopp-overview-title',
    });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'scoreState',
            state: {
              ...scoreState,
              result: {
                ...scoreState.result,
                fixOpportunities: [
                  {
                    ...scoreState.result.fixOpportunities?.[0],
                    state: 'AppliedWithUnexpectedOutcome',
                    outcome: {
                      entries: [
                        {
                          findingId: 'overview-actionability',
                          title: 'Actionability gap',
                          status: 'Unexpected',
                        },
                      ],
                    },
                  },
                ],
              },
            },
          },
        }),
      );
    });

    expect(screen.getByText('Applied with unexpected outcome')).toBeInTheDocument();
    expect(screen.getByText('Outcome after re-analysis')).toBeInTheDocument();
    expect(screen.getByText('Unexpected: Actionability gap')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Roll Back' }));
    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'rollbackFixOpportunity',
      opportunityId: 'fixopp-overview-title',
    });
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
});
