namespace PowerBIModelingService.Services.DesignStudio.Providers;

internal enum DesignProviderCapabilityKind
{
    DesignAssistance,
    GenerationAssistance,
    ScreenshotIterationAssistance,
    SemanticModelAwareAssistance,
}

internal enum DesignProviderTrustPosture
{
    AdvisoryOnly,
}

internal enum DesignProviderProvenanceRequirements
{
    Required,
    Optional,
}

internal enum DesignProviderFailureBehavior
{
    DegradeGracefully,
    ReportUnavailableButContinue,
}

internal sealed record DesignProviderWorkflowConstraints(
    bool RequiresApproval,
    bool RequiresValidation,
    bool AllowsMaterialization,
    bool AllowsReportMutation,
    bool AllowsPbirAssetGeneration,
    bool AllowsAnalyzableSurfaceCreation)
{
    internal static DesignProviderWorkflowConstraints AdvisoryOnly { get; } =
        new(
            RequiresApproval: true,
            RequiresValidation: true,
            AllowsMaterialization: false,
            AllowsReportMutation: false,
            AllowsPbirAssetGeneration: false,
            AllowsAnalyzableSurfaceCreation: false);
}

internal sealed record DesignProviderCapability(
    string ProviderId,
    string ProviderDisplayName,
    string CapabilityId,
    DesignProviderCapabilityKind CapabilityKind,
    IReadOnlyList<string> SupportedArtifactKinds,
    IReadOnlyList<string> SupportedSurfaceFamilies,
    bool RequiresExternalService,
    bool SupportsOfflineOperation,
    DesignProviderTrustPosture TrustPosture,
    DesignProviderProvenanceRequirements ProvenanceRequirements,
    DesignProviderFailureBehavior FailureBehavior,
    DesignProviderWorkflowConstraints WorkflowConstraints);
