import type {
  DesignAnalyzerConfig,
} from '../analyzer/config/types';
import type {
  ScorePanelHostToWebviewMessagePayload,
  ScoreResult,
  StoryAssessmentDiffResult,
  StoryAssessmentReportSnapshot,
} from '../analyzer/contracts/scorePanel';
import type { AnalyzerWorkspaceHandoffPayload } from '../design-studio/contracts/designStudioModels';
import {
  clampSelectedPageIndex,
} from './scorePanelProtocol';
import {
  defaultReviewPacketPreviewOptions,
  type ReviewPacketPreviewOptions,
} from '../analyzer/score/reviewPacketPreview';

export type ScorePanelStateService = ReturnType<typeof createScorePanelStateService>;

export function createScorePanelStateService() {
  let pendingMessages: ScorePanelHostToWebviewMessagePayload[] = [];
  let currentResult: ScoreResult | undefined;
  let savedConfig: DesignAnalyzerConfig | null = null;
  let selectedPageIndex = 0;
  let reviewPacketPreviewOptions = defaultReviewPacketPreviewOptions;
  let lastScoreDiagnosticsJson: string | undefined;
  let storyAssessmentCurrentSnapshot: StoryAssessmentReportSnapshot | undefined;
  let storyAssessmentDiffByPage: Record<string, StoryAssessmentDiffResult> | undefined;
  let storyAssessmentLastComparedAt: string | undefined;
  let currentHandoffPayload: AnalyzerWorkspaceHandoffPayload | undefined;

  return {
    getPendingMessages(): ScorePanelHostToWebviewMessagePayload[] {
      return pendingMessages;
    },
    setPendingMessages(messages: ScorePanelHostToWebviewMessagePayload[]): void {
      pendingMessages = messages;
    },
    enqueuePendingMessage(message: ScorePanelHostToWebviewMessagePayload): void {
      pendingMessages = [...pendingMessages, message];
    },
    shiftPendingMessage(): ScorePanelHostToWebviewMessagePayload | undefined {
      const [next, ...rest] = pendingMessages;
      pendingMessages = rest;
      return next;
    },
    resetPendingMessages(): void {
      pendingMessages = [];
    },
    getCurrentResult(): ScoreResult | undefined {
      return currentResult;
    },
    setCurrentResult(result: ScoreResult | undefined): void {
      currentResult = result;
    },
    getSavedConfig(): DesignAnalyzerConfig | null {
      return savedConfig;
    },
    setSavedConfig(config: DesignAnalyzerConfig | null): void {
      savedConfig = config;
    },
    getSelectedPageIndex(): number {
      return selectedPageIndex;
    },
    setSelectedPageIndex(pageIndex: number, pageCount: number): void {
      selectedPageIndex = clampSelectedPageIndex(pageIndex, pageCount);
    },
    getReviewPacketPreviewOptions() {
      return reviewPacketPreviewOptions;
    },
    setReviewPacketPreviewOptions(options: ReviewPacketPreviewOptions): void {
      reviewPacketPreviewOptions = options;
    },
    getLastScoreDiagnosticsJson(): string | undefined {
      return lastScoreDiagnosticsJson;
    },
    setLastScoreDiagnosticsJson(value: string | undefined): void {
      lastScoreDiagnosticsJson = value;
    },
    getStoryAssessmentCurrentSnapshot(): StoryAssessmentReportSnapshot | undefined {
      return storyAssessmentCurrentSnapshot;
    },
    setStoryAssessmentCurrentSnapshot(snapshot: StoryAssessmentReportSnapshot | undefined): void {
      storyAssessmentCurrentSnapshot = snapshot;
    },
    getStoryAssessmentDiffByPage(): Record<string, StoryAssessmentDiffResult> | undefined {
      return storyAssessmentDiffByPage;
    },
    setStoryAssessmentDiffByPage(diff: Record<string, StoryAssessmentDiffResult> | undefined): void {
      storyAssessmentDiffByPage = diff;
    },
    getStoryAssessmentLastComparedAt(): string | undefined {
      return storyAssessmentLastComparedAt;
    },
    setStoryAssessmentLastComparedAt(value: string | undefined): void {
      storyAssessmentLastComparedAt = value;
    },
    getCurrentHandoffPayload(): AnalyzerWorkspaceHandoffPayload | undefined {
      return currentHandoffPayload;
    },
    setCurrentHandoffPayload(payload: AnalyzerWorkspaceHandoffPayload | undefined): void {
      currentHandoffPayload = payload;
    },
    resetForHandoff(): void {
      currentResult = undefined;
      savedConfig = null;
      selectedPageIndex = 0;
      pendingMessages = [];
    },
    resetForDispose(): void {
      currentResult = undefined;
      savedConfig = null;
      pendingMessages = [];
      currentHandoffPayload = undefined;
    },
  };
}
