import * as vscode from 'vscode';
import * as fs from 'fs';
import type {
  AuditProviderChoice,
  ConfigPanelHostToWebviewMessage,
  ConfigPanelStatus,
  ConfigPanelWebviewToHostMessage,
} from '../analyzer/contracts/configPanel';
import {
  getGovernanceDefaultsPath,
  loadAudiencePresets,
  loadDesignAnalyzerConfig,
  resetDesignAnalyzerConfig,
  saveDesignAnalyzerConfig,
} from '../analyzer/config/store';
import { resolveWebviewAssets } from './webviewAssets';

const ACTIVE_PROVIDER_KEY = 'pbir-audit.active-provider';
const ANTHROPIC_SECRET_KEY = 'pbir-audit.anthropic-api-key';
const OPENAI_SECRET_KEY = 'pbir-audit.openai-api-key';

export class PbirConfigPanel {
  private static instance: PbirConfigPanel | undefined;

  private readonly panel: vscode.WebviewPanel;
  private readonly context: vscode.ExtensionContext;
  private readonly disposables: vscode.Disposable[] = [];
  private isDisposed = false;
  private isReady = false;
  private pendingMessages: ConfigPanelHostToWebviewMessage[] = [];

  static async createOrShow(
    context: vscode.ExtensionContext,
    _bridge?: unknown,
  ): Promise<PbirConfigPanel> {
    void _bridge;

    if (PbirConfigPanel.instance) {
      PbirConfigPanel.instance.panel.reveal(vscode.ViewColumn.Beside);
      return PbirConfigPanel.instance;
    }

    const panel = vscode.window.createWebviewPanel(
      'pbirConfig',
      'Design Analyzer Configuration',
      vscode.ViewColumn.Beside,
      {
        enableScripts: true,
        retainContextWhenHidden: true,
        localResourceRoots: [vscode.Uri.joinPath(context.extensionUri, 'webview-dist')],
      },
    );

    const instance = new PbirConfigPanel(context, panel);
    PbirConfigPanel.instance = instance;
    context.subscriptions.push({ dispose: () => instance.dispose() });
    return instance;
  }

  private constructor(
    context: vscode.ExtensionContext,
    panel: vscode.WebviewPanel,
  ) {
    this.context = context;
    this.panel = panel;
    this.panel.webview.html = this.getReactHtml();

    this.panel.onDidDispose(() => this.dispose(), null, this.disposables);
    this.panel.webview.onDidReceiveMessage(
      (message) => {
        this.handleMessage(message as ConfigPanelWebviewToHostMessage).catch((err: unknown) => {
          const msg = err instanceof Error ? err.message : String(err);
          void vscode.window.showErrorMessage(`Design Analyzer config error: ${msg}`);
        });
      },
      null,
      this.disposables,
    );
  }

  private async handleMessage(message: ConfigPanelWebviewToHostMessage): Promise<void> {
    switch (message.type) {
      case 'webviewReady':
        this.isReady = true;
        await this.postCurrentState();
        this.flushPendingMessages();
        return;
      case 'saveConfig':
        try {
          const config = await saveDesignAnalyzerConfig(this.context, message.config);
          this.postMessage({
            type: 'configState',
            config,
            presets: loadAudiencePresets(this.context),
            status: {
              level: 'success',
              message: 'Analyzer configuration saved.',
            },
          });
        } catch (error) {
          this.postError(error);
        }
        return;
      case 'resetConfig':
        try {
          const config = await resetDesignAnalyzerConfig(this.context);
          this.postMessage({
            type: 'configState',
            config,
            presets: loadAudiencePresets(this.context),
            status: {
              level: 'success',
              message: 'Analyzer configuration reset to defaults.',
            },
          });
        } catch (error) {
          this.postError(error);
        }
        return;
      case 'openGovernanceJson':
        await this.openGovernanceJson();
        return;
      case 'saveAuditProvider':
        await this.handleSaveAuditProvider(message.provider, message.apiKey);
        return;
      case 'deleteAuditProviderKey':
        await this.handleDeleteAuditProviderKey(message.provider);
        return;
    }
  }

  private async postCurrentState(status?: ConfigPanelStatus): Promise<void> {
    try {
      const config = await loadDesignAnalyzerConfig(this.context);
      this.postMessage({
        type: 'configState',
        config,
        presets: loadAudiencePresets(this.context),
        status,
      });
      await this.postAuditProviderState();
    } catch (error) {
      this.postError(error);
    }
  }

  private async postAuditProviderState(saveStatus?: ConfigPanelStatus): Promise<void> {
    const activeProvider = (this.context.globalState.get<AuditProviderChoice>(ACTIVE_PROVIDER_KEY) ?? 'anthropic');
    const anthropicKey = await this.context.secrets.get(ANTHROPIC_SECRET_KEY);
    const openaiKey = await this.context.secrets.get(OPENAI_SECRET_KEY);
    this.postMessage({
      type: 'auditProviderState',
      activeProvider,
      anthropicConfigured: Boolean(anthropicKey?.trim()),
      openaiConfigured: Boolean(openaiKey?.trim()),
      saveStatus,
    });
  }

  private async handleDeleteAuditProviderKey(provider: AuditProviderChoice): Promise<void> {
    try {
      const secretKey = provider === 'openai' ? OPENAI_SECRET_KEY : ANTHROPIC_SECRET_KEY;
      await this.context.secrets.delete(secretKey);
      const label = provider === 'openai' ? 'OpenAI GPT-4o Vision' : 'Anthropic Claude Vision';
      await this.postAuditProviderState({ level: 'success', message: `${label} API key removed.` });
    } catch (error) {
      await this.postAuditProviderState({
        level: 'error',
        message: error instanceof Error ? error.message : 'Failed to remove API key.',
      });
    }
  }

  private async handleSaveAuditProvider(provider: AuditProviderChoice, apiKey: string): Promise<void> {
    try {
      await this.context.globalState.update(ACTIVE_PROVIDER_KEY, provider);
      const secretKey = provider === 'openai' ? OPENAI_SECRET_KEY : ANTHROPIC_SECRET_KEY;
      await this.context.secrets.store(secretKey, apiKey.trim());
      const label = provider === 'openai' ? 'OpenAI GPT-4o Vision' : 'Anthropic Claude Vision';
      await this.postAuditProviderState({ level: 'success', message: `${label} API key saved.` });
    } catch (error) {
      await this.postAuditProviderState({
        level: 'error',
        message: error instanceof Error ? error.message : 'Failed to save API key.',
      });
    }
  }

  private postMessage(message: ConfigPanelHostToWebviewMessage): void {
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

  private postError(error: unknown): void {
    const message = error instanceof Error ? error.message : String(error);
    this.postMessage({
      type: 'error',
      message,
    });
  }

  private async openGovernanceJson(): Promise<void> {
    const configPath = getGovernanceDefaultsPath(this.context);

    if (!fs.existsSync(configPath)) {
      this.postMessage({
        type: 'error',
        message: `Governance JSON not found: ${configPath}`,
      });
      return;
    }

    try {
      const document = await vscode.workspace.openTextDocument(vscode.Uri.file(configPath));
      await vscode.window.showTextDocument(document, { preview: false });
    } catch (error) {
      this.postError(error);
    }
  }

  private getReactHtml(): string {
    const assets = resolveWebviewAssets({
      webview: this.panel.webview,
      extensionUri: this.context.extensionUri,
      entryFile: 'analyzer-config/index.tsx',
      fallbackScriptFile: 'analyzer-config.js',
      fallbackStyleFile: 'analyzer-config.css',
      manifestFileName: 'manifest.analyzer-config.json',
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
  <title>Design Analyzer Configuration</title>
  ${assets.styleUris
    .map((uri) => `<link href="${uri.toString()}" rel="stylesheet">`)
    .join('\n  ')}
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
  <title>Design Analyzer Configuration</title>
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
    Analyzer config assets are missing. Run <code>npm run build:webview</code> in <code>vscode-extension</code>.
  </div>
</body>
</html>`;
  }

  private getNonce(): string {
    return Array.from({ length: 32 }, () =>
      Math.floor(Math.random() * 36).toString(36),
    ).join('');
  }

  dispose(): void {
    if (this.isDisposed) {
      return;
    }

    this.isDisposed = true;

    if (PbirConfigPanel.instance === this) {
      PbirConfigPanel.instance = undefined;
    }

    this.isReady = false;
    this.pendingMessages = [];

    while (this.disposables.length > 0) {
      const disposable = this.disposables.pop();
      disposable?.dispose();
    }

    if (this.panel.visible) {
      try {
        this.panel.dispose();
      } catch {
        // No-op during extension shutdown.
      }
    }
  }
}
