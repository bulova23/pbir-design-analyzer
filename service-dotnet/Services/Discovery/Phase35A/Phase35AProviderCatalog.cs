namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35AProviderCatalog
{
    internal static IReadOnlyList<Phase35AProviderProfile> All { get; } =
    [
        new(Phase35AContracts.ProviderProfileV1, "powerbi-report-author@0.1.4", "Power BI Report Author", Phase35AProviderCategory.LocalInspection, Phase35AExecutionClass.NonExecutable, Phase35ATrustClassification.LocalReadOnly, [Phase35ACapability.PbirValidation, Phase35ACapability.PbirMetadataInspection], [Phase35AArtifactKind.PbirReport], [Phase35AReadinessRequirement.ExplicitExecutableRegistration]),
        new(Phase35AContracts.ProviderProfileV1, "powerbi-desktop", "Power BI Desktop", Phase35AProviderCategory.LaterVerification, Phase35AExecutionClass.DeferredRuntime, Phase35ATrustClassification.ExternalUntrusted, [Phase35ACapability.DesktopVerification], [Phase35AArtifactKind.VerificationRecord], [Phase35AReadinessRequirement.ExplicitExecutableRegistration]),
        new(Phase35AContracts.ProviderProfileV1, "powerbi-modeling-mcp", "Power BI Modeling MCP", Phase35AProviderCategory.SemanticModelOnly, Phase35AExecutionClass.NonExecutable, Phase35ATrustClassification.ExternalUntrusted, [Phase35ACapability.SemanticModelInspection], [Phase35AArtifactKind.SemanticModel], [Phase35AReadinessRequirement.ExplicitExecutableRegistration]),
        new(Phase35AContracts.ProviderProfileV1, "microsoft-skills.metadata", "Microsoft Skills Metadata", Phase35AProviderCategory.MetadataOnly, Phase35AExecutionClass.NonExecutable, Phase35ATrustClassification.TrustedContract, [], [], [Phase35AReadinessRequirement.ExplicitExecutableRegistration]),
        new(Phase35AContracts.ProviderProfileV1, "offline.reference-materializer", "Offline Reference/Materialization Boundary", Phase35AProviderCategory.OfflineTest, Phase35AExecutionClass.NonExecutable, Phase35ATrustClassification.LocalReadOnly, [], [Phase35AArtifactKind.OfflineFixture], [Phase35AReadinessRequirement.ExplicitExecutableRegistration])
    ];
}

