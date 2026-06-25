using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationManifestValidator
{
    internal GenerationManifestValidationResult Validate(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        MicrosoftRuntimeProviderFrameworkState runtimeState)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(providerState);
        ArgumentNullException.ThrowIfNull(executionPlanningState);
        ArgumentNullException.ThrowIfNull(runtimeState);

        var missingRequiredSections = new List<string>();
        var missingRequiredFields = new List<string>();
        var invalidReferences = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var lineageIntegrityFailures = new List<string>();
        var readinessConsistencyFailures = new List<string>();
        var providerCompatibilityFailures = new List<string>();
        var boundaryViolations = new List<string>();

        ValidateMetadata(manifest, missingRequiredSections, missingRequiredFields, unsupportedSchemaVersions);
        ValidateReferences(
            manifest,
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeState,
            missingRequiredSections,
            missingRequiredFields,
            invalidReferences);
        ValidateGenerationSpecification(manifest, specificationState, missingRequiredSections, missingRequiredFields, invalidReferences);
        ValidateCapabilitySummary(manifest, providerState, runtimeState, planning, missingRequiredSections, missingRequiredFields, providerCompatibilityFailures);
        ValidateExecutionConstraints(manifest, boundaryViolations);
        ValidateApprovalSummary(manifest, planning, executionPlanningState, runtimeState, missingRequiredSections, readinessConsistencyFailures);
        ValidateLineage(manifest, planning, specificationState, providerState, executionPlanningState, runtimeState, missingRequiredSections, lineageIntegrityFailures);
        ValidateUpstreamSchemaVersions(planning, specificationState, providerState, executionPlanningState, runtimeState, unsupportedSchemaVersions);

        return new GenerationManifestValidationResult(
            new GenerationManifestValidationDiagnostics(
                MissingRequiredSections: DistinctAndOrder(missingRequiredSections),
                MissingRequiredFields: DistinctAndOrder(missingRequiredFields),
                InvalidReferences: DistinctAndOrder(invalidReferences),
                UnsupportedSchemaVersions: DistinctAndOrder(unsupportedSchemaVersions),
                LineageIntegrityFailures: DistinctAndOrder(lineageIntegrityFailures),
                ReadinessConsistencyFailures: DistinctAndOrder(readinessConsistencyFailures),
                ProviderCompatibilityFailures: DistinctAndOrder(providerCompatibilityFailures),
                BoundaryViolations: DistinctAndOrder(boundaryViolations)));
    }

    private static void ValidateMetadata(
        GenerationManifest manifest,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> unsupportedSchemaVersions)
    {
        if (manifest.Metadata is null)
        {
            missingRequiredSections.Add("metadata");
            return;
        }

        ValidateNotBlank(manifest.Metadata.ManifestId, "metadata.manifestId", missingRequiredFields);
        ValidateSchemaVersion(manifest.Metadata.SchemaVersion, GenerationManifestContract.SchemaVersionV1, unsupportedSchemaVersions);

        if (manifest.Metadata.CreatedUtc == default)
        {
            missingRequiredFields.Add("metadata.createdUtc");
        }
    }

    private static void ValidateReferences(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        MicrosoftRuntimeProviderFrameworkState runtimeState,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> invalidReferences)
    {
        if (manifest.References is null)
        {
            missingRequiredSections.Add("references");
            return;
        }

        ValidateNotBlank(manifest.References.DesignPackageRef, "references.designPackageRef", missingRequiredFields);
        ValidateNotBlank(manifest.References.GenerationRequestRef, "references.generationRequestRef", missingRequiredFields);
        ValidateNotBlank(manifest.References.ExecutionPlanRef, "references.executionPlanRef", missingRequiredFields);
        ValidateNotBlank(manifest.References.PlanningOutcomeRef, "references.planningOutcomeRef", missingRequiredFields);
        ValidateNotBlank(manifest.References.RuntimeProviderRef, "references.runtimeProviderRef", missingRequiredFields);
        ValidateNotBlank(manifest.References.GenerationProviderRequestRef, "references.generationProviderRequestRef", missingRequiredFields);
        ValidateNotBlank(manifest.References.GenerationProviderExecutionPlanRef, "references.generationProviderExecutionPlanRef", missingRequiredFields);

        ValidateReference(manifest.References.DesignPackageRef, planning.Outcome.References.DesignPackageRef, "references.designPackageRef must match planningOutcome.references.designPackageRef.", invalidReferences);
        ValidateReference(manifest.References.GenerationRequestRef, planning.Outcome.References.GenerationRequestRef, "references.generationRequestRef must match planningOutcome.references.generationRequestRef.", invalidReferences);
        ValidateReference(manifest.References.ExecutionPlanRef, planning.Outcome.References.ExecutionPlanRef, "references.executionPlanRef must match planningOutcome.references.executionPlanRef.", invalidReferences);
        ValidateReference(manifest.References.PlanningOutcomeRef, planning.Outcome.Metadata.OutcomeId, "references.planningOutcomeRef must match planningOutcome.metadata.outcomeId.", invalidReferences);
        ValidateReference(manifest.References.RuntimeProviderRef, runtimeState.Request?.RequestId, "references.runtimeProviderRef must match microsoftRuntimeProvider.request.requestId.", invalidReferences);
        ValidateReference(manifest.References.GenerationProviderRequestRef, providerState.Request?.Metadata.RequestId, "references.generationProviderRequestRef must match generationProviderRequest.metadata.requestId.", invalidReferences);
        ValidateReference(manifest.References.GenerationProviderExecutionPlanRef, executionPlanningState.Plan?.Metadata.ExecutionPlanId, "references.generationProviderExecutionPlanRef must match generationProviderExecutionPlan.metadata.executionPlanId.", invalidReferences);
    }

    private static void ValidateGenerationSpecification(
        GenerationManifest manifest,
        PbirGenerationSpecificationState specificationState,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> invalidReferences)
    {
        if (manifest.GenerationSpecification is null)
        {
            missingRequiredSections.Add("generationSpecification");
            return;
        }

        ValidateNotBlank(manifest.GenerationSpecification.PbirGenerationSpecificationRef, "generationSpecification.pbirGenerationSpecificationRef", missingRequiredFields);
        ValidateReference(
            manifest.GenerationSpecification.PbirGenerationSpecificationRef,
            specificationState.Specification?.SpecificationId,
            "generationSpecification.pbirGenerationSpecificationRef must match pbirGenerationSpecification.specificationId.",
            invalidReferences);
    }

    private static void ValidateCapabilitySummary(
        GenerationManifest manifest,
        GenerationProviderFrameworkState providerState,
        MicrosoftRuntimeProviderFrameworkState runtimeState,
        PlanningOrchestrationResult planning,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> providerCompatibilityFailures)
    {
        if (manifest.CapabilitySummary is null)
        {
            missingRequiredSections.Add("capabilitySummary");
            return;
        }

        if (manifest.CapabilitySummary.SelectedProvider is null)
        {
            missingRequiredSections.Add("capabilitySummary.selectedProvider");
            return;
        }

        ValidateNotBlank(manifest.CapabilitySummary.SelectedProvider.ProviderId, "capabilitySummary.selectedProvider.providerId", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedProvider.ProviderName, "capabilitySummary.selectedProvider.providerName", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedProvider.ProviderVersion, "capabilitySummary.selectedProvider.providerVersion", missingRequiredFields);

        if (!manifest.CapabilitySummary.NegotiatedCapabilities.SequenceEqual(
                PreserveOrder(planning.Outcome.ReadinessSummary.CapabilitySummary.RequiredCapabilities),
                StringComparer.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.negotiatedCapabilities must match planning outcome negotiated capabilities.");
        }

        if (!manifest.CapabilitySummary.ProviderCapabilities.SequenceEqual(
                PreserveOrder(providerState.Provider?.SupportedCapabilities),
                StringComparer.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.providerCapabilities must match generation provider capabilities.");
        }

        if (!string.Equals(manifest.CapabilitySummary.SelectedProvider.ProviderId, providerState.Provider?.ProviderId, StringComparison.Ordinal) ||
            !string.Equals(manifest.CapabilitySummary.SelectedProvider.ProviderName, providerState.Provider?.ProviderName, StringComparison.Ordinal) ||
            !string.Equals(manifest.CapabilitySummary.SelectedProvider.ProviderVersion, providerState.Provider?.ProviderVersion, StringComparison.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.selectedProvider must match the selected generation provider.");
        }

        if (!manifest.CapabilitySummary.SelectedSkills.SequenceEqual(
                PreserveOrder(runtimeState.Context?.MicrosoftSkillSummary.RequiredSkillIds),
                StringComparer.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.selectedSkills must match microsoft runtime required skills.");
        }
    }

    private static void ValidateExecutionConstraints(
        GenerationManifest manifest,
        ICollection<string> boundaryViolations)
    {
        if (manifest.ExecutionConstraints is null)
        {
            boundaryViolations.Add("executionConstraints must exist.");
            return;
        }

        if (!manifest.ExecutionConstraints.DryRunOnly ||
            manifest.ExecutionConstraints.DeploymentAllowed ||
            manifest.ExecutionConstraints.ProviderInvocationAllowed ||
            manifest.ExecutionConstraints.ApiInvocationAllowed ||
            manifest.ExecutionConstraints.CliInvocationAllowed)
        {
            boundaryViolations.Add("executionConstraints must preserve the Phase 18 non-execution trust boundary.");
        }
    }

    private static void ValidateApprovalSummary(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        GenerationProviderExecutionPlanningState executionPlanningState,
        MicrosoftRuntimeProviderFrameworkState runtimeState,
        ICollection<string> missingRequiredSections,
        ICollection<string> readinessConsistencyFailures)
    {
        if (manifest.ApprovalSummary is null)
        {
            missingRequiredSections.Add("approvalSummary");
            return;
        }

        if (manifest.ApprovalSummary.DesignApproval != planning.Outcome.ReadinessSummary.ApprovalStatus)
        {
            readinessConsistencyFailures.Add("approvalSummary.designApproval must match planningOutcome.readinessSummary.approvalStatus.");
        }

        var expectedPlanningApproval = new GenerationManifestPlanningApprovalSummary(
            OutcomeStatus: planning.Outcome.Status,
            PlanningReadiness: planning.Outcome.ReadinessSummary.Status,
            ExecutionProviderReadiness: planning.Outcome.ReadinessSummary.ExecutionProviderReadiness);
        if (manifest.ApprovalSummary.PlanningApproval != expectedPlanningApproval)
        {
            readinessConsistencyFailures.Add("approvalSummary.planningApproval must match planning outcome readiness state.");
        }

        if (manifest.ApprovalSummary.RuntimeReadiness != runtimeState.Readiness)
        {
            readinessConsistencyFailures.Add("approvalSummary.runtimeReadiness must match microsoftRuntimeProvider.readiness.");
        }

        if (manifest.ApprovalSummary.GenerationReadiness != executionPlanningState.Readiness)
        {
            readinessConsistencyFailures.Add("approvalSummary.generationReadiness must match generationProviderExecutionPlan.readiness.");
        }
    }

    private static void ValidateLineage(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        MicrosoftRuntimeProviderFrameworkState runtimeState,
        ICollection<string> missingRequiredSections,
        ICollection<string> lineageIntegrityFailures)
    {
        if (manifest.Lineage is null)
        {
            missingRequiredSections.Add("lineage");
            return;
        }

        var expectedReferences = BuildImmutableReferences(
            planning.Outcome,
            specificationState.Specification?.SpecificationId,
            providerState.Request?.Metadata.RequestId,
            executionPlanningState.Plan?.Metadata.ExecutionPlanId,
            runtimeState.Request?.RequestId);

        if (!expectedReferences.All(reference => manifest.Lineage.ImmutableReferences.Contains(reference, StringComparer.Ordinal)))
        {
            lineageIntegrityFailures.Add("lineage.immutableReferences must contain every required upstream reference.");
        }

        var expectedLineage = BuildExpectedLineageEntries(
            planning.Outcome,
            specificationState.Specification?.SpecificationId,
            providerState.Request?.Metadata.RequestId,
            executionPlanningState.Plan?.Metadata.ExecutionPlanId,
            runtimeState.Request?.RequestId);

        if (!manifest.Lineage.UpstreamLineage.SequenceEqual(expectedLineage))
        {
            lineageIntegrityFailures.Add("lineage.upstreamLineage must preserve complete deterministic upstream lineage.");
        }
    }

    private static void ValidateUpstreamSchemaVersions(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        MicrosoftRuntimeProviderFrameworkState runtimeState,
        ICollection<string> unsupportedSchemaVersions)
    {
        ValidateSchemaVersion(planning.Outcome.Metadata.SchemaVersion, PlanningOutcomeContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(specificationState.Specification?.SchemaVersion, PbirGenerationSpecificationContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(providerState.SchemaVersion, GenerationProviderContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(providerState.Request?.SchemaVersion, GenerationProviderRequestContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(providerState.Provider?.SchemaVersion, GenerationProviderDefinitionContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(executionPlanningState.Plan?.Metadata.SchemaVersion, GenerationProviderExecutionPlanContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(runtimeState.Definition?.SchemaVersion, MicrosoftRuntimeProviderContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(runtimeState.Request?.SchemaVersion, MicrosoftRuntimeRequestContract.SchemaVersionV1, unsupportedSchemaVersions);
    }

    private static void ValidateReference(string? actual, string? expected, string message, ICollection<string> invalidReferences)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            invalidReferences.Add(message);
        }
    }

    private static void ValidateSchemaVersion(string? actual, string expected, ICollection<string> unsupportedSchemaVersions)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(actual))
        {
            unsupportedSchemaVersions.Add(actual);
        }
    }

    private static void ValidateNotBlank(string? value, string fieldPath, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add(fieldPath);
        }
    }

    private static string[] BuildImmutableReferences(
        PlanningOutcome outcome,
        string? specificationRef,
        string? generationProviderRequestRef,
        string? generationProviderExecutionPlanRef,
        string? runtimeProviderRef)
    {
        return new[]
        {
            outcome.References.DesignPackageRef,
            outcome.References.GenerationRequestRef,
            outcome.References.ExecutionPlanRef,
            outcome.Metadata.OutcomeId,
            specificationRef ?? string.Empty,
            generationProviderRequestRef ?? string.Empty,
            generationProviderExecutionPlanRef ?? string.Empty,
            runtimeProviderRef ?? string.Empty
        }
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();
    }

    private static PlanningLineageEntry[] BuildExpectedLineageEntries(
        PlanningOutcome outcome,
        string? specificationRef,
        string? generationProviderRequestRef,
        string? generationProviderExecutionPlanRef,
        string? runtimeProviderRef)
    {
        var manifestEntries = new List<PlanningLineageEntry>();

        if (!string.IsNullOrWhiteSpace(specificationRef))
        {
            manifestEntries.Add(new PlanningLineageEntry("pbirGenerationSpecification", specificationRef, "PBIR generation specification"));
        }

        if (!string.IsNullOrWhiteSpace(generationProviderRequestRef))
        {
            manifestEntries.Add(new PlanningLineageEntry("generationProviderRequest", generationProviderRequestRef, "Generation provider request"));
        }

        if (!string.IsNullOrWhiteSpace(generationProviderExecutionPlanRef))
        {
            manifestEntries.Add(new PlanningLineageEntry("generationProviderExecutionPlan", generationProviderExecutionPlanRef, "Generation provider execution plan"));
        }

        if (!string.IsNullOrWhiteSpace(runtimeProviderRef))
        {
            manifestEntries.Add(new PlanningLineageEntry("runtimeProvider", runtimeProviderRef, "Runtime provider request"));
        }

        return outcome.Lineage.UpstreamLineage
            .Concat(outcome.Lineage.PlanningLineage)
            .Concat(manifestEntries)
            .Distinct()
            .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
            .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Label, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] PreserveOrder(IReadOnlyList<string>? values)
    {
        return values is null
            ? []
            : values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
    }

    private static string[] DistinctAndOrder(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }
}
