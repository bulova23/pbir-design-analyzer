using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ProviderAdapterCompatibilityService
{
    internal ProviderAdapterCompatibilityEvaluation Evaluate(
        ProviderAdapterDefinition adapterDefinition,
        ProviderAdapterRequest request,
        ExecutionPlan executionPlan,
        GenerationRequest generationRequest)
    {
        ArgumentNullException.ThrowIfNull(adapterDefinition);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(executionPlan);
        ArgumentNullException.ThrowIfNull(generationRequest);

        var missingSections = new List<string>();
        var missingFields = new List<string>();
        var targetCompatibilityFailures = new List<string>();
        var capabilityCompatibilityFailures = new List<string>();
        var executionPlanCompatibilityFailures = new List<string>();
        var versionCompatibilityFailures = new List<string>();

        ValidateNotBlank(adapterDefinition.AdapterId, "adapterDefinition.adapterId", missingFields);
        ValidateNotBlank(adapterDefinition.AdapterName, "adapterDefinition.adapterName", missingFields);
        ValidateNotBlank(adapterDefinition.AdapterVersion, "adapterDefinition.adapterVersion", missingFields);
        ValidateNotBlank(adapterDefinition.ProviderCategory, "adapterDefinition.providerCategory", missingFields);
        ValidateNotBlank(request.SchemaVersion, "request.schemaVersion", missingFields);
        ValidateNotBlank(request.ExecutionPlanRef, "request.executionPlanRef", missingFields);
        ValidateNotBlank(request.GenerationRequestRef, "request.generationRequestRef", missingFields);

        if (request.SourceContractVersions is null)
        {
            missingSections.Add("request.sourceContractVersions");
        }
        else
        {
            ValidateNotBlank(request.SourceContractVersions.GenerationRequestSchemaVersion, "request.sourceContractVersions.generationRequestSchemaVersion", missingFields);
            ValidateNotBlank(request.SourceContractVersions.ExecutionPlanSchemaVersion, "request.sourceContractVersions.executionPlanSchemaVersion", missingFields);
        }

        if (request.TargetArtifactProfile is null)
        {
            missingSections.Add("request.targetArtifactProfile");
        }
        else if (!adapterDefinition.SupportedTargetProfiles.Contains(request.TargetArtifactProfile.ProfileId, StringComparer.Ordinal))
        {
            targetCompatibilityFailures.Add(request.TargetArtifactProfile.ProfileId);
        }

        foreach (var capabilityRequirement in request.CapabilityRequirements ?? [])
        {
            if (!adapterDefinition.SupportedCapabilities.Contains(capabilityRequirement, StringComparer.Ordinal) ||
                adapterDefinition.UnsupportedCapabilities.Contains(capabilityRequirement, StringComparer.Ordinal))
            {
                capabilityCompatibilityFailures.Add(capabilityRequirement);
            }
        }

        if (!string.Equals(adapterDefinition.ProviderCategory, ProviderAdapterContract.ProviderNeutralCategory, StringComparison.Ordinal))
        {
            executionPlanCompatibilityFailures.Add("adapterDefinition.providerCategory must stay providerNeutral.");
        }

        if (!string.Equals(request.SchemaVersion, ProviderAdapterContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            versionCompatibilityFailures.Add(request.SchemaVersion);
        }

        if (request.SourceContractVersions is not null)
        {
            if (!adapterDefinition.SupportedGenerationRequestSchemaVersions.Contains(request.SourceContractVersions.GenerationRequestSchemaVersion, StringComparer.Ordinal))
            {
                versionCompatibilityFailures.Add(request.SourceContractVersions.GenerationRequestSchemaVersion);
            }

            if (!adapterDefinition.SupportedExecutionPlanSchemaVersions.Contains(request.SourceContractVersions.ExecutionPlanSchemaVersion, StringComparer.Ordinal))
            {
                versionCompatibilityFailures.Add(request.SourceContractVersions.ExecutionPlanSchemaVersion);
            }
        }

        if (!string.Equals(request.ExecutionPlanRef, executionPlan.ExecutionPlanId, StringComparison.Ordinal))
        {
            executionPlanCompatibilityFailures.Add("request.executionPlanRef must match executionPlan.executionPlanId.");
        }

        if (!string.Equals(request.GenerationRequestRef, generationRequest.RequestId, StringComparison.Ordinal))
        {
            executionPlanCompatibilityFailures.Add("request.generationRequestRef must match generationRequest.requestId.");
        }

        if (!string.Equals(request.GenerationRequestRef, executionPlan.SourceReferences.GenerationRequestRef, StringComparison.Ordinal))
        {
            executionPlanCompatibilityFailures.Add("request.generationRequestRef must match executionPlan.sourceReferences.generationRequestRef.");
        }

        if (!Equals(request.TargetArtifactProfile, generationRequest.TargetArtifactProfile) ||
            !Equals(request.TargetArtifactProfile, executionPlan.TargetDefinition.TargetArtifactProfile))
        {
            executionPlanCompatibilityFailures.Add("request.targetArtifactProfile must match generationRequest and executionPlan target definitions.");
        }

        if (!AreEquivalent(request.CapabilityRequirements, executionPlan.ProviderPlanningMetadata.SupportedCapabilities))
        {
            executionPlanCompatibilityFailures.Add("request.capabilityRequirements must match executionPlan.providerPlanningMetadata.supportedCapabilities.");
        }

        if (!AreEquivalent(request.Constraints.UnsupportedCapabilities, executionPlan.PlanningConstraints.UnsupportedCapabilities))
        {
            executionPlanCompatibilityFailures.Add("request.constraints.unsupportedCapabilities must match executionPlan.planningConstraints.unsupportedCapabilities.");
        }

        if (!Equals(request.ReviewRequirements, executionPlan.ReviewRequirements))
        {
            executionPlanCompatibilityFailures.Add("request.reviewRequirements must match executionPlan.reviewRequirements.");
        }

        if (!AreEquivalent(request.SuccessContract, generationRequest.SuccessContract) ||
            !AreEquivalent(request.SuccessContract, executionPlan.SuccessContract))
        {
            executionPlanCompatibilityFailures.Add("request.successContract must match generationRequest and executionPlan success contracts.");
        }

        var diagnostics = new ProviderAdapterCompatibilityDiagnostics(
            MissingRequiredSections: missingSections.Distinct(StringComparer.Ordinal).ToArray(),
            MissingRequiredFields: missingFields.Distinct(StringComparer.Ordinal).ToArray(),
            TargetCompatibilityFailures: targetCompatibilityFailures.Distinct(StringComparer.Ordinal).ToArray(),
            CapabilityCompatibilityFailures: capabilityCompatibilityFailures.Distinct(StringComparer.Ordinal).ToArray(),
            ExecutionPlanCompatibilityFailures: executionPlanCompatibilityFailures.Distinct(StringComparer.Ordinal).ToArray(),
            VersionCompatibilityFailures: versionCompatibilityFailures.Distinct(StringComparer.Ordinal).ToArray());

        return new ProviderAdapterCompatibilityEvaluation(GetStatus(diagnostics), diagnostics);
    }

    private static ProviderAdapterCompatibilityStatus GetStatus(ProviderAdapterCompatibilityDiagnostics diagnostics)
    {
        if (diagnostics.HasStructuralFailures)
        {
            return ProviderAdapterCompatibilityStatus.Incompatible;
        }

        if (diagnostics.HasUnsupportedFailures)
        {
            return ProviderAdapterCompatibilityStatus.Unsupported;
        }

        return ProviderAdapterCompatibilityStatus.Compatible;
    }

    private static bool AreEquivalent(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
    {
        return (left ?? []).SequenceEqual(right ?? [], StringComparer.Ordinal);
    }

    private static bool AreEquivalent(GenerationRequestSuccessContract? left, GenerationRequestSuccessContract? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        return AreEquivalent(left.BusinessSuccessCriteria, right.BusinessSuccessCriteria) &&
            AreEquivalent(left.AnalyticalSuccessCriteria, right.AnalyticalSuccessCriteria) &&
            AreEquivalent(left.ValidationRequirements, right.ValidationRequirements);
    }

    private static void ValidateNotBlank(string? value, string fieldName, ICollection<string> missingFields)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(fieldName);
        }
    }
}
