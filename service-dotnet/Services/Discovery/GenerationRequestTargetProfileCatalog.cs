using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal static class GenerationRequestTargetProfileCatalog
{
    internal static bool IsSupportedArtifactType(GenerationRequestArtifactType artifactType)
    {
        return artifactType is GenerationRequestArtifactType.PbirReport or GenerationRequestArtifactType.FabricDataApp;
    }

    internal static bool IsSupportedProfileId(string profileId)
    {
        return string.Equals(profileId, GenerationRequestContract.PbirReportDefaultProfile, StringComparison.Ordinal) ||
            string.Equals(profileId, GenerationRequestContract.FabricDataAppDefaultProfile, StringComparison.Ordinal);
    }

    internal static bool IsCompatibleProfile(GenerationRequestTargetArtifactProfile targetArtifactProfile)
    {
        return targetArtifactProfile.ProfileId switch
        {
            GenerationRequestContract.PbirReportDefaultProfile =>
                targetArtifactProfile.ArtifactType == GenerationRequestArtifactType.PbirReport &&
                targetArtifactProfile.SourceExperienceType is OpportunityExperienceType.PbirReport or OpportunityExperienceType.ExecutiveDashboard or OpportunityExperienceType.OperationalMonitoringExperience or OpportunityExperienceType.AnalyticalInvestigationExperience,
            GenerationRequestContract.FabricDataAppDefaultProfile =>
                targetArtifactProfile.ArtifactType == GenerationRequestArtifactType.FabricDataApp &&
                targetArtifactProfile.SourceExperienceType == OpportunityExperienceType.FabricDataApp,
            _ => false,
        };
    }

    internal static string ToContractValue(GenerationRequestArtifactType artifactType)
    {
        return artifactType switch
        {
            GenerationRequestArtifactType.PbirReport => "pbirReport",
            GenerationRequestArtifactType.FabricDataApp => "fabricDataApp",
            GenerationRequestArtifactType.FabricApp => "fabricApp",
            _ => throw new ArgumentOutOfRangeException(nameof(artifactType), artifactType, "Unsupported generation request artifact type."),
        };
    }
}
