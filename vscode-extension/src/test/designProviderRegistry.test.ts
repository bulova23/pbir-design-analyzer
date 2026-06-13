import {
  DesignProviderRegistry,
  createDesignProviderCapability,
  designProviderCapabilitySupportsArtifactKind,
  designProviderCapabilitySupportsSurfaceFamily,
} from '../design-studio/providers/designProviderRegistry';

describe('designProviderRegistry', () => {
  it('supports optional provider registration and capability discovery', () => {
    const registry = new DesignProviderRegistry();

    registry.registerProvider({
      providerId: 'provider.mock',
      displayName: 'Mock provider',
      capabilities: [
        createDesignProviderCapability({
          providerId: 'provider.mock',
          providerDisplayName: 'Mock provider',
          capabilityId: 'concept-design',
          capabilityKind: 'designAssistance',
          supportedArtifactKinds: ['reportConcept', 'pageConcept'],
          supportedSurfaceFamilies: ['pbir', 'fabricApp'],
          requiresExternalService: false,
          supportsOfflineOperation: true,
          trustPosture: 'advisoryOnly',
          provenanceRequirements: 'required',
          failureBehavior: 'degradeGracefully',
        }),
        createDesignProviderCapability({
          providerId: 'provider.mock',
          providerDisplayName: 'Mock provider',
          capabilityId: 'semantic-hints',
          capabilityKind: 'semanticModelAwareAssistance',
          supportedArtifactKinds: ['designBrief', 'draftReportArtifact'],
          supportedSurfaceFamilies: ['pbir'],
          requiresExternalService: true,
          supportsOfflineOperation: false,
          trustPosture: 'advisoryOnly',
          provenanceRequirements: 'required',
          failureBehavior: 'reportUnavailableButContinue',
        }),
      ],
    });

    expect(registry.listProviders()).toEqual([
      expect.objectContaining({
        providerId: 'provider.mock',
        displayName: 'Mock provider',
      }),
    ]);
    expect(registry.listCapabilities()).toHaveLength(2);
    expect(registry.findCapabilities({ capabilityKind: 'designAssistance' })).toEqual([
      expect.objectContaining({
        capabilityId: 'concept-design',
        capabilityKind: 'designAssistance',
      }),
    ]);
    expect(registry.findCapabilities({ surfaceFamily: 'fabricApp' })).toEqual([
      expect.objectContaining({
        capabilityId: 'concept-design',
      }),
    ]);
    expect(registry.findCapabilities({ artifactKind: 'draftReportArtifact' })).toEqual([
      expect.objectContaining({
        capabilityId: 'semantic-hints',
      }),
    ]);
  });

  it('supports zero-provider operation and graceful provider absence', () => {
    const registry = new DesignProviderRegistry();

    expect(registry.listProviders()).toEqual([]);
    expect(registry.listCapabilities()).toEqual([]);
    expect(registry.findCapabilities({ capabilityKind: 'generationAssistance' })).toEqual([]);
    expect(registry.getProviderStatus('provider.missing')).toEqual({
      providerId: 'provider.missing',
      isRegistered: false,
      degradation: 'providerUnavailable',
      canContinueCoreWorkflow: true,
    });
  });

  it('does not allow providers to bypass approval or validation and never grants mutation authority', () => {
    const capability = createDesignProviderCapability({
      providerId: 'provider.mock',
      providerDisplayName: 'Mock provider',
      capabilityId: 'draft-layouts',
      capabilityKind: 'generationAssistance',
      supportedArtifactKinds: ['draftLayoutArtifact'],
      supportedSurfaceFamilies: ['pbir'],
      requiresExternalService: true,
      supportsOfflineOperation: false,
      trustPosture: 'advisoryOnly',
      provenanceRequirements: 'required',
      failureBehavior: 'degradeGracefully',
    });

    expect(capability.workflowConstraints).toEqual({
      requiresApproval: true,
      requiresValidation: true,
      allowsMaterialization: false,
      allowsReportMutation: false,
      allowsPbirAssetGeneration: false,
      allowsAnalyzableSurfaceCreation: false,
    });
  });

  it('matches capabilities by supported artifact kinds and surface families', () => {
    const capability = createDesignProviderCapability({
      providerId: 'provider.mock',
      providerDisplayName: 'Mock provider',
      capabilityId: 'screenshot-iteration',
      capabilityKind: 'screenshotIterationAssistance',
      supportedArtifactKinds: ['draftPageArtifact'],
      supportedSurfaceFamilies: ['fabricApp'],
      requiresExternalService: true,
      supportsOfflineOperation: false,
      trustPosture: 'advisoryOnly',
      provenanceRequirements: 'required',
      failureBehavior: 'degradeGracefully',
    });

    expect(designProviderCapabilitySupportsArtifactKind(capability, 'draftPageArtifact')).toBe(true);
    expect(designProviderCapabilitySupportsArtifactKind(capability, 'draftLayoutArtifact')).toBe(false);
    expect(designProviderCapabilitySupportsSurfaceFamily(capability, 'fabricApp')).toBe(true);
    expect(designProviderCapabilitySupportsSurfaceFamily(capability, 'pbir')).toBe(false);
  });
});
