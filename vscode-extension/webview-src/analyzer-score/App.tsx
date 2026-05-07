import React from 'react';
import type {
  AffectedVisualReference,
  FrameworkFeedbackItem,
  PageScore,
  ScorePanelHostToWebviewMessage,
  ScorePanelState,
  ScorePanelWebviewToHostMessage,
  ScoreResult,
} from '../../src/analyzer/contracts/scorePanel';
import {
  basename,
  getEnabledFrameworks,
  getPageScore,
  getResultScore,
  groupRecommendations,
} from '../../src/analyzer/score/presentation';

interface ScoreVsCodeApi {
  postMessage(message: ScorePanelWebviewToHostMessage): void;
}

declare function acquireVsCodeApi(): ScoreVsCodeApi;

type ViewState =
  | { kind: 'loading' }
  | { kind: 'error'; message: string }
  | { kind: 'ready'; state: ScorePanelState };

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
                    <p className="criterion-label">{details.label}</p>
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
                  <span>{item.text}</span>
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

export default function App(): JSX.Element {
  const [viewState, setViewState] = React.useState<ViewState>({ kind: 'loading' });
  const [activeTab, setActiveTab] = React.useState(0);
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

      setViewState({ kind: 'ready', state: message.state });
      setActiveTab(message.state.selectedPageIndex);
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

  const { state } = viewState;
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
  const allZero = isZeroScore(result);
  const scoredAt = new Date(result.scoredAt).toLocaleString();
  const scoreValue = selectedPage ? selectedPage.compositeScore : result.compositeScore;
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
    </main>
  );
}
