import {
  PbirMaterializationWorkflow,
  type PbirMaterializationRpcResponse,
} from '../services/materialization/PbirMaterializationWorkflow';

const input = { canonicalInput: true };

function response(outcome: PbirMaterializationRpcResponse['outcome'], requestId = 'preview-1'): PbirMaterializationRpcResponse {
  return {
    schemaVersion: 'pbir-local-materialization-response/v1',
    requestId,
    operation: 'pbir/materialization/preview',
    outcome,
    validatedPreview: outcome === 'absent-destination' ? {
      schemaVersion: 'pbir-materialization-orchestration-preview-identity/v1',
      previewRequestId: requestId,
      previewId: 'preview-id',
      previewHash: 'preview-hash',
      targetStateHash: 'target-hash',
      artifactRef: 'artifact-ref',
      artifactHash: 'artifact-hash',
      manifestRef: 'manifest-ref',
      manifestHash: 'manifest-hash',
    } : undefined,
    transactionId: undefined,
    activeTransactionRef: undefined,
    rollbackAvailable: false,
    writtenFiles: [{ relativePath: 'definition.pbir', byteLength: 10, hashSha256: 'hash' }],
    lineage: undefined,
    targetStateHash: 'target-hash',
    resultHash: undefined,
    diagnostics: [{ code: 'PBIR', field: 'destination', message: 'Safe diagnostic.' }],
  };
}

describe('PbirMaterializationWorkflow', () => {
  it('constructs a read-only preview request and exposes a redacted summary', async () => {
    const executeRequest = jest.fn().mockResolvedValue(response('absent-destination'));
    const workflow = new PbirMaterializationWorkflow({ executeRequest });

    const state = await workflow.preview(input);

    expect(executeRequest).toHaveBeenCalledWith(
      'pbir/materialization/preview',
      expect.objectContaining({
        schemaVersion: 'pbir-local-materialization-preview-request/v1',
        operation: 'pbir/materialization/preview',
        input,
      }),
      expect.anything(),
    );
    expect(state.outcome).toBe('absent-destination');
    expect(state.summary).toEqual(expect.objectContaining({ artifactCount: 1, destinationClassification: 'absent' }));
    expect(JSON.stringify(state)).not.toContain('canonicalInput');
  });

  it('fails closed when an adapter response is not the versioned typed response', async () => {
    const executeRequest = jest.fn().mockResolvedValue({ schemaVersion: 'unexpected', outcome: 'applied' });
    const workflow = new PbirMaterializationWorkflow({ executeRequest });

    const state = await workflow.preview(input);

    expect(state.status).toBe('disconnected');
    expect(state.summary).toBeUndefined();
  });

  it('requires explicit confirmation and sends the exact preview with a fresh transaction ID', async () => {
    const preview = response('absent-destination');
    const executeRequest = jest.fn()
      .mockResolvedValueOnce(preview)
      .mockResolvedValueOnce({ ...preview, operation: 'pbir/materialization/apply', outcome: 'applied', transactionId: 'tx-1' });
    const workflow = new PbirMaterializationWorkflow({ executeRequest }, { createTransactionId: () => 'tx-1' });

    await workflow.preview(input);
    await expect(workflow.apply(false)).resolves.toMatchObject({ status: 'confirmation-required' });
    await workflow.apply(true);

    expect(executeRequest).toHaveBeenNthCalledWith(
      2,
      'pbir/materialization/apply',
      expect.objectContaining({
        validatedPreview: preview.validatedPreview,
        transactionId: 'tx-1',
        applyApproved: true,
      }),
      expect.anything(),
    );
  });

  for (const outcome of [
    'conflict', 'stale-preview', 'recovery-required', 'cancelled', 'failure', 'transaction-reused',
  ] as const) {
    it(`clears applyable preview after ${outcome}`, async () => {
    const executeRequest = jest.fn()
      .mockResolvedValueOnce(response('absent-destination'))
      .mockResolvedValueOnce(response(outcome));
    const workflow = new PbirMaterializationWorkflow({ executeRequest });

    await workflow.preview(input);
    const result = await workflow.apply(true);

    expect(result.state.status).toBe('terminal');
    await expect(workflow.apply(true)).resolves.toMatchObject({ status: 'preview-required' });
    });
  }

  it('uses recovery inspection as a read-only operation', async () => {
    const executeRequest = jest.fn().mockResolvedValue(response('recovery-required'));
    const workflow = new PbirMaterializationWorkflow({ executeRequest });

    await workflow.inspectRecovery(input, 'preview-1');

    expect(executeRequest).toHaveBeenCalledWith(
      'pbir/materialization/recovery/inspect',
      expect.objectContaining({ previewRequestId: 'preview-1', input }),
      expect.anything(),
    );
    expect(executeRequest).not.toHaveBeenCalledWith('pbir/materialization/apply', expect.anything(), expect.anything());
  });

  for (const outcome of [
    'absent-destination', 'empty-destination', 'exact-match', 'managed-replacement', 'conflict',
    'recovery-required', 'applied', 'stale-preview', 'invalid-request', 'unsafe-destination',
    'unsupported-operation', 'schema-failure', 'transaction-reused', 'cancelled', 'failure',
  ] as const) {
    it(`maps the typed ${outcome} outcome without inventing a new status`, async () => {
      const executeRequest = jest.fn().mockResolvedValue(response(outcome));
      const workflow = new PbirMaterializationWorkflow({ executeRequest });

      const state = await workflow.preview(input);

      expect(state.outcome).toBe(outcome);
      expect(state.diagnostics[0]?.message).toBe('Safe diagnostic.');
    });
  }

  it('cancels in-flight work and ignores a late response from the old generation', async () => {
    let resolveResponse: (value: PbirMaterializationRpcResponse) => void = () => undefined;
    const executeRequest = jest.fn().mockReturnValue(new Promise<PbirMaterializationRpcResponse>((resolve) => {
      resolveResponse = resolve;
    }));
    const cancel = jest.fn();
    const workflow = new PbirMaterializationWorkflow({ executeRequest }, {
      createCancellation: () => ({ token: { isCancellationRequested: false }, cancel, dispose: jest.fn() }),
    });

    const pending = workflow.preview(input);
    workflow.cancel();
    resolveResponse(response('absent-destination'));

    await pending;
    expect(cancel).toHaveBeenCalledTimes(1);
    expect(workflow.getState()).toMatchObject({ status: 'terminal', outcome: 'cancelled' });
  });

  it('prevents a second apply while the first apply is in flight', async () => {
    let resolveApply: (value: PbirMaterializationRpcResponse) => void = () => undefined;
    const executeRequest = jest.fn()
      .mockResolvedValueOnce(response('absent-destination'))
      .mockReturnValueOnce(new Promise<PbirMaterializationRpcResponse>((resolve) => { resolveApply = resolve; }));
    const workflow = new PbirMaterializationWorkflow({ executeRequest });

    await workflow.preview(input);
    const firstApply = workflow.apply(true);
    const secondApply = await workflow.apply(true);

    expect(secondApply.status).toBe('preview-required');
    resolveApply({ ...response('absent-destination'), operation: 'pbir/materialization/apply', outcome: 'applied', transactionId: 'tx' });
    await firstApply;
    expect(executeRequest).toHaveBeenCalledTimes(2);
  });

  it('defensively redacts unsafe diagnostics and file paths before presentation', async () => {
    const executeRequest = jest.fn().mockResolvedValue({
      ...response('absent-destination'),
      diagnostics: [
        { code: 'unsafe', field: 'destination', message: '/private/staging/journal.json exception payload' },
      ],
      writtenFiles: [
        { relativePath: '../secret.json', byteLength: 1, hashSha256: 'hash' },
        { relativePath: 'definition/report.json', byteLength: 1, hashSha256: 'hash' },
      ],
    });
    const workflow = new PbirMaterializationWorkflow({ executeRequest });

    const state = await workflow.preview(input);

    expect(state.diagnostics[0]?.message).toBe('Additional local operation details were withheld.');
    expect(state.writtenFiles).toEqual([{ relativePath: 'definition/report.json', byteLength: 1, hashSha256: 'hash' }]);
  });
});
