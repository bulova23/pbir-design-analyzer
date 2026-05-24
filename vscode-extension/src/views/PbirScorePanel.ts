import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { loadDesignAnalyzerConfig } from '../analyzer/config/store';
import type { DesignAnalyzerConfig } from '../analyzer/config/types';
import type {
  AuditCaptureSummary,
  AuditFindingDisplay,
  AuditPageState,
  AuditState,
  ScorePanelHostToWebviewMessage,
  ScorePanelWebviewToHostMessage,
  ScoreRequestPayload,
  ScoreResult,
} from '../analyzer/contracts/scorePanel';
import { addCaptures, assignCapture, computeCoverage, loadSession, removeCapture, saveSession } from '../analyzer/audit/session';
import type { VisualAuditSession } from '../analyzer/audit/types';
import { AnthropicVisualAuditProvider } from '../analyzer/audit/providers/AnthropicVisualAuditProvider';
import { telemetry, bucketScore } from '../telemetry/reporter';
import { AnalyzerBridgeService } from '../services/rpc/AnalyzerBridgeService';
import { resolveWebviewAssets } from './webviewAssets';
import { normalizeScoreResultPayload } from './scoreResultPayload';
import { revealVisualInPbirExplorer } from './pbirExplorerReveal';

export class PbirScorePanel {
  private static instance: PbirScorePanel | undefined;

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
  private auditSession: VisualAuditSession | undefined;
  private auditProvider: AnthropicVisualAuditProvider;

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
    this.auditProvider = new AnthropicVisualAuditProvider(context);
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
      case 'configureAuditProvider':
        await this.handleConfigureAuditProvider();
        return;
    }
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

  private async handleConfigureAuditProvider(): Promise<void> {
    const key = await vscode.window.showInputBox({
      title: 'Configure Anthropic API Key for Visual Audit',
      prompt: 'Enter your Anthropic API key (stored in VS Code SecretStorage)',
      password: true,
      ignoreFocusOut: true,
      validateInput: (v) => v.trim().length > 0 ? undefined : 'API key is required.',
    });

    if (!key) return;

    await this.auditProvider.setApiKey(key);
    void vscode.window.showInformationMessage('Anthropic API key saved. Visual Audit is now configured.');
    this.postAuditState();
  }

  private async loadAuditSession(): Promise<VisualAuditSession> {
    if (!this.auditSession) {
      this.auditSession = await loadSession(this.context, this.reportPath);
    }
    return this.auditSession;
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
    this.selectedPageIndex = 0;
    this.postMessage({ type: 'loading' });
    const scoringStartMs = Date.now();

    try {
      if (!this.bridge) {
        this.postMessage({
          type: 'error',
          message: 'LSP bridge not available. Is the .NET service running?',
        });
        return;
      }

      const savedConfig = await loadDesignAnalyzerConfig(this.context);
      this.savedConfig = savedConfig;

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

      const normalizedResult = normalizeScoreResultPayload(response.data);
      this.currentResult = normalizedResult;

      telemetry.sendEvent('scoring.completed', {
        pageCount: normalizedResult.pageCount,
        durationMs: Date.now() - scoringStartMs,
        compositeScoreBucket: bucketScore(normalizedResult.compositeScore),
      });

      this.postMessage({
        type: 'scoreState',
        state: {
          config: savedConfig,
          result: normalizedResult,
          selectedPageIndex: this.selectedPageIndex,
        },
      });

      // Load audit session and push state to webview alongside score
      try {
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
