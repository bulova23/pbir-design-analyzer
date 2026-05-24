import React from 'react';
import type {
  AffectedVisualReference,
  AuditCaptureSummary,
  AuditFindingDisplay,
  AuditPageState,
  AuditState,
  FindingType,
  FrameworkFeedbackItem,
  PageVisualMetadataSummary,
  PageScore,
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
  groupRecommendations,
} from '../../src/analyzer/score/presentation';
import { buildQuickFixList } from '../../src/analyzer/score/quickFixes';

interface ScoreVsCodeApi {
  postMessage(message: ScorePanelWebviewToHostMessage): void;
}

declare function acquireVsCodeApi(): ScoreVsCodeApi;

type ViewState =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; state: ScorePanelState; audit?: AuditState };

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

function renderMetadataOverview(pageScores: PageScore[]): React.ReactNode {
  const pagesWithMetadata = pageScores.filter((page) => page.visualMetadata);
  if (pagesWithMetadata.length === 0) {
    return null;
  }

  return (
    <section className="panel-card">
      <h2>Parsed Visual Metadata</h2>
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
            </article>
          );
        })}
      </div>
    </section>
  );
}

function renderVisualMetadataDetail(
  summary: PageVisualMetadataSummary,
  onRevealVisual: (visual: AffectedVisualReference) => void,
): React.ReactNode {
  return (
    <section className="panel-card">
      <h2>Parsed Visual Metadata</h2>
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
    </section>
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
                <span className="framework-title">{framework.label}</span>
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
          {!providerConfigured ? (
            <button
              className="secondary-button"
              onClick={() => vscode.postMessage({ type: 'configureAuditProvider' })}
              type="button"
              title={`Configure ${providerName ?? 'AI provider'} to enable analysis`}
            >
              Configure AI Provider
            </button>
          ) : null}
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
          AI analysis requires an Anthropic API key. Click "Configure AI Provider" to add one.
        </p>
      ) : (
        <p className="audit-provider-note">
          AI provider: {providerName}. Open a page tab to analyze individual screenshots.
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
  const vscodeApiRef = React.useRef<ScoreVsCodeApi | null>(null);
  const recommendationsSectionRef = React.useRef<HTMLElement | null>(null);

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
        setActiveTab(0);
        return;
      }

      if (message.type === 'error') {
        setViewState({ kind: 'error', message: message.message });
        return;
      }

      if (message.type === 'scoreState') {
        setViewState({ kind: 'ready', state: message.state });
        setActiveTab(message.state.selectedPageIndex);
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
  const displayedRecommendations = selectedPage?.recommendations ?? result.recommendations;
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
  const groupedRecommendations = groupRecommendations(displayedRecommendations);
  const recommendationCount = groupedRecommendations.length;
  const feedbackForQuickFixes = selectedPage?.feedback ?? result.feedback ?? {};
  const flatFeedback = Object.values(feedbackForQuickFixes).flat();
  const quickFixes = buildQuickFixList(displayedRecommendations, flatFeedback);
  const allZero = isZeroScore(result);
  const scoredAt = new Date(result.scoredAt).toLocaleString();
  const scoreValue = selectedPage ? selectedPage.compositeScore : result.compositeScore;
  const pageMetadata = selectedPage?.visualMetadata
    ?? (!multiPage ? (result.visualMetadata ?? pageScores[0]?.visualMetadata) : undefined);
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
    recommendationsSectionRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    recommendationsSectionRef.current?.focus({ preventScroll: true });
  };

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
          {recommendationCount > 0 ? (
            <button
              className="primary-button"
              onClick={focusRecommendations}
              type="button"
            >
              Review Recommendations ({recommendationCount})
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
                vscodeApiRef.current?.postMessage({ type: 'selectTab', pageIndex: index });
              }}
              type="button"
            >
              {tab}
            </button>
          ))}
        </nav>
      ) : null}

      <section className="summary-card">
        <div className={`score-chip ${getScoreTone(scoreValue)}`}>
          <span>{Math.round(scoreValue)}</span>
          <small>/100</small>
        </div>
        <div className="summary-copy">
          <h2>{selectedPage ? `${selectedPage.pageName} Score` : 'Composite Score'}</h2>
          <p>
            {selectedPage
              ? `Weighted average of ${enabledFrameworks.length} enabled design frameworks on this page.`
              : multiPage
                ? `Weighted average of ${enabledFrameworks.length} enabled design frameworks across all pages.`
                : `Weighted average of ${enabledFrameworks.length} enabled design frameworks.`}
          </p>
          {typeof visualMix.data === 'number' &&
          typeof visualMix.navigation === 'number' &&
          typeof visualMix.hidden === 'number' ? (
            <p>
              Visual mix: {visualMix.data} data, {visualMix.navigation} navigation, {visualMix.hidden} hidden.
              {' '}
              {config.navigationScoring.enabled
                ? `Navigation controls count at ${config.navigationScoring.weight}% weight.`
                : 'Navigation controls use legacy full-weight treatment.'}
            </p>
          ) : null}
        </div>
      </section>

      {selectedPage?.scoringError ? (
        <section className="status-card status-card-warn">{selectedPage.scoringError}</section>
      ) : null}

      <FrameworkSection
        currentPageName={selectedPage?.pageName}
        feedbackForKey={feedbackForKey}
        frameworkValues={frameworkValues}
        onRevealVisual={revealVisual}
      />

      {overallView && multiPage ? renderMetadataOverview(pageScores) : null}
      {pageMetadata ? renderVisualMetadataDetail(pageMetadata, revealVisual) : null}

      {overallView && result.scoringErrors && Object.keys(result.scoringErrors).length > 0 ? (
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
      ) : null}

      <section
        aria-label="Recommendations"
        className="panel-card"
        ref={recommendationsSectionRef}
        tabIndex={-1}
      >
        <h2>Recommendations</h2>
        {groupedRecommendations.length === 0 ? (
          <p className="empty-text">No issues found.</p>
        ) : (
          <ul className="recommendation-list">
            {groupedRecommendations.map((recommendation, index) => (
              <li className={`recommendation-item ${recommendation.cls}`} key={`${recommendation.text}-${index}`}>
                {recommendation.text}
              </li>
            ))}
          </ul>
        )}
      </section>

      {quickFixes.length > 0 ? (
        <section aria-label="Quick fixes" className="panel-card quick-fix-card">
          <h2>Quick Fixes</h2>
          <p className="quick-fix-intro">
            Advisory next steps derived from the findings above. Each fix is a manual action — no
            visuals are modified automatically.
          </p>
          <ul className="quick-fix-list">
            {quickFixes.map((fix) => (
              <li className="quick-fix-item" key={fix.operation}>
                <div className="quick-fix-header">
                  <span className="quick-fix-label">{fix.label}</span>
                  <span className="quick-fix-operation">{fix.operation}</span>
                </div>
                {fix.detail ? <p className="quick-fix-detail">{fix.detail}</p> : null}
                {fix.affectedVisuals && fix.affectedVisuals.length > 0 ? (
                  <ul className="quick-fix-visual-list">
                    {fix.affectedVisuals.map((visual) => (
                      <li className="quick-fix-visual" key={`${visual.pageName}|${visual.visualId}`}>
                        <button
                          className="quick-fix-visual-button"
                          onClick={() => revealVisual(visual)}
                          type="button"
                        >
                          {visual.pageName} · {visual.visualType} ({shortenVisualId(visual.visualId)})
                        </button>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {audit && overallView
        ? renderAuditCoverageCard(audit, tabs.slice(1), vscodeApiRef.current!)
        : null}

      {audit && selectedPage
        ? renderAuditPageSection(
            selectedPage.pageName,
            audit.pages.find((p) => p.pageName === selectedPage.pageName),
            analyzingCaptureId,
            audit.providerConfigured,
            vscodeApiRef.current!,
          )
        : null}
    </main>
  );
}
