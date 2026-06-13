import {
  DESIGN_STUDIO_ARTIFACT_KINDS,
  DESIGN_STUDIO_APPROVAL_STATES,
  DESIGN_STUDIO_MATERIALIZATION_MODES,
  DESIGN_STUDIO_SOURCE_ROLES,
} from './designStudioModels';
import type {
  DesignBrief,
  DesignIterationRecord,
  DesignStudioArtifactKind,
  MaterializationRequest,
  RefinementProposal,
} from './designStudioModels';
import { validateMaterializationRequestSemantics } from '../materialization/materializationCoordinator';

const MATERIALIZATION_TARGET_SURFACE_TYPES = ['pbirReport', 'fabricApp', 'screenshotBundle'] as const;
const MATERIALIZATION_TARGET_ANALYZERS = ['pbirDesignReview', 'fabricAppReadiness', 'fabricAppReview'] as const;
const MATERIALIZATION_TARGET_PROFILES = [
  'default',
  'migrationReadiness',
  'executive',
  'consultant',
  'governance',
  'accessibility',
  'fabricAppQuality',
] as const;

export const DESIGN_STUDIO_PROTOCOL_VERSION = 1;
export const DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION = 1;

export const DESIGN_STUDIO_HOST_MESSAGE_TYPES = [
  'studioState',
  'artifactSaved',
  'artifactProposed',
  'artifactApproved',
  'materializationRequested',
  'iterationComparison',
  'analyzerHandoffOpened',
] as const;

export const DESIGN_STUDIO_WEBVIEW_MESSAGE_TYPES = [
  'webviewReady',
  'loadStudioState',
  'saveArtifact',
  'proposeArtifact',
  'approveArtifact',
  'requestMaterialization',
  'compareIterations',
  'openAnalyzerHandoff',
] as const;

export type DesignStudioHostMessageType = typeof DESIGN_STUDIO_HOST_MESSAGE_TYPES[number];
export type DesignStudioWebviewMessageType = typeof DESIGN_STUDIO_WEBVIEW_MESSAGE_TYPES[number];

interface DesignStudioEnvelope {
  protocolVersion: typeof DESIGN_STUDIO_PROTOCOL_VERSION;
  schemaVersion: typeof DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function readString(value: Record<string, unknown>, key: string): string | undefined {
  return typeof value[key] === 'string' ? value[key] as string : undefined;
}

function readNumber(value: Record<string, unknown>, key: string): number | undefined {
  return typeof value[key] === 'number' ? value[key] as number : undefined;
}

function isArtifactKind(value: string): value is DesignStudioArtifactKind {
  return (DESIGN_STUDIO_ARTIFACT_KINDS as readonly string[]).includes(value);
}

function isApprovalState(value: string): boolean {
  return (DESIGN_STUDIO_APPROVAL_STATES as readonly string[]).includes(value);
}

function isMaterializationMode(value: string): boolean {
  return (DESIGN_STUDIO_MATERIALIZATION_MODES as readonly string[]).includes(value);
}

function isSourceRole(value: string): boolean {
  return (DESIGN_STUDIO_SOURCE_ROLES as readonly string[]).includes(value);
}

function isArtifactId(value: string): boolean {
  return value.trim().length > 0 && value.includes(':');
}

function isArtifactVersionId(value: string): boolean {
  return /^[^@\s]+@v\d+$/.test(value);
}

function isMaterializationSurfaceType(value: string): boolean {
  return (MATERIALIZATION_TARGET_SURFACE_TYPES as readonly string[]).includes(value);
}

function isMaterializationAnalyzer(value: string): boolean {
  return (MATERIALIZATION_TARGET_ANALYZERS as readonly string[]).includes(value);
}

function isMaterializationProfile(value: string): boolean {
  return (MATERIALIZATION_TARGET_PROFILES as readonly string[]).includes(value);
}

function isSourceLineage(value: unknown): boolean {
  return Array.isArray(value) && value.every((entry) => {
    if (!isRecord(entry)) {
      return false;
    }

    const artifactId = readString(entry, 'artifactId');
    const artifactKind = readString(entry, 'artifactKind');
    const artifactVersionId = readString(entry, 'artifactVersionId');
    const sourceRole = readString(entry, 'sourceRole');
    const approvalState = readString(entry, 'approvalState');
    const approvalTimestamp = readString(entry, 'approvalTimestamp');

    return !!artifactId
      && isArtifactId(artifactId)
      && !!artifactKind
      && isArtifactKind(artifactKind)
      && !!artifactVersionId
      && isArtifactVersionId(artifactVersionId)
      && !!sourceRole
      && isSourceRole(sourceRole)
      && !!approvalState
      && isApprovalState(approvalState)
      && !!approvalTimestamp;
  });
}

function isMaterializationRequestPayload(value: unknown): value is MaterializationRequest {
  if (!isRecord(value)) {
    return false;
  }

  const artifactIds = Array.isArray(value.sourceArtifactIds) ? value.sourceArtifactIds : undefined;
  const materializationMode = readString(value, 'materializationMode');
  const targetSurfaceType = readString(value, 'targetSurfaceType');
  const targetAnalyzer = readString(value, 'targetAnalyzer');
  const targetAnalyzerProfile = readString(value, 'targetAnalyzerProfile');
  const hasBaseShape = typeof value.id === 'string'
    && isArtifactId(value.id)
    && typeof value.threadId === 'string'
    && typeof value.kind === 'string'
    && value.kind === 'materializationRequest'
    && !!materializationMode
    && isMaterializationMode(materializationMode)
    && typeof value.version === 'number'
    && typeof value.lifecycleState === 'string'
    && typeof value.approvalState === 'string'
    && isApprovalState(value.approvalState)
    && isRecord(value.provenance)
    && Array.isArray(artifactIds)
    && artifactIds.length > 0
    && artifactIds.every((artifactId) => typeof artifactId === 'string' && isArtifactId(artifactId))
    && isSourceLineage(value.sourceLineage)
    && !!targetSurfaceType
    && isMaterializationSurfaceType(targetSurfaceType)
    && !!targetAnalyzer
    && isMaterializationAnalyzer(targetAnalyzer)
    && !!targetAnalyzerProfile
    && isMaterializationProfile(targetAnalyzerProfile);

  if (!hasBaseShape) {
    return false;
  }

  return validateMaterializationRequestSemantics(value as unknown as MaterializationRequest).ok;
}

function hasCompatibleEnvelope(value: Record<string, unknown>): boolean {
  return value.protocolVersion === DESIGN_STUDIO_PROTOCOL_VERSION
    && value.schemaVersion === DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION;
}

function buildVersionMismatchMessage(protocolVersion: unknown, schemaVersion: unknown): string {
  return `Design Studio protocol mismatch. Expected protocol ${DESIGN_STUDIO_PROTOCOL_VERSION} / schema ${DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION}, received protocol ${String(protocolVersion)} / schema ${String(schemaVersion)}.`;
}

export interface DesignStudioStudioState {
  threadId: string;
  currentBrief?: DesignBrief;
  iterationHistory: DesignIterationRecord[];
  pendingRefinementProposals: RefinementProposal[];
}

export type DesignStudioHostToWebviewMessagePayload =
  | (DesignStudioEnvelope & { type: 'studioState'; state: DesignStudioStudioState })
  | (DesignStudioEnvelope & { type: 'artifactSaved'; artifactKind: DesignStudioArtifactKind; artifactId: string; version: number })
  | (DesignStudioEnvelope & { type: 'artifactProposed'; artifactKind: DesignStudioArtifactKind; artifactId: string })
  | (DesignStudioEnvelope & { type: 'artifactApproved'; artifactKind: DesignStudioArtifactKind; artifactId: string; version: number })
  | (DesignStudioEnvelope & { type: 'materializationRequested'; request: MaterializationRequest })
  | (DesignStudioEnvelope & { type: 'iterationComparison'; iterationId: string; summary: string })
  | (DesignStudioEnvelope & { type: 'analyzerHandoffOpened'; requestId: string; target: 'analyzerWorkspace' });

export type DesignStudioWebviewToHostMessagePayload =
  | (DesignStudioEnvelope & { type: 'webviewReady' })
  | (DesignStudioEnvelope & { type: 'loadStudioState'; threadId: string })
  | (DesignStudioEnvelope & { type: 'saveArtifact'; artifactKind: DesignStudioArtifactKind; artifact: unknown })
  | (DesignStudioEnvelope & { type: 'proposeArtifact'; artifactKind: DesignStudioArtifactKind; artifactId: string })
  | (DesignStudioEnvelope & { type: 'approveArtifact'; artifactKind: DesignStudioArtifactKind; artifactId: string })
  | (DesignStudioEnvelope & { type: 'requestMaterialization'; request: MaterializationRequest })
  | (DesignStudioEnvelope & { type: 'compareIterations'; baseIterationId: string; candidateIterationId: string })
  | (DesignStudioEnvelope & { type: 'openAnalyzerHandoff'; requestId: string });

export function withDesignStudioEnvelope<T extends { type: string }>(message: T): T & DesignStudioEnvelope {
  return {
    ...message,
    protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
    schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
  };
}

export function parseDesignStudioHostMessage(value: unknown):
  | { ok: true; message: DesignStudioHostToWebviewMessagePayload }
  | { ok: false; error: string } {
  if (!isRecord(value)) {
    return { ok: false, error: 'Design Studio host message must be an object.' };
  }

  if (!hasCompatibleEnvelope(value)) {
    return {
      ok: false,
      error: buildVersionMismatchMessage(value.protocolVersion, value.schemaVersion),
    };
  }

  const type = readString(value, 'type');
  if (!type) {
    return { ok: false, error: 'Design Studio host message is missing a type.' };
  }

  switch (type) {
    case 'studioState':
      return isRecord(value.state)
        ? { ok: true, message: withDesignStudioEnvelope({ type, state: value.state as unknown as DesignStudioStudioState }) }
        : { ok: false, error: 'Design Studio studioState host message is missing state.' };
    case 'artifactSaved':
    case 'artifactApproved': {
      const artifactKind = readString(value, 'artifactKind');
      const artifactId = readString(value, 'artifactId');
      const version = readNumber(value, 'version');
      return artifactKind && isArtifactKind(artifactKind) && artifactId && version !== undefined
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifactId, version }) }
        : { ok: false, error: `Design Studio ${type} host message is missing required fields.` };
    }
    case 'artifactProposed': {
      const artifactKind = readString(value, 'artifactKind');
      const artifactId = readString(value, 'artifactId');
      return artifactKind && isArtifactKind(artifactKind) && artifactId
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifactId }) }
        : { ok: false, error: 'Design Studio artifactProposed host message is missing required fields.' };
    }
    case 'materializationRequested':
      return isMaterializationRequestPayload(value.request)
        ? { ok: true, message: withDesignStudioEnvelope({ type, request: value.request as unknown as MaterializationRequest }) }
        : { ok: false, error: 'Design Studio materializationRequested host message has an invalid request payload.' };
    case 'iterationComparison': {
      const iterationId = readString(value, 'iterationId');
      const summary = readString(value, 'summary');
      return iterationId && summary
        ? { ok: true, message: withDesignStudioEnvelope({ type, iterationId, summary }) }
        : { ok: false, error: 'Design Studio iterationComparison host message is missing required fields.' };
    }
    case 'analyzerHandoffOpened': {
      const requestId = readString(value, 'requestId');
      const target = readString(value, 'target');
      return requestId && target === 'analyzerWorkspace'
        ? { ok: true, message: withDesignStudioEnvelope({ type, requestId, target }) }
        : { ok: false, error: 'Design Studio analyzerHandoffOpened host message is missing required fields.' };
    }
    default:
      return { ok: false, error: `Unsupported Design Studio host message type: ${type}.` };
  }
}

export function parseDesignStudioWebviewMessage(value: unknown):
  | { ok: true; message: DesignStudioWebviewToHostMessagePayload }
  | { ok: false; error: string } {
  if (!isRecord(value)) {
    return { ok: false, error: 'Design Studio webview message must be an object.' };
  }

  if (!hasCompatibleEnvelope(value)) {
    return {
      ok: false,
      error: buildVersionMismatchMessage(value.protocolVersion, value.schemaVersion),
    };
  }

  const type = readString(value, 'type');
  if (!type) {
    return { ok: false, error: 'Design Studio webview message is missing a type.' };
  }

  switch (type) {
    case 'webviewReady':
      return { ok: true, message: withDesignStudioEnvelope({ type }) };
    case 'loadStudioState': {
      const threadId = readString(value, 'threadId');
      return threadId
        ? { ok: true, message: withDesignStudioEnvelope({ type, threadId }) }
        : { ok: false, error: 'Design Studio loadStudioState webview message is missing threadId.' };
    }
    case 'saveArtifact': {
      const artifactKind = readString(value, 'artifactKind');
      return artifactKind && isArtifactKind(artifactKind) && 'artifact' in value
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifact: value.artifact }) }
        : { ok: false, error: 'Design Studio saveArtifact webview message is missing required fields.' };
    }
    case 'proposeArtifact':
    case 'approveArtifact': {
      const artifactKind = readString(value, 'artifactKind');
      const artifactId = readString(value, 'artifactId');
      return artifactKind && isArtifactKind(artifactKind) && artifactId
        ? { ok: true, message: withDesignStudioEnvelope({ type, artifactKind, artifactId }) }
        : { ok: false, error: `Design Studio ${type} webview message is missing required fields.` };
    }
    case 'requestMaterialization':
      return isMaterializationRequestPayload(value.request)
        ? { ok: true, message: withDesignStudioEnvelope({ type, request: value.request as unknown as MaterializationRequest }) }
        : { ok: false, error: 'Design Studio requestMaterialization webview message has an invalid request payload.' };
    case 'compareIterations': {
      const baseIterationId = readString(value, 'baseIterationId');
      const candidateIterationId = readString(value, 'candidateIterationId');
      return baseIterationId && candidateIterationId
        ? { ok: true, message: withDesignStudioEnvelope({ type, baseIterationId, candidateIterationId }) }
        : { ok: false, error: 'Design Studio compareIterations webview message is missing required fields.' };
    }
    case 'openAnalyzerHandoff': {
      const requestId = readString(value, 'requestId');
      return requestId
        ? { ok: true, message: withDesignStudioEnvelope({ type, requestId }) }
        : { ok: false, error: 'Design Studio openAnalyzerHandoff webview message is missing requestId.' };
    }
    default:
      return { ok: false, error: `Unsupported Design Studio webview message type: ${type}.` };
  }
}
