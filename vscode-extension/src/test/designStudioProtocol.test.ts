import {
  DESIGN_STUDIO_PROTOCOL_SCHEMA_VERSION,
  DESIGN_STUDIO_PROTOCOL_VERSION,
  parseDesignStudioHostMessage,
  parseDesignStudioWebviewMessage,
  withDesignStudioEnvelope,
} from '../design-studio/contracts/designStudioProtocol';

describe('designStudioProtocol', () => {
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
      },
    }));

    expect(malformed).toEqual({
      ok: false,
      error: 'Design Studio requestMaterialization webview message has an invalid request payload.',
    });
  });
});
