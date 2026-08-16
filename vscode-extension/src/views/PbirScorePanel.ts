import * as path from 'path';
import * as vscode from 'vscode';
import { loadDesignAnalyzerConfig } from '../analyzer/config/store';
import type { DesignAnalyzerConfig } from '../analyzer/config/types';
import type {
  FixApplySessionRecord,
  FixOpportunity,
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownRenderOptions,
  ScorePanelHostToWebviewMessagePayload,
  ScorePanelState,
  ScorePanelWebviewToHostMessagePayload,
  ScoreResult,
  RenderedReviewPanelState,
} from '../analyzer/contracts/scorePanel';
import type { RenderedReviewStatus } from '../analyzer/renderedReview/types';
import type { VisualAuditSession } from '../analyzer/audit/types';
import type { VisualAuditProvider } from '../analyzer/audit/providers/VisualAuditProvider';
import { createActiveProvider } from '../analyzer/audit/providers/providerSetup';
import { reviewFabricAppSurface } from '../analyzer/fabric/review/fabricAppReviewAnalyzer';
import { enrichFixPlanWithAdvisoryContent } from '../analyzer/proposalEnrichment/proposalEnrichmentOrchestrator';
import { detectAnalyzableSurface } from '../analyzer/surfaces/discovery';
import { telemetry, bucketScore } from '../telemetry/reporter';
import { AnalyzerBridgeService } from '../services/rpc/AnalyzerBridgeService';
import type { PbirAuthoringResponse } from '../services/rpc/PbirAuthoringWorkflow';
import { getDiagnosticsOutputChannel } from '../platform/outputChannels';
import {
  getRecordedBackendIssue,
  getRecordedBackendLaunchDiagnostics,
} from '../languageServer/analyzerBackendClient';
import { resolveWebviewAssets } from './webviewAssets';
import { buildFixWorkflowPayload, normalizeScoreResultPayload } from './scoreResultPayload';
import { loadIntentFeedbackSession, saveIntentFeedbackSession, upsertIntentFeedback } from '../analyzer/intentFeedback/store';
import {
  buildReviewPacketPreviewHtml,
  normalizeReviewPacketPreviewOptions,
} from '../analyzer/score/reviewPacketPreview';
import {
  buildScorePanelState,
  withScorePanelEnvelope,
} from './scorePanelProtocol';
import {
  loadReviewPacketPreviewOptions,
  saveReviewPacketPreviewOptions,
} from '../analyzer/score/reviewPacketPreviewStore';
import {
  buildReviewWorkflowExportData,
} from '../analyzer/score/reviewWorkflowExport';
import { attachNavigationTargets } from '../analyzer/score/navigationTargets';
import { buildStoryAssessmentReportSnapshot, compareStoryAssessmentSnapshots } from '../analyzer/score/storyAssessmentSnapshot';
import { loadStoryAssessmentSnapshot, saveStoryAssessmentSnapshot } from '../analyzer/score/storyAssessmentSnapshotStore';
import { buildScoreDeterminismDiagnostics } from '../analyzer/score/scoreDiagnostics';
import type { AnalyzerWorkspaceHandoffPayload } from '../design-studio/contracts/designStudioModels';
import { createScorePanelStateService } from './scorePanelStateService';
import { createScorePanelMessageRouter } from './scorePanelMessageRouter';
import { createScorePanelAuditWorkflowService } from './scorePanelAuditWorkflowService';
import { createScorePanelExportWorkflowService } from './scorePanelExportWorkflowService';
import { createScorePanelFixWorkflowService } from './scorePanelFixWorkflowService';
import { recordAnalyzerWorkspaceReturn } from '../design-studio/state/analyzerWorkspaceReturnStore';
import { getRenderedReviewSettings } from '../platform/settings';
import { buildRenderedReviewChecklist, updateRenderedReviewItem } from '../analyzer/renderedReview/reviewModel';

// Scoring reads a report; it never needs the round-trip-safe Import/Mutate snapshot contract, whose
// authoring envelope only accepts an exact pinned schema version and hard-rejects any report exported
// by a Power BI Desktop version outside that pin. Analyze's reportDirectory input is the same direct
// path used by the legacy model/pbir/scoreReport route and by Design Studio's Mutate before/after
// scoring — it exists precisely so read-only scoring never has to pass through that envelope.
export function buildPbirOptimizationAnalyzeRequest(
  reportPath: string,
  config: DesignAnalyzerConfig,
  pageName?: string,
): Record<string, unknown> {
  return {
    schemaVersion: 'pbir-authoring-rpc/v1',
    operation: 'analyze',
    analyze: {
      reportDirectory: reportPath,
      config,
      ...(pageName ? { pageName } : {}),
    },
  };
}

type PbirOptimizationScoringBridge = Pick<AnalyzerBridgeService, 'executeAuthoringRequest'>;

export async function executePbirOptimizationScore(
  bridge: PbirOptimizationScoringBridge,
  reportPath: string,
  config: DesignAnalyzerConfig,
  pageName?: string,
  log: (message: string) => void = () => undefined,
): Promise<PbirAuthoringResponse> {
  const response = await bridge.executeAuthoringRequest<PbirAuthoringResponse>(
    buildPbirOptimizationAnalyzeRequest(reportPath, config, pageName),
  );
  logOptimizationRequest(log);
  return response;
}

function logOptimizationRequest(log: (message: string) => void): void {
  log('[OptimizationScoring] rpcRoute=pbir/authoring schemaVersion=pbir-authoring-rpc/v1 operation=Analyze sourceKind=ReportReference reportPathPresent=true snapshotHandlePresent=false artifactHandlePresent=false authoringRequestPresent=false selectedReportIdentityPresent=true');
}

export class PbirScorePanel {
  private static instance: PbirScorePanel | undefined;
  private static readonly diagnosticsOutput = getDiagnosticsOutputChannel();

  private readonly panel: vscode.WebviewPanel;
  private readonly bridge: AnalyzerBridgeService | undefined;
  private readonly context: vscode.ExtensionContext;
  private readonly disposables: vscode.Disposable[] = [];
  private readonly scoreState = createScorePanelStateService();
  private readonly messageRouter;
  private readonly auditWorkflow;
  private readonly exportWorkflow;
  private readonly fixWorkflow;
  private isDisposed = false;
  private isReady = false;
  private reportPath: string;
  private pageName: string | undefined;
  private auditSession: VisualAuditSession | undefined;
  private auditProvider: VisualAuditProvider;
  private readonly fixOpportunityHistory = new Map<string, FixOpportunity>();
  private selectedFixOpportunityIds: string[] = [];
  private fixSelectionApprovalState: NonNullable<ScorePanelState['fixSelection']>['approvalState'] = 'NeedsPreview';
  private fixApplySessions: FixApplySessionRecord[] = [];
  private fixWorkflowMessage: string | undefined;

  static async createOrShow(
    context: vscode.ExtensionContext,
    bridge: AnalyzerBridgeService | undefined,
    reportPath: string,
    pageName?: string,
  ): Promise<PbirScorePanel> {
    if (PbirScorePanel.instance) {
      PbirScorePanel.instance.panel.reveal(vscode.ViewColumn.Beside);
      PbirScorePanel.instance.reportPath = reportPath;
      PbirScorePanel.instance.pageName = pageName;
      PbirScorePanel.instance.scoreState.setCurrentHandoffPayload(undefined);
      await PbirScorePanel.instance.refresh();
      return PbirScorePanel.instance;
    }

    const panel = vscode.window.createWebviewPanel(
      'pbirScorePanel',
      'PBIR Optimization Report',
      vscode.ViewColumn.Beside,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [vscode.Uri.joinPath(context.extensionUri, 'webview-dist')],
      },
    );

    const instance = new PbirScorePanel(context, panel, bridge, reportPath, pageName);
    PbirScorePanel.instance = instance;
    context.subscriptions.push({ dispose: () => instance.dispose() });
    await instance.refresh();
    return instance;
  }

  static async createOrShowHandoffShell(
    context: vscode.ExtensionContext,
    bridge: AnalyzerBridgeService | undefined,
    payload: AnalyzerWorkspaceHandoffPayload,
  ): Promise<PbirScorePanel> {
    const reportPath = payload.handoffReference.kind === 'repositoryBackedSurface'
      ? payload.handoffReference.repositoryPath
      : payload.handoffReference.kind === 'snapshotBackedSurface'
        ? payload.handoffReference.sourceLocation
        : payload.handoffReference.kind === 'syntheticPreview'
          ? payload.handoffReference.previewSourceLocation
          : payload.candidateId;

    if (PbirScorePanel.instance) {
      PbirScorePanel.instance.panel.reveal(vscode.ViewColumn.Beside);
      PbirScorePanel.instance.reportPath = reportPath;
      PbirScorePanel.instance.pageName = undefined;
      PbirScorePanel.instance.presentHandoffShell(payload);
      return PbirScorePanel.instance;
    }

    const panel = vscode.window.createWebviewPanel(
      'pbirScorePanel',
      'PBIR Optimization Report',
      vscode.ViewColumn.Beside,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [vscode.Uri.joinPath(context.extensionUri, 'webview-dist')],
      },
    );

    const instance = new PbirScorePanel(context, panel, bridge, reportPath);
    PbirScorePanel.instance = instance;
    context.subscriptions.push({ dispose: () => instance.dispose() });
    instance.presentHandoffShell(payload);
    return instance;
  }

  static async copyCurrentScoreDiagnostics(): Promise<boolean> {
    const lastScoreDiagnosticsJson = PbirScorePanel.instance?.scoreState.getLastScoreDiagnosticsJson();
    if (!lastScoreDiagnosticsJson) {
      return false;
    }

    await vscode.env.clipboard.writeText(lastScoreDiagnosticsJson);
    PbirScorePanel.diagnosticsOutput.show(true);
    return true;
  }

  async requestScreenshotUpload(): Promise<void> {
    await this.auditWorkflow.uploadScreenshots();
  }

  private constructor(
    context: vscode.ExtensionContext,
    panel: vscode.WebviewPanel,
    bridge: AnalyzerBridgeService | undefined,
    reportPath: string,
    pageName?: string,
  ) {
    this.context = context;
    this.panel = panel;
    this.bridge = bridge;
    this.reportPath = reportPath;
    this.pageName = pageName;
    this.auditProvider = createActiveProvider(context);
    this.messageRouter = createScorePanelMessageRouter({
      getPageCount: () => this.pageNamesFromResult().length,
      onReady: async () => {
        this.isReady = true;
        this.flushPendingMessages();
      },
      onRefresh: () => this.refresh(),
      onSelectTab: async (pageIndex) => {
        this.scoreState.setSelectedPageIndex(pageIndex, this.pageNamesFromResult().length);
      },
      onSetIntentFeedback: (message) => this.handleSetIntentFeedback(message),
      onUploadScreenshots: () => this.auditWorkflow.uploadScreenshots(),
      onAttachScreenshot: async (pageNameValue) => { await this.auditWorkflow.attachScreenshot(pageNameValue); },
      onRemoveScreenshot: (captureId) => this.auditWorkflow.removeScreenshot(captureId),
      onAssignCapture: (captureId, targetPageName) => this.auditWorkflow.assignCapture(captureId, targetPageName),
      onAnalyzeCapture: (captureId, pageNameValue) => this.auditWorkflow.analyzeCapture(captureId, pageNameValue),
      onExportReviewWorkflow: () => this.exportWorkflow.exportReviewWorkflow(),
      onSetReviewPacketPreviewProfile: (profile) => this.handleSetReviewPacketPreviewProfile(profile),
      onSetReviewPacketPreviewTemplateVariant: (templateVariant) => this.handleSetReviewPacketPreviewTemplateVariant(templateVariant),
      onOpenReviewPacketPreview: () => this.exportWorkflow.openReviewPacketPreview(),
      onSetRenderedReviewStatus: (itemId, status) => this.setRenderedReviewStatus(itemId, status),
      onSetRenderedReviewNote: (itemId, note) => this.setRenderedReviewNote(itemId, note),
      onAttachRenderedScreenshot: (itemId) => this.attachRenderedScreenshot(itemId),
      onToggleFixOpportunitySelection: (opportunityId) => this.fixWorkflow.toggleFixOpportunitySelection(opportunityId),
      onPreviewSelectedFixOpportunities: () => this.fixWorkflow.previewSelectedFixOpportunities(),
      onApproveSelectedFixOpportunities: () => this.fixWorkflow.approveSelectedFixOpportunities(),
      onApplySelectedFixOpportunities: () => this.fixWorkflow.applySelectedFixOpportunities(),
      onRollbackFixSession: (sessionId) => this.fixWorkflow.rollbackFixSession(sessionId),
      onRegenerateFixOpportunities: (opportunityIds) => this.fixWorkflow.regenerateFixOpportunities(opportunityIds),
      onApproveFixOpportunity: (opportunityId) => this.fixWorkflow.approveFixOpportunity(opportunityId),
      onApplyFixOpportunity: (opportunityId) => this.fixWorkflow.applyFixOpportunity(opportunityId),
      onRollbackFixOpportunity: (opportunityId) => this.fixWorkflow.rollbackFixOpportunity(opportunityId),
      onOpenSettings: async () => {
        await vscode.commands.executeCommand('pbirAnalyzer.configureScoring');
      },
    });
    this.auditWorkflow = createScorePanelAuditWorkflowService({
      context,
      getReportPath: () => this.reportPath,
      getCurrentResult: () => this.scoreState.getCurrentResult(),
      getAuditProvider: () => this.auditProvider,
      getAuditSession: () => this.auditSession,
      setAuditSession: (session) => {
        this.auditSession = session;
      },
      postMessage: (message) => this.postMessage(message),
    });
    this.exportWorkflow = createScorePanelExportWorkflowService({
      context,
      getReportPath: () => this.reportPath,
      getCurrentResult: () => this.scoreState.getCurrentResult(),
      getReviewPacketPreviewOptions: () => this.scoreState.getReviewPacketPreviewOptions(),
      getRenderedReview: () => this.scoreState.getRenderedReview(),
    });
    this.fixWorkflow = createScorePanelFixWorkflowService({
      getCurrentResult: () => this.scoreState.getCurrentResult(),
      getFixOpportunityHistory: () => this.fixOpportunityHistory,
      getSelectedFixOpportunityIds: () => this.selectedFixOpportunityIds,
      setSelectedFixOpportunityIds: (ids) => {
        this.selectedFixOpportunityIds = ids;
      },
      getFixSelectionApprovalState: () => this.fixSelectionApprovalState,
      setFixSelectionApprovalState: (state) => {
        this.fixSelectionApprovalState = state;
      },
      getFixApplySessions: () => this.fixApplySessions,
      setFixApplySessions: (sessions) => {
        this.fixApplySessions = sessions;
      },
      getFixWorkflowMessage: () => this.fixWorkflowMessage,
      setFixWorkflowMessage: (message) => {
        this.fixWorkflowMessage = message;
      },
      refresh: () => this.refresh(),
      postCurrentScoreState: () => this.postCurrentScoreState(),
    });
    this.panel.webview.html = this.getReactHtml();

    this.panel.onDidDispose(() => this.dispose(), null, this.disposables);
    this.panel.webview.onDidReceiveMessage(
      (message) => this.handleMessage(message),
      null,
      this.disposables,
    );
  }

  private async handleMessage(message: unknown): Promise<void> {
    // webview.onDidReceiveMessage is fire-and-forget in VS Code — nothing awaits this callback,
    // so an uncaught exception anywhere in the router chain becomes an unhandled promise rejection
    // that only ever reaches the extension host's own console, never the user. Every action routed
    // through here (navigate-to-target, attach screenshot, fix workflow, etc.) must fail loudly
    // instead of silently doing nothing on click.
    try {
      await this.messageRouter.route(message);
    } catch (error) {
      const detail = error instanceof Error ? error.message : String(error);
      PbirScorePanel.diagnosticsOutput.appendLine(`[MessageRouter] Unhandled error routing webview message: ${detail}`);
      void vscode.window.showErrorMessage(`PBIR Design Analyzer: the action failed unexpectedly. ${detail}`);
    }
  }

  private buildPresentationResult(result: ScoreResult): ScoreResult {
    const currentOpportunities = (result.fixOpportunities ?? []).map((item) => {
      const history = this.fixOpportunityHistory.get(item.id);
      return history
        ? {
            ...item,
            state: history.state,
            outcome: history.outcome,
          }
        : item;
    });

    const merged = [...currentOpportunities];
    for (const history of this.fixOpportunityHistory.values()) {
      if (!merged.some((item) => item.id === history.id) && history.state !== 'Previewed') {
        merged.push(history);
      }
    }

    return attachNavigationTargets({
      ...result,
      fixOpportunities: merged,
    });
  }

  private buildFixSelectionState(result: ScoreResult): ScorePanelState['fixSelection'] {
    return buildFixWorkflowPayload({
      opportunities: result.fixOpportunities ?? [],
      selectedOpportunityIds: this.selectedFixOpportunityIds,
      approvalState: this.fixSelectionApprovalState,
      message: this.fixWorkflowMessage,
      fixApplySessions: this.fixApplySessions,
    }).fixSelection;
  }

  private pageNamesFromResult(): string[] {
    const result = this.scoreState.getCurrentResult();
    if (!result) return [];
    if (result.pageScores && result.pageScores.length > 0) {
      return result.pageScores.map((p) => p.pageName);
    }
    if (result.scoredPageName) return [result.scoredPageName];
    return [];
  }

  private async loadAuditSession(): Promise<VisualAuditSession> {
    return this.auditWorkflow.loadAuditSession();
  }

  private async handleSetIntentFeedback(
    message: Extract<ScorePanelWebviewToHostMessagePayload, { type: 'setIntentFeedback' }>,
  ): Promise<void> {
    const session = await loadIntentFeedbackSession(this.context, this.reportPath);
    const analyzerVersion = String(this.context.extension.packageJSON?.version ?? 'unknown');
    const currentResult = this.scoreState.getCurrentResult();
    const savedConfig = this.scoreState.getSavedConfig();
    const reportSessionId = `${session.reportKey}:${currentResult?.scoredAt ?? new Date().toISOString()}`;

    upsertIntentFeedback(session, {
      pageName: message.pageName,
      inferredIntent: message.inferredIntent,
      storyArchetype: message.storyArchetype,
      userConfirmation: message.userConfirmation,
      note: message.note?.trim() ? message.note.trim() : undefined,
      timestamp: new Date().toISOString(),
      analyzerVersion,
      reportSessionId,
      inferenceConfidence: message.inferenceConfidence,
    });

    await saveIntentFeedbackSession(this.context, session);

    if (currentResult && savedConfig) {
      const reviewPacketPreview = buildReviewWorkflowExportData(currentResult, session.entries, undefined, this.scoreState.getRenderedReview());
      this.postScoreState(savedConfig, currentResult, session.entries, reviewPacketPreview);
    }
  }

  private async handleSetReviewPacketPreviewProfile(
    profile: ReviewWorkflowExportProfile,
  ): Promise<void> {
    this.scoreState.setReviewPacketPreviewOptions(normalizeReviewPacketPreviewOptions({
      ...this.scoreState.getReviewPacketPreviewOptions(),
      profile,
    }));
    await saveReviewPacketPreviewOptions(this.context, this.reportPath, this.scoreState.getReviewPacketPreviewOptions());
    await this.postCurrentScoreState();
  }

  private async handleSetReviewPacketPreviewTemplateVariant(
    templateVariant: ReviewWorkflowMarkdownRenderOptions['templateVariant'],
  ): Promise<void> {
    this.scoreState.setReviewPacketPreviewOptions(normalizeReviewPacketPreviewOptions({
      ...this.scoreState.getReviewPacketPreviewOptions(),
      templateVariant,
    }));
    await saveReviewPacketPreviewOptions(this.context, this.reportPath, this.scoreState.getReviewPacketPreviewOptions());
    await this.postCurrentScoreState();
  }

  async exportReviewWorkflow(): Promise<void> {
    await this.exportWorkflow.exportReviewWorkflow();
  }

  private async postCurrentScoreState(): Promise<void> {
    const currentResult = this.scoreState.getCurrentResult();
    const savedConfig = this.scoreState.getSavedConfig();
    if (!currentResult || !savedConfig) {
      return;
    }

    const intentFeedbackSession = await loadIntentFeedbackSession(this.context, this.reportPath);
    const reviewPacketPreview = buildReviewWorkflowExportData(
      currentResult,
      intentFeedbackSession.entries,
      undefined,
      this.scoreState.getRenderedReview(),
    );

    this.postScoreState(savedConfig, currentResult, intentFeedbackSession.entries, reviewPacketPreview);
  }

  private renderedReviewState(result: ScoreResult): RenderedReviewPanelState {
    const existing = this.scoreState.getRenderedReview();
    const checklist = buildRenderedReviewChecklist(result.normalizedFindings ?? []).map((item) => {
      const prior = existing?.checklist.find((candidate) => candidate.id === item.id);
      return prior ? { ...item, ...prior, findingIds: item.findingIds, pageNames: item.pageNames } : item;
    });
    const state: RenderedReviewPanelState = {
      enabled: getRenderedReviewSettings().enabled && getRenderedReviewSettings().showChecklist && checklist.length > 0,
      checklist,
      mutationFollowUp: this.fixApplySessions.length > 0
        ? 'Review the rendered report after mutation and attach a screenshot above to confirm the visual outcome.'
        : existing?.mutationFollowUp,
    };
    this.scoreState.setRenderedReview(state);
    return state;
  }

  private async setRenderedReviewStatus(itemId: string, status: RenderedReviewStatus): Promise<void> {
    const current = this.scoreState.getRenderedReview();
    if (!current) return;
    this.scoreState.setRenderedReview({
      ...current,
      checklist: current.checklist.map((item) => item.id === itemId ? updateRenderedReviewItem(item, { status }) : item),
    });
    await this.postCurrentScoreState();
  }

  private async setRenderedReviewNote(itemId: string, note: string): Promise<void> {
    const current = this.scoreState.getRenderedReview();
    if (!current) return;
    this.scoreState.setRenderedReview({
      ...current,
      checklist: current.checklist.map((item) => item.id === itemId ? updateRenderedReviewItem(item, { reviewerNote: note }) : item),
    });
    await this.postCurrentScoreState();
  }

  private async attachRenderedScreenshot(itemId: string): Promise<void> {
    const current = this.scoreState.getRenderedReview();
    const item = current?.checklist.find((candidate) => candidate.id === itemId);
    const pageName = item?.pageNames[0];
    if (!current || !item || !pageName) return;
    const capture = await this.auditWorkflow.attachScreenshot(pageName);
    if (!capture) return;
    this.scoreState.setRenderedReview({
      ...current,
      checklist: current.checklist.map((candidate) => candidate.id === itemId
        ? updateRenderedReviewItem(candidate, {
            screenshot: {
              report: this.reportPath,
              page: pageName,
              timestamp: capture.capturedAt,
              provider: 'Manual attachment',
              fileReference: capture.storedPath,
            },
          })
        : candidate),
    });
    await this.postCurrentScoreState();
  }

  private postScoreState(
    config: DesignAnalyzerConfig,
    result: ScoreResult,
    intentFeedback: ScorePanelState['intentFeedback'],
    reviewPacketPreview: ReturnType<typeof buildReviewWorkflowExportData>,
  ): void {
    const presentationResult = this.buildPresentationResult(result);
    const renderedReview = this.renderedReviewState(presentationResult);
    const reviewPacketWithRenderedReview = { ...reviewPacketPreview, renderedReview };
    const fixWorkflow = buildFixWorkflowPayload({
      opportunities: presentationResult.fixOpportunities ?? [],
      selectedOpportunityIds: this.selectedFixOpportunityIds,
      approvalState: this.fixSelectionApprovalState,
      message: this.fixWorkflowMessage,
      fixApplySessions: this.fixApplySessions,
    });
    this.postMessage({
      type: 'scoreState',
      state: buildScorePanelState({
        config,
        result: presentationResult,
        selectedPageIndex: this.scoreState.getSelectedPageIndex(),
        intentFeedback,
        storyAssessmentCurrentSnapshot: this.scoreState.getStoryAssessmentCurrentSnapshot(),
        storyAssessmentDiffByPage: this.scoreState.getStoryAssessmentDiffByPage(),
        storyAssessmentLastComparedAt: this.scoreState.getStoryAssessmentLastComparedAt(),
        fixSelection: fixWorkflow.fixSelection,
        fixApplySessions: fixWorkflow.fixApplySessions,
        reviewPacketPreview: reviewPacketWithRenderedReview,
        reviewPacketPreviewHtml: buildReviewPacketPreviewHtml(
          reviewPacketPreview,
          this.reportPath,
          this.scoreState.getReviewPacketPreviewOptions(),
        ),
        reviewPacketPreviewProfile: this.scoreState.getReviewPacketPreviewOptions().profile,
        reviewPacketPreviewTemplateVariant: this.scoreState.getReviewPacketPreviewOptions().templateVariant,
        renderedReview,
      }),
    });
  }

  private async postAuditState(): Promise<void> {
    await this.auditWorkflow.postAuditState();
  }

  private async refreshStoryAssessmentState(result: ScoreResult): Promise<void> {
    const currentSnapshot = buildStoryAssessmentReportSnapshot(result);
    if (currentSnapshot.pages.length === 0) {
      this.scoreState.setStoryAssessmentCurrentSnapshot(undefined);
      this.scoreState.setStoryAssessmentDiffByPage(undefined);
      this.scoreState.setStoryAssessmentLastComparedAt(undefined);
      return;
    }

    const priorSnapshot = await loadStoryAssessmentSnapshot(this.context, this.reportPath);
    const diff = priorSnapshot
      ? compareStoryAssessmentSnapshots(priorSnapshot, currentSnapshot)
      : undefined;

    this.scoreState.setStoryAssessmentCurrentSnapshot(currentSnapshot);
    this.scoreState.setStoryAssessmentDiffByPage(diff?.byPage);
    this.scoreState.setStoryAssessmentLastComparedAt(priorSnapshot ? result.scoredAt : undefined);
    await saveStoryAssessmentSnapshot(this.context, this.reportPath, currentSnapshot);
  }

  private async refresh(): Promise<void> {
    this.postMessage({ type: 'loading' });
    const scoringStartMs = Date.now();

    try {
      const surfaceDiscovery = detectAnalyzableSurface(this.reportPath);
      if (surfaceDiscovery.status === 'unsupported' || surfaceDiscovery.status === 'ambiguous') {
        this.postMessage({
          type: 'error',
          message: surfaceDiscovery.reason,
        });
        return;
      }

      const savedConfig = await loadDesignAnalyzerConfig(this.context);
      this.scoreState.setSavedConfig(savedConfig);
      this.scoreState.setReviewPacketPreviewOptions(
        await loadReviewPacketPreviewOptions(this.context, this.reportPath),
      );

      if (surfaceDiscovery.surface.surfaceType === 'fabricApp') {
        this.panel.title = 'Fabric App Review';
        const reviewResult = await reviewFabricAppSurface(surfaceDiscovery.surface, 'fabricAppQuality');
        const normalizedResult = await enrichFixPlanWithAdvisoryContent(
          normalizeScoreResultPayload({
            GestaltScore: reviewResult.qualityScore,
            CognitiveLoadScore: reviewResult.qualityScore,
            DataInkScore: reviewResult.qualityScore,
            AccessibilityScore: reviewResult.qualityScore,
            VisualBestPracticesScore: reviewResult.qualityScore,
            StephenFewScore: reviewResult.qualityScore,
            EnterpriseGovernanceScore: reviewResult.qualityScore,
            TufteScore: reviewResult.qualityScore,
            GraphicalPerceptionScore: reviewResult.qualityScore,
            DensityScore: reviewResult.qualityScore,
            NarrativeScore: reviewResult.qualityScore,
            CompositeScore: reviewResult.qualityScore,
            Feedback: {},
            PageCount: reviewResult.evidence.length,
            Recommendations: reviewResult.remediationGuidance,
            ReportPath: this.reportPath,
            ScoredAt: new Date().toISOString(),
            NormalizedFindings: reviewResult.normalizedFindings,
            FabricAppReview: {
              QualityScore: reviewResult.qualityScore,
              Summary: reviewResult.summary,
              RemediationGuidance: reviewResult.remediationGuidance,
              Evidence: reviewResult.evidence.map((item) => ({
                Kind: item.kind,
                Label: item.label,
                Summary: item.summary,
                FilePath: item.filePath,
                PageName: item.pageName,
                StateName: item.stateName,
              })),
            },
          }),
          {
            providerMode: 'disabled',
            enabledEnrichers: ['storytelling', 'executiveReadability'],
          },
        );
        this.scoreState.setCurrentResult(normalizedResult);
        await this.refreshStoryAssessmentState(normalizedResult);
        await this.persistAnalyzerWorkspaceReturn(normalizedResult);
        await this.captureScoreDiagnostics(normalizedResult);
        const intentFeedbackSession = await loadIntentFeedbackSession(this.context, this.reportPath);
        const reviewPacketPreview = buildReviewWorkflowExportData(
          normalizedResult,
          intentFeedbackSession.entries,
          undefined,
          this.scoreState.getRenderedReview(),
        );
        this.postScoreState(savedConfig, normalizedResult, intentFeedbackSession.entries, reviewPacketPreview);
        return;
      }

      this.panel.title = 'PBIR Optimization Report';

      if (!this.bridge) {
        const recordedIssue = getRecordedBackendIssue();
        this.postMessage({
          type: 'error',
          message: recordedIssue
            ? `${recordedIssue.message} See the PBIR Design Analyzer output channel for backend diagnostics.`
            : 'LSP bridge not available. Is the .NET service running?',
        });
        return;
      }

      const response = await executePbirOptimizationScore(
        this.bridge,
        this.reportPath,
        savedConfig,
        this.pageName,
        (message) => PbirScorePanel.diagnosticsOutput.appendLine(message),
      );
      if (!response.succeeded || !response.analyzer?.result) {
        this.postMessage({
          type: 'error',
          message: response.error?.summary ?? 'Scoring failed — no result returned.',
        });
        return;
      }
      const scoreResult = response.analyzer?.result;

      const normalizedResult = await enrichFixPlanWithAdvisoryContent(
        normalizeScoreResultPayload(scoreResult),
        {
          providerMode: 'disabled',
          enabledEnrichers: ['storytelling', 'executiveReadability'],
        },
      );
      this.scoreState.setCurrentResult(normalizedResult);
      await this.refreshStoryAssessmentState(normalizedResult);
      await this.persistAnalyzerWorkspaceReturn(normalizedResult);
      await this.captureScoreDiagnostics(normalizedResult);
      const intentFeedbackSession = await loadIntentFeedbackSession(this.context, this.reportPath);
      const reviewPacketPreview = buildReviewWorkflowExportData(
        normalizedResult,
        intentFeedbackSession.entries,
        undefined,
        this.scoreState.getRenderedReview(),
      );

      telemetry.sendEvent('scoring.completed', {
        pageCount: normalizedResult.pageCount,
        durationMs: Date.now() - scoringStartMs,
        compositeScoreBucket: bucketScore(normalizedResult.compositeScore),
      });

      this.postScoreState(savedConfig, normalizedResult, intentFeedbackSession.entries, reviewPacketPreview);

      // Load audit session and push state to webview alongside score
      try {
        this.auditProvider = createActiveProvider(this.context);
        this.auditSession = await this.auditWorkflow.loadAuditSession();
        await this.auditWorkflow.postAuditState();
      } catch {
        // Non-fatal: audit state failure should not break scoring
      }
    } catch (error) {
      this.postMessage({
        type: 'error',
        message: error instanceof Error ? error.message : String(error),
      });
    }
  }

  private presentHandoffShell(payload: AnalyzerWorkspaceHandoffPayload): void {
    const surfaceDiscovery = detectAnalyzableSurface(this.reportPath);
    this.panel.title = surfaceDiscovery.status === 'supported' && surfaceDiscovery.surface.surfaceType === 'fabricApp'
      ? 'Fabric App Review'
      : 'PBIR Optimization Report';
    this.scoreState.resetForHandoff();
    this.scoreState.setCurrentHandoffPayload(payload);
    this.postMessage({
      type: 'error',
      message: createScorePanelMessageRouter.buildHandoffMessage(payload),
    });
  }

  private async persistAnalyzerWorkspaceReturn(result: ScoreResult): Promise<void> {
    const handoffPayload = this.scoreState.getCurrentHandoffPayload();
    if (!handoffPayload) {
      return;
    }

    await recordAnalyzerWorkspaceReturn(this.context, {
      handoff: handoffPayload,
      scoreResult: result,
    });
  }

  private postMessage(message: ScorePanelHostToWebviewMessagePayload): void {
    if (!this.isReady) {
      this.scoreState.enqueuePendingMessage(message);
      return;
    }

    void this.panel.webview.postMessage(withScorePanelEnvelope(message));
  }

  private flushPendingMessages(): void {
    while (this.scoreState.getPendingMessages().length > 0) {
      const message = this.scoreState.shiftPendingMessage();
      if (message) {
        void this.panel.webview.postMessage(withScorePanelEnvelope(message));
      }
    }
  }

  private getReactHtml(): string {
    const assets = resolveWebviewAssets({
      webview: this.panel.webview,
      extensionUri: this.context.extensionUri,
      entryFile: 'analyzer-score/index.tsx',
      fallbackScriptFile: 'analyzer-score.js',
      fallbackStyleFile: 'analyzer-score.css',
      manifestFileName: 'manifest.analyzer-score.json',
    });

    if (assets.missingAssets) {
      return this.getBuildRequiredHtml();
    }

    const nonce = this.getNonce();
    const scriptSrcParts = [this.panel.webview.cspSource, `'nonce-${nonce}'`];
    const styleSrcParts = [this.panel.webview.cspSource, `'unsafe-inline'`];
    const connectSrcParts = [this.panel.webview.cspSource];

    if (assets.usingDevServer) {
      if (assets.devServerOrigin) {
        scriptSrcParts.push(assets.devServerOrigin);
        styleSrcParts.push(assets.devServerOrigin);
        connectSrcParts.push(assets.devServerOrigin);
      }

      if (assets.devServerWebSocketOrigin) {
        connectSrcParts.push(assets.devServerWebSocketOrigin);
      }

      scriptSrcParts.push("'unsafe-eval'");
    }

    return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src ${styleSrcParts.join(' ')}; script-src ${scriptSrcParts.join(' ')}; img-src ${this.panel.webview.cspSource} data: https:; connect-src ${connectSrcParts.join(' ')};">
  <title>PBIR Optimization Report</title>
  ${assets.styleUris.map((uri) => `<link href="${uri.toString()}" rel="stylesheet">`).join('\n  ')}
</head>
<body>
  <div id="root"></div>
  <script nonce="${nonce}">
    (function () {
      if (typeof process === 'undefined') {
        window.process = { env: { NODE_ENV: 'production' } };
      }
    })();
  </script>
  <script nonce="${nonce}" src="${assets.scriptUri.toString()}"></script>
</body>
</html>`;
  }

  private getBuildRequiredHtml(): string {
    return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>PBIR Optimization Report</title>
  <style>
    body {
      margin: 0;
      padding: 24px;
      font-family: var(--vscode-font-family);
      color: var(--vscode-foreground);
      background: var(--vscode-editor-background);
    }

    .message {
      border: 1px solid var(--vscode-editorWidget-border);
      border-radius: 12px;
      padding: 16px;
      background: var(--vscode-editorWidget-background);
    }
  </style>
</head>
<body>
  <div class="message">
    Analyzer score assets are missing. Run <code>npm run build:webview</code> in <code>vscode-extension</code>.
  </div>
</body>
</html>`;
  }

  private getNonce(): string {
    return Array.from({ length: 32 }, () => Math.floor(Math.random() * 36).toString(36)).join('');
  }

  private async captureScoreDiagnostics(result: ScoreResult): Promise<void> {
    const diagnostics = buildScoreDeterminismDiagnostics({
      result,
      reportPath: this.reportPath,
      extensionVersion: String(this.context.extension.packageJSON.version ?? 'unknown'),
      backendVersion: await this.readBackendVersion(),
      backendLaunchDiagnostics: getRecordedBackendLaunchDiagnostics(),
    });

    this.scoreState.setLastScoreDiagnosticsJson(JSON.stringify(diagnostics, null, 2));
    PbirScorePanel.diagnosticsOutput.appendLine(`=== ${new Date().toISOString()} :: ${path.basename(this.reportPath)} ===`);
    PbirScorePanel.diagnosticsOutput.appendLine(this.scoreState.getLastScoreDiagnosticsJson() ?? '');
    PbirScorePanel.diagnosticsOutput.appendLine('');
  }

  private async readBackendVersion(): Promise<string | undefined> {
    if (!this.bridge) {
      return undefined;
    }

    try {
      const response = await this.bridge.executeRequest('model/ping', {}) as {
        success?: boolean;
        data?: { version?: unknown };
      };

      return response?.success && typeof response.data?.version === 'string'
        ? response.data.version
        : undefined;
    } catch {
      return undefined;
    }
  }

  dispose(): void {
    if (this.isDisposed) {
      return;
    }

    this.isDisposed = true;
    this.isReady = false;
    this.scoreState.resetForDispose();

    if (PbirScorePanel.instance === this) {
      PbirScorePanel.instance = undefined;
    }

    while (this.disposables.length > 0) {
      const disposable = this.disposables.pop();
      disposable?.dispose();
    }

    try {
      this.panel.dispose();
    } catch {
      // No-op during shutdown/dispose races.
    }
  }
}
