import * as vscode from 'vscode';
import { resolveWebviewAssets } from './webviewAssets';
import { buildDesignStudioWorkspace } from '../design-studio/presentation/designStudioWorkspace';
import {
  approveRefinementProposal,
  deferRefinementProposal,
  loadRefinementState,
  rejectRefinementProposal,
} from '../design-studio/state/refinementStore';
import {
  attachAnalyzerResultsAtomically,
  completeIteration,
  loadIterationState,
  reopenIteration,
} from '../design-studio/state/iterationStore';
import {
  approveDesignBrief,
  saveDesignBriefDraft,
  submitDesignBriefForApproval,
} from '../design-studio/state/designBriefStore';
import {
  approveConceptBaseline,
  generateConceptArtifacts,
  selectConceptBaseline,
  submitConceptBaselineForApproval,
} from '../design-studio/state/conceptStore';
import {
  approveDraftArtifacts,
  generateDraftArtifacts,
  submitDraftForApproval,
} from '../design-studio/state/draftStore';
import {
  approveReviewCandidate,
  createReviewCandidate,
  submitReviewCandidateForApproval,
} from '../design-studio/state/prepareForReviewStore';
import {
  prepareAnalyzerCandidateMetadata,
  setPreviewReviewAction,
} from '../design-studio/state/previewReviewStore';
import {
  loadReviewDesignState,
  markReviewCompleted,
  recordReviewLaunch,
  syncDiscoveredAnalyzerResults,
} from '../design-studio/state/reviewDesignStore';
import {
  parseDesignStudioWebviewMessage,
  withDesignStudioEnvelope,
  type DesignStudioHostToWebviewMessagePayload,
  type DesignStudioStudioState,
} from '../design-studio/contracts/designStudioProtocol';
import type { DesignBriefDraftInput, MaterializedSurfaceCandidate } from '../design-studio/contracts/designStudioModels';
import { PBIR_COMMANDS } from '../platform/extensionIds';
import { AnalyzerBridgeService, BridgeState } from '../services/rpc/AnalyzerBridgeService';
import {
  PbirMaterializationWorkflow,
  type PbirMaterializationCancellation,
  type PbirMaterializationWorkflowState,
} from '../services/materialization/PbirMaterializationWorkflow';

declare global {
  interface Window {
    __PBIR_DESIGN_STUDIO_BOOTSTRAP__?: {
      threadId: string;
      initialStage?: string;
    };
  }
}

export class PbirDesignStudioPanel {
  private static instance: PbirDesignStudioPanel | undefined;

  private readonly panel: vscode.WebviewPanel;
  private readonly context: vscode.ExtensionContext;
  private reportPath: string;
  private readonly disposables: vscode.Disposable[] = [];
  private readonly handoffCandidatesByRequestId = new Map<string, MaterializedSurfaceCandidate>();
  private isDisposed = false;
  private isReady = false;
  private pendingMessages: DesignStudioHostToWebviewMessagePayload[] = [];
  private threadId = 'design-studio:active-report';
  private readonly materializationWorkflow: PbirMaterializationWorkflow;
  private readonly materializationInputProvider?: () => unknown | Promise<unknown>;

  static async createOrShow(
    context: vscode.ExtensionContext,
    reportPath: string,
    bridge?: AnalyzerBridgeService,
    materializationInputProvider?: () => unknown | Promise<unknown>,
    initialStage?: string,
  ): Promise<PbirDesignStudioPanel> {
    if (PbirDesignStudioPanel.instance) {
      PbirDesignStudioPanel.instance.panel.reveal(vscode.ViewColumn.Beside);
      PbirDesignStudioPanel.instance.reportPath = reportPath;
      await PbirDesignStudioPanel.instance.refresh();
      return PbirDesignStudioPanel.instance;
    }

    const panel = vscode.window.createWebviewPanel(
      'pbirDesignStudio',
      'Report Design Studio',
      vscode.ViewColumn.Beside,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [vscode.Uri.joinPath(context.extensionUri, 'webview-dist')],
      },
    );

    const instance = new PbirDesignStudioPanel(context, panel, reportPath, bridge, materializationInputProvider, initialStage);
    PbirDesignStudioPanel.instance = instance;
    context.subscriptions.push({ dispose: () => instance.dispose() });
    return instance;
  }

  private constructor(
    context: vscode.ExtensionContext,
    panel: vscode.WebviewPanel,
    reportPath: string,
    bridge?: AnalyzerBridgeService,
    materializationInputProvider?: () => unknown | Promise<unknown>,
    private readonly initialStage?: string,
  ) {
    this.context = context;
    this.panel = panel;
    this.reportPath = reportPath;
    this.materializationInputProvider = materializationInputProvider;
    this.materializationWorkflow = new PbirMaterializationWorkflow(
      {
        executeRequest: async <T>(route: 'pbir/materialization/preview' | 'pbir/materialization/apply' | 'pbir/materialization/recovery/inspect', params: unknown, token?: unknown) => {
          if (!bridge) {
            throw new Error('PBIR analyzer backend is unavailable.');
          }
          return bridge.executeRequest<T>(route, params, token as vscode.CancellationToken | undefined);
        },
      },
      {
        createCancellation: () => {
          const source = new vscode.CancellationTokenSource();
          return {
            token: source.token,
            cancel: () => (source as vscode.CancellationTokenSource & { cancel?: () => void }).cancel?.(),
            dispose: () => source.dispose(),
          } satisfies PbirMaterializationCancellation;
        },
      },
    );
    bridge?.onStateChange((state) => {
      if (state === BridgeState.ERROR || state === BridgeState.UNINITIALIZED) {
        this.materializationWorkflow.reset();
        this.postMaterializationWorkflowState();
      }
    });
    this.panel.webview.html = this.getReactHtml();

    this.panel.onDidDispose(() => this.dispose(), null, this.disposables);
    this.panel.webview.onDidReceiveMessage(
      (message) => {
        void this.handleMessage(message);
      },
      null,
      this.disposables,
    );
  }

  private async handleMessage(message: unknown): Promise<void> {
    const parsed = parseDesignStudioWebviewMessage(message);
    if (!parsed.ok) {
      void vscode.window.showWarningMessage(parsed.error);
      return;
    }

    switch (parsed.message.type) {
      case 'webviewReady':
        this.isReady = true;
        this.flushPendingMessages();
        return;
      case 'loadStudioState':
        await this.refresh();
        return;
      case 'startLocalMaterializationPreview':
        await this.startMaterializationPreview();
        return;
      case 'requestLocalMaterializationApply':
        await this.requestMaterializationApply();
        return;
      case 'inspectLocalMaterializationRecovery':
        await this.inspectMaterializationRecovery();
        return;
      case 'cancelLocalMaterialization':
        this.materializationWorkflow.cancel();
        this.postMaterializationWorkflowState();
        return;
      case 'saveArtifact': {
        if (parsed.message.artifactKind !== 'designBrief') {
          void vscode.window.showWarningMessage(`Save is not supported for ${parsed.message.artifactKind} in this MVP shell.`);
          return;
        }

        if (!isDesignBriefDraftInput(parsed.message.artifact)) {
          void vscode.window.showWarningMessage('Design Brief save payload is invalid.');
          return;
        }

        await saveDesignBriefDraft(this.context, this.threadId, parsed.message.artifact);
        await this.refresh();
        return;
      }
      case 'proposeArtifact': {
        if (parsed.message.artifactKind === 'designBrief') {
          await submitDesignBriefForApproval(this.context, this.threadId);
          await this.refresh();
          return;
        }

        if (parsed.message.artifactKind === 'reportConcept') {
          await submitConceptBaselineForApproval(this.context, this.threadId);
          await this.refresh();
          return;
        }

        if (parsed.message.artifactKind === 'draftReportArtifact') {
          await submitDraftForApproval(this.context, this.threadId);
          await this.refresh();
          return;
        }

        if (parsed.message.artifactKind === 'materializedSurfaceCandidate') {
          await submitReviewCandidateForApproval(this.context, this.threadId);
          await this.refresh();
          return;
        }

        void vscode.window.showWarningMessage(`Submit for approval is not supported for ${parsed.message.artifactKind} in this MVP shell.`);
        return;
      }
      case 'approveArtifact': {
        if (parsed.message.artifactKind === 'designBrief') {
          await approveDesignBrief(this.context, this.threadId);
          await this.refresh();
          return;
        }

        if (parsed.message.artifactKind === 'reportConcept') {
          await approveConceptBaseline(this.context, this.threadId);
          await this.refresh();
          return;
        }

        if (parsed.message.artifactKind === 'draftReportArtifact') {
          await approveDraftArtifacts(this.context, this.threadId);
          await this.refresh();
          return;
        }

        if (parsed.message.artifactKind === 'materializedSurfaceCandidate') {
          await approveReviewCandidate(this.context, this.threadId);
          await this.refresh();
          return;
        }

        void vscode.window.showWarningMessage(`Approval is not supported for ${parsed.message.artifactKind} in this MVP shell.`);
        return;
      }
      case 'createReviewCandidate':
        await createReviewCandidate(this.context, {
          threadId: this.threadId,
          reportPath: this.reportPath,
        });
        await this.refresh();
        return;
      case 'generateConcepts':
        await generateConceptArtifacts(this.context, this.threadId);
        await this.refresh();
        return;
      case 'generateDrafts':
        await generateDraftArtifacts(this.context, this.threadId);
        await this.refresh();
        return;
      case 'selectConceptBaseline':
        await selectConceptBaseline(this.context, this.threadId, parsed.message.conceptId);
        await this.refresh();
        return;
      case 'openAnalyzerHandoff': {
        const candidate = this.handoffCandidatesByRequestId.get(parsed.message.requestId);
        if (!candidate) {
          void vscode.window.showWarningMessage('No executable Design Studio handoff is available for the selected draft.');
          return;
        }

        await vscode.commands.executeCommand(PBIR_COMMANDS.openAnalyzerWorkspaceHandoff, candidate);
        await recordReviewLaunch(this.context, this.threadId, {
          requestId: parsed.message.requestId,
          candidate,
          analyzerId: candidate.analyzerHandoff.metadata.targetAnalyzer,
          analyzerProfileId: candidate.analyzerHandoff.metadata.targetAnalyzerProfile,
        });
        await this.refresh();
        return;
      }
      case 'markReviewCompleted': {
        const candidate = this.handoffCandidatesByRequestId.get(parsed.message.requestId);
        if (!candidate) {
          void vscode.window.showWarningMessage('No launched Design Studio review is available for the selected draft.');
          return;
        }

        await markReviewCompleted(this.context, this.threadId, {
          requestId: parsed.message.requestId,
          candidate,
        });
        await this.refresh();
        return;
      }
      case 'attachAnalyzerResults': {
        const candidate = this.handoffCandidatesByRequestId.get(parsed.message.requestId);
        if (!candidate) {
          void vscode.window.showWarningMessage('No completed Design Studio review is available for result attachment.');
          return;
        }

        const reviewState = await loadReviewDesignState(this.context, this.threadId);
        const availableResults = reviewState?.currentReview?.availableResults ?? [];
        if (availableResults.length === 0) {
          void vscode.window.showWarningMessage('No analyzer results are available to attach for this review.');
          return;
        }

        const attached = await attachAnalyzerResultsAtomically(this.context, this.threadId, {
          requestId: parsed.message.requestId,
          candidate,
        });

        if (!attached.ok) {
          void vscode.window.showWarningMessage(attached.error);
          await this.refresh();
          return;
        }

        await this.refresh();
        return;
      }
      case 'markPreviewReviewed':
        await setPreviewReviewAction(this.context, this.threadId, {
          previewReviewId: parsed.message.previewReviewId,
          reviewerAction: 'markedReviewed',
          reviewerNotes: parsed.message.reviewerNotes ?? '',
          reviewerId: 'user',
        });
        await this.refresh();
        return;
      case 'requestPreviewRevision':
        await setPreviewReviewAction(this.context, this.threadId, {
          previewReviewId: parsed.message.previewReviewId,
          reviewerAction: 'revisionRequested',
          reviewerNotes: parsed.message.reviewerNotes ?? '',
          reviewerId: 'user',
        });
        await this.refresh();
        return;
      case 'deferPreviewReview':
        await setPreviewReviewAction(this.context, this.threadId, {
          previewReviewId: parsed.message.previewReviewId,
          reviewerAction: 'deferred',
          reviewerNotes: parsed.message.reviewerNotes ?? '',
          reviewerId: 'user',
        });
        await this.refresh();
        return;
      case 'prepareAnalyzerCandidateMetadata':
        await prepareAnalyzerCandidateMetadata(this.context, this.threadId, {
          previewReviewId: parsed.message.previewReviewId,
          reviewerNotes: parsed.message.reviewerNotes ?? '',
          reviewerId: 'user',
        });
        await this.refresh();
        return;
      case 'requestExecutionReadiness': {
        const workspaceState = await buildDesignStudioWorkspace(this.context, this.reportPath);
        if (parsed.message.threadId !== workspaceState.threadId) {
          void vscode.window.showWarningMessage('Execution readiness request does not match the active Design Studio thread.');
          return;
        }

        if (!workspaceState.workspace.executionReadiness) {
          void vscode.window.showWarningMessage('Execution readiness is not available until preview review metadata exists.');
          return;
        }

        this.postMessage({
          type: 'executionReadinessUpdated',
          readiness: workspaceState.workspace.executionReadiness,
        });
        return;
      }
      case 'completeIteration':
        await completeIteration(this.context, this.threadId);
        await this.refresh();
        return;
      case 'reopenIteration':
        await reopenIteration(this.context, this.threadId);
        await this.refresh();
        return;
      case 'setRefinementProposalState': {
        switch (parsed.message.action) {
          case 'approve':
            await approveRefinementProposal(this.context, this.threadId, parsed.message.proposalId);
            break;
          case 'reject':
            await rejectRefinementProposal(this.context, this.threadId, parsed.message.proposalId);
            break;
          case 'defer':
            await deferRefinementProposal(this.context, this.threadId, parsed.message.proposalId);
            break;
        }

        await this.refresh();
        return;
      }
      default:
        return;
    }
  }

  async refresh(): Promise<void> {
    let workspaceState = await buildDesignStudioWorkspace(this.context, this.reportPath);
    if (workspaceState.workspace.reviewDesign?.candidateId && workspaceState.workspace.reviewDesign.requestId) {
      const candidate = workspaceState.handoffCandidatesByRequestId.get(workspaceState.workspace.reviewDesign.requestId);
      if (candidate) {
        await syncDiscoveredAnalyzerResults(this.context, workspaceState.threadId, {
          requestId: workspaceState.workspace.reviewDesign.requestId,
          candidate,
        });
        workspaceState = await buildDesignStudioWorkspace(this.context, this.reportPath);
      }
    }
    const [iterationState, refinementState] = await Promise.all([
      loadIterationState(this.context, workspaceState.threadId),
      loadRefinementState(this.context, workspaceState.threadId),
    ]);
    this.threadId = workspaceState.threadId;
    this.handoffCandidatesByRequestId.clear();
    for (const [requestId, candidate] of workspaceState.handoffCandidatesByRequestId.entries()) {
      this.handoffCandidatesByRequestId.set(requestId, candidate);
    }

    this.postMessage({
      type: 'studioState',
      state: {
        threadId: workspaceState.threadId,
        currentBrief: workspaceState.currentBrief,
        iterationHistory: iterationState?.iterations ?? [],
        pendingRefinementProposals: refinementState?.proposals ?? [],
        workspace: workspaceState.workspace,
      },
    });
    this.postMaterializationWorkflowState();
  }

  private postMessage(
    message:
      | { type: 'studioState'; state: DesignStudioStudioState }
      | { type: 'executionReadinessUpdated'; readiness: NonNullable<DesignStudioStudioState['workspace']>['executionReadiness'] }
      | { type: 'materializationWorkflowUpdated'; workflow: ReturnType<PbirDesignStudioPanel['toMaterializationWorkflowViewModel']> },
  ): void {
    const wrapped = withDesignStudioEnvelope(message) as DesignStudioHostToWebviewMessagePayload;

    if (!this.isReady) {
      this.pendingMessages.push(wrapped);
      return;
    }

    void this.panel.webview.postMessage(wrapped);
  }

  private async startMaterializationPreview(): Promise<void> {
    const input = await this.materializationInputProvider?.();
    if (input === undefined) {
      void vscode.window.showWarningMessage('Local PBIR preview is unavailable until an approved canonical PBIR input is ready.');
      return;
    }
    await vscode.window.withProgress(
      { location: vscode.ProgressLocation.Notification, title: 'Preparing local PBIR preview', cancellable: true },
      async (_progress, token) => {
        const cancellationListener = (token as vscode.CancellationToken).onCancellationRequested?.(() => {
          this.materializationWorkflow.cancel();
        });
        try {
          const state = await this.materializationWorkflow.preview(input);
          if (token.isCancellationRequested) {
            this.materializationWorkflow.cancel();
          }
          this.postMaterializationWorkflowState(state);
        } finally {
          cancellationListener?.dispose();
        }
      },
    );
  }

  private async requestMaterializationApply(): Promise<void> {
    const confirmation = await vscode.window.showWarningMessage(
      'Apply this exact validated local PBIR preview? This writes only through the local materialization transaction boundary.',
      { modal: true },
      'Apply Preview',
    );
    if (confirmation !== 'Apply Preview') {
      return;
    }
    await vscode.window.withProgress(
      { location: vscode.ProgressLocation.Notification, title: 'Applying local PBIR preview', cancellable: true },
      async (_progress, token) => {
        const cancellationListener = (token as vscode.CancellationToken).onCancellationRequested?.(() => {
          this.materializationWorkflow.cancel();
        });
        try {
          const result = await this.materializationWorkflow.apply(true);
          this.postMaterializationWorkflowState(result.state);
        } finally {
          cancellationListener?.dispose();
        }
      },
    );
  }

  private async inspectMaterializationRecovery(): Promise<void> {
    const input = await this.materializationInputProvider?.();
    const previewRequestId = this.materializationWorkflow.getPreviewRequestId();
    if (input === undefined || !previewRequestId) {
      void vscode.window.showWarningMessage('Recovery inspection requires a fresh preview request.');
      return;
    }
    await vscode.window.withProgress(
      { location: vscode.ProgressLocation.Notification, title: 'Inspecting local PBIR recovery state', cancellable: true },
      async (_progress, token) => {
        const cancellationListener = (token as vscode.CancellationToken).onCancellationRequested?.(() => {
          this.materializationWorkflow.cancel();
        });
        try {
          const state = await this.materializationWorkflow.inspectRecovery(input, previewRequestId);
          this.postMaterializationWorkflowState(state);
        } finally {
          cancellationListener?.dispose();
        }
      },
    );
  }

  private postMaterializationWorkflowState(state = this.materializationWorkflow.getState()): void {
    this.postMessage({
      type: 'materializationWorkflowUpdated',
      workflow: this.toMaterializationWorkflowViewModel(state),
    });
  }

  private toMaterializationWorkflowViewModel(state: PbirMaterializationWorkflowState) {
    return {
      status: state.status,
      outcome: state.outcome,
      summary: state.summary,
      diagnostics: state.diagnostics,
      writtenFiles: state.writtenFiles,
      transactionId: state.transactionId,
    };
  }

  private flushPendingMessages(): void {
    while (this.pendingMessages.length > 0) {
      const message = this.pendingMessages.shift();
      if (message) {
        void this.panel.webview.postMessage(message);
      }
    }
  }

  private getReactHtml(): string {
    const assets = resolveWebviewAssets({
      webview: this.panel.webview,
      extensionUri: this.context.extensionUri,
      entryFile: 'design-studio/index.tsx',
      fallbackScriptFile: 'design-studio.js',
      fallbackStyleFile: 'design-studio.css',
      manifestFileName: 'manifest.design-studio.json',
    });

    if (assets.missingAssets) {
      return '<html><body><p>Design Studio build assets are missing. Run npm run build.</p></body></html>';
    }

    const nonce = `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    const styleLinks = assets.styleUris
      .map((styleUri) => `<link rel="stylesheet" href="${String(styleUri)}">`)
      .join('\n');

    return `<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src ${this.panel.webview.cspSource} 'unsafe-inline'; script-src 'nonce-${nonce}' ${this.panel.webview.cspSource};">
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    ${styleLinks}
    <title>Report Design Studio</title>
  </head>
  <body>
    <div id="root"></div>
    <script nonce="${nonce}">
      window.__PBIR_DESIGN_STUDIO_BOOTSTRAP__ = ${JSON.stringify({ threadId: this.threadId, initialStage: this.initialStage })};
    </script>
    <script nonce="${nonce}" src="${String(assets.scriptUri)}"></script>
  </body>
</html>`;
  }

  dispose(): void {
    if (this.isDisposed) {
      return;
    }

    this.isDisposed = true;
    this.materializationWorkflow.reset();
    if (PbirDesignStudioPanel.instance === this) {
      PbirDesignStudioPanel.instance = undefined;
    }

    while (this.disposables.length > 0) {
      this.disposables.pop()?.dispose();
    }
  }
}

function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((entry) => typeof entry === 'string');
}

function isReportType(value: unknown): value is DesignBriefDraftInput['reportType'] {
  return value === 'dashboard'
    || value === 'scorecard'
    || value === 'narrativeBriefing'
    || value === 'operationalMonitoring';
}

function isOptionalString(value: unknown): value is string | undefined {
  return value === undefined || typeof value === 'string';
}

function isOptionalStringArray(value: unknown): value is string[] | undefined {
  return value === undefined || isStringArray(value);
}

function isDesignBriefDraftInput(value: unknown): value is DesignBriefDraftInput {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return typeof candidate.audience === 'string'
    && typeof candidate.businessObjective === 'string'
    && isStringArray(candidate.keyDecisions)
    && isStringArray(candidate.primaryKpis)
    && isStringArray(candidate.dimensions)
    && typeof candidate.intendedStory === 'string'
    && isStringArray(candidate.successCriteria)
    && isReportType(candidate.reportType)
    && typeof candidate.navigationExpectations === 'string'
    && isOptionalString(candidate.consumptionContext)
    && isOptionalString(candidate.decisionCadence)
    && isOptionalStringArray(candidate.narrativeRisksOrConstraints)
    && isOptionalStringArray(candidate.requiredEvidenceDomains)
    && isOptionalString(candidate.targetAnalyzableSurfaceFamily);
}
