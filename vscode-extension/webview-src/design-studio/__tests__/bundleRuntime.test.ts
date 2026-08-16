import fs from 'fs';
import path from 'path';
import { act, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { withDesignStudioEnvelope } from '../../../src/design-studio/contracts/designStudioProtocol';

const postMessage = jest.fn();

function dispatchHostMessage(message: unknown): void {
  act(() => {
    window.dispatchEvent(new MessageEvent('message', { data: message }));
  });
}

describe('Design Studio built webview bundle', () => {
  beforeEach(() => {
    postMessage.mockReset();
    document.body.innerHTML = '<div id="root"></div>';
    (globalThis as unknown as { acquireVsCodeApi: () => { postMessage: typeof postMessage } }).acquireVsCodeApi =
      () => ({ postMessage });
    (window as Window & { __PBIR_DESIGN_STUDIO_BOOTSTRAP__?: { threadId: string } }).__PBIR_DESIGN_STUDIO_BOOTSTRAP__ = {
      threadId: 'design-studio:active-report',
    };
    Reflect.deleteProperty(window as unknown as Record<string, unknown>, 'process');
  });

  it('renders without relying on process in a browser-like runtime', async () => {
    const bundlePath = path.resolve(__dirname, '../../../webview-dist/design-studio.js');
    const bundleSource = fs.readFileSync(bundlePath, 'utf8');

    expect(bundleSource).not.toContain('process.env.NODE_ENV');

    expect(() => {
      window.eval(bundleSource);
    }).not.toThrow();

    await waitFor(() => {
      expect(postMessage).toHaveBeenNthCalledWith(1, withDesignStudioEnvelope({ type: 'webviewReady' }));
      expect(postMessage).toHaveBeenNthCalledWith(2, withDesignStudioEnvelope({
        type: 'loadStudioState',
        threadId: 'design-studio:active-report',
      }));
    });

    dispatchHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:active-report',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'concept',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'inProgress', readinessLabel: 'In progress', title: 'Concept Studio', description: 'Review the concept baseline.' },
            { id: 'draft', label: 'Draft Studio', status: 'notStarted', readinessLabel: 'Not started', title: 'Draft Studio', description: 'Review the draft.' },
            { id: 'refinement', label: 'Refinement Studio', status: 'notStarted', readinessLabel: 'Not started', title: 'Refinement Studio', description: 'Review advisory changes.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'ready', readinessLabel: 'Ready', title: 'Prepare For Review', description: 'Prepare the approved draft for consultant review without changing the report.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Open Analyzer Workspace explicitly when the prepared review candidate is ready.' },
            { id: 'compare', label: 'Compare Iterations', status: 'notStarted', readinessLabel: 'Not started', title: 'Compare Iterations', description: 'Review what changed.' },
          ],
          currentStageSummary: {
            title: 'Concept Studio',
            description: 'Review the concept baseline.',
          },
          approvalCards: [],
          materializationReadiness: {
            readinessLabel: 'Ready for analysis',
            executableEligibility: 'executable',
            targetAnalyzer: 'pbirDesignReview',
            targetAnalyzerProfile: 'consultant',
            diagnostics: ['Repository-backed candidate is available for explicit analyzer handoff.'],
          },
          analyzerHandoff: {
            requestId: 'materialization-request:1',
            readinessLabel: 'Ready to open Analyzer Workspace',
            analyzerId: 'pbirDesignReview',
            analyzerProfileId: 'consultant',
            canOpen: true,
            diagnostics: ['Analysis has not started. Launch is explicit.'],
          },
        },
      },
    }));

    expect(await screen.findByRole('heading', { name: 'Report Design Studio' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Prepare For Review/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Review Design/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Compare Iterations/i })).toBeInTheDocument();
  });
});
