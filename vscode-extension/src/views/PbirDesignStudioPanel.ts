import * as vscode from 'vscode';
import { resolveWebviewAssets } from './webviewAssets';
import { buildDesignStudioWorkspace } from '../design-studio/presentation/designStudioWorkspace';
import {
  approveRefinementProposal,
  deferRefinementProposal,
  loadRefinementState,
  rejectRefinementProposal,
} from '../design-studio/state/refinementStore';
import { loadIterationState } from '../design-studio/state/iterationStore';
import {
  parseDesignStudioWebviewMessage,
  withDesignStudioEnvelope,
  type DesignStudioHostToWebviewMessagePayload,
  type DesignStudioStudioState,
} from '../design-studio/contracts/designStudioProtocol';
import type { MaterializedSurfaceCandidate } from '../design-studio/contracts/designStudioModels';
import { PBIR_COMMANDS } from '../platform/extensionIds';

declare global {
  interface Window {
    __PBIR_DESIGN_STUDIO_BOOTSTRAP__?: {
      threadId: string;
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

  static async createOrShow(
    context: vscode.ExtensionContext,
    reportPath: string,
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

    const instance = new PbirDesignStudioPanel(context, panel, reportPath);
    PbirDesignStudioPanel.instance = instance;
    context.subscriptions.push({ dispose: () => instance.dispose() });
    return instance;
  }

  private constructor(
    context: vscode.ExtensionContext,
    panel: vscode.WebviewPanel,
    reportPath: string,
  ) {
    this.context = context;
    this.panel = panel;
    this.reportPath = reportPath;
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
      case 'openAnalyzerHandoff': {
        const candidate = this.handoffCandidatesByRequestId.get(parsed.message.requestId);
        if (!candidate) {
          void vscode.window.showWarningMessage('No executable Design Studio handoff is available for the selected draft.');
          return;
        }

        await vscode.commands.executeCommand(PBIR_COMMANDS.openAnalyzerWorkspaceHandoff, candidate);
        return;
      }
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
    const workspaceState = await buildDesignStudioWorkspace(this.context, this.reportPath);
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
        currentBrief: undefined,
        iterationHistory: iterationState?.iterations ?? [],
        pendingRefinementProposals: refinementState?.proposals ?? [],
        workspace: workspaceState.workspace,
      },
    });
  }

  private postMessage(message: { type: 'studioState'; state: DesignStudioStudioState }): void {
    const wrapped = withDesignStudioEnvelope(message) as DesignStudioHostToWebviewMessagePayload;

    if (!this.isReady) {
      this.pendingMessages.push(wrapped);
      return;
    }

    void this.panel.webview.postMessage(wrapped);
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
      window.__PBIR_DESIGN_STUDIO_BOOTSTRAP__ = ${JSON.stringify({ threadId: this.threadId })};
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
    if (PbirDesignStudioPanel.instance === this) {
      PbirDesignStudioPanel.instance = undefined;
    }

    while (this.disposables.length > 0) {
      this.disposables.pop()?.dispose();
    }
  }
}
