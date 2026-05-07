import * as vscode from 'vscode';
import { loadDesignAnalyzerConfig } from '../analyzer/config/store';
import type { DesignAnalyzerConfig } from '../analyzer/config/types';
import type {
  ScorePanelHostToWebviewMessage,
  ScorePanelWebviewToHostMessage,
  ScoreRequestPayload,
  ScoreResult,
} from '../analyzer/contracts/scorePanel';
import { LSPModelService } from '../services/lsp/LSPModelService';
import { resolveWebviewAssets } from './webviewAssets';
import { normalizeScoreResultPayload } from './scoreResultPayload';
import { revealVisualInPbirExplorer } from './pbirExplorerReveal';

export class PbirScorePanel {
  private static instance: PbirScorePanel | undefined;

  private readonly panel: vscode.WebviewPanel;
  private readonly bridge: LSPModelService | undefined;
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

  static async createOrShow(
    context: vscode.ExtensionContext,
    bridge: LSPModelService | undefined,
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
    bridge: LSPModelService | undefined,
    reportPath: string,
    pageName?: string,
  ) {
    this.context = context;
    this.panel = panel;
    this.bridge = bridge;
    this.reportPath = reportPath;
    this.pageName = pageName;
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
    }
  }

  private async refresh(): Promise<void> {
    this.selectedPageIndex = 0;
    this.postMessage({ type: 'loading' });

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
      this.postMessage({
        type: 'scoreState',
        state: {
          config: savedConfig,
          result: normalizedResult,
          selectedPageIndex: this.selectedPageIndex,
        },
      });
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
