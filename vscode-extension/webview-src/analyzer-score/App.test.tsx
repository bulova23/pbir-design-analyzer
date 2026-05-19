import React from 'react';
import { act, fireEvent, render, screen } from '@testing-library/react';
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
        visualMetadata: {
          pageName: 'Overview',
          visiblePageTitle: 'Executive Overview',
          canvasWidth: 1280,
          canvasHeight: 720,
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
        visualMetadata: {
          pageName: 'Details',
          visiblePageTitle: 'Detail Comparison',
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
            state: scoreState,
          },
        }),
      );
    });

    expect(screen.getByText('Optimization Report')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /review recommendations/i })).toBeInTheDocument();
    expect(screen.getByText(/Visual mix: 12 data, 4 navigation, 1 hidden/i)).toBeInTheDocument();
    expect(screen.getByText('Parsed Visual Metadata')).toBeInTheDocument();
    expect(screen.getByText(/Executive Overview/i)).toBeInTheDocument();
    expect(screen.getByText(/Detail Comparison/i)).toBeInTheDocument();
    expect(screen.getByText(/Score Breakdown - Grid alignment 35\/35, Figure\/ground 30\/30, Similarity 0\/20, Visual presence 15\/15\./i)).toBeInTheDocument();
    fireEvent.click(screen.getByText('Gestalt Principles'));
    expect(screen.getAllByText('35/35').length).toBeGreaterThan(0);
    expect(screen.getAllByText('0/20').length).toBeGreaterThan(0);
    expect(screen.getAllByText(/Improve:/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Heuristic').length).toBeGreaterThan(0);
    expect(screen.getByText('Objective')).toBeInTheDocument();
    expect(screen.getByText('Style')).toBeInTheDocument();
    expect(screen.getByText(/aim for 2–5 distinct types\./i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Details' }));

    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'selectTab',
      pageIndex: 2,
    });
    expect(screen.getByText(/Detail Comparison/i)).toBeInTheDocument();
    fireEvent.click(screen.getByText('Cognitive Load'));
    expect(screen.getAllByText('74/100').length).toBeGreaterThan(0);
    expect(screen.getByText(/Details density is acceptable\./i)).toBeInTheDocument();
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

    fireEvent.click(screen.getByText('Cognitive Load'));
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
});
