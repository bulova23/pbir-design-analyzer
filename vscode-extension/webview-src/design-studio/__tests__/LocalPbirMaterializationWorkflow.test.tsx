import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { LocalPbirMaterializationWorkflow } from '../components/LocalPbirMaterializationWorkflow';
import type { DesignStudioMaterializationWorkflowViewModel } from '../../../src/design-studio/contracts/designStudioProtocol';

const baseState: DesignStudioMaterializationWorkflowViewModel = {
  status: 'idle',
  diagnostics: [],
  writtenFiles: [],
};

describe('LocalPbirMaterializationWorkflow', () => {
  it('provides an accessible preview entry point and loading cancellation', () => {
    const onPreview = jest.fn();
    const onCancel = jest.fn();

    render(<LocalPbirMaterializationWorkflow workflow={{ ...baseState, status: 'previewing' }} onPreview={onPreview} onApply={jest.fn()} onRecovery={jest.fn()} onCancel={onCancel} />);

    expect(screen.getByRole('status')).toHaveTextContent('Preparing local PBIR preview');
    fireEvent.click(screen.getByRole('button', { name: 'Cancel local PBIR operation' }));
    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('requires explicit confirmation before apply and prevents double submission', () => {
    const onApply = jest.fn();
    render(
      <LocalPbirMaterializationWorkflow
        workflow={{
          ...baseState,
          status: 'preview-ready',
          outcome: 'absent-destination',
          summary: {
            destinationClassification: 'absent',
            artifactCount: 2,
            identityReference: 'preview-id',
            rollbackAvailable: false,
          },
        }}
        onPreview={jest.fn()}
        onApply={onApply}
        onRecovery={jest.fn()}
        onCancel={jest.fn()}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Apply this preview' }));
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: 'Confirm local PBIR apply' }));
    fireEvent.click(screen.getByRole('button', { name: 'Confirm local PBIR apply' }));
    expect(onApply).toHaveBeenCalledTimes(1);
  });

  it('renders conflict and recovery-required outcomes with fresh-preview guidance', () => {
    render(
      <LocalPbirMaterializationWorkflow
        workflow={{ ...baseState, status: 'terminal', outcome: 'recovery-required' }}
        onPreview={jest.fn()}
        onApply={jest.fn()}
        onRecovery={jest.fn()}
        onCancel={jest.fn()}
      />,
    );

    expect(screen.getByText('Recovery required')).toBeInTheDocument();
    expect(screen.getByText('Start a fresh preview before applying again.')).toBeInTheDocument();
  });
});
