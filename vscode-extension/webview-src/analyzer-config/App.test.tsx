import React from 'react';
import { act, fireEvent, render, screen } from '@testing-library/react';
import '@testing-library/jest-dom';
import App from './App';
import type { DesignAnalyzerConfig } from '../../src/analyzer/config/types';

const postMessage = jest.fn();

const sampleConfig: DesignAnalyzerConfig = {
  frameworks: [
    { id: 'gestalt', name: 'Gestalt Principles', enabled: true, weight: 60 },
    { id: 'cognitive', name: 'Cognitive Load', enabled: true, weight: 40 },
    { id: 'dataink', name: 'Data-Ink Ratio', enabled: false, weight: 0, optional: true },
  ],
  navigationScoring: {
    enabled: true,
    weight: 25,
  },
  governance: [
    {
      id: 'maxVisualsPerPage',
      name: 'Max Visuals Per Page',
      value: 15,
      adminOnly: true,
      severity: 'warning',
    },
  ],
};

describe('Analyzer Config App', () => {
  beforeEach(() => {
    postMessage.mockReset();
    (globalThis as unknown as { acquireVsCodeApi: () => { postMessage: typeof postMessage } }).acquireVsCodeApi =
      () => ({
        postMessage,
      });
  });

  it('signals readiness and posts the edited config back to the host', async () => {
    render(<App />);

    expect(postMessage).toHaveBeenCalledWith({ type: 'webviewReady' });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'configState',
            config: sampleConfig,
          },
        }),
      );
    });

    expect(screen.getByText('Design Analyzer Configuration')).toBeInTheDocument();
    expect(screen.getByText('Navigation Treatment')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /save configuration/i }));

    expect(postMessage).toHaveBeenLastCalledWith({
      type: 'saveConfig',
      config: sampleConfig,
    });
  });

  it('applies an audience preset and posts the overlaid config to the host', async () => {
    render(<App />);

    expect(postMessage).toHaveBeenCalledWith({ type: 'webviewReady' });

    await act(async () => {
      window.dispatchEvent(
        new MessageEvent('message', {
          data: {
            type: 'configState',
            config: sampleConfig,
            presets: [
              {
                id: 'executive',
                name: 'Executive',
                description: 'Tighter visual budget.',
                governanceOverrides: { maxVisualsPerPage: 4 },
                navigationScoring: { weight: 18 },
              },
            ],
          },
        }),
      );
    });

    fireEvent.change(screen.getByLabelText(/audience preset/i), {
      target: { value: 'executive' },
    });

    fireEvent.click(screen.getByRole('button', { name: /save configuration/i }));

    const lastCall = postMessage.mock.calls[postMessage.mock.calls.length - 1][0];
    expect(lastCall.type).toBe('saveConfig');
    expect(lastCall.config.appliedAudiencePresetId).toBe('executive');
    expect(
      lastCall.config.governance.find((rule: { id: string }) => rule.id === 'maxVisualsPerPage').value,
    ).toBe(4);
    expect(lastCall.config.navigationScoring.weight).toBe(18);
  });
});
