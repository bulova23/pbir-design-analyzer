import {
  createDraftProviderCapabilityPlaceholder,
  draftProviderSupportsArtifactKind,
} from '../design-studio/providers/draftProviderAdapter';

describe('draftProviderAdapter', () => {
  it('creates provider-neutral capability placeholders for future draft providers', () => {
    const placeholder = createDraftProviderCapabilityPlaceholder({
      providerId: 'provider.mock',
      providerDisplayName: 'Mock provider',
      capabilityId: 'draft-layouts',
      capabilityKind: 'generationAssistance',
      supportedSurfaceFamilies: ['pbir', 'fabricApp'],
      requiresExternalService: false,
      supportsOfflineOperation: true,
      trustPosture: 'advisoryOnly',
      provenanceRequirements: 'required',
      failureBehavior: 'degradeGracefully',
    });

    expect(placeholder).toEqual({
      providerId: 'provider.mock',
      providerDisplayName: 'Mock provider',
      capabilityId: 'draft-layouts',
      capabilityKind: 'generationAssistance',
      supportedArtifactKinds: [
        'draftReportArtifact',
        'draftPageArtifact',
        'draftLayoutArtifact',
        'draftNavigationArtifact',
      ],
      supportedSurfaceFamilies: ['pbir', 'fabricApp'],
      requiresExternalService: false,
      supportsOfflineOperation: true,
      trustPosture: 'advisoryOnly',
      provenanceRequirements: 'required',
      failureBehavior: 'degradeGracefully',
      workflowConstraints: {
        requiresApproval: true,
        requiresValidation: true,
        allowsMaterialization: false,
        allowsReportMutation: false,
        allowsPbirAssetGeneration: false,
        allowsAnalyzableSurfaceCreation: false,
      },
    });
  });

  it('matches only the draft artifact kinds supported by the provider seam', () => {
    const placeholder = createDraftProviderCapabilityPlaceholder({
      providerId: 'provider.mock',
      providerDisplayName: 'Mock provider',
      capabilityId: 'draft-layouts',
      capabilityKind: 'generationAssistance',
      supportedArtifactKinds: ['draftLayoutArtifact'],
      supportedSurfaceFamilies: ['pbir'],
      requiresExternalService: false,
      supportsOfflineOperation: true,
      trustPosture: 'advisoryOnly',
      provenanceRequirements: 'required',
      failureBehavior: 'degradeGracefully',
    });

    expect(draftProviderSupportsArtifactKind(placeholder, 'draftLayoutArtifact')).toBe(true);
    expect(draftProviderSupportsArtifactKind(placeholder, 'draftNavigationArtifact')).toBe(false);
  });
});
