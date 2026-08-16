import type {
  AuditState,
  IntentFeedbackConfirmation,
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownTemplateVariant,
  ScorePanelNavigationTarget,
  ScorePanelHostToWebviewMessagePayload,
  ScorePanelState,
  ScorePanelWebviewToHostMessagePayload,
  StoryConfidence,
} from '../analyzer/contracts/scorePanel';
import type { RenderedReviewStatus } from '../analyzer/renderedReview/types';

export const SCORE_PANEL_PROTOCOL_VERSION = 1;
export const SCORE_PANEL_SCHEMA_VERSION = 1;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function hasCompatibleEnvelope(value: Record<string, unknown>): boolean {
  return value.protocolVersion === SCORE_PANEL_PROTOCOL_VERSION
    && value.schemaVersion === SCORE_PANEL_SCHEMA_VERSION;
}

function buildVersionMismatchMessage(protocolVersion: unknown, schemaVersion: unknown): string {
  return `Score panel protocol mismatch. Expected protocol ${SCORE_PANEL_PROTOCOL_VERSION} / schema ${SCORE_PANEL_SCHEMA_VERSION}, received protocol ${String(protocolVersion)} / schema ${String(schemaVersion)}.`;
}

function readString(value: Record<string, unknown>, key: string): string | undefined {
  return typeof value[key] === 'string' ? value[key] as string : undefined;
}

function readNumber(value: Record<string, unknown>, key: string): number | undefined {
  return typeof value[key] === 'number' ? value[key] as number : undefined;
}

function hasStringArray(value: Record<string, unknown>, key: string): boolean {
  return Array.isArray(value[key]) && (value[key] as unknown[]).every((entry) => typeof entry === 'string');
}

function getPageCount(state: Pick<ScorePanelState, 'result'>): number {
  return Array.isArray(state.result.pageScores) ? state.result.pageScores.length : 0;
}

function hasOnlyAllowedKeys(value: Record<string, unknown>, allowedKeys: string[]): boolean {
  return Object.keys(value).every((key) => allowedKeys.includes(key));
}

function validateGuidedStoryImprovement(value: unknown): string | undefined {
  if (!isRecord(value)) {
    return 'Guided Story Improvements recommendations must be objects.';
  }

  const allowedKeys = ['id', 'title', 'summary', 'rationale', 'expectedImpact', 'priority', 'relatedImpactArea', 'navigationTarget'];
  if (!hasOnlyAllowedKeys(value, allowedKeys)) {
    return 'Guided Story Improvements recommendations may only include safe public fields.';
  }

  const targetError = value.navigationTarget === undefined
    ? undefined
    : validateNavigationTarget(value.navigationTarget);
  if (targetError) {
    return targetError;
  }

  return undefined;
}

function validateNavigationTarget(value: unknown): string | undefined {
  if (!isRecord(value)) {
    return 'Score panel navigation targets must be objects.';
  }

  const allowedKeys = ['kind', 'pageName', 'visualId', 'reportElement', 'label', 'reason', 'supportState'];
  if (!hasOnlyAllowedKeys(value, allowedKeys)) {
    return 'Score panel navigation targets may only include approved public fields.';
  }

  const kind = readString(value, 'kind');
  const label = readString(value, 'label');
  const reason = readString(value, 'reason');
  const supportState = readString(value, 'supportState');
  const pageName = readString(value, 'pageName');
  const visualId = readString(value, 'visualId');
  const reportElement = readString(value, 'reportElement');

  if (!kind || !label || !reason || !supportState) {
    return 'Score panel navigation targets are missing required fields.';
  }

  if (!['visual', 'page', 'report'].includes(kind)) {
    return 'Score panel navigation targets must use a supported kind.';
  }

  if (!['direct', 'fallback', 'unavailable'].includes(supportState)) {
    return 'Score panel navigation targets must use a supported supportState.';
  }

  if (kind === 'visual' && (!pageName || !visualId)) {
    return 'Score panel navigation targets for visuals must include pageName and visualId.';
  }

  if (kind === 'page' && !pageName) {
    return 'Score panel navigation targets for pages must include pageName.';
  }

  if (kind === 'report' && reportElement !== undefined && !['reportJson', 'pageJson', 'themeJson'].includes(reportElement)) {
    return 'Score panel navigation targets for reports must use a supported reportElement.';
  }

  return undefined;
}

function validateGuidedStoryImprovements(value: unknown): string | undefined {
  if (!isRecord(value)) {
    return 'Guided Story Improvements must be an object.';
  }

  const allowedKeys = ['highPriorityImprovements', 'mediumPriorityImprovements', 'storyImprovementRationale'];
  if (!hasOnlyAllowedKeys(value, allowedKeys)) {
    return 'Guided Story Improvements may only include the approved public fields.';
  }

  for (const key of ['highPriorityImprovements', 'mediumPriorityImprovements']) {
    const entries = value[key];
    if (entries !== undefined) {
      if (!Array.isArray(entries)) {
        return 'Guided Story Improvements recommendation groups must be arrays.';
      }

      for (const entry of entries) {
        const error = validateGuidedStoryImprovement(entry);
        if (error) {
          return error;
        }
      }
    }
  }

  if ('storyImprovementRationale' in value && typeof value.storyImprovementRationale !== 'string') {
    return 'Guided Story Improvements rationale must be a string.';
  }

  return undefined;
}

function validateGuidedStoryImprovementsInResult(value: Record<string, unknown>): string | undefined {
  const resultError = value.guidedStoryImprovements === undefined
    ? undefined
    : validateGuidedStoryImprovements(value.guidedStoryImprovements);
  if (resultError) {
    return resultError;
  }

  if (Array.isArray(value.pageScores)) {
    for (const pageScore of value.pageScores) {
      if (!isRecord(pageScore) || pageScore.guidedStoryImprovements === undefined) {
        continue;
      }

      const pageError = validateGuidedStoryImprovements(pageScore.guidedStoryImprovements);
      if (pageError) {
        return pageError;
      }
    }
  }

  return undefined;
}

export function clampSelectedPageIndex(selectedPageIndex: number, pageCount: number): number {
  if (!Number.isFinite(selectedPageIndex)) {
    return 0;
  }

  const normalizedIndex = Math.trunc(selectedPageIndex);
  return Math.min(Math.max(normalizedIndex, 0), Math.max(pageCount, 0));
}

export function normalizeScorePanelState(state: ScorePanelState): ScorePanelState {
  const normalizedSelectedPageIndex = clampSelectedPageIndex(
    state.selectedPageIndex,
    getPageCount(state),
  );

  return normalizedSelectedPageIndex === state.selectedPageIndex
    ? state
    : {
        ...state,
        selectedPageIndex: normalizedSelectedPageIndex,
      };
}

export function withScorePanelEnvelope<T extends { type: string }>(message: T) {
  return {
    ...message,
    protocolVersion: SCORE_PANEL_PROTOCOL_VERSION,
    schemaVersion: SCORE_PANEL_SCHEMA_VERSION,
  };
}

export function buildScorePanelState(state: Omit<ScorePanelState, 'protocolVersion' | 'schemaVersion'>): ScorePanelState {
  return normalizeScorePanelState({
    ...state,
    protocolVersion: SCORE_PANEL_PROTOCOL_VERSION,
    schemaVersion: SCORE_PANEL_SCHEMA_VERSION,
  });
}

export function parseScorePanelState(value: unknown):
  | { ok: true; state: ScorePanelState }
  | { ok: false; error: string } {
  if (!isRecord(value)) {
    return { ok: false, error: 'Score panel state payload must be an object.' };
  }

  if (!hasCompatibleEnvelope(value)) {
    return {
      ok: false,
      error: buildVersionMismatchMessage(value.protocolVersion, value.schemaVersion),
    };
  }

  if (!isRecord(value.config) || !isRecord(value.result)) {
    return { ok: false, error: 'Score panel state payload is missing config or result data.' };
  }

  if (typeof value.selectedPageIndex !== 'number') {
    return { ok: false, error: 'Score panel state payload is missing a numeric selectedPageIndex.' };
  }

  if (!Array.isArray(value.intentFeedback)) {
    return { ok: false, error: 'Score panel state payload is missing intent feedback state.' };
  }

  const guidedStoryError = validateGuidedStoryImprovementsInResult(value.result);
  if (guidedStoryError) {
    return { ok: false, error: guidedStoryError };
  }

  return {
    ok: true,
    state: normalizeScorePanelState(value as unknown as ScorePanelState),
  };
}

export function parseScorePanelHostMessage(value: unknown):
  | { ok: true; message: ScorePanelHostToWebviewMessagePayload }
  | { ok: false; error: string } {
  if (!isRecord(value)) {
    return { ok: false, error: 'Score panel host message must be an object.' };
  }

  if (!hasCompatibleEnvelope(value)) {
    return {
      ok: false,
      error: buildVersionMismatchMessage(value.protocolVersion, value.schemaVersion),
    };
  }

  const type = readString(value, 'type');
  if (!type) {
    return { ok: false, error: 'Score panel host message is missing a type.' };
  }

  switch (type) {
    case 'loading':
      return { ok: true, message: { type } };
    case 'error': {
      const message = readString(value, 'message');
      return message
        ? { ok: true, message: { type, message } }
        : { ok: false, error: 'Score panel error message is missing text.' };
    }
    case 'auditAnalyzing': {
      const captureId = readString(value, 'captureId');
      return captureId
        ? { ok: true, message: { type, captureId } }
        : { ok: false, error: 'Score panel auditAnalyzing message is missing captureId.' };
    }
    case 'auditState':
      return isRecord(value.audit)
        ? { ok: true, message: { type, audit: value.audit as unknown as AuditState } }
        : { ok: false, error: 'Score panel auditState payload is invalid.' };
    case 'scoreState': {
      const parsedState = parseScorePanelState(value.state);
      return parsedState.ok
        ? { ok: true, message: { type, state: parsedState.state } }
        : parsedState;
    }
    default:
      return { ok: false, error: `Unsupported score panel host message type: ${type}.` };
  }
}

export function parseScorePanelWebviewMessage(value: unknown):
  | { ok: true; message: ScorePanelWebviewToHostMessagePayload }
  | { ok: false; error: string } {
  if (!isRecord(value)) {
    return { ok: false, error: 'Score panel webview message must be an object.' };
  }

  if (!hasCompatibleEnvelope(value)) {
    return {
      ok: false,
      error: buildVersionMismatchMessage(value.protocolVersion, value.schemaVersion),
    };
  }

  const type = readString(value, 'type');
  if (!type) {
    return { ok: false, error: 'Score panel webview message is missing a type.' };
  }

  if ([
    'webviewReady',
    'refresh',
    'uploadScreenshots',
    'exportReviewWorkflow',
    'openReviewPacketPreview',
    'previewSelectedFixOpportunities',
    'approveSelectedFixOpportunities',
    'applySelectedFixOpportunities',
    'openSettings',
  ].includes(type)) {
    return { ok: true, message: { type } as ScorePanelWebviewToHostMessagePayload };
  }

  switch (type) {
    case 'selectTab': {
      const pageIndex = readNumber(value, 'pageIndex');
      return pageIndex !== undefined
        ? { ok: true, message: { type, pageIndex } }
        : { ok: false, error: 'Score panel selectTab message is missing pageIndex.' };
    }
    case 'setIntentFeedback': {
      const pageName = readString(value, 'pageName');
      const inferredIntent = readString(value, 'inferredIntent');
      const userConfirmation = readString(value, 'userConfirmation');
      if (!pageName || !inferredIntent || !userConfirmation) {
        return { ok: false, error: 'Score panel setIntentFeedback message is missing required fields.' };
      }

      return {
        ok: true,
        message: {
          type,
          pageName,
          inferredIntent,
          storyArchetype: readString(value, 'storyArchetype'),
          userConfirmation: userConfirmation as IntentFeedbackConfirmation,
          inferenceConfidence: readString(value, 'inferenceConfidence') as StoryConfidence | undefined,
          note: readString(value, 'note'),
        },
      };
    }
    case 'revealVisual': {
      const pageName = readString(value, 'pageName');
      const visualId = readString(value, 'visualId');
      return pageName && visualId
        ? { ok: true, message: { type, pageName, visualId } }
        : { ok: false, error: 'Score panel revealVisual message is missing required fields.' };
    }
    case 'navigateToTarget': {
      const targetError = validateNavigationTarget(value.target);
      return targetError
        ? { ok: false, error: `Score panel navigateToTarget message is invalid. ${targetError}` }
        : {
            ok: true,
            message: {
              type,
              target: value.target as ScorePanelNavigationTarget,
            },
          };
    }
    case 'attachScreenshot': {
      const pageName = readString(value, 'pageName');
      return pageName
        ? { ok: true, message: { type, pageName } }
        : { ok: false, error: 'Score panel attachScreenshot message is missing pageName.' };
    }
    case 'setRenderedReviewStatus': {
      const itemId = readString(value, 'itemId');
      const status = readString(value, 'status');
      const validStatuses: RenderedReviewStatus[] = ['Not Reviewed', 'Reviewed', 'Confirmed', 'Rejected', 'Deferred'];
      return itemId && status && validStatuses.includes(status as RenderedReviewStatus)
        ? { ok: true, message: { type, itemId, status: status as RenderedReviewStatus } }
        : { ok: false, error: 'Score panel rendered review status message is missing required fields.' };
    }
    case 'setRenderedReviewNote': {
      const itemId = readString(value, 'itemId');
      return itemId
        ? { ok: true, message: { type, itemId, note: readString(value, 'note') ?? '' } }
        : { ok: false, error: 'Score panel rendered review note message is missing itemId.' };
    }
    case 'openInPbiLens': {
      const pageName = readString(value, 'pageName');
      const visualId = readString(value, 'visualId');
      return { ok: true, message: { type, pageName, visualId } };
    }
    case 'attachRenderedScreenshot': {
      const itemId = readString(value, 'itemId');
      return itemId
        ? { ok: true, message: { type, itemId } }
        : { ok: false, error: 'Score panel rendered screenshot message is missing itemId.' };
    }
    case 'removeScreenshot':
    case 'analyzeCapture': {
      const captureId = readString(value, 'captureId');
      if (!captureId) {
        return { ok: false, error: `Score panel ${type} message is missing captureId.` };
      }

      if (type === 'removeScreenshot') {
        return { ok: true, message: { type, captureId } };
      }

      const pageName = readString(value, 'pageName');
      return pageName
        ? { ok: true, message: { type, captureId, pageName } }
        : { ok: false, error: 'Score panel analyzeCapture message is missing pageName.' };
    }
    case 'assignCapture': {
      const captureId = readString(value, 'captureId');
      const targetPageName = readString(value, 'targetPageName');
      return captureId && targetPageName
        ? { ok: true, message: { type, captureId, targetPageName } }
        : { ok: false, error: 'Score panel assignCapture message is missing required fields.' };
    }
    case 'setReviewPacketPreviewProfile': {
      const profile = readString(value, 'profile');
      return profile
        ? { ok: true, message: { type, profile: profile as ReviewWorkflowExportProfile } }
        : { ok: false, error: 'Score panel preview profile message is missing profile.' };
    }
    case 'setReviewPacketPreviewTemplateVariant': {
      const templateVariant = readString(value, 'templateVariant');
      return templateVariant
        ? { ok: true, message: { type, templateVariant: templateVariant as ReviewWorkflowMarkdownTemplateVariant } }
        : { ok: false, error: 'Score panel preview template message is missing templateVariant.' };
    }
    case 'toggleFixOpportunitySelection':
    case 'approveFixOpportunity':
    case 'applyFixOpportunity':
    case 'rollbackFixOpportunity': {
      const opportunityId = readString(value, 'opportunityId');
      return opportunityId
        ? { ok: true, message: { type, opportunityId } as ScorePanelWebviewToHostMessagePayload }
        : { ok: false, error: `Score panel ${type} message is missing opportunityId.` };
    }
    case 'rollbackFixSession': {
      const sessionId = readString(value, 'sessionId');
      return sessionId
        ? { ok: true, message: { type, sessionId } }
        : { ok: false, error: 'Score panel rollbackFixSession message is missing sessionId.' };
    }
    case 'regenerateFixOpportunities':
      return value.opportunityIds === undefined || hasStringArray(value, 'opportunityIds')
        ? {
            ok: true,
            message: {
              type,
              opportunityIds: value.opportunityIds as string[] | undefined,
            },
          }
        : { ok: false, error: 'Score panel regenerateFixOpportunities message has invalid opportunityIds.' };
    default:
      return { ok: false, error: `Unsupported score panel webview message type: ${type}.` };
  }
}
