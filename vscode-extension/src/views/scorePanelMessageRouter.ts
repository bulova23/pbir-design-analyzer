import * as vscode from 'vscode';
import type {
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownRenderOptions,
  ScorePanelWebviewToHostMessagePayload,
} from '../analyzer/contracts/scorePanel';
import { revealNavigationTargetInPbirExplorer, revealVisualInPbirExplorer } from './pbirExplorerReveal';
import { clampSelectedPageIndex, parseScorePanelWebviewMessage } from './scorePanelProtocol';

type IntentFeedbackMessage = Extract<ScorePanelWebviewToHostMessagePayload, { type: 'setIntentFeedback' }>;

export type ScorePanelMessageRouterDependencies = {
  getPageCount: () => number;
  onReady: () => void | Promise<void>;
  onRefresh: () => void | Promise<void>;
  onSelectTab: (pageIndex: number) => void | Promise<void>;
  onSetIntentFeedback: (message: IntentFeedbackMessage) => void | Promise<void>;
  onUploadScreenshots: () => void | Promise<void>;
  onAttachScreenshot: (pageName: string) => void | Promise<void>;
  onRemoveScreenshot: (captureId: string) => void | Promise<void>;
  onAssignCapture: (captureId: string, targetPageName: string) => void | Promise<void>;
  onAnalyzeCapture: (captureId: string, pageName: string) => void | Promise<void>;
  onExportReviewWorkflow: () => void | Promise<void>;
  onSetReviewPacketPreviewProfile: (profile: ReviewWorkflowExportProfile) => void | Promise<void>;
  onSetReviewPacketPreviewTemplateVariant: (templateVariant: ReviewWorkflowMarkdownRenderOptions['templateVariant']) => void | Promise<void>;
  onOpenReviewPacketPreview: () => void | Promise<void>;
  onSetRenderedReviewStatus?: (itemId: string, status: Extract<ScorePanelWebviewToHostMessagePayload, { type: 'setRenderedReviewStatus' }>['status']) => void | Promise<void>;
  onSetRenderedReviewNote?: (itemId: string, note: string) => void | Promise<void>;
  onAttachRenderedScreenshot?: (itemId: string) => void | Promise<void>;
  onOpenInPbiLens?: (pageName?: string, visualId?: string) => void | Promise<void>;
  onToggleFixOpportunitySelection: (opportunityId: string) => void | Promise<void>;
  onPreviewSelectedFixOpportunities: () => void | Promise<void>;
  onApproveSelectedFixOpportunities: () => void | Promise<void>;
  onApplySelectedFixOpportunities: () => void | Promise<void>;
  onRollbackFixSession: (sessionId: string) => void | Promise<void>;
  onRegenerateFixOpportunities: (opportunityIds?: string[]) => void | Promise<void>;
  onApproveFixOpportunity: (opportunityId: string) => void | Promise<void>;
  onApplyFixOpportunity: (opportunityId: string) => void | Promise<void>;
  onRollbackFixOpportunity: (opportunityId: string) => void | Promise<void>;
  onOpenSettings: () => void | Promise<void>;
};

type ScorePanelMessageRouterFactory = ((deps: ScorePanelMessageRouterDependencies) => {
  route: (message: unknown) => Promise<void>;
}) & {
  buildHandoffMessage: (input: {
    candidateId: string;
    analyzerId: string;
    analyzerProfileId: string;
  }) => string;
};

export const createScorePanelMessageRouter: ScorePanelMessageRouterFactory = Object.assign(
  (deps: ScorePanelMessageRouterDependencies) => ({
    async route(message: unknown): Promise<void> {
      const parsedMessage = parseScorePanelWebviewMessage(message);
      if (!parsedMessage.ok) {
        void vscode.window.showWarningMessage(parsedMessage.error);
        return;
      }

      const payload = parsedMessage.message;
      switch (payload.type) {
        case 'webviewReady':
          await deps.onReady();
          return;
        case 'refresh':
          await deps.onRefresh();
          return;
        case 'selectTab':
          await deps.onSelectTab(clampSelectedPageIndex(payload.pageIndex, deps.getPageCount()));
          return;
        case 'setIntentFeedback':
          await deps.onSetIntentFeedback(payload);
          return;
        case 'revealVisual': {
          const revealed = await revealVisualInPbirExplorer(payload.pageName, payload.visualId);
          if (!revealed) {
            void vscode.window.showWarningMessage(
              `Could not locate '${payload.visualId}' on page '${payload.pageName}' in the PBIR sidecar.`,
            );
          }
          return;
        }
        case 'navigateToTarget': {
          const revealed = await revealNavigationTargetInPbirExplorer(payload.target);
          if (!revealed) {
            void vscode.window.showWarningMessage(
              `Could not navigate to '${payload.target.label}'. ${payload.target.reason}`,
            );
          }
          return;
        }
        case 'uploadScreenshots':
          await deps.onUploadScreenshots();
          return;
        case 'attachScreenshot':
          await deps.onAttachScreenshot(payload.pageName);
          return;
        case 'removeScreenshot':
          await deps.onRemoveScreenshot(payload.captureId);
          return;
        case 'assignCapture':
          await deps.onAssignCapture(payload.captureId, payload.targetPageName);
          return;
        case 'analyzeCapture':
          await deps.onAnalyzeCapture(payload.captureId, payload.pageName);
          return;
        case 'exportReviewWorkflow':
          await deps.onExportReviewWorkflow();
          return;
        case 'setReviewPacketPreviewProfile':
          await deps.onSetReviewPacketPreviewProfile(payload.profile);
          return;
        case 'setReviewPacketPreviewTemplateVariant':
          await deps.onSetReviewPacketPreviewTemplateVariant(payload.templateVariant);
          return;
        case 'openReviewPacketPreview':
          await deps.onOpenReviewPacketPreview();
          return;
        case 'setRenderedReviewStatus':
          await deps.onSetRenderedReviewStatus?.(payload.itemId, payload.status);
          return;
        case 'setRenderedReviewNote':
          await deps.onSetRenderedReviewNote?.(payload.itemId, payload.note);
          return;
        case 'attachRenderedScreenshot':
          await deps.onAttachRenderedScreenshot?.(payload.itemId);
          return;
        case 'openInPbiLens':
          await deps.onOpenInPbiLens?.(payload.pageName, payload.visualId);
          return;
        case 'toggleFixOpportunitySelection':
          await deps.onToggleFixOpportunitySelection(payload.opportunityId);
          return;
        case 'previewSelectedFixOpportunities':
          await deps.onPreviewSelectedFixOpportunities();
          return;
        case 'approveSelectedFixOpportunities':
          await deps.onApproveSelectedFixOpportunities();
          return;
        case 'applySelectedFixOpportunities':
          await deps.onApplySelectedFixOpportunities();
          return;
        case 'rollbackFixSession':
          await deps.onRollbackFixSession(payload.sessionId);
          return;
        case 'regenerateFixOpportunities':
          await deps.onRegenerateFixOpportunities(payload.opportunityIds);
          return;
        case 'approveFixOpportunity':
          await deps.onApproveFixOpportunity(payload.opportunityId);
          return;
        case 'applyFixOpportunity':
          await deps.onApplyFixOpportunity(payload.opportunityId);
          return;
        case 'rollbackFixOpportunity':
          await deps.onRollbackFixOpportunity(payload.opportunityId);
          return;
        case 'openSettings':
          await deps.onOpenSettings();
          return;
      }
    },
  }),
  {
    buildHandoffMessage: ({
      candidateId,
      analyzerId,
      analyzerProfileId,
    }: {
      candidateId: string;
      analyzerId: string;
      analyzerProfileId: string;
    }): string => `Analyzer Workspace opened from Design Studio handoff for candidate ${candidateId}. Analysis has not started. Run Retry when you are ready to start ${analyzerId} with profile ${analyzerProfileId}.`,
  },
);
