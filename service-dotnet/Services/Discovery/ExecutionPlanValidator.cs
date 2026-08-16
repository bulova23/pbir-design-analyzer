using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ExecutionPlanValidator
{
    internal ExecutionPlanValidationResult Validate(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var unsupportedTargets = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var dependencyFailures = new List<string>();
        var capabilityInconsistencies = new List<string>();
        var targetCompatibilityFailures = new List<string>();
        var reviewRequirementFailures = new List<string>();

        ValidateNotBlank(plan.SchemaVersion, "schemaVersion", missingFields);
        if (!string.IsNullOrWhiteSpace(plan.SchemaVersion) &&
            !string.Equals(plan.SchemaVersion, ExecutionPlanContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            unsupportedSchemaVersions.Add(plan.SchemaVersion);
        }

        ValidateNotBlank(plan.ExecutionPlanId, "executionPlanId", missingFields);

        if (plan.SourceReferences is null)
        {
            missingSections.Add("sourceReferences");
        }
        else
        {
            ValidateNotBlank(plan.SourceReferences.GenerationRequestRef, "sourceReferences.generationRequestRef", missingFields);
            ValidateNotBlank(plan.SourceReferences.SourceDesignPackageRef, "sourceReferences.sourceDesignPackageRef", missingFields);
        }

        if (plan.TargetDefinition is null)
        {
            missingSections.Add("targetDefinition");
        }
        else if (plan.TargetDefinition.TargetArtifactProfile is null)
        {
            missingSections.Add("targetDefinition.targetArtifactProfile");
        }
        else
        {
            var targetArtifactProfile = plan.TargetDefinition.TargetArtifactProfile;

            ValidateNotBlank(targetArtifactProfile.ProfileId, "targetDefinition.targetArtifactProfile.profileId", missingFields);

            if (!GenerationRequestTargetProfileCatalog.IsSupportedArtifactType(targetArtifactProfile.ArtifactType))
            {
                unsupportedTargets.Add(GenerationRequestTargetProfileCatalog.ToContractValue(targetArtifactProfile.ArtifactType));
            }

            if (!string.IsNullOrWhiteSpace(targetArtifactProfile.ProfileId) &&
                !GenerationRequestTargetProfileCatalog.IsSupportedProfileId(targetArtifactProfile.ProfileId))
            {
                unsupportedTargets.Add(targetArtifactProfile.ProfileId);
            }

            if (!GenerationRequestTargetProfileCatalog.IsCompatibleProfile(targetArtifactProfile))
            {
                targetCompatibilityFailures.Add("targetDefinition.targetArtifactProfile is incompatible with the requested experience type.");
            }

            if (plan.TargetDefinition.ExperienceType != targetArtifactProfile.SourceExperienceType)
            {
                targetCompatibilityFailures.Add("targetDefinition.experienceType must match targetDefinition.targetArtifactProfile.sourceExperienceType.");
            }
        }

        if (plan.ProviderPlanningMetadata is null)
        {
            missingSections.Add("providerPlanningMetadata");
        }
        else
        {
            ValidateNotBlank(plan.ProviderPlanningMetadata.ProviderCategory, "providerPlanningMetadata.providerCategory", missingFields);

            if (!string.Equals(plan.ProviderPlanningMetadata.ProviderCategory, ExecutionPlanContract.ProviderNeutralPlanningCategory, StringComparison.Ordinal))
            {
                capabilityInconsistencies.Add("providerPlanningMetadata.providerCategory must stay providerNeutralPlanning.");
            }

            if (plan.ProviderPlanningMetadata.CapabilityModel is null)
            {
                missingSections.Add("providerPlanningMetadata.capabilityModel");
            }
            else
            {
                var expectedSupportedCapabilities = ExecutionPlanBuilder.BuildSupportedCapabilities(plan.ProviderPlanningMetadata.CapabilityModel);
                var expectedUnsupportedCapabilities = ExecutionPlanBuilder.BuildUnsupportedCapabilities(plan.ProviderPlanningMetadata.CapabilityModel);

                if (!AreEquivalent(plan.ProviderPlanningMetadata.SupportedCapabilities, expectedSupportedCapabilities) ||
                    !AreEquivalent(plan.ProviderPlanningMetadata.UnsupportedCapabilities, expectedUnsupportedCapabilities))
                {
                    capabilityInconsistencies.Add("providerPlanningMetadata capability declarations must match supportedCapabilities and unsupportedCapabilities.");
                }
            }
        }

        if (plan.PlannedWorkUnits is null || plan.PlannedWorkUnits.Count == 0)
        {
            missingSections.Add("plannedWorkUnits");
        }
        else
        {
            foreach (var workUnit in plan.PlannedWorkUnits)
            {
                ValidateNotBlank(workUnit.WorkUnitId, "plannedWorkUnits.workUnitId", missingFields);
                ValidateNotBlank(workUnit.Title, "plannedWorkUnits.title", missingFields);
                ValidateNotBlank(workUnit.Objective, "plannedWorkUnits.objective", missingFields);
            }
        }

        if (plan.DependencyGraph is null)
        {
            missingSections.Add("dependencyGraph");
        }
        else
        {
            if (plan.DependencyGraph.ExecutionOrder is null || plan.DependencyGraph.ExecutionOrder.Count == 0)
            {
                missingSections.Add("dependencyGraph.executionOrder");
            }

            if (plan.DependencyGraph.Dependencies is null || plan.DependencyGraph.Dependencies.Count == 0)
            {
                missingSections.Add("dependencyGraph.dependencies");
            }
            else
            {
                ValidateDependencies(plan.PlannedWorkUnits, plan.DependencyGraph, dependencyFailures, missingFields);
            }
        }

        if (plan.PlanningConstraints is null)
        {
            missingSections.Add("planningConstraints");
        }
        else if (plan.ProviderPlanningMetadata is not null)
        {
            if (!AreEquivalent(plan.PlanningConstraints.UnsupportedCapabilities, plan.ProviderPlanningMetadata.UnsupportedCapabilities))
            {
                capabilityInconsistencies.Add("planningConstraints.unsupportedCapabilities must match providerPlanningMetadata.unsupportedCapabilities.");
            }
        }

        if (plan.ReviewRequirements is null)
        {
            missingSections.Add("reviewRequirements");
        }
        else
        {
            if (!plan.ReviewRequirements.DesignApprovalRequired)
            {
                reviewRequirementFailures.Add("reviewRequirements.designApprovalRequired must stay true.");
            }

            if (!plan.ReviewRequirements.GenerationApprovalRequired)
            {
                reviewRequirementFailures.Add("reviewRequirements.generationApprovalRequired must stay true.");
            }

            if (!plan.ReviewRequirements.AnalyzerReviewRequired)
            {
                reviewRequirementFailures.Add("reviewRequirements.analyzerReviewRequired must stay true.");
            }
        }

        if (plan.SuccessContract is null)
        {
            missingSections.Add("successContract");
        }
        else
        {
            if (plan.SuccessContract.BusinessSuccessCriteria is null || plan.SuccessContract.BusinessSuccessCriteria.Count == 0)
            {
                missingSections.Add("successContract.businessSuccessCriteria");
            }

            if (plan.SuccessContract.AnalyticalSuccessCriteria is null || plan.SuccessContract.AnalyticalSuccessCriteria.Count == 0)
            {
                missingSections.Add("successContract.analyticalSuccessCriteria");
            }

            if (plan.SuccessContract.ValidationRequirements is null || plan.SuccessContract.ValidationRequirements.Count == 0)
            {
                missingSections.Add("successContract.validationRequirements");
            }
        }

        var diagnostics = new ExecutionPlanValidationDiagnostics(
            MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
            MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedTargetProfiles: unsupportedTargets.Distinct(StringComparer.Ordinal).ToArray(),
            UnsupportedSchemaVersions: unsupportedSchemaVersions.Distinct(StringComparer.Ordinal).ToArray(),
            DependencyFailures: dependencyFailures.Distinct(StringComparer.Ordinal).ToArray(),
            CapabilityInconsistencies: capabilityInconsistencies.Distinct(StringComparer.Ordinal).ToArray(),
            TargetCompatibilityFailures: targetCompatibilityFailures.Distinct(StringComparer.Ordinal).ToArray(),
            ReviewRequirementFailures: reviewRequirementFailures.Distinct(StringComparer.Ordinal).ToArray());

        return new ExecutionPlanValidationResult(diagnostics);
    }

    private static void ValidateDependencies(
        IReadOnlyList<ExecutionPlanWorkUnit>? plannedWorkUnits,
        ExecutionPlanDependencyGraph dependencyGraph,
        ICollection<string> dependencyFailures,
        ICollection<string> missingFields)
    {
        var workUnitIds = (plannedWorkUnits ?? []).Select(unit => unit.WorkUnitId).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet(StringComparer.Ordinal);

        foreach (var workUnitId in dependencyGraph.ExecutionOrder ?? [])
        {
            if (!workUnitIds.Contains(workUnitId))
            {
                dependencyFailures.Add($"dependencyGraph.executionOrder references unknown work unit {workUnitId}.");
            }
        }

        foreach (var dependency in dependencyGraph.Dependencies)
        {
            ValidateNotBlank(dependency.WorkUnitId, "dependencyGraph.dependencies.workUnitId", missingFields);

            if (!workUnitIds.Contains(dependency.WorkUnitId))
            {
                dependencyFailures.Add($"dependencyGraph.dependencies references unknown work unit {dependency.WorkUnitId}.");
            }

            foreach (var prerequisite in dependency.Prerequisites)
            {
                if (!workUnitIds.Contains(prerequisite))
                {
                    dependencyFailures.Add($"dependencyGraph.dependencies references unknown work unit {prerequisite}.");
                }

                if (string.Equals(prerequisite, dependency.WorkUnitId, StringComparison.Ordinal))
                {
                    dependencyFailures.Add($"dependencyGraph.dependencies contains a self dependency for {dependency.WorkUnitId}.");
                }
            }
        }
    }

    private static bool AreEquivalent(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        return (left ?? []).SequenceEqual(right ?? [], StringComparer.Ordinal);
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }
}
