export const PBIR_MATERIALIZATION_ROUTES = {
  preview: 'pbir/materialization/preview',
  apply: 'pbir/materialization/apply',
  recovery: 'pbir/materialization/recovery/inspect',
} as const;

export type PbirMaterializationRoute = typeof PBIR_MATERIALIZATION_ROUTES[keyof typeof PBIR_MATERIALIZATION_ROUTES];

export const PBIR_MATERIALIZATION_OUTCOMES = [
  'absent-destination',
  'empty-destination',
  'exact-match',
  'managed-replacement',
  'conflict',
  'recovery-required',
  'applied',
  'stale-preview',
  'invalid-request',
  'unsafe-destination',
  'unsupported-operation',
  'schema-failure',
  'transaction-reused',
  'cancelled',
  'failure',
] as const;

export type PbirMaterializationOutcome = typeof PBIR_MATERIALIZATION_OUTCOMES[number];

export interface PbirMaterializationRpcDiagnostic {
  code: string;
  field: string;
  message: string;
}

export interface PbirMaterializationPreviewIdentity {
  schemaVersion: string;
  previewRequestId: string;
  previewId: string;
  previewHash: string;
  targetStateHash: string;
  artifactRef: string;
  artifactHash: string;
  manifestRef: string;
  manifestHash: string;
}

export interface PbirMaterializationRpcResponse {
  schemaVersion: 'pbir-local-materialization-response/v1';
  requestId: string;
  operation: PbirMaterializationRoute;
  outcome: PbirMaterializationOutcome;
  validatedPreview?: PbirMaterializationPreviewIdentity;
  transactionId?: string;
  activeTransactionRef?: string;
  rollbackAvailable: boolean;
  writtenFiles: Array<{ relativePath: string; byteLength: number; hashSha256: string }>;
  lineage?: unknown;
  targetStateHash?: string;
  resultHash?: string;
  diagnostics: PbirMaterializationRpcDiagnostic[];
}

export interface PbirMaterializationCancellation {
  readonly token: unknown;
  cancel(): void;
  dispose(): void;
}

export interface PbirMaterializationRpcClient {
  executeRequest<T>(route: PbirMaterializationRoute, params: unknown, token?: unknown): Promise<T>;
}

export interface PbirMaterializationWorkflowState {
  status: 'idle' | 'previewing' | 'preview-ready' | 'inspecting-recovery' | 'applying' | 'terminal' | 'preview-required' | 'disconnected';
  outcome?: PbirMaterializationOutcome;
  summary?: {
    destinationClassification: 'absent' | 'empty' | 'exact-match' | 'managed-replacement' | 'conflict' | 'recovery-required' | 'unknown';
    artifactCount: number;
    identityReference?: string;
    previewHash?: string;
    targetStateHash?: string;
    activeTransactionRef?: string;
    rollbackAvailable: boolean;
  };
  diagnostics: PbirMaterializationRpcDiagnostic[];
  writtenFiles: Array<{ relativePath: string; byteLength: number; hashSha256: string }>;
  transactionId?: string;
}

export type PbirMaterializationActionResult =
  | { status: 'confirmation-required'; state: PbirMaterializationWorkflowState }
  | { status: 'preview-required'; state: PbirMaterializationWorkflowState }
  | { status: 'completed'; state: PbirMaterializationWorkflowState };

const APPLYABLE_PREVIEW_OUTCOMES = new Set<PbirMaterializationOutcome>([
  'absent-destination',
  'empty-destination',
  'managed-replacement',
]);

type DestinationClassification = NonNullable<PbirMaterializationWorkflowState['summary']>['destinationClassification'];

function isRpcResponse(value: unknown): value is PbirMaterializationRpcResponse {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return false;
  const candidate = value as Record<string, unknown>;
  return candidate.schemaVersion === 'pbir-local-materialization-response/v1'
    && typeof candidate.requestId === 'string'
    && typeof candidate.operation === 'string'
    && (PBIR_MATERIALIZATION_ROUTES.preview === candidate.operation
      || PBIR_MATERIALIZATION_ROUTES.apply === candidate.operation
      || PBIR_MATERIALIZATION_ROUTES.recovery === candidate.operation)
    && typeof candidate.outcome === 'string'
    && (PBIR_MATERIALIZATION_OUTCOMES as readonly string[]).includes(candidate.outcome)
    && typeof candidate.rollbackAvailable === 'boolean'
    && Array.isArray(candidate.writtenFiles)
    && Array.isArray(candidate.diagnostics);
}

function destinationClassification(outcome: PbirMaterializationOutcome): DestinationClassification {
  switch (outcome) {
    case 'absent-destination': return 'absent';
    case 'empty-destination': return 'empty';
    case 'exact-match': return 'exact-match';
    case 'managed-replacement': return 'managed-replacement';
    case 'conflict': return 'conflict';
    case 'recovery-required': return 'recovery-required';
    default: return 'unknown';
  }
}

function redactDiagnosticMessage(message: string): string {
  return /(?:[A-Za-z]:[\\/]|\/[^\s]+|staging|journal|backup|quarantine|exception|payload|transaction internals?)/i.test(message)
    ? 'Additional local operation details were withheld.'
    : message;
}

function isSafeRelativePath(relativePath: string): boolean {
  return relativePath.length > 0
    && !relativePath.startsWith('/')
    && !/^[A-Za-z]:[\\/]/.test(relativePath)
    && !relativePath.split(/[\\/]/).includes('..');
}

function emptyState(status: PbirMaterializationWorkflowState['status']): PbirMaterializationWorkflowState {
  return { status, diagnostics: [], writtenFiles: [] };
}

export class PbirMaterializationWorkflow {
  private generation = 0;
  private requestSequence = 0;
  private previewIdentity: PbirMaterializationPreviewIdentity | undefined;
  private previewInput: unknown;
  private cancellation: PbirMaterializationCancellation | undefined;
  private state: PbirMaterializationWorkflowState = emptyState('idle');

  constructor(
    private readonly rpc: PbirMaterializationRpcClient,
    private readonly dependencies: {
      createCancellation?: () => PbirMaterializationCancellation;
      createRequestId?: (kind: 'preview' | 'apply' | 'recovery') => string;
      createTransactionId?: () => string;
    } = {},
  ) {}

  getState(): PbirMaterializationWorkflowState {
    return this.state;
  }

  getPreviewRequestId(): string | undefined {
    return this.previewIdentity?.previewRequestId;
  }

  async preview(input: unknown): Promise<PbirMaterializationWorkflowState> {
    this.clearPreview();
    const generation = ++this.generation;
    this.state = emptyState('previewing');
    const cancellation = this.beginCancellation();
    try {
      const result = await this.rpc.executeRequest<PbirMaterializationRpcResponse>(
        PBIR_MATERIALIZATION_ROUTES.preview,
        {
          schemaVersion: 'pbir-local-materialization-preview-request/v1',
          requestId: this.requestId('preview'),
          operation: PBIR_MATERIALIZATION_ROUTES.preview,
          input,
        },
        cancellation.token,
      );
      if (generation !== this.generation) {
        return this.state;
      }
      if (!isRpcResponse(result)) throw new Error('The local PBIR response was invalid.');
      this.previewInput = input;
      this.previewIdentity = result.validatedPreview;
      this.state = this.project(result, APPLYABLE_PREVIEW_OUTCOMES.has(result.outcome) ? 'preview-ready' : 'terminal');
      return this.state;
    } catch {
      if (generation === this.generation) {
        this.clearPreview();
        this.state = emptyState('disconnected');
      }
      return this.state;
    } finally {
      cancellation.dispose();
      if (this.cancellation === cancellation) this.cancellation = undefined;
    }
  }

  async apply(confirmed: boolean): Promise<PbirMaterializationActionResult> {
    if (!this.previewIdentity || !this.previewInput || this.state.status !== 'preview-ready') {
      this.state = emptyState('preview-required');
      return { status: 'preview-required', state: this.state };
    }
    if (!confirmed) {
      return { status: 'confirmation-required', state: { ...this.state } };
    }
    const generation = ++this.generation;
    this.state = { ...this.state, status: 'applying' };
    const cancellation = this.beginCancellation();
    try {
      const result = await this.rpc.executeRequest<PbirMaterializationRpcResponse>(
        PBIR_MATERIALIZATION_ROUTES.apply,
        {
          schemaVersion: 'pbir-local-materialization-apply-request/v1',
          requestId: this.requestId('apply'),
          operation: PBIR_MATERIALIZATION_ROUTES.apply,
          input: this.previewInput,
          validatedPreview: this.previewIdentity,
          transactionId: this.transactionId(),
          applyApproved: true,
        },
        cancellation.token,
      );
      if (!isRpcResponse(result)) throw new Error('The local PBIR response was invalid.');
      if (generation !== this.generation) return { status: 'completed', state: this.state };
      this.state = this.project(result, 'terminal');
      this.clearPreview();
      return { status: 'completed', state: this.state };
    } catch {
      if (generation === this.generation) {
        this.clearPreview();
        this.state = emptyState('disconnected');
      }
      return { status: 'completed', state: this.state };
    } finally {
      cancellation.dispose();
      if (this.cancellation === cancellation) this.cancellation = undefined;
    }
  }

  async inspectRecovery(input: unknown, previewRequestId: string): Promise<PbirMaterializationWorkflowState> {
    const generation = ++this.generation;
    this.state = emptyState('inspecting-recovery');
    const cancellation = this.beginCancellation();
    try {
      const result = await this.rpc.executeRequest<PbirMaterializationRpcResponse>(
        PBIR_MATERIALIZATION_ROUTES.recovery,
        {
          schemaVersion: 'pbir-local-materialization-recovery-inspect-request/v1',
          requestId: this.requestId('recovery'),
          operation: PBIR_MATERIALIZATION_ROUTES.recovery,
          input,
          previewRequestId,
        },
        cancellation.token,
      );
      if (!isRpcResponse(result)) throw new Error('The local PBIR response was invalid.');
      if (generation !== this.generation) return this.state;
      this.clearPreview();
      this.state = this.project(result, 'terminal');
      return this.state;
    } catch {
      if (generation === this.generation) this.state = emptyState('disconnected');
      return this.state;
    } finally {
      cancellation.dispose();
      if (this.cancellation === cancellation) this.cancellation = undefined;
    }
  }

  cancel(): void {
    this.cancellation?.cancel();
    this.generation += 1;
    this.clearPreview();
    this.state = { ...emptyState('terminal'), outcome: 'cancelled' };
  }

  reset(): void {
    this.generation += 1;
    this.cancellation?.cancel();
    this.clearPreview();
    this.state = emptyState('idle');
  }

  private project(result: PbirMaterializationRpcResponse, status: PbirMaterializationWorkflowState['status']): PbirMaterializationWorkflowState {
    return {
      status,
      outcome: result.outcome,
      summary: {
        destinationClassification: destinationClassification(result.outcome),
        artifactCount: result.writtenFiles.length,
        identityReference: result.validatedPreview?.previewId,
        previewHash: result.validatedPreview?.previewHash,
        targetStateHash: result.targetStateHash,
        activeTransactionRef: result.activeTransactionRef,
        rollbackAvailable: result.rollbackAvailable,
      },
      diagnostics: result.diagnostics.map(({ code, field, message }) => ({ code, field, message: redactDiagnosticMessage(message) })),
      writtenFiles: result.writtenFiles
        .filter(({ relativePath }) => isSafeRelativePath(relativePath))
        .map(({ relativePath, byteLength, hashSha256 }) => ({ relativePath, byteLength, hashSha256 })),
      transactionId: result.transactionId,
    };
  }

  private beginCancellation(): PbirMaterializationCancellation {
    this.cancellation?.cancel();
    const cancellation = this.dependencies.createCancellation?.() ?? {
      token: { isCancellationRequested: false },
      cancel: () => undefined,
      dispose: () => undefined,
    };
    this.cancellation = cancellation;
    return cancellation;
  }

  private requestId(kind: 'preview' | 'apply' | 'recovery'): string {
    return this.dependencies.createRequestId?.(kind) ?? `phase34-${kind}-${++this.requestSequence}`;
  }

  private transactionId(): string {
    return this.dependencies.createTransactionId?.() ?? `phase34-tx-${Date.now().toString(36)}`;
  }

  private clearPreview(): void {
    this.previewIdentity = undefined;
    this.previewInput = undefined;
  }
}
