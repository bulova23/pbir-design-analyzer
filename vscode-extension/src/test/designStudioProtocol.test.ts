import {
  DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
  DESIGN_STUDIO_PROTOCOL_VERSION,
  parseDesignStudioHostMessage,
  parseDesignStudioWebviewMessage,
  withDesignStudioEnvelope,
} from '../design-studio/contracts/designStudioProtocol';

describe('designStudioProtocol', () => {
  it('accepts local materialization intents and validates safe workflow updates', () => {
    expect(parseDesignStudioWebviewMessage(withDesignStudioEnvelope({ type: 'startLocalMaterializationPreview' }))).toEqual(expect.objectContaining({ ok: true }));
    expect(parseDesignStudioWebviewMessage(withDesignStudioEnvelope({ type: 'requestLocalMaterializationApply' }))).toEqual(expect.objectContaining({ ok: true }));
    expect(parseDesignStudioWebviewMessage(withDesignStudioEnvelope({ type: 'inspectLocalMaterializationRecovery' }))).toEqual(expect.objectContaining({ ok: true }));
    expect(parseDesignStudioWebviewMessage(withDesignStudioEnvelope({ type: 'cancelLocalMaterialization' }))).toEqual(expect.objectContaining({ ok: true }));

    const update = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'materializationWorkflowUpdated',
      workflow: {
        status: 'preview-ready',
        outcome: 'absent-destination',
        summary: { destinationClassification: 'absent', artifactCount: 1, rollbackAvailable: false },
        diagnostics: [{ code: 'PBIR', field: 'destination', message: 'Safe diagnostic.' }],
        writtenFiles: [{ relativePath: 'definition.pbir', byteLength: 10, hashSha256: 'hash' }],
      },
    }));

    expect(update).toEqual(expect.objectContaining({ ok: true }));
  });

  it('rejects malformed materialization workflow updates before rendering', () => {
    const result = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'materializationWorkflowUpdated',
      workflow: { status: 'preview-ready', diagnostics: [{ message: 'raw' }], writtenFiles: [] },
    }));

    expect(result).toEqual(expect.objectContaining({ ok: false }));
  });

  it('accepts valid host messages', () => {
    const result = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'artifactApproved',
      artifactKind: 'reportConcept',
      artifactId: 'report-concept:thread-1',
      version: 3,
    }));

    expect(result).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'artifactApproved',
        artifactKind: 'reportConcept',
        artifactId: 'report-concept:thread-1',
        version: 3,
      },
    });
  });

  it('accepts valid webview messages', () => {
    const result = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'loadStudioState',
      threadId: 'thread-1',
    }));

    expect(result).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'loadStudioState',
        threadId: 'thread-1',
      },
    });
  });

  it('accepts refinement proposal state transitions with explicit action vocabulary', () => {
    const result = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'setRefinementProposalState',
      proposalId: 'refinement-proposal:thread-1:issues:issues:1:1',
      action: 'defer',
    }));

    expect(result).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'setRefinementProposalState',
        proposalId: 'refinement-proposal:thread-1:issues:issues:1:1',
        action: 'defer',
      },
    });
  });

  it('accepts Concept Studio execution messages', () => {
    const generate = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'generateConcepts',
    }));
    const selectBaseline = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'selectConceptBaseline',
      conceptId: 'concept-narrative',
    }));

    expect(generate).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'generateConcepts',
      },
    });
    expect(selectBaseline).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'selectConceptBaseline',
        conceptId: 'concept-narrative',
      },
    });
  });

  it('accepts Draft Studio execution messages', () => {
    const generate = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'generateDrafts',
    }));

    expect(generate).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'generateDrafts',
      },
    });
  });

  it('accepts Prepare For Review execution messages', () => {
    const create = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'createReviewCandidate',
    }));

    expect(create).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'createReviewCandidate',
      },
    });
  });

  it('accepts Workflow Completion execution messages', () => {
    const complete = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'completeIteration',
    }));
    const reopen = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'reopenIteration',
    }));

    expect(complete).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'completeIteration',
      },
    });
    expect(reopen).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'reopenIteration',
      },
    });
  });

  it('accepts explicit analyzer-result attachment messages', () => {
    const attach = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'attachAnalyzerResults',
      requestId: 'materialization-request:thread-1',
    }));

    expect(attach).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'attachAnalyzerResults',
        requestId: 'materialization-request:thread-1',
      },
    });
  });

  it('accepts explicit Design Studio preview review action messages', () => {
    const markReviewed = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'markPreviewReviewed',
      previewReviewId: 'designStudioPreviewReview:phase27',
      reviewerNotes: 'Preview inspected.',
    }));
    const requestRevision = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'requestPreviewRevision',
      previewReviewId: 'designStudioPreviewReview:phase27',
    }));
    const deferReview = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'deferPreviewReview',
      previewReviewId: 'designStudioPreviewReview:phase27',
    }));
    const prepareAnalyzerMetadata = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'prepareAnalyzerCandidateMetadata',
      previewReviewId: 'designStudioPreviewReview:phase27',
    }));

    expect(markReviewed).toEqual({
      ok: true,
      message: expect.objectContaining({
        type: 'markPreviewReviewed',
        previewReviewId: 'designStudioPreviewReview:phase27',
        reviewerNotes: 'Preview inspected.',
      }),
    });
    expect(requestRevision).toEqual({
      ok: true,
      message: expect.objectContaining({
        type: 'requestPreviewRevision',
        previewReviewId: 'designStudioPreviewReview:phase27',
      }),
    });
    expect(deferReview).toEqual({
      ok: true,
      message: expect.objectContaining({
        type: 'deferPreviewReview',
        previewReviewId: 'designStudioPreviewReview:phase27',
      }),
    });
    expect(prepareAnalyzerMetadata).toEqual({
      ok: true,
      message: expect.objectContaining({
        type: 'prepareAnalyzerCandidateMetadata',
        previewReviewId: 'designStudioPreviewReview:phase27',
      }),
    });
  });

  it('accepts Design Studio execution readiness request messages', () => {
    const result = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'requestExecutionReadiness',
      threadId: 'design-studio:active-report',
    }));

    expect(result).toEqual({
      ok: true,
      message: {
        protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
        schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
        type: 'requestExecutionReadiness',
        threadId: 'design-studio:active-report',
      },
    });
  });

  it('accepts Design Studio execution readiness update messages and rejects malformed readiness payloads', () => {
    const readiness = createExecutionReadinessPayload();
    const accepted = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'executionReadinessUpdated',
      readiness,
    }));
    const rejected = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'executionReadinessUpdated',
      readiness: {
        ...readiness,
        schemaVersion: 'design-studio-execution-readiness/v2',
      },
    }));

    expect(accepted).toEqual({
      ok: true,
      message: expect.objectContaining({
        type: 'executionReadinessUpdated',
        readiness,
      }),
    });
    expect(rejected).toEqual({
      ok: false,
      error: 'Design Studio executionReadinessUpdated host message has an invalid readiness payload.',
    });
  });

  it('accepts Design Studio host state with preview review package and handoff metadata', () => {
    const hash = 'a'.repeat(64);
    const result = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:preview-review',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'previewReview',
          stages: [
            { id: 'brief', label: 'Design Brief', status: 'approved', readinessLabel: 'Approved', title: 'Design Brief', description: 'Define the brief.' },
            { id: 'concept', label: 'Concept Studio', status: 'approved', readinessLabel: 'Approved', title: 'Concept Studio', description: 'Approve the concept.' },
            { id: 'draft', label: 'Draft Studio', status: 'approved', readinessLabel: 'Approved', title: 'Draft Studio', description: 'Approve the draft.' },
            { id: 'materialize', label: 'Prepare For Review', status: 'approved', readinessLabel: 'Approved', title: 'Prepare For Review', description: 'Prepare a candidate.' },
            { id: 'previewReview', label: 'Preview Review', status: 'ready', readinessLabel: 'Pending Review', title: 'Preview Review', description: 'Review preview package metadata.' },
            { id: 'handoff', label: 'Review Design', status: 'blocked', readinessLabel: 'Blocked', title: 'Review Design', description: 'Analyzer handoff stays explicit.' },
          ],
          currentStageSummary: {
            title: 'Preview Review',
            description: 'Review preview package metadata.',
          },
          approvalCards: [],
          previewReview: {
            previewReviewId: 'designStudioPreviewReview:phase27',
            schemaVersion: 'design-studio-preview-review/v1',
            previewPackageId: 'pbirPreviewPackage:phase27',
            previewPackageSchemaVersion: 'pbir-preview-package/v1',
            previewPackageHash: hash,
            generatedUtc: '2026-06-27T11:30:00.000Z',
            reviewHandoffId: 'pbirReviewHandoff:phase27',
            reviewHandoffSchemaVersion: 'pbir-review-handoff/v1',
            reviewReadiness: 'readyForDesignReview',
            readinessState: 'readyForDesignReview',
            reviewerAction: 'pending',
            reviewerNotes: '',
            requiredReviewerAction: 'Review local preview outputs.',
            summary: {
              fileCount: 1,
              warningCount: 0,
              rejectedArtifactCount: 0,
              hashCount: 1,
            },
            references: {
              previewMarkdown: 'preview/report-preview.md',
              reviewHandoff: 'pbirReviewHandoff:phase27',
            },
            fileInventory: [
              {
                artifactType: 'previewMarkdown',
                relativePath: 'preview/report-preview.md',
                reference: 'preview/report-preview.md',
                contentType: 'text/markdown',
                hashSha256: hash,
                byteLength: 20,
              },
            ],
            hashInventory: [
              {
                hashKind: 'package',
                referenceId: 'pbirPreviewPackage:phase27',
                hashSha256: hash,
                description: 'Package hash',
              },
            ],
            lineage: {
              previewPackageRef: 'pbirPreviewPackage:phase27',
              generationManifestRef: 'generationManifest:phase27',
              pbirIrRef: 'pbirIr:phase27',
              previewManifestRef: 'pbirPreviewManifest:phase27',
              sourceWriteManifestRef: 'pbirLocalWriteManifest:phase27',
              immutableLineage: ['pbirPreviewPackage:phase27'],
            },
            rollbackMetadata: {
              rollbackPlanRef: 'rollback:phase27',
              rollbackPlanHash: hash,
              actionCount: 1,
              automaticRollbackExecuted: false,
            },
            analyzerBoundary: {
              validationOccurred: false,
              automaticValidationRequested: false,
              automaticValidationAllowed: false,
              workspaceLaunchRequested: false,
              validationStatus: 'No Analyzer Workspace validation has occurred.',
            },
            reviewOnlyBoundary: {
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
            },
            warnings: [],
            rejectedArtifacts: [],
            canMarkReviewed: true,
            canRequestRevision: true,
            canDeferReview: true,
            canPrepareAnalyzerCandidateMetadata: true,
          },
        },
      },
    }));

    expect(result).toEqual({
      ok: true,
      message: expect.objectContaining({
        type: 'studioState',
      }),
    });
  });

  it('rejects protocol version mismatches', () => {
    const result = parseDesignStudioHostMessage({
      protocolVersion: 999,
      schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
      type: 'artifactSaved',
      artifactKind: 'designBrief',
      artifactId: 'design-brief:thread-1',
      version: 1,
    });

    expect(result).toEqual({
      ok: false,
      error: `Design Studio protocol mismatch. Expected protocol ${DESIGN_STUDIO_PROTOCOL_VERSION} / schema ${DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION}, received protocol 999 / schema ${DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION}.`,
    });
  });

  it('rejects schema version mismatches', () => {
    const result = parseDesignStudioWebviewMessage({
      protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
      schemaVersion: 999,
      type: 'loadStudioState',
      threadId: 'thread-1',
    });

    expect(result).toEqual({
      ok: false,
      error: `Design Studio protocol mismatch. Expected protocol ${DESIGN_STUDIO_PROTOCOL_VERSION} / schema ${DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION}, received protocol ${DESIGN_STUDIO_PROTOCOL_VERSION} / schema 999.`,
    });
  });

  it('rejects unsupported message types safely', () => {
    const result = parseDesignStudioWebviewMessage({
      protocolVersion: DESIGN_STUDIO_PROTOCOL_VERSION,
      schemaVersion: DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
      type: 'launchTask6',
    });

    expect(result).toEqual({
      ok: false,
      error: 'Unsupported Design Studio webview message type: launchTask6.',
    });
  });

  it('rejects malformed payloads safely', () => {
    const result = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'artifactApproved',
      artifactKind: 'reportConcept',
      artifactId: 42,
      version: '3',
    }));

    expect(result).toEqual({
      ok: false,
      error: 'Design Studio artifactApproved host message is missing required fields.',
    });
  });

  it('rejects malformed preview review host payloads at the protocol boundary', () => {
    const result = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'design-studio:preview-review',
        iterationHistory: [],
        pendingRefinementProposals: [],
        workspace: {
          reportLabel: 'Sales & Production',
          currentStage: 'previewReview',
          stages: [
            { id: 'previewReview', label: 'Preview Review', status: 'ready', readinessLabel: 'Pending Review', title: 'Preview Review', description: 'Review preview package metadata.' },
          ],
          currentStageSummary: {
            title: 'Preview Review',
            description: 'Review preview package metadata.',
          },
          approvalCards: [],
          previewReview: {
            previewReviewId: 'designStudioPreviewReview:phase27',
            schemaVersion: 'design-studio-preview-review/v2',
            previewPackageId: 'pbirPreviewPackage:phase27',
          },
        },
      },
    }));

    expect(result).toEqual({
      ok: false,
      error: 'Design Studio studioState host message has an invalid nested state payload.',
    });
  });

  it('rejects nested studio state payloads with invalid thread lineage and guardrail shape', () => {
    const result = parseDesignStudioHostMessage(withDesignStudioEnvelope({
      type: 'studioState',
      state: {
        threadId: 'thread-host',
        currentBrief: {
          id: 'design-brief:thread-other',
          threadId: 'thread-other',
        },
        iterationHistory: [
          {
            id: 'design-iteration:thread-other:1',
            threadId: 'thread-other',
            kind: 'designIterationRecord',
            sourceArtifactVersionIds: ['draft-report:thread-other@v2'],
            approvalCheckpoint: {
              validationApproval: {
                approvalKind: 'validationApproval',
                approvalState: 'approved',
              },
            },
            guardrails: {
              autoOptimizationTriggered: false,
            },
          },
        ],
        pendingRefinementProposals: [],
      },
    }));

    expect(result).toEqual({
      ok: false,
      error: 'Design Studio studioState host message has an invalid nested state payload.',
    });
  });

  it('deep-validates nested materialization requests', () => {
    const malformed = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'requestMaterialization',
      request: {
        id: 'materialization-request:thread-1',
        threadId: 'thread-1',
        kind: 'materializationRequest',
        materializationMode: 'draftToSurfaceCandidate',
        version: 1,
        lifecycleState: 'proposed',
        approvalState: 'pendingApproval',
        approvalKind: 'materializationApproval',
        createdAt: '2026-06-13T12:00:00.000Z',
        updatedAt: '2026-06-13T12:00:00.000Z',
        authorSource: 'system',
        provenance: { source: 'system' },
        sourceArtifactIds: ['draft-report:thread-1'],
        sourceLineage: [
          {
            artifactId: 'draft-report:thread-1',
            artifactKind: 'draftReportArtifact',
            artifactVersionId: 'not-a-version-id',
            sourceRole: 'primary',
            approvalState: 'approved',
            approvalTimestamp: '2026-06-13T12:00:00.000Z',
          },
        ],
        targetSurfaceType: 'pbirReport',
        targetAnalyzer: 'notAnAnalyzer',
        targetAnalyzerProfile: 'consultant',
        handoffContext: {
          degradedMappings: [],
          omittedEvidence: [],
        },
      },
    }));

    expect(malformed).toEqual({
      ok: false,
      error: 'Design Studio requestMaterialization webview message has an invalid request payload.',
    });
  });

  it('rejects semantically invalid materialization requests before coordinator trust', () => {
    const malformed = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'requestMaterialization',
      request: {
        id: 'materialization-request:thread-1',
        threadId: 'thread-1',
        kind: 'materializationRequest',
        materializationMode: 'draftToSurfaceCandidate',
        version: 0,
        lifecycleState: 'draft',
        approvalState: 'approved',
        approvalKind: 'designApproval',
        createdAt: 'not-a-timestamp',
        updatedAt: '2026-06-13T12:00:00.000Z',
        authorSource: 'system',
        provenance: { source: 'system', timestamp: '2026-06-13T12:00:00.000Z' },
        sourceArtifactIds: ['draft-report:thread-1'],
        sourceLineage: [
          {
            artifactId: 'draft-report:thread-1',
            artifactKind: 'draftReportArtifact',
            artifactVersionId: 'draft-report:thread-1@v1',
            sourceRole: 'primary',
            approvalState: 'approved',
            approvalTimestamp: '2026-06-13T12:00:00.000Z',
          },
          {
            artifactId: 'draft-report:thread-1',
            artifactKind: 'draftReportArtifact',
            artifactVersionId: 'draft-report:thread-1@v1',
            sourceRole: 'primary',
            approvalState: 'approved',
            approvalTimestamp: '2026-06-13T12:00:00.000Z',
          },
        ],
        targetSurfaceType: 'pbirReport',
        targetAnalyzer: 'fabricAppReview',
        targetAnalyzerProfile: 'consultant',
        handoffContext: {
          degradedMappings: [],
          omittedEvidence: [],
        },
      },
    }));

    expect(malformed).toEqual({
      ok: false,
      error: 'Design Studio requestMaterialization webview message has an invalid request payload.',
    });
  });

  it('rejects malformed refinement proposal state transitions safely', () => {
    const result = parseDesignStudioWebviewMessage(withDesignStudioEnvelope({
      type: 'setRefinementProposalState',
      proposalId: 'refinement-proposal:thread-1:issues:1',
      action: 'archive',
    }));

    expect(result).toEqual({
      ok: false,
      error: 'Design Studio setRefinementProposalState webview message is missing required fields.',
    });
  });
});

function createExecutionReadinessPayload() {
  return {
    schemaVersion: 'design-studio-execution-readiness/v1',
    readinessSummary: 'readyForDesignReview',
    readinessLabel: 'Ready for Design Review',
    stageSummaries: [
      {
        stageId: 'architecture',
        section: 'Architecture',
        status: 'ready',
        summary: 'Architecture certification and readiness classification.',
        items: [
          { label: 'Architecture certification status', value: 'Certified' },
        ],
      },
      {
        stageId: 'planning',
        section: 'Planning',
        status: 'ready',
        summary: 'Planning outcome, generation manifest, and pipeline verification.',
        items: [
          { label: 'Generation Manifest status', value: 'ReadyForGenerator' },
        ],
      },
      {
        stageId: 'generation',
        section: 'Generation',
        status: 'ready',
        summary: 'PBIR generation specification, canonical IR, preview package, and preview review.',
        items: [
          { label: 'Preview Package readiness', value: 'Packaged' },
        ],
      },
      {
        stageId: 'runtime',
        section: 'Runtime',
        status: 'ready',
        summary: 'Runtime and provider readiness without invocation.',
        items: [
          { label: 'Runtime Provider readiness', value: 'ReadyForRuntimeProvider' },
        ],
      },
      {
        stageId: 'skills',
        section: 'Skills',
        status: 'ready',
        summary: 'Skill metadata and capability coverage only.',
        items: [
          { label: 'Selected provider', value: 'reference-pbir-generation-provider' },
        ],
      },
      {
        stageId: 'review',
        section: 'Review',
        status: 'ready',
        summary: 'Design approval, preview review, and Analyzer handoff readiness.',
        items: [
          { label: 'Preview review status', value: 'Pending' },
        ],
      },
    ],
    warningSummaries: [
      {
        category: 'unsupportedCapability',
        severity: 'info',
        message: 'PBIR generation is not implemented.',
      },
    ],
    reviewerActionsAvailable: ['Review readiness dashboard'],
    lineageReferences: [
      {
        stage: 'generationManifest',
        referenceId: 'generationManifest:phase28',
        schemaVersion: 'generation-manifest/v1',
      },
    ],
    architectureCertificationReference: {
      certificationId: 'architectureCertification:phase28',
      readinessReportId: 'architectureReadiness:phase28',
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
}
