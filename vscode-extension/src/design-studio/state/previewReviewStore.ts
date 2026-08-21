import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import type * as vscode from 'vscode';

export const DESIGN_STUDIO_PREVIEW_REVIEW_SCHEMA_VERSION = 'design-studio-preview-review/v1';

export type DesignStudioPreviewReviewReviewerAction =
  | 'pending'
  | 'markedReviewed'
  | 'revisionRequested'
  | 'deferred'
  | 'analyzerCandidateMetadataPrepared';

export type DesignStudioPreviewReviewReadinessState =
  | 'readyForDesignReview'
  | 'revisionRequested'
  | 'deferred'
  | 'readyForAnalyzerCandidateMetadata'
  | 'blocked';

export interface DesignStudioPreviewReviewFileInventoryItem {
  artifactType: string;
  relativePath: string;
  reference: string;
  contentType: string;
  hashSha256: string;
  byteLength: number;
}

export interface DesignStudioPreviewReviewHashInventoryItem {
  hashKind: string;
  referenceId: string;
  hashSha256: string;
  description: string;
}

export interface DesignStudioPreviewReviewLineage {
  previewPackageRef: string;
  generationManifestRef: string;
  pbirIrRef: string;
  previewManifestRef: string;
  sourceWriteManifestRef: string;
  immutableLineage: string[];
}

export interface DesignStudioPreviewReviewRollbackMetadata {
  rollbackPlanRef: string;
  rollbackPlanHash: string;
  actionCount: number;
  automaticRollbackExecuted: boolean;
}

export interface DesignStudioPreviewPackageReviewReference {
  packageId: string;
  schemaVersion: 'pbir-preview-package/v1';
  packageHash: string;
  generatedUtc: string;
  metadataOnly: boolean;
  localOnly: boolean;
  containsPhysicalFileContent: boolean;
  zipCreated: boolean;
  deployableArtifactsAllowed: boolean;
  summary: {
    fileCount: number;
    warningCount: number;
    rejectedArtifactCount: number;
  };
  fileInventory: DesignStudioPreviewReviewFileInventoryItem[];
  hashInventory: DesignStudioPreviewReviewHashInventoryItem[];
  lineage: DesignStudioPreviewReviewLineage;
  rollbackMetadata: DesignStudioPreviewReviewRollbackMetadata;
  warnings: string[];
  rejectedArtifacts: string[];
}

export interface DesignStudioPreviewReviewHandoffReference {
  handoffId: string;
  schemaVersion: 'pbir-review-handoff/v1';
  reviewTarget: 'DesignStudio' | 'AnalyzerWorkspace';
  reviewReadiness: 'incomplete' | 'readyForDesignReview' | 'readyForAnalyzerReview' | 'blocked';
  requiredReviewerAction: string;
  previewPackageReference: {
    packageId: string;
    schemaVersion: 'pbir-preview-package/v1';
    packageHash: string;
  };
  pbirIrReference: {
    irId: string;
    schemaVersion: string;
    contentHash: string;
  };
  analyzerWorkspaceBoundary: {
    validationOccurred: boolean;
    automaticValidationRequested: boolean;
    automaticValidationAllowed: boolean;
    workspaceLaunchRequested: boolean;
    validationStatus: string;
  };
  deploymentBoundary: {
    deploymentRequested: boolean;
    deploymentAllowed: boolean;
  };
  warnings: string[];
}

export interface DesignStudioPreviewReviewBoundaryRequests {
  automaticAnalyzerExecutionRequested: boolean;
  automaticAnalyzerLaunchRequested: boolean;
  microsoftSkillsExecutionRequested: boolean;
  providerInvocationRequested: boolean;
  apiInvocationRequested: boolean;
  cliInvocationRequested: boolean;
  deploymentRequested: boolean;
}

export interface DesignStudioPreviewReviewInput {
  schemaVersion: typeof DESIGN_STUDIO_PREVIEW_REVIEW_SCHEMA_VERSION;
  previewReviewId: string;
  previewPackage: DesignStudioPreviewPackageReviewReference;
  reviewHandoff: DesignStudioPreviewReviewHandoffReference;
  reviewerAction: DesignStudioPreviewReviewReviewerAction;
  reviewerNotes: string;
  readinessState: DesignStudioPreviewReviewReadinessState;
  warnings: string[];
  boundaryRequests: DesignStudioPreviewReviewBoundaryRequests;
}

export interface DesignStudioPreviewReviewOnlyBoundary {
  reportMutationAllowed: false;
  analyzerExecutionAllowed: false;
  analyzerLaunchAllowed: false;
  microsoftSkillsExecutionAllowed: false;
  providerInvocationAllowed: false;
  apiInvocationAllowed: false;
  cliInvocationAllowed: false;
  deploymentAllowed: false;
  deployablePbirGenerationAllowed: false;
  reportJsonGenerationAllowed: false;
  definitionPbirGenerationAllowed: false;
}

export interface DesignStudioAnalyzerCandidateMetadata {
  prepared: boolean;
  preparedAt: string;
  preparedBy: string;
  previewPackageId: string;
  reviewHandoffId: string;
  pbirIrReference: string;
  previewPackageHash: string;
  analyzerExecutionRequested: false;
  analyzerLaunchRequested: false;
  validationOccurred: false;
}

export interface DesignStudioPreviewReviewRecord extends DesignStudioPreviewReviewInput {
  recordedAt: string;
  reviewTimestamp?: string;
  reviewerId?: string;
  analyzerCandidateMetadata?: DesignStudioAnalyzerCandidateMetadata;
  reviewOnlyBoundary: DesignStudioPreviewReviewOnlyBoundary;
}

export interface DesignStudioPreviewReviewState {
  threadId: string;
  currentReview?: DesignStudioPreviewReviewRecord;
  history: DesignStudioPreviewReviewRecord[];
}

export interface DesignStudioPreviewReviewSafetyGateResult {
  isAllowed: boolean;
  reasons: string[];
}

type PersistedDesignStudioPreviewReviewState = DesignStudioPreviewReviewState;

function threadKey(threadId: string): string {
  return crypto.createHash('md5').update(threadId).digest('hex').slice(0, 16);
}

function sessionDir(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(context.globalStorageUri.fsPath, 'design-studio', 'threads', threadKey(threadId));
}

function manifestPath(context: vscode.ExtensionContext, threadId: string): string {
  return path.join(sessionDir(context, threadId), 'preview-review.json');
}

function readPersistedState(filePath: string): PersistedDesignStudioPreviewReviewState | undefined {
  try {
    return JSON.parse(fs.readFileSync(filePath, 'utf8')) as PersistedDesignStudioPreviewReviewState;
  } catch {
    return undefined;
  }
}

function writePersistedState(filePath: string, state: PersistedDesignStudioPreviewReviewState): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, JSON.stringify(state, null, 2), 'utf8');
}

function isHash(value: string): boolean {
  return /^[a-f0-9]{64}$/i.test(value);
}

function hasForbiddenDeployablePath(value: string): string | undefined {
  const normalized = value.replace(/\\/g, '/').toLowerCase();
  const fileName = normalized.split('/').at(-1) ?? normalized;
  const forbidden = [
    'report.json',
    'definition.pbir',
    'model.bim',
    'tmdl',
  ];

  return forbidden.find((entry) => fileName === entry || normalized.includes(`/${entry}/`));
}

function createReviewOnlyBoundary(): DesignStudioPreviewReviewOnlyBoundary {
  return {
    reportMutationAllowed: false,
    analyzerExecutionAllowed: false,
    analyzerLaunchAllowed: false,
    microsoftSkillsExecutionAllowed: false,
    providerInvocationAllowed: false,
    apiInvocationAllowed: false,
    cliInvocationAllowed: false,
    deploymentAllowed: false,
    deployablePbirGenerationAllowed: false,
    reportJsonGenerationAllowed: false,
    definitionPbirGenerationAllowed: false,
  };
}

function cloneReviewRecord(record: DesignStudioPreviewReviewRecord): DesignStudioPreviewReviewRecord {
  return JSON.parse(JSON.stringify(record)) as DesignStudioPreviewReviewRecord;
}

function buildRecord(
  input: DesignStudioPreviewReviewInput,
  recordedAt: string,
): DesignStudioPreviewReviewRecord {
  return {
    ...input,
    previewPackage: {
      ...input.previewPackage,
      fileInventory: input.previewPackage.fileInventory.map((file) => ({ ...file })),
      hashInventory: input.previewPackage.hashInventory.map((entry) => ({ ...entry })),
      lineage: {
        ...input.previewPackage.lineage,
        immutableLineage: [...input.previewPackage.lineage.immutableLineage],
      },
      rollbackMetadata: { ...input.previewPackage.rollbackMetadata },
      warnings: [...input.previewPackage.warnings],
      rejectedArtifacts: [...input.previewPackage.rejectedArtifacts],
    },
    reviewHandoff: {
      ...input.reviewHandoff,
      previewPackageReference: { ...input.reviewHandoff.previewPackageReference },
      pbirIrReference: { ...input.reviewHandoff.pbirIrReference },
      analyzerWorkspaceBoundary: { ...input.reviewHandoff.analyzerWorkspaceBoundary },
      deploymentBoundary: { ...input.reviewHandoff.deploymentBoundary },
      warnings: [...input.reviewHandoff.warnings],
    },
    warnings: [...input.warnings],
    boundaryRequests: { ...input.boundaryRequests },
    recordedAt,
    reviewOnlyBoundary: createReviewOnlyBoundary(),
  };
}

export class DesignStudioPreviewReviewSafetyGate {
  validate(input: DesignStudioPreviewReviewInput): DesignStudioPreviewReviewSafetyGateResult {
    const reasons: string[] = [];

    if (input.schemaVersion !== DESIGN_STUDIO_PREVIEW_REVIEW_SCHEMA_VERSION) {
      reasons.push('preview review schema version must be design-studio-preview-review/v1.');
    }

    if (!input.previewReviewId.trim()) {
      reasons.push('preview review id is required.');
    }

    if (input.previewPackage.schemaVersion !== 'pbir-preview-package/v1'
      || input.reviewHandoff.schemaVersion !== 'pbir-review-handoff/v1') {
      reasons.push('preview package and review handoff references must use their v1 contracts.');
    }

    if (!input.previewPackage.metadataOnly
      || !input.previewPackage.localOnly
      || input.previewPackage.containsPhysicalFileContent
      || input.previewPackage.zipCreated
      || input.previewPackage.deployableArtifactsAllowed) {
      reasons.push('preview review package must be metadata-only, local-only, and non-deployable.');
    }

    if (input.previewPackage.fileInventory.length === 0) {
      reasons.push('preview review file inventory is required.');
    }

    for (const file of input.previewPackage.fileInventory) {
      const forbidden = hasForbiddenDeployablePath(file.relativePath) ?? hasForbiddenDeployablePath(file.reference);
      if (forbidden) {
        reasons.push(`preview review cannot reference deployable artifact paths: ${forbidden}.`);
      }

      if (!isHash(file.hashSha256)) {
        reasons.push('preview review file inventory must include complete SHA-256 hashes.');
      }
    }

    if (input.previewPackage.hashInventory.length === 0
      || input.previewPackage.hashInventory.some((entry) => !isHash(entry.hashSha256))
      || !isHash(input.previewPackage.packageHash)
      || !isHash(input.reviewHandoff.previewPackageReference.packageHash)
      || !isHash(input.reviewHandoff.pbirIrReference.contentHash)
      || !isHash(input.previewPackage.rollbackMetadata.rollbackPlanHash)) {
      reasons.push('preview review hash inventory must include complete SHA-256 hashes.');
    }

    const lineage = input.previewPackage.lineage;
    if (!lineage.previewPackageRef.trim()
      || !lineage.generationManifestRef.trim()
      || !lineage.pbirIrRef.trim()
      || !lineage.previewManifestRef.trim()
      || !lineage.sourceWriteManifestRef.trim()
      || lineage.immutableLineage.length === 0
      || !lineage.immutableLineage.includes(input.previewPackage.packageId)) {
      reasons.push('preview review lineage must include preview package, generation manifest, PBIR IR, preview manifest, source write manifest, and immutable lineage references.');
    }

    if (input.reviewHandoff.previewPackageReference.packageId !== input.previewPackage.packageId
      || input.reviewHandoff.previewPackageReference.packageHash !== input.previewPackage.packageHash) {
      reasons.push('preview review handoff must reference the supplied preview package.');
    }

    if (input.reviewHandoff.analyzerWorkspaceBoundary.validationOccurred
      || input.reviewHandoff.analyzerWorkspaceBoundary.automaticValidationRequested
      || input.reviewHandoff.analyzerWorkspaceBoundary.automaticValidationAllowed
      || input.reviewHandoff.analyzerWorkspaceBoundary.workspaceLaunchRequested) {
      reasons.push('Analyzer Workspace validation and launch must remain manual and not occurred.');
    }

    if (input.reviewHandoff.deploymentBoundary.deploymentRequested
      || input.reviewHandoff.deploymentBoundary.deploymentAllowed) {
      reasons.push('deployment requests are not allowed from Design Studio preview review.');
    }

    if (input.boundaryRequests.automaticAnalyzerExecutionRequested) {
      reasons.push('automatic Analyzer execution is not allowed from Design Studio preview review.');
    }

    if (input.boundaryRequests.automaticAnalyzerLaunchRequested) {
      reasons.push('automatic Analyzer launch is not allowed from Design Studio preview review.');
    }

    if (input.boundaryRequests.microsoftSkillsExecutionRequested
      || input.boundaryRequests.providerInvocationRequested
      || input.boundaryRequests.apiInvocationRequested
      || input.boundaryRequests.cliInvocationRequested
      || input.boundaryRequests.deploymentRequested) {
      reasons.push('Microsoft Skills, provider, API, CLI, and deployment requests are not allowed from Design Studio preview review.');
    }

    return {
      isAllowed: reasons.length === 0,
      reasons: [...new Set(reasons)].sort((left, right) => left.localeCompare(right)),
    };
  }
}

export async function loadDesignStudioPreviewReviewState(
  context: vscode.ExtensionContext,
  threadId: string,
): Promise<DesignStudioPreviewReviewState | undefined> {
  return readPersistedState(manifestPath(context, threadId));
}

export async function recordPreviewReviewHandoff(
  context: vscode.ExtensionContext,
  threadId: string,
  input: DesignStudioPreviewReviewInput,
  recordedAt = new Date().toISOString(),
): Promise<DesignStudioPreviewReviewState> {
  const safety = new DesignStudioPreviewReviewSafetyGate().validate(input);
  if (!safety.isAllowed) {
    throw new Error(safety.reasons.join('\n'));
  }

  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const currentReview = buildRecord(input, recordedAt);
  const history = existing?.currentReview
    ? [...existing.history.map(cloneReviewRecord), cloneReviewRecord(existing.currentReview)]
    : existing?.history.map(cloneReviewRecord) ?? [];
  const nextState: DesignStudioPreviewReviewState = {
    threadId,
    currentReview,
    history,
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function setPreviewReviewAction(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    previewReviewId: string;
    reviewerAction: Exclude<DesignStudioPreviewReviewReviewerAction, 'pending' | 'analyzerCandidateMetadataPrepared'>;
    reviewerNotes: string;
    reviewerId: string;
  },
  reviewTimestamp = new Date().toISOString(),
): Promise<DesignStudioPreviewReviewState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const currentReview = existing?.currentReview;
  if (!currentReview || currentReview.previewReviewId !== input.previewReviewId) {
    throw new Error('Preview review action must match the active Design Studio preview review.');
  }

  const readinessState: DesignStudioPreviewReviewReadinessState = input.reviewerAction === 'revisionRequested'
    ? 'revisionRequested'
    : input.reviewerAction === 'deferred'
      ? 'deferred'
      : 'readyForDesignReview';
  const nextReview: DesignStudioPreviewReviewRecord = {
    ...cloneReviewRecord(currentReview),
    reviewerAction: input.reviewerAction,
    reviewerNotes: input.reviewerNotes,
    reviewerId: input.reviewerId,
    reviewTimestamp,
    readinessState,
  };
  const nextState: DesignStudioPreviewReviewState = {
    threadId,
    currentReview: nextReview,
    history: existing?.history.map(cloneReviewRecord) ?? [],
  };
  writePersistedState(filePath, nextState);
  return nextState;
}

export async function prepareAnalyzerCandidateMetadata(
  context: vscode.ExtensionContext,
  threadId: string,
  input: {
    previewReviewId: string;
    reviewerNotes: string;
    reviewerId: string;
  },
  reviewTimestamp = new Date().toISOString(),
): Promise<DesignStudioPreviewReviewState> {
  const filePath = manifestPath(context, threadId);
  const existing = readPersistedState(filePath);
  const currentReview = existing?.currentReview;
  if (!currentReview || currentReview.previewReviewId !== input.previewReviewId) {
    throw new Error('Analyzer candidate metadata preparation must match the active Design Studio preview review.');
  }

  const nextReview: DesignStudioPreviewReviewRecord = {
    ...cloneReviewRecord(currentReview),
    reviewerAction: 'analyzerCandidateMetadataPrepared',
    reviewerNotes: input.reviewerNotes,
    reviewerId: input.reviewerId,
    reviewTimestamp,
    readinessState: 'readyForAnalyzerCandidateMetadata',
    analyzerCandidateMetadata: {
      prepared: true,
      preparedAt: reviewTimestamp,
      preparedBy: input.reviewerId,
      previewPackageId: currentReview.previewPackage.packageId,
      reviewHandoffId: currentReview.reviewHandoff.handoffId,
      pbirIrReference: currentReview.reviewHandoff.pbirIrReference.irId,
      previewPackageHash: currentReview.previewPackage.packageHash,
      analyzerExecutionRequested: false,
      analyzerLaunchRequested: false,
      validationOccurred: false,
    },
  };
  const nextState: DesignStudioPreviewReviewState = {
    threadId,
    currentReview: nextReview,
    history: existing?.history.map(cloneReviewRecord) ?? [],
  };
  writePersistedState(filePath, nextState);
  return nextState;
}
