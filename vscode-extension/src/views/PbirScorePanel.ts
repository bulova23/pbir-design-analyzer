import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { loadDesignAnalyzerConfig } from '../analyzer/config/store';
import type { DesignAnalyzerConfig } from '../analyzer/config/types';
import type {
  AuditCaptureSummary,
  AuditFindingDisplay,
  AuditPageState,
  AuditState,
  FixApplySessionRecord,
  FixOpportunity,
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownRenderOptions,
  ScorePanelHostToWebviewMessage,
  ScorePanelState,
  ScorePanelWebviewToHostMessage,
  ScoreRequestPayload,
  ScoreResult,
} from '../analyzer/contracts/scorePanel';
import { addCaptures, assignCapture, computeCoverage, loadSession, removeCapture, saveSession } from '../analyzer/audit/session';
import type { VisualAuditSession } from '../analyzer/audit/types';
import type { VisualAuditProvider } from '../analyzer/audit/providers/VisualAuditProvider';
import { createActiveProvider } from '../analyzer/audit/providers/providerSetup';
import { reviewFabricAppSurface } from '../analyzer/fabric/review/fabricAppReviewAnalyzer';
import { enrichFixPlanWithAdvisoryContent } from '../analyzer/proposalEnrichment/proposalEnrichmentOrchestrator';
import { detectAnalyzableSurface } from '../analyzer/surfaces/discovery';
import { telemetry, bucketScore } from '../telemetry/reporter';
import { AnalyzerBridgeService } from '../services/rpc/AnalyzerBridgeService';
import {
  getRecordedBackendIssue,
  getRecordedBackendLaunchDiagnostics,
} from '../languageServer/analyzerBackendClient';
import { resolveWebviewAssets } from './webviewAssets';
import { buildFixWorkflowPayload, normalizeScoreResultPayload } from './scoreResultPayload';
import { revealVisualInPbirExplorer } from './pbirExplorerReveal';
import { loadIntentFeedbackSession, saveIntentFeedbackSession, upsertIntentFeedback } from '../analyzer/intentFeedback/store';
import {
  buildReviewWorkflowExportData,
  exportReviewWorkflowAsHtml,
  exportReviewWorkflowAsJson,
  exportReviewWorkflowAsMarkdown,
  exportReviewWorkflowAsPdf,
} from '../analyzer/score/reviewWorkflowExport';
import {
  buildReviewPacketPreviewHtml,
  defaultReviewPacketPreviewOptions,
  normalizeReviewPacketPreviewOptions,
} from '../analyzer/score/reviewPacketPreview';
import {
  loadReviewPacketPreviewOptions,
  saveReviewPacketPreviewOptions,
} from '../analyzer/score/reviewPacketPreviewStore';
import { chooseProfiledDocumentExportOptions } from '../analyzer/score/reviewWorkflowExportPrompts';
import {
  applyFixOpportunity,
  applyFixOpportunityBatch,
  rollbackFixOpportunity,
  rollbackFixSession,
} from '../analyzer/fixes/fixApplyEngine';
import { evaluateFixOpportunityCompatibility } from '../analyzer/fixes/fixCompatibility';
import {
  createFixApplySessionRecord,
  markFixSessionRegenerated,
  recordFixSessionRollback,
} from '../analyzer/fixes/fixSessionHistory';
import { evaluateFixOutcome, summarizeBatchFixOutcomes } from '../analyzer/fixes/fixOutcomeEvaluator';
import { buildScoreDeterminismDiagnostics } from '../analyzer/score/scoreDiagnostics';

export class PbirScorePanel {
  private static instance: PbirScorePanel | undefined;
  private static readonly diagnosticsOutput = vscode.window.createOutputChannel('PBIR Score Diagnostics');

  private readonly panel: vscode.WebviewPanel;
  private readonly bridge: AnalyzerBridgeService | undefined;
  private readonly context: vscode.ExtensionContext;
  private readonly disposables: vscode.Disposable[] = [];
  private isDisposed = false;
  private isReady = false;
  private pendingMessages: ScorePanelHostToWebviewMessage[] = [];
  private reportPath: string;
  private pageName: string | undefined;
  private currentResult: ScoreResult | undefined;
  private savedConfig: DesignAnalyzerConfig | null = null;
  private selectedPageIndex = 0;
  private reviewPacketPreviewOptions = defaultReviewPacketPreviewOptions;
  private auditSession: VisualAuditSession | undefined;
  private auditProvider: VisualAuditProvider;
  private readonly fixOpportunityHistory = new Map<string, FixOpportunity>();
  private selectedFixOpportunityIds: string[] = [];
  private fixSelectionApprovalState: NonNullable<ScorePanelState['fixSelection']>['approvalState'] = 'NeedsPreview';
  private fixApplySessions: FixApplySessionRecord[] = [];
  private fixWorkflowMessage: string | undefined;
  private lastScoreDiagnosticsJson: string | undefined;

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

  static async copyCurrentScoreDiagnostics(): Promise<boolean> {
    if (!PbirScorePanel.instance?.lastScoreDiagnosticsJson) {
      return false;
    }

    await vscode.env.clipboard.writeText(PbirScorePanel.instance.lastScoreDiagnosticsJson);
    PbirScorePanel.diagnosticsOutput.show(true);
    return true;
  }

  async requestScreenshotUpload(): Promise<void> {
    await this.handleUploadScreenshots();
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
    this.panel.webview.html = this.getReactHtml();

    this.panel.onDidDispose(() => this.dispose(), null, this.disposables);
    this.panel.webview.onDidReceiveMessage(
      (message) => this.handleMessage(message as ScorePanelWebviewToHostMessage),
      null,
      this.disposables,
    );
  }

  private async handleMessage(message: ScorePanelWebviewToHostMessage): Promise<void> {
    switch (message.type) {
      case 'webviewReady':
        this.isReady = true;
        this.flushPendingMessages();
        return;
      case 'refresh':
        await this.refresh();
        return;
      case 'selectTab':
        this.selectedPageIndex = message.pageIndex;
        return;
      case 'setIntentFeedback':
        await this.handleSetIntentFeedback(message);
        return;
      case 'revealVisual': {
        const revealed = await revealVisualInPbirExplorer(message.pageName, message.visualId);
        if (!revealed) {
          void vscode.window.showWarningMessage(
            `Could not locate '${message.visualId}' on page '${message.pageName}' in the PBIR sidecar.`,
          );
        }
        return;
      }
      case 'uploadScreenshots':
        await this.handleUploadScreenshots();
        return;
      case 'attachScreenshot':
        await this.handleAttachScreenshot(message.pageName);
        return;
      case 'removeScreenshot':
        await this.handleRemoveScreenshot(message.captureId);
        return;
      case 'assignCapture':
        await this.handleAssignCapture(message.captureId, message.targetPageName);
        return;
      case 'analyzeCapture':
        await this.handleAnalyzeCapture(message.captureId, message.pageName);
        return;
      case 'exportReviewWorkflow':
        await this.handleExportReviewWorkflow();
        return;
      case 'setReviewPacketPreviewProfile':
        await this.handleSetReviewPacketPreviewProfile(message.profile);
        return;
      case 'setReviewPacketPreviewTemplateVariant':
        await this.handleSetReviewPacketPreviewTemplateVariant(message.templateVariant);
        return;
      case 'openReviewPacketPreview':
        await this.handleOpenReviewPacketPreview();
        return;
      case 'toggleFixOpportunitySelection':
        await this.handleToggleFixOpportunitySelection(message.opportunityId);
        return;
      case 'previewSelectedFixOpportunities':
        await this.handlePreviewSelectedFixOpportunities();
        return;
      case 'approveSelectedFixOpportunities':
        await this.handleApproveSelectedFixOpportunities();
        return;
      case 'applySelectedFixOpportunities':
        await this.handleApplySelectedFixOpportunities();
        return;
      case 'rollbackFixSession':
        await this.handleRollbackFixSession(message.sessionId);
        return;
      case 'regenerateFixOpportunities':
        await this.handleRegenerateFixOpportunities(message.opportunityIds);
        return;
      case 'approveFixOpportunity':
        await this.handleApproveFixOpportunity(message.opportunityId);
        return;
      case 'applyFixOpportunity':
        await this.handleApplyFixOpportunity(message.opportunityId);
        return;
      case 'rollbackFixOpportunity':
        await this.handleRollbackFixOpportunity(message.opportunityId);
        return;
      case 'openSettings':
        await vscode.commands.executeCommand('pbirAnalyzer.configureScoring');
        return;
    }
  }

  private findFixOpportunity(opportunityId: string): FixOpportunity | undefined {
    return this.currentResult?.fixOpportunities?.find((item) => item.id === opportunityId)
      ?? this.fixOpportunityHistory.get(opportunityId);
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

    return {
      ...result,
      fixOpportunities: merged,
    };
  }

  private currentPreviewableOpportunities(): FixOpportunity[] {
    return (this.currentResult?.fixOpportunities ?? []).filter((item) => item.state !== 'Applied' && item.state !== 'RolledBack');
  }

  private selectedFixOpportunities(): FixOpportunity[] {
    const selectedSet = new Set(this.selectedFixOpportunityIds);
    return this.currentPreviewableOpportunities().filter((item) => selectedSet.has(item.id));
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

  private async handleToggleFixOpportunitySelection(opportunityId: string): Promise<void> {
    const opportunity = this.findFixOpportunity(opportunityId);
    if (!opportunity || opportunity.state === 'Applied' || opportunity.state === 'RolledBack') {
      return;
    }

    this.selectedFixOpportunityIds = this.selectedFixOpportunityIds.includes(opportunityId)
      ? this.selectedFixOpportunityIds.filter((id) => id !== opportunityId)
      : [...this.selectedFixOpportunityIds, opportunityId];
    this.fixSelectionApprovalState = 'NeedsPreview';
    this.fixWorkflowMessage = undefined;
    await this.postCurrentScoreState();
  }

  private async handlePreviewSelectedFixOpportunities(): Promise<void> {
    const selected = this.selectedFixOpportunities();
    if (selected.length === 0) {
      this.fixWorkflowMessage = 'Select one or more opportunities before previewing fixes.';
      await this.postCurrentScoreState();
      return;
    }

    const compatibility = evaluateFixOpportunityCompatibility(selected);
    if (!compatibility.isCompatible) {
      this.fixSelectionApprovalState = 'NeedsPreview';
      this.fixWorkflowMessage = 'Selected opportunities are incompatible or stale. Resolve the blocked items before previewing.';
      await this.postCurrentScoreState();
      return;
    }

    this.fixSelectionApprovalState = 'Previewed';
    this.fixWorkflowMessage = undefined;
    await this.postCurrentScoreState();
  }

  private async handleApproveSelectedFixOpportunities(): Promise<void> {
    const selected = this.selectedFixOpportunities();
    const compatibility = evaluateFixOpportunityCompatibility(selected);
    if (selected.length === 0 || !compatibility.isCompatible || this.fixSelectionApprovalState !== 'Previewed') {
      return;
    }

    this.fixSelectionApprovalState = 'Approved';
    await this.postCurrentScoreState();
  }

  private async handleApplySelectedFixOpportunities(): Promise<void> {
    const selected = this.selectedFixOpportunities();
    const previousResult = this.currentResult;
    if (selected.length === 0 || !previousResult || this.fixSelectionApprovalState !== 'Approved') {
      return;
    }

    const applyResult = applyFixOpportunityBatch(selected);
    if (applyResult.state !== 'Applied') {
      for (const opportunity of selected) {
        this.fixOpportunityHistory.set(opportunity.id, {
          ...opportunity,
          state: applyResult.state,
        });
      }
      this.fixWorkflowMessage = applyResult.state === 'Stale'
        ? 'Selected opportunities are stale or drifted. Regenerate them before retrying.'
        : 'Selected opportunities cannot be applied together.';
      this.fixSelectionApprovalState = 'NeedsPreview';
      await this.postCurrentScoreState();
      return;
    }

    await this.refresh();
    if (!this.currentResult) {
      return;
    }

    const outcomeItems = selected.map((opportunity) => {
      const outcome = evaluateFixOutcome(opportunity, previousResult, this.currentResult!);
      this.fixOpportunityHistory.set(opportunity.id, {
        ...opportunity,
        state: outcome.nextState,
        outcome: outcome.outcome,
      });
      return {
        opportunityId: opportunity.id,
        title: opportunity.title,
        state: outcome.nextState,
        outcome: outcome.outcome,
      };
    });

    const groupedOutcomeSummary = summarizeBatchFixOutcomes(outcomeItems);
    const session = createFixApplySessionRecord({
      appliedAt: applyResult.session?.appliedAt ?? new Date().toISOString(),
      opportunities: selected.map((opportunity) => ({
        id: opportunity.id,
        title: opportunity.title,
        state: this.fixOpportunityHistory.get(opportunity.id)?.state ?? 'Applied',
      })),
      rollbackAvailable: applyResult.session?.rollbackAvailable ?? false,
      groupedOutcomeSummary,
    });

    this.fixApplySessions = [
      {
        ...session,
        id: applyResult.session?.id ?? session.id,
      },
      ...this.fixApplySessions,
    ];
    this.selectedFixOpportunityIds = [];
    this.fixSelectionApprovalState = 'NeedsPreview';
    this.fixWorkflowMessage = undefined;
    await this.postCurrentScoreState();
  }

  private async handleRollbackFixSession(sessionId: string): Promise<void> {
    const session = this.fixApplySessions.find((item) => item.id === sessionId);
    if (!session) {
      return;
    }

    const opportunities = session.opportunityIds
      .map((id) => this.fixOpportunityHistory.get(id))
      .filter((item): item is FixOpportunity => Boolean(item));
    const rollback = rollbackFixSession(session, opportunities);
    this.fixApplySessions = this.fixApplySessions.map((item) => item.id === sessionId
      ? recordFixSessionRollback(item, rollback.rollbackHistory[rollback.rollbackHistory.length - 1])
      : item);
    for (const opportunity of opportunities) {
      this.fixOpportunityHistory.set(opportunity.id, {
        ...opportunity,
        state: 'RolledBack',
        outcome: undefined,
      });
    }

    await this.refresh();
    await this.postCurrentScoreState();
  }

  private async handleRegenerateFixOpportunities(opportunityIds?: string[]): Promise<void> {
    const currentSelection = this.currentResult
      ? this.buildFixSelectionState(this.buildPresentationResult(this.currentResult))
      : undefined;
    const staleIds = opportunityIds
      ?? currentSelection?.compatibility.blockingReasons
        .filter((reason) => reason.code === 'staleOpportunity' || reason.code === 'targetDrifted')
        .flatMap((reason) => reason.opportunityIds)
      ?? [];

    const staleSet = new Set(staleIds);
    await this.refresh();
    const regeneratedOpportunityIds = (this.currentResult?.fixOpportunities ?? [])
      .filter((item) => staleSet.has(item.id) || staleSet.has(item.remediationItemId))
      .map((item) => item.id);

    if (this.fixApplySessions[0]) {
      this.fixApplySessions[0] = markFixSessionRegenerated(this.fixApplySessions[0], {
        staleOpportunityIds: staleIds,
        regeneratedOpportunityIds,
      });
    }

    this.selectedFixOpportunityIds = regeneratedOpportunityIds;
    this.fixSelectionApprovalState = 'NeedsPreview';
    this.fixWorkflowMessage = staleIds.length > 0
      ? `Regenerated ${regeneratedOpportunityIds.length} opportunity${regeneratedOpportunityIds.length === 1 ? '' : 'ies'} from stale selections.`
      : 'Fix opportunities regenerated from the latest score state.';
    await this.postCurrentScoreState();
  }

  private async handleApproveFixOpportunity(opportunityId: string): Promise<void> {
    const opportunity = this.findFixOpportunity(opportunityId);
    if (!opportunity) {
      void vscode.window.showWarningMessage(`Fix opportunity '${opportunityId}' is no longer available.`);
      return;
    }

    this.fixOpportunityHistory.set(opportunityId, {
      ...opportunity,
      state: 'Approved',
    });
    await this.postCurrentScoreState();
  }

  private async handleApplyFixOpportunity(opportunityId: string): Promise<void> {
    const opportunity = this.findFixOpportunity(opportunityId);
    const previousResult = this.currentResult;
    if (!opportunity || !previousResult) {
      void vscode.window.showWarningMessage(`Fix opportunity '${opportunityId}' is no longer available.`);
      return;
    }

    const applyResult = applyFixOpportunity(opportunity);
    this.fixOpportunityHistory.set(opportunityId, {
      ...opportunity,
      state: applyResult.state,
    });

    if (applyResult.state !== 'Applied') {
      await this.postCurrentScoreState();
      return;
    }

    await this.refresh();
    if (!this.currentResult) {
      return;
    }

    const outcome = evaluateFixOutcome(opportunity, previousResult, this.currentResult);
    this.fixOpportunityHistory.set(opportunityId, {
      ...opportunity,
      state: outcome.nextState,
      outcome: outcome.outcome,
    });
    await this.postCurrentScoreState();
  }

  private async handleRollbackFixOpportunity(opportunityId: string): Promise<void> {
    const opportunity = this.findFixOpportunity(opportunityId);
    if (!opportunity) {
      void vscode.window.showWarningMessage(`Fix opportunity '${opportunityId}' is no longer available for rollback.`);
      return;
    }

    const rollbackResult = rollbackFixOpportunity(opportunity);
    this.fixOpportunityHistory.set(opportunityId, {
      ...opportunity,
      state: rollbackResult.state,
      outcome: undefined,
    });
    await this.refresh();
    await this.postCurrentScoreState();
  }

  private pageNamesFromResult(): string[] {
    const result = this.currentResult;
    if (!result) return [];
    if (result.pageScores && result.pageScores.length > 0) {
      return result.pageScores.map((p) => p.pageName);
    }
    if (result.scoredPageName) return [result.scoredPageName];
    return [];
  }

  private async handleUploadScreenshots(): Promise<void> {
    const uris = await vscode.window.showOpenDialog({
      title: 'Select Report Screenshots',
      canSelectMany: true,
      canSelectFiles: true,
      canSelectFolders: false,
      filters: { Images: ['png', 'jpg', 'jpeg', 'webp'] },
      openLabel: 'Add Screenshots',
    });

    if (!uris || uris.length === 0) return;

    const session = await this.loadAuditSession();
    const pageNames = this.pageNamesFromResult();
    await addCaptures(this.context, session, uris.map((u) => u.fsPath), pageNames);
    await saveSession(this.context, session);
    this.auditSession = session;
    this.postAuditState();
  }

  private async handleAttachScreenshot(pageName: string): Promise<void> {
    const uris = await vscode.window.showOpenDialog({
      title: `Attach Screenshot to "${pageName}"`,
      canSelectMany: false,
      canSelectFiles: true,
      canSelectFolders: false,
      filters: { Images: ['png', 'jpg', 'jpeg', 'webp'] },
      openLabel: 'Attach',
    });

    if (!uris || uris.length === 0) return;

    const session = await this.loadAuditSession();
    await addCaptures(this.context, session, [uris[0].fsPath], [pageName]);
    await saveSession(this.context, session);
    this.auditSession = session;
    this.postAuditState();
  }

  private async handleRemoveScreenshot(captureId: string): Promise<void> {
    const session = await this.loadAuditSession();
    const allCaptures = [
      ...session.pages.flatMap((p) => p.captures),
      ...session.unmatchedCaptures,
    ];
    const capture = allCaptures.find((c) => c.captureId === captureId);

    removeCapture(session, captureId);

    if (capture?.storedPath && fs.existsSync(capture.storedPath)) {
      try {
        fs.unlinkSync(capture.storedPath);
      } catch {
        // Non-fatal: asset cleanup failure
      }
    }

    await saveSession(this.context, session);
    this.auditSession = session;
    this.postAuditState();
  }

  private async handleAssignCapture(captureId: string, targetPageName: string): Promise<void> {
    const session = await this.loadAuditSession();
    assignCapture(session, captureId, targetPageName);
    await saveSession(this.context, session);
    this.auditSession = session;
    this.postAuditState();
  }

  private async handleAnalyzeCapture(captureId: string, pageName: string): Promise<void> {
    const session = await this.loadAuditSession();
    const page = session.pages.find((p) => p.pageName === pageName);
    const capture = page?.captures.find((c) => c.captureId === captureId);

    if (!capture) {
      void vscode.window.showErrorMessage(`Capture ${captureId} not found for page "${pageName}".`);
      return;
    }

    this.postMessage({ type: 'auditAnalyzing', captureId });

    try {
      const pageScore = this.currentResult?.pageScores?.find((p) => p.pageName === pageName);
      const findings = await this.auditProvider.analyzeCapture({ capture, pageName, pageScore });

      if (page) {
        page.findings = page.findings.filter((f) => f.captureId !== captureId);
        page.findings.push(...findings);
      }

      await saveSession(this.context, session);
      this.auditSession = session;
      this.postAuditState();
    } catch (err) {
      void vscode.window.showErrorMessage(
        `Audit analysis failed: ${err instanceof Error ? err.message : String(err)}`,
      );
      this.postAuditState();
    }
  }

  private async loadAuditSession(): Promise<VisualAuditSession> {
    if (!this.auditSession) {
      this.auditSession = await loadSession(this.context, this.reportPath);
    }
    return this.auditSession;
  }

  private async handleSetIntentFeedback(
    message: Extract<ScorePanelWebviewToHostMessage, { type: 'setIntentFeedback' }>,
  ): Promise<void> {
    const session = await loadIntentFeedbackSession(this.context, this.reportPath);
    const analyzerVersion = String(this.context.extension.packageJSON?.version ?? 'unknown');
    const reportSessionId = `${session.reportKey}:${this.currentResult?.scoredAt ?? new Date().toISOString()}`;

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

    if (this.currentResult && this.savedConfig) {
      const reviewPacketPreview = buildReviewWorkflowExportData(this.currentResult, session.entries);
      this.postScoreState(this.savedConfig, this.currentResult, session.entries, reviewPacketPreview);
    }
  }

  private async handleSetReviewPacketPreviewProfile(
    profile: ReviewWorkflowExportProfile,
  ): Promise<void> {
    this.reviewPacketPreviewOptions = normalizeReviewPacketPreviewOptions({
      ...this.reviewPacketPreviewOptions,
      profile,
    });
    await saveReviewPacketPreviewOptions(this.context, this.reportPath, this.reviewPacketPreviewOptions);
    await this.postCurrentScoreState();
  }

  private async handleSetReviewPacketPreviewTemplateVariant(
    templateVariant: ReviewWorkflowMarkdownRenderOptions['templateVariant'],
  ): Promise<void> {
    this.reviewPacketPreviewOptions = normalizeReviewPacketPreviewOptions({
      ...this.reviewPacketPreviewOptions,
      templateVariant,
    });
    await saveReviewPacketPreviewOptions(this.context, this.reportPath, this.reviewPacketPreviewOptions);
    await this.postCurrentScoreState();
  }

  private async handleOpenReviewPacketPreview(): Promise<void> {
    if (!this.currentResult) {
      void vscode.window.showWarningMessage('Score the report before opening the review packet preview.');
      return;
    }

    const session = await loadIntentFeedbackSession(this.context, this.reportPath);
    const reviewPacketPreview = buildReviewWorkflowExportData(this.currentResult, session.entries);
    const html = buildReviewPacketPreviewHtml(
      reviewPacketPreview,
      this.reportPath,
      this.reviewPacketPreviewOptions,
    );
    const tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-review-preview-'));
    const reportName = path.basename(this.reportPath).replace(/\.Report$/i, '');
    const profileSuffix = this.reviewPacketPreviewOptions.profile.toLowerCase();
    const tempFilePath = path.join(tempDir, `${reportName}-${profileSuffix}-preview.html`);

    fs.writeFileSync(tempFilePath, html, 'utf8');
    await vscode.env.openExternal(vscode.Uri.file(tempFilePath));
  }

  async exportReviewWorkflow(): Promise<void> {
    if (!this.currentResult) {
      void vscode.window.showWarningMessage('Score the report before exporting the review summary.');
      return;
    }

    const session = await loadIntentFeedbackSession(this.context, this.reportPath);
    const exportData = buildReviewWorkflowExportData(this.currentResult, session.entries);
    const formatChoice = await vscode.window.showQuickPick(
      [
        { label: 'Markdown', description: 'Human-readable review summary (.md)' },
        { label: 'HTML', description: 'Styled consultant packet (.html)' },
        { label: 'PDF', description: 'Fixed-layout consultant packet (.pdf)' },
        { label: 'JSON', description: 'Machine-readable review workflow snapshot (.json)' },
      ],
      { placeHolder: 'Choose review export format' },
    );

    if (!formatChoice) return;

    const selectedFormat = formatChoice.label.toLowerCase();
    const isMarkdown = selectedFormat === 'markdown';
    const isHtml = selectedFormat === 'html';
    const isPdf = selectedFormat === 'pdf';
    let markdownProfile: ReviewWorkflowExportProfile = 'consultant';
    let markdownOptions: ReviewWorkflowMarkdownRenderOptions = {};

    if (isMarkdown || isHtml || isPdf) {
      const exportSelection = await chooseProfiledDocumentExportOptions(
        isMarkdown ? 'markdown' : isHtml ? 'html' : 'pdf',
        this.reviewPacketPreviewOptions,
      );
      if (!exportSelection) return;
      markdownProfile = exportSelection.profile;
      markdownOptions = {
        templateVariant: exportSelection.templateVariant,
        branding: exportSelection.branding,
      };
    }

    const saveUri = await vscode.window.showSaveDialog({
      defaultUri: vscode.Uri.file(
        path.join(
          path.dirname(this.reportPath),
          `review-workflow-summary.${isMarkdown ? 'md' : isHtml ? 'html' : isPdf ? 'pdf' : 'json'}`,
        ),
      ),
      filters: isMarkdown
        ? { Markdown: ['md'] }
        : isHtml
          ? { HTML: ['html'] }
          : isPdf
            ? { PDF: ['pdf'] }
            : { JSON: ['json'] },
      saveLabel: 'Export',
    });

    if (!saveUri) return;

    if (isPdf) {
      const content = await exportReviewWorkflowAsPdf(exportData, markdownProfile, markdownOptions);
      fs.writeFileSync(saveUri.fsPath, content);
    } else {
      const content = isMarkdown
        ? exportReviewWorkflowAsMarkdown(exportData, markdownProfile, markdownOptions)
        : isHtml
          ? exportReviewWorkflowAsHtml(exportData, markdownProfile, markdownOptions)
          : exportReviewWorkflowAsJson(exportData);

      fs.writeFileSync(saveUri.fsPath, content, 'utf8');
    }

    const openAction = 'Open File';
    const choice = await vscode.window.showInformationMessage(
      `Review workflow summary exported to ${path.basename(saveUri.fsPath)}`,
      openAction,
    );

    if (choice === openAction) {
      if (isPdf) {
        await vscode.commands.executeCommand('vscode.open', saveUri);
      } else {
        await vscode.window.showTextDocument(saveUri);
      }
    }
  }

  private async handleExportReviewWorkflow(): Promise<void> {
    await this.exportReviewWorkflow();
  }

  private async postCurrentScoreState(): Promise<void> {
    if (!this.currentResult || !this.savedConfig) {
      return;
    }

    const intentFeedbackSession = await loadIntentFeedbackSession(this.context, this.reportPath);
    const reviewPacketPreview = buildReviewWorkflowExportData(
      this.currentResult,
      intentFeedbackSession.entries,
    );

    this.postScoreState(this.savedConfig, this.currentResult, intentFeedbackSession.entries, reviewPacketPreview);
  }

  private postScoreState(
    config: DesignAnalyzerConfig,
    result: ScoreResult,
    intentFeedback: ScorePanelState['intentFeedback'],
    reviewPacketPreview: ReturnType<typeof buildReviewWorkflowExportData>,
  ): void {
    const presentationResult = this.buildPresentationResult(result);
    const fixWorkflow = buildFixWorkflowPayload({
      opportunities: presentationResult.fixOpportunities ?? [],
      selectedOpportunityIds: this.selectedFixOpportunityIds,
      approvalState: this.fixSelectionApprovalState,
      message: this.fixWorkflowMessage,
      fixApplySessions: this.fixApplySessions,
    });
    this.postMessage({
      type: 'scoreState',
      state: {
        config,
        result: presentationResult,
        selectedPageIndex: this.selectedPageIndex,
        intentFeedback,
        fixSelection: fixWorkflow.fixSelection,
        fixApplySessions: fixWorkflow.fixApplySessions,
        reviewPacketPreview,
        reviewPacketPreviewHtml: buildReviewPacketPreviewHtml(
          reviewPacketPreview,
          this.reportPath,
          this.reviewPacketPreviewOptions,
        ),
        reviewPacketPreviewProfile: this.reviewPacketPreviewOptions.profile,
        reviewPacketPreviewTemplateVariant: this.reviewPacketPreviewOptions.templateVariant,
      },
    });
  }

  private buildAuditState(session: VisualAuditSession, providerConfigured: boolean): AuditState {
    const pageNames = this.pageNamesFromResult();
    const coverage = computeCoverage(session, pageNames);

    const pages: AuditPageState[] = session.pages.map((p) => ({
      pageName: p.pageName,
      captures: p.captures.map((c) => ({
        captureId: c.captureId,
        pageName: c.pageName,
        stateName: c.stateName,
        fileName: c.fileName,
        storedPath: c.storedPath,
        findingCount: p.findings.filter((f) => f.captureId === c.captureId).length,
      })),
      findings: p.findings.map((f): AuditFindingDisplay => ({
        findingId: f.findingId,
        captureId: f.captureId,
        findingType: f.findingType,
        severity: f.severity,
        confidence: f.confidence,
        issueSource: f.issueSource,
        text: f.text,
        recommendation: f.recommendation,
        regionHint: f.regionHint,
      })),
    }));

    const unmatchedCaptures: AuditCaptureSummary[] = session.unmatchedCaptures.map((c) => ({
      captureId: c.captureId,
      pageName: c.pageName,
      stateName: c.stateName,
      fileName: c.fileName,
      storedPath: c.storedPath,
      findingCount: 0,
    }));

    return {
      coverage,
      pages,
      unmatchedCaptures,
      isAnalyzing: false,
      providerName: this.auditProvider.providerName,
      providerConfigured,
    };
  }

  private async postAuditState(): Promise<void> {
    const session = await this.loadAuditSession();
    const providerConfigured = await this.auditProvider.isConfigured();
    const auditState = this.buildAuditState(session, providerConfigured);
    this.postMessage({ type: 'auditState', audit: auditState });
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
      this.savedConfig = savedConfig;
      this.reviewPacketPreviewOptions = await loadReviewPacketPreviewOptions(this.context, this.reportPath);

      if (surfaceDiscovery.surface.surfaceType === 'fabricApp') {
        this.panel.title = 'Fabric App Review';
        const reviewResult = reviewFabricAppSurface(surfaceDiscovery.surface, 'fabricAppQuality');
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
        this.currentResult = normalizedResult;
        await this.captureScoreDiagnostics(normalizedResult);
        const intentFeedbackSession = await loadIntentFeedbackSession(this.context, this.reportPath);
        const reviewPacketPreview = buildReviewWorkflowExportData(
          normalizedResult,
          intentFeedbackSession.entries,
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

      const requestPayload: ScoreRequestPayload = {
        reportPath: this.reportPath,
        config: savedConfig,
      };

      if (this.pageName) {
        requestPayload.pageName = this.pageName;
      }

      const response = (await this.bridge.executeRequest(
        'model/pbir/scoreReport',
        requestPayload,
      )) as { success: boolean; error?: string; data?: ScoreResult };

      if (!response?.success || !response.data) {
        this.postMessage({
          type: 'error',
          message: response?.error ?? 'Scoring failed — no result returned.',
        });
        return;
      }

      const normalizedResult = await enrichFixPlanWithAdvisoryContent(
        normalizeScoreResultPayload(response.data),
        {
          providerMode: 'disabled',
          enabledEnrichers: ['storytelling', 'executiveReadability'],
        },
      );
      this.currentResult = normalizedResult;
      await this.captureScoreDiagnostics(normalizedResult);
      const intentFeedbackSession = await loadIntentFeedbackSession(this.context, this.reportPath);
      const reviewPacketPreview = buildReviewWorkflowExportData(
        normalizedResult,
        intentFeedbackSession.entries,
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
        this.auditSession = await loadSession(this.context, this.reportPath);
        const providerConfigured = await this.auditProvider.isConfigured();
        const auditState = this.buildAuditState(this.auditSession, providerConfigured);
        this.postMessage({ type: 'auditState', audit: auditState });
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

  private postMessage(message: ScorePanelHostToWebviewMessage): void {
    if (!this.isReady) {
      this.pendingMessages.push(message);
      return;
    }

    void this.panel.webview.postMessage(message);
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

    this.lastScoreDiagnosticsJson = JSON.stringify(diagnostics, null, 2);
    PbirScorePanel.diagnosticsOutput.appendLine(`=== ${new Date().toISOString()} :: ${path.basename(this.reportPath)} ===`);
    PbirScorePanel.diagnosticsOutput.appendLine(this.lastScoreDiagnosticsJson);
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
    this.pendingMessages = [];
    this.currentResult = undefined;
    this.savedConfig = null;

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
