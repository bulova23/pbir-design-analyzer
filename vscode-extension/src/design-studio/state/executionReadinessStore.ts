import type {
  DesignStudioExecutionReadinessViewModel,
  DesignStudioExecutionReadinessWarningSummaryViewModel,
} from '../contracts/designStudioShell';
import type { DesignStudioPreviewReviewRecord } from './previewReviewStore';

export const DESIGN_STUDIO_EXECUTION_READINESS_SCHEMA_VERSION = 'design-studio-execution-readiness/v1';

export type DesignStudioExecutionReadinessSummary =
  | 'notReady'
  | 'readyForDesignReview'
  | 'readyForAnalyzerReview'
  | 'readyForGenerationProvider'
  | 'blocked';

export interface DesignStudioExecutionReadinessSafetyGateResult {
  isAllowed: boolean;
  reasons: string[];
}

const orderedStageIds = ['architecture', 'planning', 'generation', 'runtime', 'skills', 'review'];
const readinessLabels: Record<DesignStudioExecutionReadinessSummary, DesignStudioExecutionReadinessViewModel['readinessLabel']> = {
  notReady: 'Not Ready',
  readyForDesignReview: 'Ready for Design Review',
  readyForAnalyzerReview: 'Ready for Analyzer Review',
  readyForGenerationProvider: 'Ready for Generation Provider',
  blocked: 'Blocked',
};

export class DesignStudioExecutionReadinessSafetyGate {
  validate(input: DesignStudioExecutionReadinessViewModel): DesignStudioExecutionReadinessSafetyGateResult {
    const reasons: string[] = [];

    if (input.schemaVersion !== DESIGN_STUDIO_EXECUTION_READINESS_SCHEMA_VERSION) {
      reasons.push('execution readiness schema version must be design-studio-execution-readiness/v1.');
    }

    if (!['notReady', 'readyForDesignReview', 'readyForAnalyzerReview', 'readyForGenerationProvider', 'blocked'].includes(input.readinessSummary)) {
      reasons.push('execution readiness summary is invalid.');
    }

    if (input.stageSummaries.length !== orderedStageIds.length
      || !orderedStageIds.every((stageId, index) => input.stageSummaries[index]?.stageId === stageId)) {
      reasons.push('execution readiness stage summaries must preserve deterministic architecture, planning, generation, runtime, skills, and review ordering.');
    }

    if (input.stageSummaries.some((stage) =>
      !stage.section.trim()
      || !stage.status.trim()
      || !stage.summary.trim()
      || stage.items.length === 0
      || stage.items.some((item) => !item.label.trim() || !item.value.trim()))) {
      reasons.push('execution readiness stage summaries are malformed.');
    }

    if (input.warningSummaries.some((warning) => !warning.category.trim() || !warning.severity.trim() || !warning.message.trim())) {
      reasons.push('execution readiness warning summaries are malformed.');
    }

    if (input.reviewerActionsAvailable.length === 0
      || input.reviewerActionsAvailable.some((action) => !action.trim())) {
      reasons.push('execution readiness reviewer actions are malformed.');
    }

    if (input.lineageReferences.length === 0
      || input.lineageReferences.some((reference) => !reference.stage.trim() || !reference.referenceId.trim() || !reference.schemaVersion.trim())) {
      reasons.push('execution readiness lineage references are malformed.');
    }

    if (!input.architectureCertificationReference.certificationId.trim()
      || !input.architectureCertificationReference.readinessReportId.trim()
      || input.architectureCertificationReference.schemaVersion !== 'architecture-certification/v1') {
      reasons.push('execution readiness architecture certification reference is malformed.');
    }

    if (input.trustBoundary.executionAllowed
      || input.trustBoundary.providerInvocationAllowed
      || input.trustBoundary.microsoftSkillsExecutionAllowed
      || input.trustBoundary.apiInvocationAllowed
      || input.trustBoundary.cliInvocationAllowed
      || input.trustBoundary.deploymentAllowed
      || input.trustBoundary.automaticAnalyzerValidationAllowed
      || input.trustBoundary.automaticAnalyzerLaunchAllowed) {
      reasons.push('execution readiness dashboard cannot allow execution, provider invocation, APIs, CLI, deployment, or Analyzer automation.');
    }

    return {
      isAllowed: reasons.length === 0,
      reasons: [...new Set(reasons)].sort((left, right) => left.localeCompare(right)),
    };
  }
}

export function buildDesignStudioExecutionReadinessDashboard(
  record: DesignStudioPreviewReviewRecord,
): DesignStudioExecutionReadinessViewModel {
  const readinessSummary = classifyReadiness(record);
  const generationManifestRef = record.previewPackage.lineage.generationManifestRef;
  const pbirIrRef = record.previewPackage.lineage.pbirIrRef;
  const selectedProvider = inferSelectedProvider(record);
  const selectedSkills = inferSelectedSkills(record);
  const dashboard: DesignStudioExecutionReadinessViewModel = {
    schemaVersion: DESIGN_STUDIO_EXECUTION_READINESS_SCHEMA_VERSION,
    readinessSummary,
    readinessLabel: readinessLabels[readinessSummary],
    stageSummaries: [
      {
        stageId: 'architecture',
        section: 'Architecture',
        status: 'ready',
        summary: 'Architecture certification and readiness classification.',
        items: [
          { label: 'Architecture certification status', value: 'Certified planning architecture' },
          { label: 'Architecture readiness classification', value: 'ReadyForExecutionImplementation' },
        ],
      },
      {
        stageId: 'planning',
        section: 'Planning',
        status: 'ready',
        summary: 'Planning outcome, Generation Manifest, and pipeline verification.',
        items: [
          { label: 'Planning outcome status', value: 'Approved' },
          { label: 'Generation Manifest status', value: 'ReadyForGenerator' },
          { label: 'Pipeline verification status', value: 'Verified' },
        ],
      },
      {
        stageId: 'generation',
        section: 'Generation',
        status: 'ready',
        summary: 'PBIR generation specification, canonical IR, preview package, and preview review.',
        items: [
          { label: 'PBIR Generation Specification readiness', value: 'ReadyForGenerationProvider' },
          { label: 'PBIR IR readiness', value: 'ReadyForSerializer' },
          { label: 'Preview Package readiness', value: record.previewPackage.schemaVersion === 'pbir-preview-package/v1' ? 'Packaged' : 'Not Ready' },
          { label: 'Preview Review status', value: previewReviewStatusLabel(record.reviewerAction) },
        ],
      },
      {
        stageId: 'runtime',
        section: 'Runtime',
        status: 'ready',
        summary: 'Runtime and provider readiness without invocation.',
        items: [
          { label: 'Runtime Provider readiness', value: 'ReadyForRuntimeProvider' },
          { label: 'Microsoft Runtime Provider readiness', value: 'ReadyForMicrosoftRuntimeProvider' },
          { label: 'Generation Provider readiness', value: 'ReadyForGenerationProvider' },
        ],
      },
      {
        stageId: 'skills',
        section: 'Skills',
        status: selectedSkills.length > 0 ? 'ready' : 'notReady',
        summary: 'Skill metadata and capability coverage only.',
        items: [
          { label: 'Skill readiness', value: selectedSkills.length > 0 ? 'ReadyForSkillProviderMetadata' : 'Not Ready' },
          { label: 'Selected provider', value: selectedProvider },
          { label: 'Selected skills', value: selectedSkills.join(', ') },
          { label: 'Capability coverage summary', value: `${record.previewPackage.fileInventory.length} preview artifacts; ${record.previewPackage.hashInventory.length} hash references.` },
        ],
      },
      {
        stageId: 'review',
        section: 'Review',
        status: record.reviewHandoff.reviewReadiness === 'blocked' ? 'blocked' : 'ready',
        summary: 'Design approval, preview review, and Analyzer handoff readiness.',
        items: [
          { label: 'Design approval status', value: 'Approved' },
          { label: 'Preview review status', value: previewReviewStatusLabel(record.reviewerAction) },
          { label: 'Analyzer handoff readiness', value: reviewReadinessLabel(record.reviewHandoff.reviewReadiness) },
        ],
      },
    ],
    warningSummaries: buildWarnings(record),
    reviewerActionsAvailable: buildReviewerActions(record),
    lineageReferences: buildLineageReferences(record),
    architectureCertificationReference: {
      certificationId: `architectureCertification:${generationManifestRef}`,
      readinessReportId: `architectureReadiness:${generationManifestRef}`,
      schemaVersion: 'architecture-certification/v1',
      readiness: 'ReadyForExecutionImplementation',
      isCertified: true,
    },
    trustBoundary: {
      executionAllowed: false,
      providerInvocationAllowed: false,
      microsoftSkillsExecutionAllowed: false,
      apiInvocationAllowed: false,
      cliInvocationAllowed: false,
      deploymentAllowed: false,
      automaticAnalyzerValidationAllowed: false,
      automaticAnalyzerLaunchAllowed: false,
    },
  };

  const safety = new DesignStudioExecutionReadinessSafetyGate().validate(dashboard);
  if (!safety.isAllowed) {
    return {
      ...dashboard,
      readinessSummary: 'blocked',
      readinessLabel: readinessLabels.blocked,
      warningSummaries: [
        ...dashboard.warningSummaries,
        ...safety.reasons.map<DesignStudioExecutionReadinessWarningSummaryViewModel>((message) => ({
          category: 'blockingIssue',
          severity: 'error',
          message,
        })),
      ].sort(compareWarnings),
    };
  }

  if (!pbirIrRef.trim()) {
    return {
      ...dashboard,
      readinessSummary: 'blocked',
      readinessLabel: readinessLabels.blocked,
    };
  }

  return dashboard;
}

function classifyReadiness(record: DesignStudioPreviewReviewRecord): DesignStudioExecutionReadinessSummary {
  if (record.reviewHandoff.reviewReadiness === 'blocked') {
    return 'blocked';
  }

  if (record.reviewerAction === 'revisionRequested' || record.reviewerAction === 'deferred') {
    return 'notReady';
  }

  if (record.reviewerAction === 'markedReviewed'
    || record.reviewerAction === 'analyzerCandidateMetadataPrepared'
    || record.reviewHandoff.reviewReadiness === 'readyForAnalyzerReview') {
    return 'readyForAnalyzerReview';
  }

  return 'readyForDesignReview';
}

function previewReviewStatusLabel(action: DesignStudioPreviewReviewRecord['reviewerAction']): string {
  switch (action) {
    case 'markedReviewed':
      return 'Marked Reviewed';
    case 'revisionRequested':
      return 'Revision Requested';
    case 'deferred':
      return 'Deferred';
    case 'analyzerCandidateMetadataPrepared':
      return 'Analyzer Candidate Metadata Prepared';
    default:
      return 'Pending';
  }
}

function reviewReadinessLabel(readiness: DesignStudioPreviewReviewRecord['reviewHandoff']['reviewReadiness']): string {
  switch (readiness) {
    case 'readyForAnalyzerReview':
      return 'ReadyForAnalyzerReview';
    case 'readyForDesignReview':
      return 'ReadyForDesignReview';
    case 'blocked':
      return 'Blocked';
    default:
      return 'Incomplete';
  }
}

function inferSelectedProvider(record: DesignStudioPreviewReviewRecord): string {
  const provider = record.previewPackage.hashInventory.find((entry) => entry.hashKind.toLowerCase().includes('provider'));
  return provider?.referenceId ?? 'reference-pbir-generation-provider';
}

function inferSelectedSkills(record: DesignStudioPreviewReviewRecord): string[] {
  const skillEntries = record.previewPackage.hashInventory
    .filter((entry) => entry.hashKind.toLowerCase().includes('skill'))
    .map((entry) => entry.referenceId);

  return skillEntries.length > 0
    ? [...new Set(skillEntries)].sort((left, right) => left.localeCompare(right))
    : ['powerbi.report.create', 'powerbi.visual.create'];
}

function buildWarnings(record: DesignStudioPreviewReviewRecord): DesignStudioExecutionReadinessViewModel['warningSummaries'] {
  const warnings: DesignStudioExecutionReadinessWarningSummaryViewModel[] = [
    warning('unsupportedCapability', 'info', 'Analyzer Workspace automation is not implemented.'),
    warning('unsupportedCapability', 'info', 'Deployment is not implemented.'),
    warning('unsupportedCapability', 'info', 'Microsoft Skills execution is not implemented.'),
    warning('unsupportedCapability', 'info', 'PBIR generation is not implemented.'),
    warning('unsupportedCapability', 'info', 'Provider, API, and CLI invocation are not implemented.'),
    ...record.warnings.map((message) => warning('previewReview', 'info', message)),
    ...record.previewPackage.warnings.map((message) => warning('previewPackage', 'info', message)),
    ...record.reviewHandoff.warnings.map((message) => warning('reviewHandoff', 'info', message)),
  ]
    .filter((entry, index, allWarnings) =>
      allWarnings.findIndex((candidate) => candidate.category === entry.category && candidate.message === entry.message) === index);

  return warnings.sort(compareWarnings);
}

function compareWarnings(
  left: DesignStudioExecutionReadinessViewModel['warningSummaries'][number],
  right: DesignStudioExecutionReadinessViewModel['warningSummaries'][number],
): number {
  return left.category.localeCompare(right.category) || left.message.localeCompare(right.message);
}

function warning(
  category: string,
  severity: DesignStudioExecutionReadinessWarningSummaryViewModel['severity'],
  message: string,
): DesignStudioExecutionReadinessWarningSummaryViewModel {
  return { category, severity, message };
}

function buildReviewerActions(record: DesignStudioPreviewReviewRecord): string[] {
  return [
    'Review readiness dashboard',
    ...(record.reviewerAction !== 'markedReviewed' ? ['Mark preview reviewed'] : []),
    ...(record.reviewerAction !== 'revisionRequested' ? ['Request revision'] : []),
    ...(record.reviewerAction !== 'deferred' ? ['Defer review'] : []),
    ...(record.reviewerAction !== 'analyzerCandidateMetadataPrepared' ? ['Prepare Analyzer candidate metadata'] : []),
  ].sort((left, right) => left.localeCompare(right));
}

function buildLineageReferences(record: DesignStudioPreviewReviewRecord): DesignStudioExecutionReadinessViewModel['lineageReferences'] {
  return [
    {
      stage: 'generationManifest',
      referenceId: record.previewPackage.lineage.generationManifestRef,
      schemaVersion: 'generation-manifest/v1',
    },
    {
      stage: 'pbirIr',
      referenceId: record.previewPackage.lineage.pbirIrRef,
      schemaVersion: record.reviewHandoff.pbirIrReference.schemaVersion,
    },
    {
      stage: 'previewManifest',
      referenceId: record.previewPackage.lineage.previewManifestRef,
      schemaVersion: 'pbir-preview-manifest/v1',
    },
    {
      stage: 'previewPackage',
      referenceId: record.previewPackage.packageId,
      schemaVersion: record.previewPackage.schemaVersion,
    },
    {
      stage: 'reviewHandoff',
      referenceId: record.reviewHandoff.handoffId,
      schemaVersion: record.reviewHandoff.schemaVersion,
    },
  ].sort((left, right) => left.stage.localeCompare(right.stage) || left.referenceId.localeCompare(right.referenceId));
}
