import * as fs from 'fs';
import * as vscode from 'vscode';
import { addCaptures, assignCapture, computeCoverage, loadSession, removeCapture, saveSession } from '../analyzer/audit/session';
import type { VisualAuditSession } from '../analyzer/audit/types';
import type { VisualAuditProvider } from '../analyzer/audit/providers/VisualAuditProvider';
import type {
  AuditCaptureSummary,
  AuditFindingDisplay,
  AuditPageState,
  AuditState,
  ScorePanelHostToWebviewMessagePayload,
  ScoreResult,
} from '../analyzer/contracts/scorePanel';

type AuditDeps = {
  context: vscode.ExtensionContext;
  getReportPath: () => string;
  getCurrentResult: () => ScoreResult | undefined;
  getAuditProvider: () => VisualAuditProvider;
  getAuditSession: () => VisualAuditSession | undefined;
  setAuditSession: (session: VisualAuditSession | undefined) => void;
  postMessage: (message: ScorePanelHostToWebviewMessagePayload) => void;
  showOpenDialog?: typeof vscode.window.showOpenDialog;
  showErrorMessage?: typeof vscode.window.showErrorMessage;
  addCaptures?: typeof addCaptures;
  loadSession?: typeof loadSession;
  saveSession?: typeof saveSession;
  removeCapture?: typeof removeCapture;
  assignCapture?: typeof assignCapture;
  unlinkStoredPath?: (storedPath: string) => void;
};

export function createScorePanelAuditWorkflowService(deps: AuditDeps) {
  const showOpenDialog = deps.showOpenDialog ?? vscode.window.showOpenDialog;
  const showErrorMessage = deps.showErrorMessage ?? vscode.window.showErrorMessage;
  const addCapturesImpl = deps.addCaptures ?? addCaptures;
  const loadSessionImpl = deps.loadSession ?? loadSession;
  const saveSessionImpl = deps.saveSession ?? saveSession;
  const removeCaptureImpl = deps.removeCapture ?? removeCapture;
  const assignCaptureImpl = deps.assignCapture ?? assignCapture;
  const unlinkStoredPath = deps.unlinkStoredPath ?? ((storedPath: string) => {
    if (fs.existsSync(storedPath)) {
      try {
        fs.unlinkSync(storedPath);
      } catch {
        // Non-fatal asset cleanup failure.
      }
    }
  });

  function pageNamesFromResult(): string[] {
    const result = deps.getCurrentResult();
    if (!result) return [];
    if (result.pageScores && result.pageScores.length > 0) {
      return result.pageScores.map((page) => page.pageName);
    }
    if (result.scoredPageName) {
      return [result.scoredPageName];
    }
    return [];
  }

  async function ensureAuditSession(): Promise<VisualAuditSession> {
    const existing = deps.getAuditSession();
    if (existing) {
      return existing;
    }

    const loaded = await loadSessionImpl(deps.context, deps.getReportPath());
    deps.setAuditSession(loaded);
    return loaded;
  }

  return {
    pageNamesFromResult,
    async uploadScreenshots(): Promise<void> {
      const uris = await showOpenDialog({
        title: 'Select Report Screenshots',
        canSelectMany: true,
        canSelectFiles: true,
        canSelectFolders: false,
        filters: { Images: ['png', 'jpg', 'jpeg', 'webp'] },
        openLabel: 'Add Screenshots',
      });

      if (!uris || uris.length === 0) {
        return;
      }

      const session = await ensureAuditSession();
      await addCapturesImpl(deps.context, session, uris.map((uri) => uri.fsPath), pageNamesFromResult());
      await saveSessionImpl(deps.context, session);
      deps.setAuditSession(session);
      await this.postAuditState();
    },
    async attachScreenshot(pageName: string): Promise<VisualAuditSession['pages'][number]['captures'][number] | undefined> {
      const uris = await showOpenDialog({
        title: `Attach Screenshot to "${pageName}"`,
        canSelectMany: false,
        canSelectFiles: true,
        canSelectFolders: false,
        filters: { Images: ['png', 'jpg', 'jpeg', 'webp'] },
        openLabel: 'Attach',
      });

      if (!uris || uris.length === 0) {
        return;
      }

      const session = await ensureAuditSession();
      await addCapturesImpl(deps.context, session, [uris[0].fsPath], [pageName]);
      await saveSessionImpl(deps.context, session);
      deps.setAuditSession(session);
      await this.postAuditState();
      return session.pages.find((page) => page.pageName === pageName)?.captures.at(-1);
    },
    async removeScreenshot(captureId: string): Promise<void> {
      const session = await ensureAuditSession();
      const allCaptures = [
        ...session.pages.flatMap((page) => page.captures),
        ...session.unmatchedCaptures,
      ];
      const capture = allCaptures.find((item) => item.captureId === captureId);

      removeCaptureImpl(session, captureId);
      if (capture?.storedPath) {
        unlinkStoredPath(capture.storedPath);
      }

      await saveSessionImpl(deps.context, session);
      deps.setAuditSession(session);
      await this.postAuditState();
    },
    async assignCapture(captureId: string, targetPageName: string): Promise<void> {
      const session = await ensureAuditSession();
      assignCaptureImpl(session, captureId, targetPageName);
      await saveSessionImpl(deps.context, session);
      deps.setAuditSession(session);
      await this.postAuditState();
    },
    async analyzeCapture(captureId: string, pageName: string): Promise<void> {
      const session = await ensureAuditSession();
      const page = session.pages.find((item) => item.pageName === pageName);
      const capture = page?.captures.find((item) => item.captureId === captureId);

      if (!capture) {
        void showErrorMessage(`Capture ${captureId} not found for page "${pageName}".`);
        return;
      }

      deps.postMessage({ type: 'auditAnalyzing', captureId });

      try {
        const pageScore = deps.getCurrentResult()?.pageScores?.find((item) => item.pageName === pageName);
        const findings = await deps.getAuditProvider().analyzeCapture({ capture, pageName, pageScore });

        if (page) {
          page.findings = page.findings.filter((finding) => finding.captureId !== captureId);
          page.findings.push(...findings);
        }

        await saveSessionImpl(deps.context, session);
        deps.setAuditSession(session);
        await this.postAuditState();
      } catch (error) {
        void showErrorMessage(`Audit analysis failed: ${error instanceof Error ? error.message : String(error)}`);
        await this.postAuditState();
      }
    },
    async loadAuditSession(): Promise<VisualAuditSession> {
      return ensureAuditSession();
    },
    buildAuditState(session: VisualAuditSession, providerConfigured: boolean): AuditState {
      const coverage = computeCoverage(session, pageNamesFromResult());

      const pages: AuditPageState[] = session.pages.map((page) => ({
        pageName: page.pageName,
        captures: page.captures.map((capture) => ({
          captureId: capture.captureId,
          pageName: capture.pageName,
          stateName: capture.stateName,
          fileName: capture.fileName,
          storedPath: capture.storedPath,
          findingCount: page.findings.filter((finding) => finding.captureId === capture.captureId).length,
        })),
        findings: page.findings.map((finding): AuditFindingDisplay => ({
          findingId: finding.findingId,
          captureId: finding.captureId,
          findingType: finding.findingType,
          severity: finding.severity,
          confidence: finding.confidence,
          issueSource: finding.issueSource,
          text: finding.text,
          recommendation: finding.recommendation,
          regionHint: finding.regionHint,
        })),
      }));

      const unmatchedCaptures: AuditCaptureSummary[] = session.unmatchedCaptures.map((capture) => ({
        captureId: capture.captureId,
        pageName: capture.pageName,
        stateName: capture.stateName,
        fileName: capture.fileName,
        storedPath: capture.storedPath,
        findingCount: 0,
      }));

      return {
        coverage,
        pages,
        unmatchedCaptures,
        isAnalyzing: false,
        providerName: deps.getAuditProvider().providerName,
        providerConfigured,
      };
    },
    async postAuditState(): Promise<void> {
      const session = await ensureAuditSession();
      const providerConfigured = await deps.getAuditProvider().isConfigured();
      deps.postMessage({
        type: 'auditState',
        audit: this.buildAuditState(session, providerConfigured),
      });
    },
  };
}
