import React from 'react';
import type {
  AffectedVisualReference,
  ActionabilityBreakdown,
  AuditFindingDisplay,
  AuditPageState,
  AuditState,
  BenchmarkComparisonSummary,
  FixOpportunity,
  FixOpportunityCategory,
  FixOpportunityState,
  FixOutcomeStatus,
  FindingType,
  FrameworkFeedbackItem,
  IntentFeedbackConfirmation,
  IntentFeedbackEntry,
  NormalizedFinding,
  NormalizedFindingSeverity,
  OverviewSummary,
  PageIntentProfile,
  PageIntentProfileType,
  PageVisualMetadataSummary,
  PageScore,
  ReviewPresentationPersona,
  ReviewPresentationPersonaProfile,
  ReviewerPersona,
  ScorePanelHostToWebviewMessage,
  ScorePanelState,
  ScorePanelWebviewToHostMessage,
  ScoreResult,
  VisualMetadataItem,
} from '../../src/analyzer/contracts/scorePanel';
import {
  basename,
  getEnabledFrameworks,
  getPageScore,
  getResultScore,
} from '../../src/analyzer/score/presentation';
import { applyPersonaPresentation, getReviewPresentationPersonaProfiles } from '../../src/analyzer/score/personaPresentation';
import { buildReviewerComments } from '../../src/analyzer/score/reviewerComments';
import { buildContextAwareRemediationQueue, type ContextAwareRemediationQueue } from './remediationQueue';
import {
  getAdvisoryPriorityLabel,
  getProposalEnrichmentSummary,
  hasProposalEnrichmentContent,
} from './proposalEnrichment';

interface ScoreVsCodeApi {
  postMessage(message: ScorePanelWebviewToHostMessage): void;
}

declare function acquireVsCodeApi(): ScoreVsCodeApi;

type ViewState =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; state: ScorePanelState; audit?: AuditState };

type ReviewStatus = 'confirmed' | 'partial' | 'mismatch' | 'unreviewed';
type IssueGroupingMode = 'severity' | 'impactArea';

interface PageReviewEntry {
  pageName: string;
  status: ReviewStatus;
  summary?: PageScore['inferredStorySummary'];
}

interface IntentFeedbackState {
  confirmation?: IntentFeedbackConfirmation;
  note?: string;
}

interface IssueFilterState {
  severity: NormalizedFindingSeverity | 'all';
  pageName: string | 'all';
  dimension: 'layout' | 'story' | 'accessibility' | 'consistency' | 'navigation' | 'actionability' | 'all';
  impactArea: NormalizedFinding['impactArea'] | 'all';
  scope: NormalizedFinding['scope'] | 'all';
  detectionType: NormalizedFinding['detectionType'] | 'all';
}

function isZeroScore(result: ScoreResult): boolean {
  return (
    result.gestaltScore === 0 &&
    result.cognitiveLoadScore === 0 &&
    result.dataInkScore === 0 &&
    result.accessibilityScore === 0 &&
    result.visualBestPracticesScore === 0 &&
    result.stephenFewScore === 0 &&
    result.enterpriseGovernanceScore === 0 &&
    result.tufteScore === 0 &&
    result.graphicalPerceptionScore === 0 &&
    result.densityScore === 0 &&
    result.narrativeScore === 0
  );
}

function getScoreTone(score: number): string {
  if (score >= 75) {
    return 'tone-good';
  }

  if (score >= 50) {
    return 'tone-warn';
  }

  return 'tone-bad';
}

function averageFrameworkScore(pageScores: PageScore[], normalizedKey: string): number {
  if (pageScores.length === 0) {
    return 0;
  }

  const total = pageScores.reduce((sum, page) => sum + getPageScore(page, normalizedKey), 0);
  return Math.round((total / pageScores.length) * 100) / 100;
}

function shortenVisualId(visualId: string): string {
  if (visualId.length <= 12) {
    return visualId;
  }

  return `${visualId.slice(0, 8)}…`;
}

function isScoredFeedbackItem(
  item: FrameworkFeedbackItem,
): item is FrameworkFeedbackItem & { earnedPoints: number; possiblePoints: number } {
  return typeof item.earnedPoints === 'number' && typeof item.possiblePoints === 'number';
}

function getFeedbackCriterionLabel(text: string): string {
  const separatorIndex = text.indexOf(':');
  return separatorIndex > 0 ? text.slice(0, separatorIndex).trim() : text.trim();
}

function formatPoints(points: number): string {
  const rounded = Math.round(points * 10) / 10;
  return Number.isInteger(rounded) ? rounded.toFixed(0) : rounded.toFixed(1);
}

function getFindingTypeLabel(findingType: FindingType): string {
  switch (findingType) {
    case 'objective':
      return 'Objective';
    case 'stylePreference':
      return 'Style';
    default:
      return 'Heuristic';
  }
}

function getFindingTypeClassName(findingType: FindingType): string {
  switch (findingType) {
    case 'objective':
      return 'finding-badge-objective';
    case 'stylePreference':
      return 'finding-badge-style';
    default:
      return 'finding-badge-heuristic';
  }
}

function renderFindingBadge(findingType: FindingType): React.ReactNode {
  return (
    <span className={`finding-badge ${getFindingTypeClassName(findingType)}`}>
      {getFindingTypeLabel(findingType)}
    </span>
  );
}

function buildScoreBreakdown(items: FrameworkFeedbackItem[] | undefined): string | undefined {
  if (!items || items.length === 0) {
    return undefined;
  }

  const scoredItems = items.filter(isScoredFeedbackItem);
  if (scoredItems.length === 0) {
    return undefined;
  }

  const breakdown = scoredItems
    .map((item) => (
      `${getFeedbackCriterionLabel(item.text)} ${formatPoints(item.earnedPoints)}/${formatPoints(item.possiblePoints)}`
    ))
    .join(', ');

  return `Score Breakdown - ${breakdown}.`;
}

function splitFeedbackDetail(text: string): {
  label: string;
  detail: string;
  recommendation?: string;
} {
  const separatorIndex = text.indexOf(':');
  const label = getFeedbackCriterionLabel(text);
  const remainder = separatorIndex > 0 ? text.slice(separatorIndex + 1).trim() : text.trim();
  const divider = remainder.includes(' — ')
    ? ' — '
    : remainder.includes(' – ')
      ? ' – '
      : undefined;

  if (!divider) {
    return {
      label,
      detail: remainder,
    };
  }

  const [detail, recommendation] = remainder.split(divider, 2);
  return {
    label,
    detail: detail.trim(),
    recommendation: recommendation?.trim(),
  };
}

function getNormalizedFindingSeverityOrder(severity: NormalizedFindingSeverity): number {
  switch (severity) {
    case 'high':
      return 0;
    case 'medium':
      return 1;
    case 'low':
      return 2;
    default:
      return 3;
  }
}

function getNormalizedFindingSeverityLabel(severity: NormalizedFindingSeverity): string {
  switch (severity) {
    case 'high':
      return 'High severity';
    case 'medium':
      return 'Medium severity';
    case 'low':
      return 'Low severity';
    default:
      return 'Informational';
  }
}

function getNormalizedFindingSeverityClassName(severity: NormalizedFindingSeverity): string {
  switch (severity) {
    case 'high':
      return 'issue-severity-high';
    case 'medium':
      return 'issue-severity-medium';
    case 'low':
      return 'issue-severity-low';
    default:
      return 'issue-severity-info';
  }
}

function getFixOpportunityCategoryLabel(category: FixOpportunityCategory): string {
  switch (category) {
    case 'title':
      return 'Title';
    case 'semanticColor':
      return 'Semantic color';
    case 'alignment':
      return 'Alignment';
    case 'spacing':
      return 'Spacing';
    case 'grid':
      return 'Grid';
    case 'navigation':
      return 'Navigation';
    default:
      return 'Cross-page consistency';
  }
}

function getFixOpportunityStateLabel(state: FixOpportunityState): string {
  switch (state) {
    case 'Previewed':
      return 'Previewed';
    case 'Approved':
      return 'Approved';
    case 'Applied':
      return 'Applied';
    case 'RolledBack':
      return 'Rolled back';
    case 'Stale':
      return 'Stale';
    case 'FailedValidation':
      return 'Failed validation';
    default:
      return 'Applied with unexpected outcome';
  }
}

function getFixOutcomeStatusLabel(status: FixOutcomeStatus): string {
  switch (status) {
    case 'Resolved':
      return 'Resolved';
    case 'Improved':
      return 'Improved';
    case 'Unchanged':
      return 'Unchanged';
    default:
      return 'Unexpected';
  }
}

function remediationMatchesOpportunity(
  item: ContextAwareRemediationQueue['items'][number],
  opportunity: FixOpportunity,
): boolean {
  if (opportunity.remediationItemId === item.id) {
    return true;
  }

  return opportunity.sourceFindingIds.some((findingId) => item.sourceFindingIds.includes(findingId));
}

function remediationMatchesProposalEnrichment(
  item: ContextAwareRemediationQueue['items'][number],
  enrichment: NonNullable<ScoreResult['proposalEnrichments']>[number],
): boolean {
  if (enrichment.remediationItemId === item.id) {
    return true;
  }

  if (enrichment.provenance.sourceFindingIds.some((findingId) => item.sourceFindingIds.includes(findingId))) {
    return true;
  }

  const firstAffectedPage = item.affectedPages[0];
  if (!firstAffectedPage) {
    return false;
  }

  return (enrichment.titleSuggestions ?? []).some((suggestion) => suggestion.title.includes(firstAffectedPage));
}

function getDetectionTypeLabel(detectionType: NormalizedFinding['detectionType']): string {
  switch (detectionType) {
    case 'aiAssisted':
      return 'AI-assisted';
    case 'mixed':
      return 'Mixed';
    default:
      return 'Deterministic';
  }
}

function getScopeLabel(scope: NormalizedFinding['scope']): string {
  switch (scope) {
    case 'crossPage':
      return 'Cross-page';
    default:
      return scope[0].toUpperCase() + scope.slice(1);
  }
}

function getImpactAreaLabel(impactArea: NormalizedFinding['impactArea']): string {
  switch (impactArea) {
    case 'kpiEffectiveness':
      return 'KPI effectiveness';
    default:
      return impactArea[0].toUpperCase() + impactArea.slice(1);
  }
}

function getDimensionLabel(dimension: IssueFilterState['dimension']): string {
  if (dimension === 'all') {
    return 'All dimensions';
  }

  return dimension[0].toUpperCase() + dimension.slice(1);
}

function getMatrixStatusClassName(status: NonNullable<ScoreResult['crossPageMatrix']>['rows'][number]['cells'][number]['status']): string {
  switch (status) {
    case 'weak':
      return 'matrix-status-weak';
    case 'watch':
      return 'matrix-status-watch';
    case 'strong':
      return 'matrix-status-strong';
    default:
      return 'matrix-status-unknown';
  }
}

function getMatrixStatusLabel(status: NonNullable<ScoreResult['crossPageMatrix']>['rows'][number]['cells'][number]['status']): string {
  return status[0].toUpperCase() + status.slice(1);
}

function mapDimensionToImpactAreas(dimension: Exclude<IssueFilterState['dimension'], 'all'>): NormalizedFinding['impactArea'][] {
  switch (dimension) {
    case 'layout':
      return ['layout', 'density'];
    case 'story':
      return ['storytelling', 'kpiEffectiveness', 'benchmark'];
    case 'consistency':
      return ['governance', 'metadata'];
    default:
      return [dimension];
  }
}

function buildPersonaDefaultFilters(profile: ReviewPresentationPersonaProfile | undefined): IssueFilterState {
  if (!profile) {
    return {
      severity: 'all',
      pageName: 'all',
      dimension: 'all',
      impactArea: 'all',
      scope: 'all',
      detectionType: 'all',
    };
  }

  const impactArea = profile.emphasizedImpactAreas[0] ?? 'all';
  return {
    severity: profile.defaultSeverityFilter?.includes('high') ? 'high' : 'all',
    pageName: 'all',
    dimension: impactArea === 'actionability'
      ? 'actionability'
      : impactArea === 'accessibility'
        ? 'accessibility'
        : impactArea === 'governance' || impactArea === 'metadata'
          ? 'consistency'
          : impactArea === 'navigation'
            ? 'navigation'
            : impactArea === 'storytelling' || impactArea === 'kpiEffectiveness' || impactArea === 'benchmark'
              ? 'story'
              : impactArea === 'layout' || impactArea === 'density'
                ? 'layout'
                : 'all',
    impactArea,
    scope: profile.id === 'governance' ? 'crossPage' : 'all',
    detectionType: profile.defaultDetectionTypes?.[0] ?? 'all',
  };
}

function summarizeActiveIssueFilters(filters: IssueFilterState): string[] {
  const items: string[] = [];
  if (filters.severity !== 'all') {
    items.push(`Severity: ${getNormalizedFindingSeverityLabel(filters.severity)}`);
  }
  if (filters.pageName !== 'all') {
    items.push(`Page: ${filters.pageName}`);
  }
  if (filters.dimension !== 'all') {
    items.push(`Dimension: ${getDimensionLabel(filters.dimension)}`);
  }
  if (filters.impactArea !== 'all') {
    items.push(`Impact: ${getImpactAreaLabel(filters.impactArea)}`);
  }
  if (filters.scope !== 'all') {
    items.push(`Scope: ${getScopeLabel(filters.scope)}`);
  }
  if (filters.detectionType !== 'all') {
    items.push(`Detection: ${getDetectionTypeLabel(filters.detectionType)}`);
  }
  return items;
}

function buildVisibleFindings(
  findings: NormalizedFinding[] | undefined,
  selectedPageName: string | undefined,
): NormalizedFinding[] {
  if (!findings || findings.length === 0) {
    return [];
  }

  if (!selectedPageName) {
    return findings;
  }

  return findings.filter((finding) => {
    if (finding.scope === 'report') {
      return true;
    }

    if (finding.affectedPages.length === 0) {
      return true;
    }

    return finding.affectedPages.includes(selectedPageName);
  });
}

function applyIssueFilters(
  findings: NormalizedFinding[],
  filters: IssueFilterState,
): NormalizedFinding[] {
  return findings.filter((finding) => {
    if (filters.severity !== 'all' && finding.severity !== filters.severity) {
      return false;
    }

    if (filters.pageName !== 'all' && !finding.affectedPages.includes(filters.pageName)) {
      return false;
    }

    if (filters.dimension !== 'all' && !mapDimensionToImpactAreas(filters.dimension).includes(finding.impactArea)) {
      return false;
    }

    if (filters.impactArea !== 'all' && finding.impactArea !== filters.impactArea) {
      return false;
    }

    if (filters.scope !== 'all' && finding.scope !== filters.scope) {
      return false;
    }

    if (filters.detectionType !== 'all' && finding.detectionType !== filters.detectionType) {
      return false;
    }

    return true;
  });
}

function buildIssueGroups(
  findings: NormalizedFinding[],
  groupingMode: IssueGroupingMode,
): Array<{ key: string; label: string; findings: NormalizedFinding[]; defaultOpen: boolean }> {
  if (groupingMode === 'impactArea') {
    const groups = findings.reduce<Record<string, NormalizedFinding[]>>((acc, finding) => {
      const key = finding.impactArea;
      acc[key] = acc[key] ?? [];
      acc[key].push(finding);
      return acc;
    }, {});

    return Object.entries(groups)
      .sort((left, right) => left[0].localeCompare(right[0]))
      .map(([key, items]) => ({
        key,
        label: getImpactAreaLabel(key as NormalizedFinding['impactArea']),
        findings: items.sort((left, right) => {
          const severityDiff = getNormalizedFindingSeverityOrder(left.severity) - getNormalizedFindingSeverityOrder(right.severity);
          return severityDiff !== 0 ? severityDiff : right.confidence - left.confidence;
        }),
        defaultOpen: key === 'actionability' || key === 'benchmark',
      }));
  }

  const grouped = findings.reduce<Record<NormalizedFindingSeverity, NormalizedFinding[]>>(
    (groups, finding) => {
      groups[finding.severity].push(finding);
      return groups;
    },
    {
      high: [],
      medium: [],
      low: [],
      info: [],
    },
  );

  return (Object.keys(grouped) as NormalizedFindingSeverity[])
    .filter((severity) => grouped[severity].length > 0)
    .sort((left, right) => (
      getNormalizedFindingSeverityOrder(left) - getNormalizedFindingSeverityOrder(right)
    ))
    .map((severity) => ({
      key: severity,
      label: getNormalizedFindingSeverityLabel(severity),
      findings: grouped[severity],
      defaultOpen: severity === 'high',
    }));
}

function renderIssuesWorkspace(
  props: {
    findings: NormalizedFinding[];
    filters: IssueFilterState;
    groupingMode: IssueGroupingMode;
    pageOptions: string[];
    activeFilterSummary: string[];
    onFilterChange: (key: keyof IssueFilterState, value: string) => void;
    onGroupingModeChange: (value: IssueGroupingMode) => void;
    onClearFilters: () => void;
    onResetToPersonaDefaults: () => void;
  },
): React.ReactNode {
  const {
    findings,
    filters,
    groupingMode,
    pageOptions,
    activeFilterSummary,
    onFilterChange,
    onGroupingModeChange,
    onClearFilters,
    onResetToPersonaDefaults,
  } = props;
  if (findings.length === 0) {
    return (
      <section aria-label="Issues workspace" className="panel-card issues-card">
        <div className="issues-section-head">
          <div>
            <p className="section-kicker">Primary review surface</p>
            <h2>Issues</h2>
          </div>
        </div>
        <p className="empty-text">No normalized issues were generated for this view.</p>
      </section>
    );
  }

  const filteredFindings = applyIssueFilters(findings, filters);
  const groups = buildIssueGroups(filteredFindings, groupingMode);

  return (
    <section aria-label="Issues workspace" className="panel-card issues-card">
      <div className="issues-section-head">
        <div>
          <p className="section-kicker">Primary review surface</p>
          <h2>Issues</h2>
        </div>
        <p className="issues-section-copy">
          Review the highest-priority problems first, then expand evidence only when needed.
        </p>
      </div>
      <div className="issues-toolbar">
        <label>
          Severity
          <select aria-label="Issue severity filter" onChange={(event) => onFilterChange('severity', event.target.value)} value={filters.severity}>
            <option value="all">All</option>
            <option value="high">High</option>
            <option value="medium">Medium</option>
            <option value="low">Low</option>
            <option value="info">Info</option>
          </select>
        </label>
        <label>
          Page
          <select aria-label="Issue page filter" onChange={(event) => onFilterChange('pageName', event.target.value)} value={filters.pageName}>
            <option value="all">All</option>
            {pageOptions.map((pageName) => (
              <option key={pageName} value={pageName}>{pageName}</option>
            ))}
          </select>
        </label>
        <label>
          Dimension
          <select aria-label="Issue dimension filter" onChange={(event) => onFilterChange('dimension', event.target.value)} value={filters.dimension}>
            <option value="all">All</option>
            <option value="layout">Layout</option>
            <option value="story">Story</option>
            <option value="accessibility">Accessibility</option>
            <option value="consistency">Consistency</option>
            <option value="navigation">Navigation</option>
            <option value="actionability">Actionability</option>
          </select>
        </label>
        <label>
          Impact
          <select aria-label="Issue impact filter" onChange={(event) => onFilterChange('impactArea', event.target.value)} value={filters.impactArea}>
            <option value="all">All</option>
            <option value="actionability">Actionability</option>
            <option value="accessibility">Accessibility</option>
            <option value="benchmark">Benchmark</option>
            <option value="density">Density</option>
            <option value="governance">Governance</option>
            <option value="kpiEffectiveness">KPI effectiveness</option>
            <option value="layout">Layout</option>
            <option value="metadata">Metadata</option>
            <option value="navigation">Navigation</option>
            <option value="storytelling">Storytelling</option>
          </select>
        </label>
        <label>
          Scope
          <select aria-label="Issue scope filter" onChange={(event) => onFilterChange('scope', event.target.value)} value={filters.scope}>
            <option value="all">All</option>
            <option value="visual">Visual</option>
            <option value="page">Page</option>
            <option value="crossPage">Cross-page</option>
            <option value="report">Report</option>
          </select>
        </label>
        <label>
          Detection
          <select aria-label="Issue detection filter" onChange={(event) => onFilterChange('detectionType', event.target.value)} value={filters.detectionType}>
            <option value="all">All</option>
            <option value="deterministic">Deterministic</option>
            <option value="aiAssisted">AI-assisted</option>
            <option value="mixed">Mixed</option>
          </select>
        </label>
        <label>
          Group by
          <select aria-label="Issue grouping mode" onChange={(event) => onGroupingModeChange(event.target.value as IssueGroupingMode)} value={groupingMode}>
            <option value="severity">Severity</option>
            <option value="impactArea">Category</option>
          </select>
        </label>
      </div>
      {activeFilterSummary.length > 0 ? (
        <div className="active-filter-summary" aria-label="Active issue filters">
          <p>{activeFilterSummary.join(' · ')}</p>
          <div className="active-filter-actions">
            <button className="link-button" onClick={onResetToPersonaDefaults} type="button">Use review mode defaults</button>
            <button className="link-button" onClick={onClearFilters} type="button">Clear filters</button>
          </div>
        </div>
      ) : null}
      <p className="issues-results-copy">
        Showing {filteredFindings.length} of {findings.length} finding(s).
      </p>
      <div className="issues-group-list">
        {groups.map((group) => (
          <details
            className="issue-group"
            key={group.key}
            open={group.defaultOpen}
          >
            <summary className="issue-group-summary">
              <span>{group.label}</span>
              <span>{group.findings.length}</span>
            </summary>
            <div className="issue-card-list">
              {group.findings.map((finding) => (
                <details className="issue-card" key={finding.id}>
                  <summary className="issue-card-summary">
                    <div className="issue-card-head">
                      <div>
                        <h3>{finding.title}</h3>
                        <p className="issue-card-copy">{finding.summary}</p>
                      </div>
                      <span className={`issue-severity-badge ${getNormalizedFindingSeverityClassName(finding.severity)}`}>
                        {finding.severity}
                      </span>
                    </div>
                    <dl className="issue-meta-grid">
                      <div>
                        <dt>Confidence</dt>
                        <dd>{finding.confidence}</dd>
                      </div>
                      <div>
                        <dt>Scope</dt>
                        <dd>{getScopeLabel(finding.scope)}</dd>
                      </div>
                      <div>
                        <dt>Detection</dt>
                        <dd>{getDetectionTypeLabel(finding.detectionType)}</dd>
                      </div>
                      <div>
                        <dt>Impact</dt>
                        <dd>{getImpactAreaLabel(finding.impactArea)}</dd>
                      </div>
                    </dl>
                    <p className="issue-recommendation-lead">
                      <strong>Fix first:</strong> {finding.recommendation}
                    </p>
                  </summary>
                  <div className="issue-card-body">
                    <div className="issue-detail-block">
                      <p className="issue-detail-label">Affected pages</p>
                      <p>{finding.affectedPages.length > 0 ? finding.affectedPages.join(', ') : 'Report-wide'}</p>
                    </div>
                    <div className="issue-detail-block">
                      <p className="issue-detail-label">Framework impact</p>
                      <p>{finding.frameworkImpact.length > 0 ? finding.frameworkImpact.join(', ') : 'Not mapped'}</p>
                    </div>
                    {finding.evidence.length > 0 ? (
                      <div className="issue-detail-block">
                        <p className="issue-detail-label">Evidence references</p>
                        <ul className="issue-evidence-list">
                          {finding.evidence.map((evidence, index) => (
                            <li className="issue-evidence-item" key={`${finding.id}-evidence-${index}`}>
                              <strong>{evidence.label}</strong>
                              {evidence.pageName ? ` · ${evidence.pageName}` : ''}
                              {evidence.detail ? ` — ${evidence.detail}` : ''}
                            </li>
                          ))}
                        </ul>
                      </div>
                    ) : null}
                  </div>
                </details>
              ))}
            </div>
          </details>
        ))}
      </div>
    </section>
  );
}

function renderEvidence(
  affectedVisuals: AffectedVisualReference[],
  currentPageName: string | undefined,
  onRevealVisual: (visual: AffectedVisualReference) => void,
): React.ReactNode {
  if (affectedVisuals.length === 0) {
    return null;
  }

  const showPageName = affectedVisuals.some((visual) => visual.pageName !== currentPageName);

  return (
    <details className="evidence-details">
      <summary className="evidence-summary">
        Show affected visuals ({affectedVisuals.length})
      </summary>
      <ul className="evidence-list">
        {affectedVisuals.map((visual) => (
          <li key={`${visual.pageName}-${visual.visualId}`}>
            <button
              className="evidence-button"
              onClick={() => onRevealVisual(visual)}
              title={`${visual.pageName} > ${visual.visualType} > ${visual.visualId}`}
              type="button"
            >
              {showPageName ? (
                <span className="evidence-page">{visual.pageName}</span>
              ) : null}
              <span className="evidence-label">
                {visual.visualType} {shortenVisualId(visual.visualId)}
              </span>
            </button>
          </li>
        ))}
      </ul>
    </details>
  );
}

function renderOverviewWorkspace(
  overviewSummary: OverviewSummary | undefined,
  frameworkValues: Array<{ key: string; label: string; score: number; weightLabel: string }>,
  scoreValue: number,
  visualMix: { data?: number; navigation?: number; hidden?: number },
  crossPageMatrix: ScoreResult['crossPageMatrix'],
  selectedPageName: string | undefined,
  workspacePersona: ReviewPresentationPersona,
  personaProfiles: ReviewPresentationPersonaProfile[],
  onWorkspacePersonaChange: (persona: ReviewPresentationPersona) => void,
  onMatrixCellClick: (pageName: string, dimension: Exclude<IssueFilterState['dimension'], 'all'>) => void,
  onReturnToReportContext: () => void,
): React.ReactNode {
  if (!overviewSummary) {
    return null;
  }

  const visibleRows = selectedPageName
    ? crossPageMatrix?.rows.filter((row) => row.pageName === selectedPageName)
    : crossPageMatrix?.rows;

  return (
    <section aria-label="Overview workspace" className="panel-card overview-card">
      <div className="overview-head">
        <div>
          <p className="section-kicker">Executive summary</p>
          <h2>Overview</h2>
          <p className="overview-summary-copy">{overviewSummary.executiveSummary}</p>
          <label className="overview-persona-picker" htmlFor="workspace-persona">
            <span>Review mode</span>
            <select
              aria-label="Workspace review mode"
              id="workspace-persona"
              onChange={(event) => onWorkspacePersonaChange(event.target.value as ReviewPresentationPersona)}
              value={workspacePersona}
            >
              {personaProfiles.map((profile) => (
                <option key={profile.id} value={profile.id}>{profile.label}</option>
              ))}
            </select>
          </label>
          <p className="overview-helper-copy">
            Review modes change how findings are prioritized and explained. They do not change the underlying score.
          </p>
        </div>
        <div className={`score-chip ${getScoreTone(scoreValue)}`}>
          <span>{Math.round(scoreValue)}</span>
          <small>/100</small>
        </div>
      </div>
      <div className="overview-badges">
        <span className="overview-badge">Maturity: {overviewSummary.maturityBand}</span>
        <span className="overview-badge">Risk: {overviewSummary.riskBand}</span>
        <span className="overview-badge">High issues: {overviewSummary.severityDistribution.high}</span>
        <span className="overview-badge">Medium issues: {overviewSummary.severityDistribution.medium}</span>
      </div>
      <p className="overview-copy">
        Benchmark summary: {overviewSummary.benchmarkSummary}
      </p>
      <p className="overview-copy">
        Cross-page summary: {overviewSummary.crossPageSummary.headline}
      </p>
      {typeof visualMix.data === 'number' &&
      typeof visualMix.navigation === 'number' &&
      typeof visualMix.hidden === 'number' ? (
        <p className="overview-copy">
          Visual mix: {visualMix.data} data, {visualMix.navigation} navigation, {visualMix.hidden} hidden.
        </p>
      ) : null}
      {frameworkValues.length > 0 ? (
        <div className="summary-framework-list">
          {frameworkValues.map((fw) => (
            <div className="summary-framework-row" key={fw.key}>
              <span className="summary-framework-label">{fw.label}</span>
              <div className="summary-framework-bar-track">
                <span
                  className={`summary-framework-bar-fill ${getScoreTone(fw.score)}`}
                  style={{ width: `${Math.round(fw.score)}%` }}
                />
              </div>
              <span className={`summary-framework-score ${getScoreTone(fw.score)}`}>
                {Math.round(fw.score)}
              </span>
            </div>
          ))}
        </div>
      ) : null}
      <div className="overview-grid">
        <div className="overview-list-card">
          <h3>Top strengths</h3>
          <ul>
            {overviewSummary.topStrengths.map((item) => (
              <li key={item.id}><strong>{item.title}</strong> {item.detail}</li>
            ))}
          </ul>
        </div>
        <div className="overview-list-card">
          <h3>Top weaknesses</h3>
          <ul>
            {overviewSummary.topWeaknesses.map((item) => (
              <li key={item.id}><strong>{item.title}</strong> {item.detail}</li>
            ))}
          </ul>
        </div>
        <div className="overview-list-card">
          <h3>Top issues</h3>
          <ul>
            {overviewSummary.topIssues.map((item) => (
              <li key={item.id}><strong>{item.title}</strong> {item.detail}</li>
            ))}
          </ul>
        </div>
        <div className="overview-list-card">
          <h3>Top actions</h3>
          <ol>
            {overviewSummary.topActions.map((item) => (
              <li key={item.id}><strong>{item.title}</strong> {item.detail}</li>
            ))}
          </ol>
        </div>
      </div>
      {crossPageMatrix && visibleRows && visibleRows.length > 0 ? (
        <div className="overview-matrix">
          <div className="overview-matrix-head">
            <h3>Cross-page matrix</h3>
            {selectedPageName ? (
              <button className="link-button" onClick={onReturnToReportContext} type="button">
                Back to full matrix
              </button>
            ) : null}
          </div>
          <div className="matrix-scroll">
            <table>
              <thead>
                <tr>
                  <th>Page</th>
                  {crossPageMatrix.dimensions.map((dimension) => (
                    <th key={dimension}>{getDimensionLabel(dimension)}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {visibleRows.map((row) => (
                  <tr key={row.pageName}>
                    <th>{row.pageName}</th>
                    {row.cells.map((cell) => (
                      <td key={`${row.pageName}-${cell.dimension}`}>
                        <button
                          aria-label={`Filter issues for ${cell.pageName} ${getDimensionLabel(cell.dimension)}`}
                          className={`matrix-cell-button ${getMatrixStatusClassName(cell.status)} ${cell.severity ? getNormalizedFindingSeverityClassName(cell.severity) : ''}`}
                          onClick={() => onMatrixCellClick(cell.pageName, cell.dimension)}
                          title={cell.summary}
                          type="button"
                        >
                          <strong>{getMatrixStatusLabel(cell.status)}</strong>
                          <span>{cell.findingCount} finding{cell.findingCount === 1 ? '' : 's'}</span>
                          {cell.highSeverityCount > 0 ? <small>{cell.highSeverityCount} high</small> : null}
                        </button>
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}
    </section>
  );
}

function renderPagePurposeAnalysisSection(props: {
  analysis: ScoreResult['pagePurposeAnalysis'] | PageScore['pagePurposeAnalysis'];
  storySummary: NonNullable<ScoreResult['inferredStorySummary'] | PageScore['inferredStorySummary']>;
  pageIntentProfile: PageIntentProfile | undefined;
  selectedProfile: PageIntentProfileType;
  onProfileChange: (next: PageIntentProfileType) => void;
  actionabilityBreakdown: ActionabilityBreakdown | undefined;
  benchmarkComparison: BenchmarkComparisonSummary | undefined;
  confirmation: IntentFeedbackConfirmation | undefined;
  note: string;
  onConfirm: (next: IntentFeedbackConfirmation) => void;
  onNoteChange: (next: string) => void;
  onSaveNote: () => void;
  noteSaved: boolean;
  expanded: boolean;
  onToggleExpanded: () => void;
}): React.ReactNode {
  const {
    analysis,
    storySummary,
    pageIntentProfile,
    selectedProfile,
    onProfileChange,
    actionabilityBreakdown,
    benchmarkComparison,
    confirmation,
    note,
    onConfirm,
    onNoteChange,
    onSaveNote,
    noteSaved,
    expanded,
    onToggleExpanded,
  } = props;

  if (!analysis) {
    return null;
  }

  return (
    <section className="panel-card page-purpose-card">
      <div className="issues-section-head">
        <div>
          <p className="section-kicker">Reasoning workflow</p>
          <h2>Page Purpose Analysis</h2>
        </div>
        <p className="issues-section-copy">
          Understand what this page appears to do, why the analyzer believes that, and where the decision-support gaps still sit.
        </p>
      </div>
      <div className="page-purpose-summary">
        <div className="overview-badges">
          <span className="overview-badge">Purpose: {analysis.inferredPurpose}</span>
          {analysis.confidence ? <span className="overview-badge">Confidence: {analysis.confidence}</span> : null}
          {typeof analysis.actionabilityScore === 'number' ? (
            <span className="overview-badge">Actionability: {analysis.actionabilityScore.toFixed(0)}/100</span>
          ) : null}
          {analysis.benchmarkStatus ? <span className="overview-badge">Benchmark: {analysis.benchmarkStatus}</span> : null}
        </div>
        {analysis.topGaps.length > 0 ? (
          <p className="overview-copy">
            <strong>Top gaps:</strong> {analysis.topGaps.join(' · ')}
          </p>
        ) : null}
        <h3>Why This Matters</h3>
        <p className="overview-summary-copy">{analysis.whyThisMatters}</p>
        <button className="secondary-button" onClick={onToggleExpanded} type="button">
          {expanded ? 'Hide Full Reasoning' : 'Show Full Reasoning'}
        </button>
      </div>
      {expanded ? (
        <div className="page-purpose-details">
          {renderPageStorySummary(storySummary)}
          {renderPageIntentProfileSummary(
            pageIntentProfile,
            selectedProfile,
            onProfileChange,
          )}
          {renderActionabilityBreakdown(actionabilityBreakdown)}
          {renderBenchmarkComparison(benchmarkComparison)}
          <section className="panel-card story-review-card">
            {renderStoryIntentReview(
              storySummary,
              confirmation,
              note,
              onConfirm,
              onNoteChange,
              onSaveNote,
              noteSaved,
            )}
          </section>
        </div>
      ) : null}
    </section>
  );
}

function renderFixPlanSection(
  queue: ContextAwareRemediationQueue,
  fixOpportunities: FixOpportunity[] | undefined,
  proposalEnrichments: ScoreResult['proposalEnrichments'] | undefined,
  fixSelection: ScorePanelState['fixSelection'],
  fixApplySessions: NonNullable<ScorePanelState['fixApplySessions']>,
  expandedOpportunityIds: string[],
  onToggleOpportunity: (opportunityId: string) => void,
  onToggleOpportunitySelection: (opportunityId: string) => void,
  onPreviewSelected: () => void,
  onApproveSelected: () => void,
  onApplySelected: () => void,
  onRollbackSession: (sessionId: string) => void,
  onRegenerate: (opportunityIds?: string[]) => void,
): React.ReactNode {
  const fixPlan = queue.items;
  const selectedIds = new Set(fixSelection?.selectedOpportunityIds ?? []);
  const selectionBlocked = (fixSelection?.compatibility.blockingReasons.length ?? 0) > 0;
  const selectedCount = fixSelection?.selectedOpportunityIds.length ?? 0;

  return (
    <section aria-label="Fix plan" className="panel-card fix-plan-card">
      <div className="issues-section-head">
        <div>
          <p className="section-kicker">Consultant workflow</p>
          <h2>Fix Plan</h2>
        </div>
        <p className="issues-section-copy">
          Convert the selected problem area into a sequenced remediation queue.
        </p>
      </div>
      <div className="issue-detail-block">
        <p className="issue-detail-label">Remediation Focus</p>
        <p>{queue.focus.label}</p>
        <p className="issues-section-copy">{queue.focus.helperText}</p>
      </div>
      <div className="issue-detail-block">
        <p className="issue-detail-label">Batch workflow</p>
        <p className="issues-section-copy">
          Select compatible deterministic opportunities, preview them together, approve the preview, then apply and re-analyze as one batch.
        </p>
        <p className="fix-plan-recommendation">
          <strong>Selected opportunities:</strong> {selectedCount}
        </p>
        {fixSelection?.message ? (
          <p className="fix-plan-recommendation">{fixSelection.message}</p>
        ) : null}
        <div className="export-actions">
          <button className="secondary-button" disabled={selectedCount === 0} onClick={onPreviewSelected} type="button">
            Preview selected
          </button>
          <button
            className="secondary-button"
            disabled={selectedCount === 0 || selectionBlocked || fixSelection?.approvalState !== 'Previewed'}
            onClick={onApproveSelected}
            type="button"
          >
            Approve selected
          </button>
          <button
            className="primary-button"
            disabled={selectedCount === 0 || selectionBlocked || fixSelection?.approvalState !== 'Approved'}
            onClick={onApplySelected}
            type="button"
          >
            Apply selected
          </button>
          <button className="secondary-button" onClick={() => onRegenerate(fixSelection?.selectedOpportunityIds)} type="button">
            Regenerate stale
          </button>
        </div>
      </div>
      {selectionBlocked ? (
        <div className="issue-detail-block">
          <p className="issue-detail-label">Compatibility</p>
          <ul className="issue-evidence-list">
            {fixSelection?.compatibility.blockingReasons.map((reason, index) => (
              <li className="issue-evidence-item" key={`${reason.code}-${index}`}>
                {reason.message} ({reason.code})
              </li>
            ))}
          </ul>
        </div>
      ) : null}
      {fixSelection?.groupedPreview ? (
        <div className="issue-detail-block">
          <p className="issue-detail-label">Grouped preview</p>
          <p className="fix-plan-recommendation">
            <strong>Changed files:</strong> {fixSelection.groupedPreview.summary.changedFileCount} · <strong>Changed objects:</strong> {fixSelection.groupedPreview.summary.changedObjectCount}
          </p>
          <p className="fix-plan-recommendation">
            <strong>Expected outcomes:</strong> {fixSelection.groupedPreview.expectedOutcomes.join(' · ')}
          </p>
          <div className="issue-detail-block">
            <p className="issue-detail-label">Mutation facts</p>
            <table>
              <thead>
                <tr>
                  <th align="left">Page</th>
                  <th align="left">Object</th>
                  <th align="left">Property</th>
                  <th align="left">Before</th>
                  <th align="left">After</th>
                </tr>
              </thead>
              <tbody>
                {fixSelection.groupedPreview.mutationFacts.map((row, index) => (
                  <tr key={`${row.objectId}-${row.property}-${index}`}>
                    <td>{row.pageName ?? 'Report-wide'}</td>
                    <td>{row.objectId}</td>
                    <td>{row.property}</td>
                    <td>{String(row.before)}</td>
                    <td>{String(row.after)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="issue-detail-block">
            <p className="issue-detail-label">Grouped by page / object / property</p>
            <ul className="issue-evidence-list">
              {fixSelection.groupedPreview.pageGroups.map((pageGroup) => (
                <li className="issue-evidence-item" key={pageGroup.pageName}>
                  <strong>{pageGroup.pageName}</strong>
                  <ul className="issue-evidence-list">
                    {pageGroup.objectGroups.map((objectGroup) => (
                      <li className="issue-evidence-item" key={`${pageGroup.pageName}-${objectGroup.objectId}`}>
                        {objectGroup.objectId}: {objectGroup.propertyChanges.map((change) => change.property).join(' · ')}
                      </li>
                    ))}
                  </ul>
                </li>
              ))}
            </ul>
          </div>
        </div>
      ) : null}
      {fixApplySessions.length > 0 ? (
        <div className="issue-detail-block">
          <p className="issue-detail-label">Session history</p>
          <ul className="issue-evidence-list">
            {fixApplySessions.map((session) => (
              <li className="issue-evidence-item" key={session.id}>
                <strong>{session.opportunityTitles.join(' · ')}</strong>
                <p className="issues-section-copy">
                  Applied {session.appliedAt} · Rollback {session.rollbackAvailable ? 'available' : 'unavailable'}
                </p>
                {session.groupedOutcomeSummary ? (
                  <p className="issues-section-copy">
                    {session.groupedOutcomeSummary.statuses.map((status) => `${getFixOutcomeStatusLabel(status.status)} ${status.count}`).join(' · ')}
                  </p>
                ) : null}
                {session.staleOpportunityIds?.length ? (
                  <p className="issues-section-copy">
                    Stale: {session.staleOpportunityIds.join(' · ')}
                  </p>
                ) : null}
                {session.regeneratedOpportunityIds?.length ? (
                  <p className="issues-section-copy">
                    Regenerated: {session.regeneratedOpportunityIds.join(' · ')}
                  </p>
                ) : null}
                <div className="export-actions">
                  <button
                    className="secondary-button"
                    disabled={!session.rollbackAvailable}
                    onClick={() => onRollbackSession(session.id)}
                    type="button"
                  >
                    Roll back session
                  </button>
                </div>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
      {fixPlan.length === 0 ? (
        <p className="empty-text">No remediation actions were generated for this problem area.</p>
      ) : (
        <ol className="fix-plan-list">
          {fixPlan.map((item) => (
            <li className="fix-plan-item" key={item.id}>
              {(() => {
                const relatedOpportunities = (fixOpportunities ?? []).filter((opportunity) => remediationMatchesOpportunity(item, opportunity));

                return (
                  <>
              <div className="fix-plan-head">
                <h3>{item.title}</h3>
                <span className={`issue-severity-badge ${getNormalizedFindingSeverityClassName(item.severity)}`}>{item.severity}</span>
              </div>
              <p>{item.detail}</p>
              <dl className="issue-meta-grid">
                <div>
                  <dt>Impact</dt>
                  <dd>{item.impact}</dd>
                </div>
                <div>
                  <dt>Effort</dt>
                  <dd>{item.effort}</dd>
                </div>
                <div>
                  <dt>Scope</dt>
                  <dd>{getScopeLabel(item.scope)}</dd>
                </div>
                <div>
                  <dt>Affected pages</dt>
                  <dd>{item.affectedPages.length > 0 ? item.affectedPages.join(', ') : 'Report-wide'}</dd>
                </div>
              </dl>
              <p className="fix-plan-recommendation"><strong>Why:</strong> {item.why}</p>
              <p className="fix-plan-recommendation"><strong>Recommended action:</strong> {item.recommendedAction}</p>
              {item.findingCoverageLabel ? (
                <p className="fix-plan-recommendation"><strong>Finding Coverage:</strong> {item.findingCoverageLabel}</p>
              ) : null}
              {item.resolvedOutcomes.length > 0 ? (
                <div className="issue-detail-block">
                  <p className="issue-detail-label">Resolves</p>
                  <ul className="issue-evidence-list">
                    {item.resolvedOutcomes.map((outcome) => (
                      <li className="issue-evidence-item" key={`${item.id}-${outcome}`}>{outcome}</li>
                    ))}
                  </ul>
                </div>
              ) : null}
              <div className="issue-detail-block">
                <p className="issue-detail-label">Source findings</p>
                <ul className="issue-evidence-list">
                  {item.sourceFindings.map((finding) => (
                    <li className="issue-evidence-item" key={`${item.id}-${finding.id}`}>
                      {finding.title} ({getNormalizedFindingSeverityLabel(finding.severity)})
                    </li>
                  ))}
                </ul>
              </div>
              {(() => {
                const enrichment = proposalEnrichments?.find((entry) => remediationMatchesProposalEnrichment(item, entry));
                if (!hasProposalEnrichmentContent(enrichment)) {
                  return null;
                }

                return (
                  <div className="issue-detail-block">
                    <p className="issue-detail-label">AI-enriched guidance</p>
                    {enrichment?.source === 'fallback' ? (
                      <p className="issues-section-copy">{getProposalEnrichmentSummary(enrichment)}</p>
                    ) : null}
                    <p className="fix-plan-recommendation"><strong>Expected resolutions:</strong></p>
                    {enrichment?.titleSuggestions?.length ? (
                      <p className="fix-plan-recommendation">
                        <strong>Suggested title:</strong> {enrichment.titleSuggestions[0].title}
                      </p>
                    ) : null}
                    {enrichment?.explanation ? (
                      <p className="fix-plan-recommendation">{enrichment.explanation.shortText}</p>
                    ) : null}
                    {enrichment?.whyThisMatters ? (
                      <p className="fix-plan-recommendation">{enrichment.whyThisMatters.text}</p>
                    ) : null}
                    {enrichment?.advisoryPriority ? (
                      <p className="fix-plan-recommendation">
                        <strong>{getAdvisoryPriorityLabel(enrichment.advisoryPriority.tier)}</strong>: {enrichment.advisoryPriority.rationale}
                      </p>
                    ) : null}
                    {enrichment?.expectedOutcome ? (
                      <p className="fix-plan-recommendation">{enrichment.expectedOutcome.text}</p>
                    ) : null}
                    {enrichment?.advisoryAlternatives?.length ? (
                      <ul className="issue-evidence-list">
                        {enrichment.advisoryAlternatives.map((alternative) => (
                          <li className="issue-evidence-item" key={`${item.id}-${alternative.title}`}>
                            <strong>{alternative.title}</strong>: {alternative.description}
                          </li>
                        ))}
                      </ul>
                    ) : null}
                  </div>
                );
              })()}
              <div className="issue-detail-block">
                <p className="issue-detail-label">Fix opportunities</p>
                {relatedOpportunities.length ? (
                  <ul className="issue-evidence-list">
                    {relatedOpportunities.map((opportunity) => {
                      const expanded = expandedOpportunityIds.includes(opportunity.id);
                      const selected = selectedIds.has(opportunity.id);

                      return (
                        <li className="issue-evidence-item" key={opportunity.id}>
                          <div className="fix-plan-head">
                            <div>
                              <strong>{opportunity.title}</strong>
                              <p className="issues-section-copy">
                                {getFixOpportunityCategoryLabel(opportunity.category)} · {opportunity.summary}
                              </p>
                            </div>
                            <span className="overview-badge">{getFixOpportunityStateLabel(opportunity.state)}</span>
                          </div>
                          <label className="issues-section-copy">
                            <input
                              aria-label={`Select ${opportunity.title}`}
                              checked={selected}
                              onChange={() => onToggleOpportunitySelection(opportunity.id)}
                              type="checkbox"
                            />
                            {' '}
                            Select for grouped preview/apply
                          </label>
                          <p className="fix-plan-recommendation">
                            <strong>Opportunity resolutions:</strong> {opportunity.expectedResolutions.join(' · ')}
                          </p>
                          <p className="fix-plan-recommendation">
                            <strong>Confidence:</strong> {opportunity.confidence}/100
                          </p>
                          <p className="fix-plan-recommendation">
                            <strong>Rollback:</strong> {opportunity.rollbackPlan.fileBackups.length > 0 ? 'Available' : 'Unavailable'}
                          </p>
                          <div className="export-actions">
                            <button
                              className="secondary-button"
                              onClick={() => onToggleOpportunity(opportunity.id)}
                              type="button"
                            >
                              {expanded ? 'Hide Preview' : 'Show Preview'}
                            </button>
                          </div>
                          {expanded ? (
                            <div className="issue-detail-block">
                              <p className="issue-detail-label">Mutation preview</p>
                              <table>
                                <thead>
                                  <tr>
                                    <th align="left">Object</th>
                                    <th align="left">Property</th>
                                    <th align="left">Before</th>
                                    <th align="left">After</th>
                                  </tr>
                                </thead>
                                <tbody>
                                  {opportunity.previewRows.map((row, index) => (
                                    <tr key={`${opportunity.id}-${row.objectId}-${row.property}-${index}`}>
                                      <td>{row.pageName ? `${row.pageName} · ${row.objectId}` : row.objectId}</td>
                                      <td>{row.property}</td>
                                      <td>{String(row.before)}</td>
                                      <td>{String(row.after)}</td>
                                    </tr>
                                  ))}
                                </tbody>
                              </table>
                            </div>
                          ) : null}
                          {opportunity.outcome?.entries.length ? (
                            <div className="issue-detail-block">
                              <p className="issue-detail-label">Outcome after re-analysis</p>
                              <ul className="issue-evidence-list">
                                {opportunity.outcome.entries.map((entry) => (
                                  <li className="issue-evidence-item" key={`${opportunity.id}-${entry.findingId}`}>
                                    {getFixOutcomeStatusLabel(entry.status)}: {entry.title}
                                  </li>
                                ))}
                              </ul>
                            </div>
                          ) : null}
                        </li>
                      );
                    })}
                  </ul>
                ) : (
                  <p className="issues-section-copy">
                    Advisory only: no safe metadata-only fix is currently available for this remediation.
                  </p>
                )}
              </div>
                  </>
                );
              })()}
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

function renderFeedback(
  items: FrameworkFeedbackItem[] | undefined,
  currentPageName: string | undefined,
  onRevealVisual: (visual: AffectedVisualReference) => void,
): React.ReactNode {
  if (!items || items.length === 0) {
    return <p className="feedback-empty">No feedback available.</p>;
  }

  const scoredItems = items.filter(isScoredFeedbackItem);
  const supplementalItems = items.filter((item) => !isScoredFeedbackItem(item));

  return (
    <div className="feedback-layout">
      {scoredItems.length > 0 ? (
        <ul className="criterion-list">
          {scoredItems.map((item, index) => {
            const details = splitFeedbackDetail(item.text);
            const pointsTone = getScoreTone((item.earnedPoints / item.possiblePoints) * 100);

            return (
              <li className={`criterion-card ${item.ok ? 'criterion-pass' : 'criterion-fail'}`} key={`${item.text}-${index}`}>
                <div className="criterion-head">
                  <div>
                    <div className="criterion-title-row">
                      <p className="criterion-label">{details.label}</p>
                      {renderFindingBadge(item.findingType)}
                    </div>
                    <p className="criterion-state">{item.ok ? 'Meeting expectation' : 'Needs improvement'}</p>
                  </div>
                  <span className={`criterion-points ${pointsTone}`}>
                    {formatPoints(item.earnedPoints)}/{formatPoints(item.possiblePoints)}
                  </span>
                </div>
                <p className="criterion-detail">
                  <strong>Finding:</strong> {details.detail}
                </p>
                {!item.ok && details.recommendation ? (
                  <p className="criterion-detail criterion-improve">
                    <strong>Improve:</strong> {details.recommendation}
                  </p>
                ) : null}
                {renderEvidence(item.affectedVisuals ?? [], currentPageName, onRevealVisual)}
              </li>
            );
          })}
        </ul>
      ) : null}

      {supplementalItems.length > 0 ? (
        <div className="feedback-notes">
          <p className="feedback-notes-title">Additional context</p>
          <ul className="feedback-list">
            {supplementalItems.map((item, index) => (
              <li className={`feedback-item ${item.ok ? 'feedback-pass' : 'feedback-fail'}`} key={`${item.text}-${index}`}>
                <span className="feedback-icon">{item.ok ? '✓' : '!'}</span>
                <div className="feedback-copy">
                  <div className="feedback-copy-head">
                    {renderFindingBadge(item.findingType)}
                    <span>{item.text}</span>
                  </div>
                  {renderEvidence(item.affectedVisuals ?? [], currentPageName, onRevealVisual)}
                </div>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </div>
  );
}

function formatMetadataBoolean(value: boolean | undefined, truthy: string, falsy: string): string | undefined {
  if (typeof value !== 'boolean') {
    return undefined;
  }

  return value ? truthy : falsy;
}

function formatMetadataNumber(value: number | undefined): string | undefined {
  if (typeof value !== 'number') {
    return undefined;
  }

  return Number.isInteger(value) ? `${value}` : value.toFixed(1);
}

function buildMetadataTags(item: VisualMetadataItem): string[] {
  const tags = [
    item.isSlicer ? 'Slicer' : undefined,
    item.isNavigationElement ? 'Navigation' : undefined,
    item.isDecorative ? 'Decorative' : undefined,
    formatMetadataBoolean(item.hasLegend, 'Legend', 'No legend'),
    formatMetadataBoolean(item.hasAxisLabels, 'Axis labels', 'No axis labels'),
    formatMetadataBoolean(item.hasDataLabels, 'Data labels', 'No data labels'),
    formatMetadataBoolean(item.hasBorder, 'Border', 'No border'),
    formatMetadataBoolean(item.hasShadow, 'Shadow', 'Flat'),
    item.cornerRadius !== undefined ? `Radius ${formatMetadataNumber(item.cornerRadius)} px` : undefined,
    item.backgroundFillColor ? `Fill ${item.backgroundFillColor}` : undefined,
    item.fontColor ? `Font ${item.fontColor}` : undefined,
  ];

  return tags.filter((tag): tag is string => Boolean(tag));
}

function buildRoleHints(item: VisualMetadataItem): string[] {
  const entries: Array<[string, string[]]> = [
    ['Category', item.categoryHints],
    ['Value', item.valueHints],
    ['Series', item.seriesHints],
    ['Measure', item.measureHints],
  ];

  return entries
    .filter(([, values]) => values.length > 0)
    .map(([label, values]) => `${label}: ${values.join(', ')}`);
}

function formatChartIntent(intent: string | undefined): string | undefined {
  if (!intent) {
    return undefined;
  }

  return intent
    .split(/[-_\s]+/g)
    .filter(Boolean)
    .map((token, index) => {
      const lower = token.toLowerCase();
      return index === 0 ? lower : lower;
    })
    .join(' ');
}

function renderSemanticAssignments(assignments: PageVisualMetadataSummary['semanticColorMap']): React.ReactNode {
  if (assignments.length === 0) {
    return null;
  }

  return (
    <div className="semantic-assignment-list">
      {assignments.map((assignment, index) => (
        <div className="semantic-assignment-card" key={`${assignment.sourcePageName}-${assignment.sourceVisualId}-${assignment.semanticKey}-${index}`}>
          <span aria-hidden="true" className="semantic-color-swatch" style={{ backgroundColor: assignment.color }} />
          <div>
            <p className="semantic-assignment-key">{assignment.displayLabel ?? assignment.semanticKey}</p>
            <p className="semantic-assignment-meta">{assignment.semanticKey} · {assignment.color}</p>
          </div>
        </div>
      ))}
    </div>
  );
}

function renderChartIntentSummary(summary: VisualMetadataItem['chartIntent'] | PageVisualMetadataSummary['chartIntentSummary']): React.ReactNode {
  if (!summary) {
    return null;
  }

  const intent = formatChartIntent(summary.intent);
  return (
    <div className="chart-intent-card">
      <p className="chart-intent-title">
        Page intent: {intent ?? summary.intent}
        {summary.confidence ? <span className="chart-intent-confidence">{summary.confidence} confidence</span> : null}
      </p>
      {summary.fitStatus ? <p className="chart-intent-copy">Fit status: {summary.fitStatus}</p> : null}
      {summary.evidence.length > 0 ? (
        <p className="chart-intent-copy">
          <strong>Evidence:</strong> {summary.evidence.join(' · ')}
        </p>
      ) : null}
      {summary.recommendedAlternatives.length > 0 ? (
        <p className="chart-intent-copy">
          <strong>Alternatives:</strong> {summary.recommendedAlternatives.join(', ')}
        </p>
      ) : null}
    </div>
  );
}

function renderPageStorySummary(summary: ScoreResult['inferredStorySummary'] | PageScore['inferredStorySummary']): React.ReactNode {
  if (!summary) {
    return null;
  }

  return (
    <section className="panel-card chart-intent-card">
      <h2>Inferred Page Story</h2>
      <p className="chart-intent-title">{summary.inferredStory}</p>
      <p className="chart-intent-copy"><strong>Intent profile:</strong> {summary.intentProfile}</p>
      <p className="chart-intent-copy"><strong>Story archetype:</strong> {summary.storyArchetype}</p>
      <p className="chart-intent-copy"><strong>Confidence:</strong> {summary.confidence}</p>
      {summary.evidence.length > 0 ? (
        <p className="chart-intent-copy">
          <strong>Evidence:</strong> {summary.evidence.join(' · ')}
        </p>
      ) : null}
    </section>
  );
}

function formatPageIntentProfile(profile: PageIntentProfileType): string {
  switch (profile) {
    case 'executive':
      return 'Executive';
    case 'operational':
      return 'Operational';
    case 'appendix':
      return 'Appendix';
    default:
      return 'Analytical';
  }
}

function normalizeLegacyIntentProfile(intentProfile: string | undefined): PageIntentProfileType {
  switch (intentProfile) {
    case 'executiveOverview':
      return 'executive';
    case 'operationalMonitoring':
      return 'operational';
    case 'detailReference':
      return 'appendix';
    default:
      return 'analytical';
  }
}

function renderPageIntentProfileSummary(
  summary: PageIntentProfile | undefined,
  selectedProfile: PageIntentProfileType,
  onProfileChange: (next: PageIntentProfileType) => void,
): React.ReactNode {
  if (!summary) {
    return null;
  }

  return (
    <section className="panel-card chart-intent-card">
      <h2>Page Intent Profile</h2>
      <p className="chart-intent-copy"><strong>Inferred profile:</strong> {formatPageIntentProfile(summary.inferredProfile)}</p>
      <p className="chart-intent-copy"><strong>Selected profile:</strong> {formatPageIntentProfile(selectedProfile)}</p>
      <p className="chart-intent-copy"><strong>Actionability expectation:</strong> {summary.actionabilityExpectation}</p>
      <label className="story-note-label" htmlFor="page-intent-override">Manual override</label>
      <select
        aria-label="Page intent profile override"
        className="audit-assign-select"
        id="page-intent-override"
        onChange={(event) => onProfileChange(event.target.value as PageIntentProfileType)}
        value={selectedProfile}
      >
        <option value="executive">Executive</option>
        <option value="operational">Operational</option>
        <option value="analytical">Analytical</option>
        <option value="appendix">Appendix</option>
      </select>
      {summary.reviewGuidance.length > 0 ? (
        <ul className="recommendation-list">
          {summary.reviewGuidance.map((guidance) => (
            <li className="recommendation-item rec-low" key={guidance}>{guidance}</li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

function renderActionabilityBreakdown(summary: ActionabilityBreakdown | undefined): React.ReactNode {
  if (!summary) {
    return null;
  }

  const checks = [
    { label: 'Target / benchmark', ok: summary.targetBenchmarkPresent },
    { label: 'Exception visibility', ok: summary.exceptionVisibility },
    { label: 'Urgency signaling', ok: summary.urgencySignaling },
    { label: 'Prior-period context', ok: summary.priorPeriodContext },
    { label: 'Drill / evidence path', ok: summary.drillPathPresent },
  ];

  return (
    <section className="panel-card consistency-card">
      <h2>Actionability</h2>
      <p className="consistency-summary-copy">{summary.summary}</p>
      <p className="chart-intent-copy"><strong>Actionability score:</strong> {summary.score.toFixed(1)} / 100</p>
      <p className="chart-intent-copy"><strong>Expectation level:</strong> {summary.expectationLevel}</p>
      <div className="consistency-check-grid">
        {checks.map((check) => (
          <div className={`consistency-check ${check.ok ? 'consistency-check-pass' : 'consistency-check-fail'}`} key={check.label}>
            <strong>{check.ok ? 'Present' : 'Missing'}</strong>
            <span>{check.label}</span>
          </div>
        ))}
      </div>
      {summary.gaps.length > 0 ? (
        <ul className="recommendation-list">
          {summary.gaps.map((gap) => (
            <li className="recommendation-item rec-medium" key={gap}>{gap}</li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}

function renderBenchmarkComparison(summary: BenchmarkComparisonSummary | undefined): React.ReactNode {
  if (!summary) {
    return null;
  }

  return (
    <section className="panel-card consistency-card">
      <h2>Benchmark and Archetype</h2>
      <p className="chart-intent-copy"><strong>Archetype:</strong> {summary.archetype}</p>
      <p className="chart-intent-copy"><strong>Benchmark:</strong> {summary.benchmarkLabel}</p>
      <p className="consistency-summary-copy">{summary.insight}</p>
      {summary.beautifulButUseless ? (
        <p className="story-review-note story-review-note-warn">
          Beautiful but useless risk detected: the page polish is outpacing its decision support.
        </p>
      ) : null}
    </section>
  );
}

function renderReviewerCommentGenerator(
  page: PageScore,
  selectedProfile: PageIntentProfileType,
  persona: ReviewerPersona,
  onPersonaChange: (next: ReviewerPersona) => void,
): React.ReactNode {
  const generated = buildReviewerComments(page, { selectedProfile, persona });

  return (
    <section className="panel-card story-review-card">
      <h2>Reviewer Comment Generator</h2>
      <label className="story-note-label" htmlFor="reviewer-persona">Persona</label>
      <select
        aria-label="Reviewer persona"
        className="audit-assign-select"
        id="reviewer-persona"
        onChange={(event) => onPersonaChange(event.target.value as ReviewerPersona)}
        value={persona}
      >
        <option value="coach">Coach</option>
        <option value="consultant">Consultant</option>
        <option value="executiveReviewer">Executive reviewer</option>
        <option value="strictDesignCritic">Strict design critic</option>
      </select>
      <p className="chart-intent-copy"><strong>{generated.headline}</strong></p>
      <ul className="recommendation-list">
        {generated.comments.map((comment) => (
          <li className="recommendation-item rec-low" key={comment}>{comment}</li>
        ))}
      </ul>
    </section>
  );
}

function buildStoryConfirmationKey(
  pageName: string | undefined,
  summary: NonNullable<ScoreResult['inferredStorySummary'] | PageScore['inferredStorySummary']>,
): string {
  const scope = pageName ?? 'report';
  return `${scope}:${summary.intentProfile}:${summary.storyArchetype}`;
}

function buildIntentFeedbackLookup(entries: IntentFeedbackEntry[]): Record<string, IntentFeedbackState> {
  return entries.reduce<Record<string, IntentFeedbackState>>((lookup, entry) => {
    const key = `${entry.pageName}:${entry.inferredIntent}:${entry.storyArchetype ?? 'unknown'}`;
    lookup[key] = {
      confirmation: entry.userConfirmation,
      note: entry.note,
    };
    return lookup;
  }, {});
}

function getReviewStatus(
  confirmation: IntentFeedbackConfirmation | undefined,
): ReviewStatus {
  switch (confirmation) {
    case 'yes':
      return 'confirmed';
    case 'partial':
      return 'partial';
    case 'no':
      return 'mismatch';
    default:
      return 'unreviewed';
  }
}

function getReviewStatusLabel(status: ReviewStatus): string {
  switch (status) {
    case 'confirmed':
      return 'Confirmed';
    case 'partial':
      return 'Partial / Needs clarification';
    case 'mismatch':
      return 'Mismatch / Needs review';
    default:
      return 'Not reviewed';
  }
}

function getReviewStatusClassName(status: ReviewStatus): string {
  switch (status) {
    case 'confirmed':
      return 'review-status-good';
    case 'partial':
      return 'review-status-warn';
    case 'mismatch':
      return 'review-status-bad';
    default:
      return 'review-status-muted';
  }
}

function buildPageReviewEntries(
  pageScores: PageScore[],
  result: ScoreResult,
  lookup: Record<string, IntentFeedbackState>,
): PageReviewEntry[] {
  if (pageScores.length > 0) {
    return pageScores.map((page) => {
      const summary = page.inferredStorySummary;
      const key = summary ? buildStoryConfirmationKey(page.pageName, summary) : undefined;
      return {
        pageName: page.pageName,
        status: getReviewStatus(key ? lookup[key]?.confirmation : undefined),
        summary,
      };
    });
  }

  if (result.inferredStorySummary || result.scoredPageName) {
    const pageName = result.scoredPageName ?? 'Report';
    const summary = result.inferredStorySummary;
    const key = summary ? buildStoryConfirmationKey(pageName, summary) : undefined;
    return [
      {
        pageName,
        status: getReviewStatus(key ? lookup[key]?.confirmation : undefined),
        summary,
      },
    ];
  }

  return [];
}

function renderReviewSummary(props: {
  entries: PageReviewEntry[];
  activeFilter: ReviewStatus | 'all';
  onFilterChange: (next: ReviewStatus | 'all') => void;
  onSelectPage: (pageName: string) => void;
  onExport: () => void;
}): React.ReactNode {
  const { entries, activeFilter, onFilterChange, onSelectPage, onExport } = props;
  if (entries.length === 0) {
    return null;
  }

  const counts = {
    confirmed: entries.filter((entry) => entry.status === 'confirmed').length,
    partial: entries.filter((entry) => entry.status === 'partial').length,
    mismatch: entries.filter((entry) => entry.status === 'mismatch').length,
    unreviewed: entries.filter((entry) => entry.status === 'unreviewed').length,
  };
  const reviewedCount = counts.confirmed + counts.partial + counts.mismatch;
  const filters: Array<{ value: ReviewStatus | 'all'; label: string; count: number }> = [
    { value: 'all', label: 'All statuses', count: entries.length },
    { value: 'confirmed', label: 'Confirmed', count: counts.confirmed },
    { value: 'partial', label: 'Partial / Needs clarification', count: counts.partial },
    { value: 'mismatch', label: 'Mismatch / Needs review', count: counts.mismatch },
    { value: 'unreviewed', label: 'Not reviewed', count: counts.unreviewed },
  ];
  const groups: ReviewStatus[] = activeFilter === 'all'
    ? ['confirmed', 'partial', 'mismatch', 'unreviewed']
    : [activeFilter];

  return (
    <section className="panel-card review-summary-card">
      <div className="review-summary-head">
        <div>
          <h2>Review Summary</h2>
          <p className="review-summary-copy">
            Track which pages have confirmed intent, which remain ambiguous, and which need review before sharing the report more broadly.
          </p>
          <button
            className="secondary-button review-summary-export"
            onClick={onExport}
            type="button"
          >
            Export Review Summary
          </button>
        </div>
        <div className="review-summary-stats">
          <div className="review-stat-card">
            <strong>{entries.length}</strong>
            <span>Total pages</span>
          </div>
          <div className="review-stat-card">
            <strong>{reviewedCount}</strong>
            <span>Pages reviewed</span>
          </div>
          <div className="review-stat-card">
            <strong>{counts.confirmed}</strong>
            <span>Confirmed</span>
          </div>
          <div className="review-stat-card">
            <strong>{counts.partial}</strong>
            <span>Partial</span>
          </div>
          <div className="review-stat-card">
            <strong>{counts.mismatch}</strong>
            <span>Mismatch</span>
          </div>
          <div className="review-stat-card">
            <strong>{counts.unreviewed}</strong>
            <span>Unreviewed</span>
          </div>
        </div>
      </div>
      <div aria-label="Review status filters" className="review-filter-row" role="group">
        {filters.map((filter) => (
          <button
            className={`review-filter-chip ${activeFilter === filter.value ? 'review-filter-chip-active' : ''}`}
            key={filter.value}
            onClick={() => onFilterChange(filter.value)}
            type="button"
          >
            {filter.label} ({filter.count})
          </button>
        ))}
      </div>
      <div className="review-group-list">
        {groups.map((status) => {
          const matchingEntries = entries.filter((entry) => entry.status === status);
          if (matchingEntries.length === 0) {
            return null;
          }

          return (
            <section className="review-group-card" key={status}>
              <div className="review-group-head">
                <h3>{getReviewStatusLabel(status)}</h3>
                <span className={`review-status-pill ${getReviewStatusClassName(status)}`}>
                  {matchingEntries.length}
                </span>
              </div>
              {status === 'mismatch' ? (
                <p className="review-group-copy">
                  Tighten the title, lead KPI band, or supporting visuals before treating these pages as review-ready.
                </p>
              ) : null}
              <div className="review-page-list">
                {matchingEntries.map((entry) => (
                  <button
                    aria-label={`Review page ${entry.pageName}`}
                    className="review-page-button"
                    key={`${status}-${entry.pageName}`}
                    onClick={() => onSelectPage(entry.pageName)}
                    type="button"
                  >
                    <span className="review-page-name">{entry.pageName}</span>
                    <span className="review-page-meta">
                      {entry.summary?.intentProfile ?? 'No inferred story'}{entry.summary?.storyArchetype ? ` · ${entry.summary.storyArchetype}` : ''}
                    </span>
                  </button>
                ))}
              </div>
            </section>
          );
        })}
      </div>
    </section>
  );
}

function renderReviewPacketPreview(
  preview: ScorePanelState['reviewPacketPreview'],
  previewHtml: ScorePanelState['reviewPacketPreviewHtml'],
  previewProfile: NonNullable<ScorePanelState['reviewPacketPreviewProfile']>,
  previewTemplateVariant: NonNullable<ScorePanelState['reviewPacketPreviewTemplateVariant']>,
  onProfileChange: (profile: NonNullable<ScorePanelState['reviewPacketPreviewProfile']>) => void,
  onTemplateVariantChange: (variant: NonNullable<ScorePanelState['reviewPacketPreviewTemplateVariant']>) => void,
  onOpenFullPacket: () => void,
): React.ReactNode {
  if (!preview) {
    return null;
  }

  return (
    <section className="panel-card review-packet-card">
      <div className="review-packet-head">
        <div>
          <h2>Review Packet Preview</h2>
          <p className="review-summary-copy">
            This is the same downstream review packet structure currently used for export, shown here so you can validate it before sharing.
          </p>
        </div>
        <div className={`review-packet-status review-packet-status-${preview.executiveSummary.overallStatus.toLowerCase().replace(/\s+/g, '-')}`}>
          <strong>{preview.executiveSummary.overallStatus}</strong>
          <span>Review coverage: {preview.executiveSummary.reviewCoveragePercent}%</span>
        </div>
      </div>

      <div className="review-packet-grid">
        <article className="review-packet-section">
          <h3>Executive Summary</h3>
          <p>{preview.executiveSummary.headline}</p>
          <p className="review-packet-meta">
            Composite score {preview.compositeScore} / 100 across {preview.pageCount} page(s).
          </p>
        </article>

        <article className="review-packet-section">
          <h3>Intent Validation Summary</h3>
          <ul className="review-packet-list">
            <li>Confirmed: {preview.intentValidationSummary.confirmedPages.length}</li>
            <li>Partial: {preview.intentValidationSummary.partialPages.length}</li>
            <li>Mismatch: {preview.intentValidationSummary.mismatchPages.length}</li>
            <li>Unreviewed: {preview.intentValidationSummary.unreviewedPages.length}</li>
          </ul>
        </article>
      </div>

      {previewHtml ? (
        <div className="review-packet-preview-shell">
          <div className="review-packet-preview-toolbar">
            <div className="review-packet-preview-toolbar-main">
              <span>
                {previewProfile === 'consultant' && previewTemplateVariant === 'brandedConsultant'
                  ? 'Branded consultant packet preview'
                  : `${previewProfile.charAt(0).toUpperCase()}${previewProfile.slice(1)} packet preview`}
              </span>
              <span className="review-packet-meta">Read-only HTML renderer</span>
            </div>
            <div className="review-packet-preview-controls">
              <label className="review-packet-control">
                <span>Preview profile</span>
                <select
                  aria-label="Preview profile"
                  onChange={(event) => onProfileChange(event.target.value as NonNullable<ScorePanelState['reviewPacketPreviewProfile']>)}
                  value={previewProfile}
                >
                  <option value="consultant">Consultant</option>
                  <option value="executive">Executive</option>
                  <option value="governance">Governance</option>
                </select>
              </label>
              {previewProfile === 'consultant' ? (
                <label className="review-packet-control">
                  <span>Consultant template</span>
                  <select
                    aria-label="Consultant template"
                    onChange={(event) => onTemplateVariantChange(event.target.value as NonNullable<ScorePanelState['reviewPacketPreviewTemplateVariant']>)}
                    value={previewTemplateVariant}
                  >
                    <option value="standard">Standard</option>
                    <option value="brandedConsultant">Branded consultant</option>
                  </select>
                </label>
              ) : null}
              <button className="secondary-button" onClick={onOpenFullPacket} type="button">
                Open Full Packet
              </button>
            </div>
          </div>
          <iframe
            className="review-packet-preview-frame"
            sandbox="allow-same-origin"
            srcDoc={previewHtml}
            title="Review packet HTML preview"
          />
        </div>
      ) : (
        <>
          {preview.remediationQueue.length > 0 ? (
            <article className="review-packet-section">
              <h3>Remediation Queue</h3>
              <ul className="review-packet-list">
                {preview.remediationQueue.map((item) => (
                  <li key={`${item.pageName}-${item.reviewStatus}`}>
                    <strong>{item.pageName}</strong>: {item.reason}
                  </li>
                ))}
              </ul>
            </article>
          ) : null}

          {preview.topRecommendations.length > 0 ? (
            <article className="review-packet-section">
              <h3>Top Recommendations</h3>
              <ul className="review-packet-list">
                {preview.topRecommendations.map((recommendation) => (
                  <li key={recommendation}>{recommendation}</li>
                ))}
              </ul>
            </article>
          ) : null}

          {preview.crossPageConsistencyRollup ? (
            <article className="review-packet-section">
              <h3>Cross-Page Consistency Rollup</h3>
              {preview.crossPageConsistencyRollup.overallFinding ? (
                <p>{preview.crossPageConsistencyRollup.overallFinding}</p>
              ) : null}
              <ul className="review-packet-list">
                {preview.crossPageConsistencyRollup.issuesByCategory.map(([category, count]) => (
                  <li key={category}>{category}: {count}</li>
                ))}
              </ul>
            </article>
          ) : null}
        </>
      )}
    </section>
  );
}

function renderStoryIntentReview(
  summary: NonNullable<ScoreResult['inferredStorySummary'] | PageScore['inferredStorySummary']>,
  confirmation: IntentFeedbackConfirmation | undefined,
  note: string,
  onConfirm: (next: IntentFeedbackConfirmation) => void,
  onNoteChange: (next: string) => void,
  onSaveNote: () => void,
  noteSaved: boolean,
): React.ReactNode {
  const options: Array<{ value: IntentFeedbackConfirmation; label: string }> = [
    { value: 'yes', label: 'Yes' },
    { value: 'partial', label: 'Partially' },
    { value: 'no', label: 'No' },
  ];
  const status = getReviewStatus(confirmation);

  return (
    <div className="story-review-block">
      <h2>Intent Feedback</h2>
      <p className="story-review-status">
        <strong>Review status:</strong>{' '}
        <span className={`review-status-pill ${getReviewStatusClassName(status)}`}>
          {getReviewStatusLabel(status)}
        </span>
      </p>
      <p className="story-review-title">Does this match your intent?</p>
      <div aria-label="Intent confirmation" className="story-review-actions" role="group">
        {options.map((option) => (
          <button
            className={`story-review-button ${confirmation === option.value ? 'story-review-button-active' : ''}`}
            key={option.value}
            onClick={() => onConfirm(option.value)}
            type="button"
          >
            {option.label}
          </button>
        ))}
      </div>
      {confirmation === 'yes' ? (
        <p className="story-review-note story-review-note-good">
          Confirmed by you during this session.
        </p>
      ) : null}
      {confirmation === undefined ? (
        <p className="story-review-note story-review-note-neutral">
          Not reviewed yet. Confirm the inferred story before using it as review evidence.
        </p>
      ) : null}
      {confirmation === 'partial' ? (
        <p className="story-review-note story-review-note-warn">
          Partially aligned with your intent. The page reads as {summary.intentProfile} with a {summary.storyArchetype} structure.
        </p>
      ) : null}
      {confirmation === 'no' ? (
        <div className="story-review-note story-review-note-bad">
          <p><strong>Intent mismatch detected.</strong></p>
          <p>The page currently reads as {summary.intentProfile} with a {summary.storyArchetype} structure.</p>
          <p>Consider tightening the title, lead KPI band, or supporting visuals so the page communicates the intended story more clearly.</p>
        </div>
      ) : null}
      <div className="story-note-block">
        <label className="story-note-label" htmlFor="story-review-note">
          Reviewer note
        </label>
        <textarea
          aria-label="Reviewer note"
          className="story-note-input"
          disabled={!confirmation}
          id="story-review-note"
          onChange={(event) => onNoteChange(event.target.value)}
          placeholder={confirmation
            ? 'Optional note for this review state.'
            : 'Choose Yes, Partially, or No before saving a reviewer note.'}
          rows={3}
          value={note}
        />
        <div className="story-note-actions">
          <button
            className="secondary-button"
            disabled={!confirmation}
            onClick={onSaveNote}
            type="button"
          >
            Save Note
          </button>
          {!confirmation ? (
            <p className="story-note-hint">
              Select a review status first so the note is attached to a specific review outcome.
            </p>
          ) : noteSaved ? (
            <p className="story-note-hint story-note-hint-saved">
              Reviewer note saved for this page review.
            </p>
          ) : (
            <p className="story-note-hint">
              Notes persist with the current review status and stay out of scoring.
            </p>
          )}
        </div>
      </div>
    </div>
  );
}

function formatConsistencyCategory(category: string): string {
  switch (category) {
    case 'metricGovernance':
      return 'Metric Governance';
    case 'semanticColors':
      return 'Semantic Colors';
    default:
      return category
        .replace(/([a-z])([A-Z])/g, '$1 $2')
        .replace(/[-_]/g, ' ')
        .replace(/\b\w/g, (match) => match.toUpperCase());
  }
}

function renderReportConsistencySummary(summary: ScoreResult['reportConsistencySummary']): React.ReactNode {
  if (!summary) {
    return null;
  }

  const checks = [
    { label: 'Title anchors', ok: summary.consistentTitleAnchors },
    { label: 'Filter band', ok: summary.consistentFilterBand },
    { label: 'Metric labels', ok: summary.consistentMetricLabels },
    { label: 'Semantic colors', ok: summary.consistentSemanticColors },
  ];

  return (
    <section className="panel-card consistency-card">
      <h2>Cross-Page Consistency</h2>
      {summary.overallFinding ? <p className="consistency-summary-copy">{summary.overallFinding}</p> : null}
      <div className="consistency-check-grid">
        {checks.map((check) => (
          <div className={`consistency-check ${check.ok ? 'consistency-check-pass' : 'consistency-check-fail'}`} key={check.label}>
            <strong>{check.ok ? 'Consistent' : 'Needs review'}</strong>
            <span>{check.label}</span>
          </div>
        ))}
      </div>
      {summary.issues.length > 0 ? (
        <div className="consistency-issue-groups">
          {summary.issues.map((issue, index) => (
            <article className={`consistency-issue-card severity-${issue.severity}`} key={`${issue.issueCategory}-${index}`}>
              <div className="consistency-issue-header">
                <h3>{formatConsistencyCategory(issue.category)}</h3>
                <span className={`consistency-severity-chip severity-${issue.severity}`}>{issue.severity}</span>
              </div>
              <p className="consistency-issue-copy">{issue.overallFinding}</p>
              {issue.affectedPages.length > 0 ? (
                <p className="consistency-issue-meta">Affected pages: {issue.affectedPages.join(', ')}</p>
              ) : null}
              <p className="consistency-issue-meta">Confidence: {issue.confidence}</p>
              <p className="consistency-issue-remediation">{issue.recommendedRemediation}</p>
            </article>
          ))}
        </div>
      ) : summary.findings.length > 0 ? (
        <ul className="recommendation-list consistency-finding-list">
          {summary.findings.map((finding, index) => (
            <li className="recommendation-item rec-medium" key={`${finding}-${index}`}>
              {finding}
            </li>
          ))}
        </ul>
      ) : (
        <p className="empty-text">No cross-page consistency issues detected.</p>
      )}
    </section>
  );
}

function renderPageConsistencyNotes(notes: string[] | undefined): React.ReactNode {
  if (!notes || notes.length === 0) {
    return null;
  }

  return (
    <section className="panel-card consistency-card">
      <h2>Page Consistency Notes</h2>
      <ul className="recommendation-list consistency-finding-list">
        {notes.map((note, index) => (
          <li className="recommendation-item rec-low" key={`${note}-${index}`}>
            {note}
          </li>
        ))}
      </ul>
    </section>
  );
}

function renderMetadataOverview(pageScores: PageScore[]): React.ReactNode {
  const pagesWithMetadata = pageScores.filter((page) => page.visualMetadata);
  if (pagesWithMetadata.length === 0) {
    return null;
  }

  return (
    <details className="panel-card collapsible-panel">
      <summary className="collapsible-summary">
        <span>Parsed Visual Metadata</span>
        <span className="collapsible-caret" aria-hidden="true">▾</span>
      </summary>
      <div className="collapsible-body">
        <p className="empty-text">
          Page-level parser coverage snapshot across the report. Open a page tab for per-visual detail.
        </p>
        <div className="metadata-overview-grid">
          {pagesWithMetadata.map((page) => {
            const summary = page.visualMetadata!;
            return (
              <article className="metadata-overview-card" key={page.pageName}>
                <div className="metadata-overview-head">
                  <div>
                    <p className="metadata-overview-page">{summary.pageName}</p>
                    <p className="metadata-overview-title">
                      {summary.visiblePageTitle ?? 'No visible page title detected'}
                    </p>
                  </div>
                  <strong>{summary.visualCount}</strong>
                </div>
                <p className="metadata-overview-copy">
                  {summary.visibleTitleVisualCount} title-bearing visual(s), {summary.legendVisualCount} with legends,
                  {' '}
                  {summary.axisLabelVisualCount} with axis labels, {summary.dataLabelVisualCount} with data labels.
                </p>
                {summary.chartIntentSummary ? (
                  <p className="metadata-overview-copy">
                    <strong>Page intent:</strong> {formatChartIntent(summary.chartIntentSummary.intent)}
                  </p>
                ) : null}
              </article>
            );
          })}
        </div>
      </div>
    </details>
  );
}

function renderVisualMetadataDetail(
  summary: PageVisualMetadataSummary,
  onRevealVisual: (visual: AffectedVisualReference) => void,
): React.ReactNode {
  return (
    <details className="panel-card collapsible-panel">
      <summary className="collapsible-summary">
        <span>Parsed Visual Metadata</span>
        <span className="collapsible-caret" aria-hidden="true">▾</span>
      </summary>
      <div className="collapsible-body">
        <p className="empty-text">
          {summary.visiblePageTitle
            ? `Visible page title: ${summary.visiblePageTitle}.`
            : 'No visible page title was detected on this page.'}
          {' '}
          {summary.canvasWidth && summary.canvasHeight
            ? `Canvas ${Math.round(summary.canvasWidth)} × ${Math.round(summary.canvasHeight)}.`
            : 'Canvas size not exposed by PBIR.'}
        </p>
        <div className="metadata-stat-grid">
          <div className="metadata-stat-card">
            <strong>{summary.visualCount}</strong>
            <span>Total visuals</span>
          </div>
          <div className="metadata-stat-card">
            <strong>{summary.visibleTitleVisualCount}</strong>
            <span>Title-bearing</span>
          </div>
          <div className="metadata-stat-card">
            <strong>{summary.slicerCount}</strong>
            <span>Slicers</span>
          </div>
          <div className="metadata-stat-card">
            <strong>{summary.legendVisualCount}</strong>
            <span>Legends</span>
          </div>
          <div className="metadata-stat-card">
            <strong>{summary.axisLabelVisualCount}</strong>
            <span>Axis labels</span>
          </div>
          <div className="metadata-stat-card">
            <strong>{summary.formattedVisualCount}</strong>
            <span>Formatting facts</span>
          </div>
        </div>
        {renderChartIntentSummary(summary.chartIntentSummary)}
        {summary.semanticColorMap.length > 0 ? (
          <div className="metadata-section">
            <p className="metadata-section-title">Semantic colors</p>
            {renderSemanticAssignments(summary.semanticColorMap)}
          </div>
        ) : null}
        {summary.visuals.length > 0 ? (
          <ul className="metadata-visual-list">
            {summary.visuals.map((item) => {
              const tags = buildMetadataTags(item);
              const roleHints = buildRoleHints(item);
              const visibleText = item.bestVisibleText ?? item.visibleTitleText ?? item.textBoxText ?? item.visibleSubtitleText;

              return (
                <li className="metadata-visual-card" key={`${summary.pageName}-${item.visualId}`}>
                  <div className="metadata-visual-head">
                    <div>
                      <p className="metadata-visual-title">
                        {visibleText ?? `${item.visualType} ${shortenVisualId(item.visualId)}`}
                      </p>
                      <p className="metadata-visual-meta">
                        {item.visualType} · {item.visualId} · {Math.round(item.width)} × {Math.round(item.height)} at {Math.round(item.x)},{' '}
                        {Math.round(item.y)}
                      </p>
                    </div>
                    <button
                      className="secondary-button metadata-reveal-button"
                      onClick={() => onRevealVisual({
                        pageName: summary.pageName,
                        visualId: item.visualId,
                        visualType: item.visualType,
                      })}
                      type="button"
                    >
                      Reveal
                    </button>
                  </div>
                  {item.visibleSubtitleText && item.visibleSubtitleText !== visibleText ? (
                    <p className="metadata-visual-copy">
                      <strong>Subtitle:</strong> {item.visibleSubtitleText}
                    </p>
                  ) : null}
                  {item.textBoxText && item.textBoxText !== visibleText ? (
                    <p className="metadata-visual-copy">
                      <strong>Text:</strong> {item.textBoxText}
                    </p>
                  ) : null}
                  {roleHints.length > 0 ? (
                    <p className="metadata-visual-copy">
                      <strong>Role hints:</strong> {roleHints.join(' · ')}
                    </p>
                  ) : null}
                  {item.chartIntent ? (
                    <p className="metadata-visual-copy">
                      <strong>Chart intent:</strong> {formatChartIntent(item.chartIntent.intent)}
                      {item.chartIntent.fitStatus ? ` · ${item.chartIntent.fitStatus}` : ''}
                    </p>
                  ) : null}
                  {item.semanticColors.length > 0 ? (
                    <div className="metadata-section">
                      <p className="metadata-section-title">Semantic colors</p>
                      {renderSemanticAssignments(item.semanticColors)}
                    </div>
                  ) : null}
                  {tags.length > 0 ? (
                    <div className="metadata-tag-row">
                      {tags.map((tag) => (
                        <span className="metadata-tag" key={`${item.visualId}-${tag}`}>
                          {tag}
                        </span>
                      ))}
                    </div>
                  ) : null}
                </li>
              );
            })}
          </ul>
        ) : (
          <p className="empty-text">No per-visual metadata was exposed for this page.</p>
        )}
      </div>
    </details>
  );
}

function FrameworkSection(props: {
  feedbackForKey: (normalizedKey: string) => FrameworkFeedbackItem[] | undefined;
  frameworkValues: Array<{ key: string; label: string; score: number; weightLabel: string }>;
  currentPageName: string | undefined;
  onRevealVisual: (visual: AffectedVisualReference) => void;
}): JSX.Element {
  return (
    <section className="framework-section">
      {props.frameworkValues.map((framework) => {
        const feedbackItems = props.feedbackForKey(framework.key);
        const breakdown = buildScoreBreakdown(feedbackItems);

        return (
          <details className="framework-card" key={framework.key}>
            <summary className="framework-summary">
              <div className="framework-heading">
                <div className="framework-title-group">
                  <span className="framework-title">{framework.label}</span>
                  <span className="framework-caret" aria-hidden="true">▾</span>
                </div>
                <span className="framework-weight">{framework.weightLabel}</span>
              </div>
              {breakdown ? <p className="framework-breakdown">{breakdown}</p> : null}
              <div className="framework-meter">
                <div className="framework-bar">
                  <span
                    className={`framework-bar-fill ${getScoreTone(framework.score)}`}
                    style={{ width: `${Math.round(framework.score)}%` }}
                  />
                </div>
                <strong className="framework-score">{Math.round(framework.score)}</strong>
              </div>
            </summary>
            <div className="framework-body">
              {renderFeedback(
                feedbackItems,
                props.currentPageName,
                props.onRevealVisual,
              )}
            </div>
          </details>
        );
      })}
    </section>
  );
}

function getSeverityClass(severity: AuditFindingDisplay['severity']): string {
  if (severity === 'critical') return 'audit-finding-critical';
  if (severity === 'info') return 'audit-finding-info';
  return 'audit-finding-warning';
}

function getConfidenceLabel(confidence: AuditFindingDisplay['confidence']): string {
  if (confidence === 'high') return 'High confidence';
  if (confidence === 'low') return 'Low confidence';
  return 'Medium confidence';
}

function getIssueSourceLabel(issueSource: AuditFindingDisplay['issueSource']): string {
  return issueSource === 'metadataModel' ? 'Metadata / model' : 'Rendered / layout';
}

function renderAuditCoverageCard(
  audit: AuditState,
  pageNames: string[],
  vscode: ScoreVsCodeApi,
): React.ReactNode {
  const { coverage, unmatchedCaptures, providerConfigured, providerName } = audit;
  const missingPages = coverage.totalPages - coverage.pagesWithCaptures;

  return (
    <section className="panel-card audit-coverage-card">
      <div className="audit-coverage-head">
        <h2>Visual Audit Coverage</h2>
        <div className="audit-coverage-actions">
          <button
            className="secondary-button"
            onClick={() => vscode.postMessage({ type: 'uploadScreenshots' })}
            type="button"
          >
            Upload Screenshots
          </button>
          <button
            className="secondary-button"
            onClick={() => vscode.postMessage({ type: 'openSettings' })}
            title="Open Design Analyzer Configuration to set up an AI provider"
            type="button"
          >
            Configure AI Provider
          </button>
        </div>
      </div>

      <div className="audit-coverage-stats">
        <div className="audit-stat">
          <strong>{coverage.pagesWithCaptures}</strong>
          <span>of {coverage.totalPages} pages covered</span>
        </div>
        <div className="audit-stat">
          <strong>{coverage.pagesWithFindings}</strong>
          <span>pages with findings</span>
        </div>
        {coverage.unmatchedCaptures > 0 ? (
          <div className="audit-stat audit-stat-warn">
            <strong>{coverage.unmatchedCaptures}</strong>
            <span>unmatched screenshots</span>
          </div>
        ) : null}
        {missingPages > 0 ? (
          <div className="audit-stat audit-stat-missing">
            <strong>{missingPages}</strong>
            <span>pages without screenshots</span>
          </div>
        ) : null}
      </div>

      {unmatchedCaptures.length > 0 ? (
        <div className="audit-unmatched">
          <p className="audit-unmatched-label">Unmatched screenshots — assign to a page:</p>
          <ul className="audit-unmatched-list">
            {unmatchedCaptures.map((capture) => (
              <li className="audit-unmatched-item" key={capture.captureId}>
                <span className="audit-capture-filename">{capture.fileName}</span>
                <select
                  className="audit-assign-select"
                  defaultValue=""
                  onChange={(e) => {
                    if (e.target.value) {
                      vscode.postMessage({
                        type: 'assignCapture',
                        captureId: capture.captureId,
                        targetPageName: e.target.value,
                      });
                    }
                  }}
                >
                  <option value="" disabled>Assign to page…</option>
                  {pageNames.map((name) => (
                    <option key={name} value={name}>{name}</option>
                  ))}
                </select>
                <button
                  className="audit-remove-button"
                  onClick={() => vscode.postMessage({ type: 'removeScreenshot', captureId: capture.captureId })}
                  title="Remove screenshot"
                  type="button"
                >
                  ×
                </button>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {!providerConfigured ? (
        <p className="audit-provider-note">
          No AI provider configured. Click "Configure AI Provider" to open settings and add your key.
        </p>
      ) : (
        <p className="audit-provider-note">
          AI provider: {providerName}. Open a page tab to analyze individual screenshots. Use "Configure AI Provider" to change the provider or rotate your key.
        </p>
      )}
    </section>
  );
}

function renderAuditPageSection(
  pageName: string,
  auditPageState: AuditPageState | undefined,
  analyzingCaptureId: string | undefined,
  providerConfigured: boolean,
  vscode: ScoreVsCodeApi,
): React.ReactNode {
  return (
    <section className="panel-card audit-page-section">
      <div className="audit-page-head">
        <h2>Visual Audit — {pageName}</h2>
        <button
          className="secondary-button"
          onClick={() => vscode.postMessage({ type: 'attachScreenshot', pageName })}
          type="button"
        >
          {auditPageState?.captures.length ? 'Replace / Add Screenshot' : 'Attach Screenshot'}
        </button>
      </div>

      {!auditPageState || auditPageState.captures.length === 0 ? (
        <p className="empty-text">No screenshots attached to this page.</p>
      ) : (
        <>
          <div className="audit-captures">
            {auditPageState.captures.map((capture) => {
              const isAnalyzing = capture.captureId === analyzingCaptureId;
              return (
                <div className="audit-capture-card" key={capture.captureId}>
                  <div className="audit-capture-head">
                    <span className="audit-capture-filename">{capture.fileName}</span>
                    {capture.stateName ? (
                      <span className="audit-capture-state">{capture.stateName}</span>
                    ) : null}
                    <div className="audit-capture-actions">
                      {providerConfigured ? (
                        <button
                          className="primary-button audit-analyze-button"
                          disabled={isAnalyzing}
                          onClick={() => vscode.postMessage({ type: 'analyzeCapture', captureId: capture.captureId, pageName })}
                          type="button"
                        >
                          {isAnalyzing ? 'Analyzing…' : 'Analyze'}
                        </button>
                      ) : null}
                      <button
                        className="audit-remove-button"
                        onClick={() => vscode.postMessage({ type: 'removeScreenshot', captureId: capture.captureId })}
                        title="Remove screenshot"
                        type="button"
                      >
                        ×
                      </button>
                    </div>
                  </div>
                  <p className="audit-capture-meta">{capture.findingCount} finding(s)</p>
                </div>
              );
            })}
          </div>

          {auditPageState.findings.length > 0 ? (
            <ul className="audit-findings-list">
              {auditPageState.findings.map((finding) => (
                <li className={`audit-finding-item ${getSeverityClass(finding.severity)}`} key={finding.findingId}>
                  <div className="audit-finding-head">
                    <span className="audit-finding-type">{finding.findingType}</span>
                    <span className="audit-finding-confidence">{getConfidenceLabel(finding.confidence)}</span>
                  </div>
                  <p className="audit-finding-region">
                    <strong>Issue source:</strong> {getIssueSourceLabel(finding.issueSource)}
                  </p>
                  <p className="audit-finding-text">{finding.text}</p>
                  {finding.recommendation ? (
                    <p className="audit-finding-rec">
                      <strong>Fix:</strong> {finding.recommendation}
                    </p>
                  ) : null}
                  {finding.regionHint ? (
                    <p className="audit-finding-region">
                      <strong>Region:</strong> {finding.regionHint}
                    </p>
                  ) : null}
                </li>
              ))}
            </ul>
          ) : (
            <p className="empty-text">
              {auditPageState.captures.length > 0 && providerConfigured
                ? 'No findings yet. Click "Analyze" to run AI-assisted review.'
                : 'Attach a screenshot and configure the AI provider to run analysis.'}
            </p>
          )}
        </>
      )}
    </section>
  );
}

export default function App(): JSX.Element {
  const [viewState, setViewState] = React.useState<ViewState>({ kind: 'loading' });
  const [activeTab, setActiveTab] = React.useState(0);
  const [analyzingCaptureId, setAnalyzingCaptureId] = React.useState<string | undefined>(undefined);
  const [storyIntentFeedback, setStoryIntentFeedback] = React.useState<Record<string, IntentFeedbackState>>({});
  const [savedStoryNoteKey, setSavedStoryNoteKey] = React.useState<string | undefined>(undefined);
  const [intentProfileOverrides, setIntentProfileOverrides] = React.useState<Record<string, PageIntentProfileType>>({});
  const [reviewerPersonaByPage, setReviewerPersonaByPage] = React.useState<Record<string, ReviewerPersona>>({});
  const [workspacePersona, setWorkspacePersona] = React.useState<ReviewPresentationPersona>('default');
  const [pagePurposeExpanded, setPagePurposeExpanded] = React.useState(false);
  const [reviewStatusFilter, setReviewStatusFilter] = React.useState<ReviewStatus | 'all'>('all');
  const [issueFilters, setIssueFilters] = React.useState<IssueFilterState>({
    severity: 'all',
    pageName: 'all',
    dimension: 'all',
    impactArea: 'all',
    scope: 'all',
    detectionType: 'all',
  });
  const [issueFiltersDirty, setIssueFiltersDirty] = React.useState(false);
  const [issueGroupingMode, setIssueGroupingMode] = React.useState<IssueGroupingMode>('severity');
  const [expandedOpportunityIds, setExpandedOpportunityIds] = React.useState<string[]>([]);
  const vscodeApiRef = React.useRef<ScoreVsCodeApi | null>(null);
  const issuesSectionRef = React.useRef<HTMLElement | null>(null);
  const fixPlanSectionRef = React.useRef<HTMLDivElement | null>(null);

  if (!vscodeApiRef.current) {
    vscodeApiRef.current = acquireVsCodeApi();
  }

  React.useEffect(() => {
    const handleMessage = (event: MessageEvent<ScorePanelHostToWebviewMessage>) => {
      const message = event.data;
      if (!message || typeof message !== 'object' || !('type' in message)) {
        return;
      }

      if (message.type === 'loading') {
        setViewState({ kind: 'loading' });
        setReviewStatusFilter('all');
        setStoryIntentFeedback({});
        setSavedStoryNoteKey(undefined);
        setIntentProfileOverrides({});
        setReviewerPersonaByPage({});
        setWorkspacePersona('default');
        setIssueFilters({
          severity: 'all',
          pageName: 'all',
          dimension: 'all',
          impactArea: 'all',
          scope: 'all',
          detectionType: 'all',
        });
        setIssueFiltersDirty(false);
        setIssueGroupingMode('severity');
        setExpandedOpportunityIds([]);
        return;
      }

      if (message.type === 'error') {
        setViewState({ kind: 'error', message: message.message });
        return;
      }

      if (message.type === 'scoreState') {
        setViewState({ kind: 'ready', state: message.state });
        setActiveTab(message.state.selectedPageIndex);
        setReviewStatusFilter('all');
        setStoryIntentFeedback(buildIntentFeedbackLookup(message.state.intentFeedback ?? []));
        setSavedStoryNoteKey(undefined);
        const availableProfiles = message.state.result.personaPresentation?.availablePersonas ?? getReviewPresentationPersonaProfiles();
        const nextPersona = message.state.result.personaPresentation?.activePersona ?? 'default';
        setWorkspacePersona(nextPersona);
        setPagePurposeExpanded(false);
        setIssueFilters(buildPersonaDefaultFilters(availableProfiles.find((profile) => profile.id === nextPersona)));
        setIssueFiltersDirty(false);
        return;
      }

      if (message.type === 'auditState') {
        setViewState((prev) =>
          prev.kind === 'ready' ? { ...prev, audit: message.audit } : prev,
        );
        setAnalyzingCaptureId(undefined);
        return;
      }

      if (message.type === 'auditAnalyzing') {
        setAnalyzingCaptureId(message.captureId);
        return;
      }
    };

    window.addEventListener('message', handleMessage);
    vscodeApiRef.current?.postMessage({ type: 'webviewReady' });

    return () => {
      window.removeEventListener('message', handleMessage);
    };
  }, []);

  if (viewState.kind === 'loading') {
    return (
      <main className="page-shell loading-shell">
        <div className="loading-card">
          <div className="spinner" />
          <p>Analysing PBIR report…</p>
        </div>
      </main>
    );
  }

  if (viewState.kind === 'error') {
    return (
      <main className="page-shell">
        <section className="error-card">
          <h1>Scoring failed</h1>
          <p>{viewState.message}</p>
          <button
            className="primary-button"
            onClick={() => vscodeApiRef.current?.postMessage({ type: 'refresh' })}
            type="button"
          >
            Retry
          </button>
        </section>
      </main>
    );
  }

  const { state, audit } = viewState;
  const { config, result } = state;
  const pageScores = result.pageScores ?? [];
  const multiPage = pageScores.length > 1;
  const tabs = multiPage ? ['Overall', ...pageScores.map((page) => page.pageName)] : [];
  const selectedPage = multiPage && activeTab > 0 ? pageScores[activeTab - 1] : undefined;
  const overallView = !selectedPage;
  const frameworkWeights = selectedPage?.frameworkWeights ?? result.frameworkWeights ?? {};
  const enabledFrameworks = getEnabledFrameworks(config, frameworkWeights);
  const frameworkValues = enabledFrameworks.map((framework) => ({
    key: framework.normalizedKey,
    label: framework.label,
    score: selectedPage
      ? getPageScore(selectedPage, framework.normalizedKey)
      : multiPage
        ? averageFrameworkScore(pageScores, framework.normalizedKey)
        : getResultScore(result, framework.normalizedKey),
    weightLabel: framework.weightLabel,
  }));
  const allZero = isZeroScore(result);
  const scoredAt = new Date(result.scoredAt).toLocaleString();
  const scoreValue = selectedPage ? selectedPage.compositeScore : result.compositeScore;
  const storySummary = selectedPage?.inferredStorySummary
    ?? (!multiPage ? (result.inferredStorySummary ?? pageScores[0]?.inferredStorySummary) : undefined);
  const rawIntentProfile = selectedPage?.pageIntentProfile?.inferredProfile
    ?? result.pageIntentProfile?.inferredProfile
    ?? normalizeLegacyIntentProfile(storySummary?.intentProfile);
  const intentProfileKey = selectedPage?.pageName ?? result.scoredPageName ?? '__overall';
  const selectedIntentProfile = intentProfileOverrides[intentProfileKey] ?? rawIntentProfile;
  const selectedReviewerPersona = reviewerPersonaByPage[intentProfileKey] ?? 'consultant';
  const storyConfirmationKey = storySummary
    ? buildStoryConfirmationKey(selectedPage?.pageName, storySummary)
    : undefined;
  const storyFeedback = storyConfirmationKey ? storyIntentFeedback[storyConfirmationKey] : undefined;
  const storyConfirmation = storyFeedback?.confirmation;
  const storyNote = storyFeedback?.note ?? '';
  const pageMetadata = selectedPage?.visualMetadata
    ?? (!multiPage ? (result.visualMetadata ?? pageScores[0]?.visualMetadata) : undefined);
  const pagePurposeAnalysis = selectedPage?.pagePurposeAnalysis
    ?? (!multiPage ? (result.pagePurposeAnalysis ?? pageScores[0]?.pagePurposeAnalysis) : undefined);
  const reviewEntries = buildPageReviewEntries(pageScores, result, storyIntentFeedback);
  const personaProfiles = result.personaPresentation?.availablePersonas ?? getReviewPresentationPersonaProfiles();
  const personaPresentation = result.overviewSummary
    ? applyPersonaPresentation({
        persona: workspacePersona,
        findings: result.normalizedFindings ?? [],
        overviewSummary: result.overviewSummary,
        fixPlan: result.fixPlan ?? [],
      })
    : undefined;
  const visibleFindings = buildVisibleFindings(personaPresentation?.findings ?? result.normalizedFindings, selectedPage?.pageName);
  const remediationQueue = buildContextAwareRemediationQueue({
    findings: personaPresentation?.findings ?? result.normalizedFindings,
    selectedPageName: selectedPage?.pageName,
    filters: issueFilters,
  });
  const visualMix = selectedPage
    ? {
        data: selectedPage.dataVisualCount,
        navigation: selectedPage.navigationVisualCount,
        hidden: selectedPage.hiddenVisualCount,
      }
    : {
        data: result.dataVisualCount,
        navigation: result.navigationVisualCount,
        hidden: result.hiddenVisualCount,
      };
  const feedbackForKey = (normalizedKey: string) =>
    selectedPage
      ? selectedPage.feedback?.[normalizedKey]
      : result.feedback?.[normalizedKey];
  const revealVisual = (visual: AffectedVisualReference) => {
    vscodeApiRef.current?.postMessage({
      type: 'revealVisual',
      pageName: visual.pageName,
      visualId: visual.visualId,
    });
  };
  const focusRecommendations = () => {
    fixPlanSectionRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    fixPlanSectionRef.current?.focus({ preventScroll: true });
  };
  const focusIssues = () => {
    issuesSectionRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    issuesSectionRef.current?.focus({ preventScroll: true });
  };
  const personaDefaults = buildPersonaDefaultFilters(personaProfiles.find((profile) => profile.id === workspacePersona));
  const activeFilterSummary = summarizeActiveIssueFilters(issueFilters);

  return (
    <main className="page-shell">
      <section className="hero-card">
        <div>
          <p className="eyebrow">PBIR Design Analyzer</p>
          <h1>Optimization Report</h1>
          <p className="report-name" title={result.reportPath}>
            {basename(result.reportPath)}
          </p>
          <p className="hero-meta">
            {result.pageCount} page(s) · Scored {scoredAt}
          </p>
        </div>
        <div className="hero-actions">
          <button
            className="secondary-button"
            onClick={() => vscodeApiRef.current?.postMessage({ type: 'refresh' })}
            type="button"
          >
            Refresh
          </button>
          {remediationQueue.items.length > 0 ? (
            <button
              className="primary-button"
              onClick={focusRecommendations}
              type="button"
            >
              Review Fix Plan ({remediationQueue.items.length})
            </button>
          ) : null}
        </div>
      </section>

      {allZero ? (
        <section className="status-card status-card-warn">
          {result.feedback?.gestalt?.[0]?.text ?? 'No data visuals were detected in this report.'}
        </section>
      ) : null}

      {multiPage ? (
        <nav className="tab-row" aria-label="Score tabs">
          {tabs.map((tab, index) => (
            <button
              className={`tab-button ${index === activeTab ? 'tab-button-active' : ''}`}
              key={tab}
              onClick={() => {
                setActiveTab(index);
                setPagePurposeExpanded(false);
                vscodeApiRef.current?.postMessage({ type: 'selectTab', pageIndex: index });
              }}
              type="button"
            >
              {tab}
            </button>
          ))}
        </nav>
      ) : null}

      {renderOverviewWorkspace(
        personaPresentation?.overviewSummary ?? result.overviewSummary,
        frameworkValues,
        scoreValue,
        visualMix,
        result.crossPageMatrix,
        selectedPage?.pageName,
        workspacePersona,
        personaProfiles,
        (nextPersona) => {
          setWorkspacePersona(nextPersona);
          if (!issueFiltersDirty) {
            const profile = personaProfiles.find((item) => item.id === nextPersona);
            setIssueFilters(buildPersonaDefaultFilters(profile));
          }
        },
        (pageName, dimension) => {
          setIssueFilters((prev) => ({
            ...prev,
            pageName,
            dimension,
            impactArea: 'all',
          }));
          setIssueFiltersDirty(true);
          focusIssues();
        },
        () => {
          setActiveTab(0);
          setPagePurposeExpanded(false);
          vscodeApiRef.current?.postMessage({ type: 'selectTab', pageIndex: 0 });
        },
      )}

      {selectedPage?.scoringError ? (
        <section className="status-card status-card-warn">{selectedPage.scoringError}</section>
      ) : null}

      {overallView && multiPage ? renderReviewSummary({
        entries: reviewEntries,
        activeFilter: reviewStatusFilter,
        onFilterChange: setReviewStatusFilter,
        onSelectPage: (pageName) => {
          const pageIndex = pageScores.findIndex((page) => page.pageName === pageName);
          if (pageIndex < 0) {
            return;
          }

          const nextTab = pageIndex + 1;
          setActiveTab(nextTab);
          vscodeApiRef.current?.postMessage({ type: 'selectTab', pageIndex: nextTab });
        },
        onExport: () => vscodeApiRef.current?.postMessage({ type: 'exportReviewWorkflow' }),
      }) : null}

      {storySummary && pagePurposeAnalysis ? renderPagePurposeAnalysisSection({
        analysis: pagePurposeAnalysis,
        storySummary,
        pageIntentProfile: selectedPage?.pageIntentProfile ?? result.pageIntentProfile,
        selectedProfile: selectedIntentProfile,
        onProfileChange: (next) => setIntentProfileOverrides((prev) => ({ ...prev, [intentProfileKey]: next })),
        actionabilityBreakdown: selectedPage?.actionabilityBreakdown ?? result.actionabilityBreakdown,
        benchmarkComparison: selectedPage?.benchmarkComparison ?? result.benchmarkComparison,
        confirmation: storyConfirmation,
        note: storyNote,
        onConfirm: (next) => {
          if (!storyConfirmationKey) {
            return;
          }

          setStoryIntentFeedback((prev) => ({
            ...prev,
            [storyConfirmationKey]: {
              confirmation: next,
              note: prev[storyConfirmationKey]?.note ?? '',
            },
          }));
          setSavedStoryNoteKey(undefined);
          const note = storyIntentFeedback[storyConfirmationKey]?.note?.trim();
          vscodeApiRef.current?.postMessage({
            type: 'setIntentFeedback',
            pageName: selectedPage?.pageName ?? result.scoredPageName ?? pageScores[0]?.pageName ?? 'Report',
            inferredIntent: storySummary.intentProfile,
            storyArchetype: storySummary.storyArchetype,
            userConfirmation: next,
            inferenceConfidence: storySummary.confidence,
            note: note ? note : undefined,
          });
        },
        onNoteChange: (nextNote) => {
          if (!storyConfirmationKey) {
            return;
          }

          setStoryIntentFeedback((prev) => ({
            ...prev,
            [storyConfirmationKey]: {
              confirmation: prev[storyConfirmationKey]?.confirmation,
              note: nextNote,
            },
          }));
          setSavedStoryNoteKey(undefined);
        },
        onSaveNote: () => {
          if (!storyConfirmationKey || !storyConfirmation) {
            return;
          }

          const noteToSave = storyIntentFeedback[storyConfirmationKey]?.note?.trim();
          vscodeApiRef.current?.postMessage({
            type: 'setIntentFeedback',
            pageName: selectedPage?.pageName ?? result.scoredPageName ?? pageScores[0]?.pageName ?? 'Report',
            inferredIntent: storySummary.intentProfile,
            storyArchetype: storySummary.storyArchetype,
            userConfirmation: storyConfirmation,
            inferenceConfidence: storySummary.confidence,
            note: noteToSave ? noteToSave : undefined,
          });
          setSavedStoryNoteKey(storyConfirmationKey);
        },
        noteSaved: savedStoryNoteKey === storyConfirmationKey,
        expanded: pagePurposeExpanded,
        onToggleExpanded: () => setPagePurposeExpanded((prev) => !prev),
      }) : null}
      <section ref={issuesSectionRef} tabIndex={-1}>
        {renderIssuesWorkspace({
          findings: visibleFindings,
          filters: issueFilters,
          groupingMode: issueGroupingMode,
          pageOptions: pageScores.map((page) => page.pageName),
          activeFilterSummary,
          onFilterChange: (key, value) => {
            setIssueFiltersDirty(true);
            setIssueFilters((prev) => ({ ...prev, [key]: value as never }));
          },
          onGroupingModeChange: setIssueGroupingMode,
          onClearFilters: () => {
            setIssueFiltersDirty(false);
            setIssueFilters({
              severity: 'all',
              pageName: 'all',
              dimension: 'all',
              impactArea: 'all',
              scope: 'all',
              detectionType: 'all',
            });
          },
          onResetToPersonaDefaults: () => {
            setIssueFiltersDirty(false);
            setIssueFilters(personaDefaults);
          },
        })}
      </section>

      <div ref={fixPlanSectionRef} tabIndex={-1}>
        {renderFixPlanSection(
          remediationQueue,
          result.fixOpportunities,
          result.proposalEnrichments,
          viewState.state.fixSelection,
          viewState.state.fixApplySessions ?? [],
          expandedOpportunityIds,
          (opportunityId) => setExpandedOpportunityIds((prev) => (
            prev.includes(opportunityId)
              ? prev.filter((id) => id !== opportunityId)
              : [...prev, opportunityId]
          )),
          (opportunityId) => vscodeApiRef.current?.postMessage({ type: 'toggleFixOpportunitySelection', opportunityId }),
          () => vscodeApiRef.current?.postMessage({ type: 'previewSelectedFixOpportunities' }),
          () => vscodeApiRef.current?.postMessage({ type: 'approveSelectedFixOpportunities' }),
          () => vscodeApiRef.current?.postMessage({ type: 'applySelectedFixOpportunities' }),
          (sessionId) => vscodeApiRef.current?.postMessage({ type: 'rollbackFixSession', sessionId }),
          (opportunityIds) => vscodeApiRef.current?.postMessage({ type: 'regenerateFixOpportunities', opportunityIds }),
        )}
      </div>
      {selectedPage ? renderReviewerCommentGenerator(
        selectedPage,
        selectedIntentProfile,
        selectedReviewerPersona,
        (next) => setReviewerPersonaByPage((prev) => ({ ...prev, [intentProfileKey]: next })),
      ) : null}

      <details className="panel-card evidence-section">
        <summary className="collapsible-summary">
          <span>Evidence</span>
          <span className="collapsible-caret" aria-hidden="true">▾</span>
        </summary>
        <div className="collapsible-body evidence-stack">
          {overallView && multiPage ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>Review Packet Preview</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                {renderReviewPacketPreview(
                  state.reviewPacketPreview,
                  state.reviewPacketPreviewHtml,
                  state.reviewPacketPreviewProfile ?? 'consultant',
                  state.reviewPacketPreviewTemplateVariant ?? 'brandedConsultant',
                  (profile) => vscodeApiRef.current?.postMessage({ type: 'setReviewPacketPreviewProfile', profile }),
                  (templateVariant) => vscodeApiRef.current?.postMessage({ type: 'setReviewPacketPreviewTemplateVariant', templateVariant }),
                  () => vscodeApiRef.current?.postMessage({ type: 'openReviewPacketPreview' }),
                )}
              </div>
            </details>
          ) : null}

          <details className="evidence-subsection">
            <summary className="collapsible-summary">
                <span>Design Framework Analysis</span>
              <span className="collapsible-caret" aria-hidden="true">▾</span>
            </summary>
            <div className="collapsible-body">
              <FrameworkSection
                currentPageName={selectedPage?.pageName}
                feedbackForKey={feedbackForKey}
                frameworkValues={frameworkValues}
                onRevealVisual={revealVisual}
              />
            </div>
          </details>

          {overallView && multiPage ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>Cross-Page Consistency</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                {renderReportConsistencySummary(result.reportConsistencySummary)}
              </div>
            </details>
          ) : null}

          {selectedPage ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>Page Consistency Notes</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                {renderPageConsistencyNotes(selectedPage.reportConsistencyNotes)}
              </div>
            </details>
          ) : null}

          {overallView && multiPage ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>Metadata Overview</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                {renderMetadataOverview(pageScores)}
              </div>
            </details>
          ) : null}

          {pageMetadata ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>Parsed Metadata</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                {renderVisualMetadataDetail(pageMetadata, revealVisual)}
              </div>
            </details>
          ) : null}

          {overallView && result.scoringErrors && Object.keys(result.scoringErrors).length > 0 ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>Scoring Internals</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                <section className="panel-card">
                  <h2>Page Errors</h2>
                  <ul className="recommendation-list">
                    {Object.entries(result.scoringErrors).map(([page, message]) => (
                      <li className="recommendation-item rec-high" key={page}>
                        <strong>{page}:</strong> {message}
                      </li>
                    ))}
                  </ul>
                </section>
              </div>
            </details>
          ) : null}

          {audit && overallView ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>AI Screenshot Audit</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                {renderAuditCoverageCard(audit, tabs.slice(1), vscodeApiRef.current!)}
              </div>
            </details>
          ) : null}

          {audit && selectedPage ? (
            <details className="evidence-subsection">
              <summary className="collapsible-summary">
                <span>AI Screenshot Audit</span>
                <span className="collapsible-caret" aria-hidden="true">▾</span>
              </summary>
              <div className="collapsible-body">
                {renderAuditPageSection(
                  selectedPage.pageName,
                  audit.pages.find((p) => p.pageName === selectedPage.pageName),
                  analyzingCaptureId,
                  audit.providerConfigured,
                  vscodeApiRef.current!,
                )}
              </div>
            </details>
          ) : null}
        </div>
      </details>
      <section aria-label="Export actions" className="panel-card export-card">
        <div className="issues-section-head">
          <div>
            <p className="section-kicker">Downstream artifact</p>
            <h2>Export</h2>
          </div>
          <p className="issues-section-copy">
            Generate a review packet after you have worked through the overview, issues, fix plan, and evidence.
          </p>
        </div>
        <div className="export-actions">
          <button
            className="primary-button"
            onClick={() => vscodeApiRef.current?.postMessage({ type: 'exportReviewWorkflow' })}
            type="button"
          >
            Export Review Summary
          </button>
          {state.reviewPacketPreviewHtml ? (
            <button
              className="secondary-button"
              onClick={() => vscodeApiRef.current?.postMessage({ type: 'openReviewPacketPreview' })}
              type="button"
            >
              Open Packet Preview
            </button>
          ) : null}
        </div>
      </section>
    </main>
  );
}
