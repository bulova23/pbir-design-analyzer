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
        RuntimeProviderFrameworkState runtimeProviderState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(planning);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(providerState);
        ArgumentNullException.ThrowIfNull(executionPlanningState);
        ArgumentNullException.ThrowIfNull(runtimeProviderState);
        ArgumentNullException.ThrowIfNull(microsoftRuntimeState);

        var missingRequiredSections = new List<string>();
        var missingRequiredFields = new List<string>();
        var invalidReferences = new List<string>();
        var unsupportedSchemaVersions = new List<string>();
        var lineageIntegrityFailures = new List<string>();
        var readinessConsistencyFailures = new List<string>();
        var providerCompatibilityFailures = new List<string>();
        var generationSpecificationCompletenessFailures = new List<string>();
        var boundaryViolations = new List<string>();

        ValidateMetadata(manifest, missingRequiredSections, missingRequiredFields, unsupportedSchemaVersions);
        ValidateSourceReferences(
            manifest,
            planning,
            specificationState,
            providerState,
            executionPlanningState,
            runtimeProviderState,
            missingRequiredSections,
            missingRequiredFields,
            invalidReferences);
        ValidateCapabilitySummary(manifest, providerState, microsoftRuntimeState, planning, missingRequiredSections, missingRequiredFields, providerCompatibilityFailures);
        ValidateExecutionConstraints(manifest, boundaryViolations);
        ValidateReadinessSummary(manifest, planning, providerState, executionPlanningState, runtimeProviderState, missingRequiredSections, readinessConsistencyFailures);
        ValidateApprovalSummary(manifest, planning, providerState, microsoftRuntimeState, missingRequiredSections, readinessConsistencyFailures);
        ValidateLineage(manifest, planning, specificationState, providerState, executionPlanningState, runtimeProviderState, missingRequiredSections, lineageIntegrityFailures);
        ValidateGenerationSpecification(specificationState, generationSpecificationCompletenessFailures);
        ValidateUpstreamSchemaVersions(planning, specificationState, providerState, executionPlanningState, runtimeProviderState, microsoftRuntimeState, unsupportedSchemaVersions);

        return new GenerationManifestValidationResult(
            new GenerationManifestValidationDiagnostics(
                MissingRequiredSections: DistinctAndOrder(missingRequiredSections),
                MissingRequiredFields: DistinctAndOrder(missingRequiredFields),
                InvalidReferences: DistinctAndOrder(invalidReferences),
                UnsupportedSchemaVersions: DistinctAndOrder(unsupportedSchemaVersions),
                LineageIntegrityFailures: DistinctAndOrder(lineageIntegrityFailures),
                ReadinessConsistencyFailures: DistinctAndOrder(readinessConsistencyFailures),
                ProviderCompatibilityFailures: DistinctAndOrder(providerCompatibilityFailures),
                GenerationSpecificationCompletenessFailures: DistinctAndOrder(generationSpecificationCompletenessFailures),
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

    private static void ValidateSourceReferences(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        ICollection<string> missingRequiredSections,
        ICollection<string> missingRequiredFields,
        ICollection<string> invalidReferences)
    {
        if (manifest.SourceReferences is null)
        {
            missingRequiredSections.Add("sourceReferences");
            return;
        }

        ValidateNotBlank(manifest.SourceReferences.DesignPackageRef, "sourceReferences.designPackageRef", missingRequiredFields);
        ValidateNotBlank(manifest.SourceReferences.GenerationRequestRef, "sourceReferences.generationRequestRef", missingRequiredFields);
        ValidateNotBlank(manifest.SourceReferences.ExecutionPlanRef, "sourceReferences.executionPlanRef", missingRequiredFields);
        ValidateNotBlank(manifest.SourceReferences.PlanningOutcomeRef, "sourceReferences.planningOutcomeRef", missingRequiredFields);
        ValidateNotBlank(manifest.SourceReferences.RuntimeProviderRef, "sourceReferences.runtimeProviderRef", missingRequiredFields);
        ValidateNotBlank(manifest.SourceReferences.GenerationProviderRequestRef, "sourceReferences.generationProviderRequestRef", missingRequiredFields);
        ValidateNotBlank(manifest.SourceReferences.GenerationProviderExecutionPlanRef, "sourceReferences.generationProviderExecutionPlanRef", missingRequiredFields);
        ValidateNotBlank(manifest.SourceReferences.PbirGenerationSpecificationRef, "sourceReferences.pbirGenerationSpecificationRef", missingRequiredFields);

        ValidateReference(manifest.SourceReferences.DesignPackageRef, planning.Outcome.References.DesignPackageRef, "sourceReferences.designPackageRef must match planningOutcome.references.designPackageRef.", invalidReferences);
        ValidateReference(manifest.SourceReferences.GenerationRequestRef, planning.Outcome.References.GenerationRequestRef, "sourceReferences.generationRequestRef must match planningOutcome.references.generationRequestRef.", invalidReferences);
        ValidateReference(manifest.SourceReferences.ExecutionPlanRef, planning.Outcome.References.ExecutionPlanRef, "sourceReferences.executionPlanRef must match planningOutcome.references.executionPlanRef.", invalidReferences);
        ValidateReference(manifest.SourceReferences.PlanningOutcomeRef, planning.Outcome.Metadata.OutcomeId, "sourceReferences.planningOutcomeRef must match planningOutcome.metadata.outcomeId.", invalidReferences);
        ValidateReference(manifest.SourceReferences.RuntimeProviderRef, runtimeProviderState.Request?.RequestId, "sourceReferences.runtimeProviderRef must match runtimeProvider.request.requestId.", invalidReferences);
        ValidateReference(manifest.SourceReferences.GenerationProviderRequestRef, providerState.Request?.Metadata.RequestId, "sourceReferences.generationProviderRequestRef must match generationProviderRequest.metadata.requestId.", invalidReferences);
        ValidateReference(manifest.SourceReferences.GenerationProviderExecutionPlanRef, executionPlanningState.Plan?.Metadata.ExecutionPlanId, "sourceReferences.generationProviderExecutionPlanRef must match generationProviderExecutionPlan.metadata.executionPlanId.", invalidReferences);
        ValidateReference(manifest.SourceReferences.PbirGenerationSpecificationRef, specificationState.Specification?.SpecificationId, "sourceReferences.pbirGenerationSpecificationRef must match pbirGenerationSpecification.specificationId.", invalidReferences);
    }

    private static void ValidateCapabilitySummary(
        GenerationManifest manifest,
        GenerationProviderFrameworkState providerState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
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

        if (manifest.CapabilitySummary.SelectedGenerationProvider is null)
        {
            missingRequiredSections.Add("capabilitySummary.selectedGenerationProvider");
            return;
        }

        if (manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider is null)
        {
            missingRequiredSections.Add("capabilitySummary.selectedMicrosoftRuntimeProvider");
            return;
        }

        ValidateNotBlank(manifest.CapabilitySummary.SelectedGenerationProvider.ProviderId, "capabilitySummary.selectedGenerationProvider.providerId", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedGenerationProvider.ProviderName, "capabilitySummary.selectedGenerationProvider.providerName", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedGenerationProvider.ProviderVersion, "capabilitySummary.selectedGenerationProvider.providerVersion", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderId, "capabilitySummary.selectedMicrosoftRuntimeProvider.providerId", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderName, "capabilitySummary.selectedMicrosoftRuntimeProvider.providerName", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderVersion, "capabilitySummary.selectedMicrosoftRuntimeProvider.providerVersion", missingRequiredFields);
        ValidateNotBlank(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderCategory, "capabilitySummary.selectedMicrosoftRuntimeProvider.providerCategory", missingRequiredFields);

        if (!manifest.CapabilitySummary.NegotiatedCapabilities.SequenceEqual(
                PreserveOrder(planning.Outcome.ReadinessSummary.CapabilitySummary.RequiredCapabilities),
                StringComparer.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.negotiatedCapabilities must match planning outcome negotiated capabilities.");
        }

        if (!string.Equals(manifest.CapabilitySummary.SelectedGenerationProvider.ProviderId, providerState.Provider?.ProviderId, StringComparison.Ordinal) ||
            !string.Equals(manifest.CapabilitySummary.SelectedGenerationProvider.ProviderName, providerState.Provider?.ProviderName, StringComparison.Ordinal) ||
            !string.Equals(manifest.CapabilitySummary.SelectedGenerationProvider.ProviderVersion, providerState.Provider?.ProviderVersion, StringComparison.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.selectedGenerationProvider must match the selected generation provider.");
        }

        if (!string.Equals(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderId, microsoftRuntimeState.Definition?.ProviderId, StringComparison.Ordinal) ||
            !string.Equals(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderName, microsoftRuntimeState.Definition?.ProviderName, StringComparison.Ordinal) ||
            !string.Equals(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderVersion, microsoftRuntimeState.Definition?.ProviderVersion, StringComparison.Ordinal) ||
            !string.Equals(manifest.CapabilitySummary.SelectedMicrosoftRuntimeProvider.ProviderCategory, microsoftRuntimeState.Definition?.ProviderCategory, StringComparison.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.selectedMicrosoftRuntimeProvider must match the selected Microsoft runtime provider.");
        }

        if (!manifest.CapabilitySummary.SelectedSkills.SequenceEqual(
                PreserveOrder(microsoftRuntimeState.Context?.MicrosoftSkillSummary.RequiredSkillIds),
                StringComparer.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.selectedSkills must match microsoft runtime required skills.");
        }

        if (!manifest.CapabilitySummary.SelectedProviderCandidates.SequenceEqual(
                PreserveOrder(microsoftRuntimeState.Context?.MicrosoftSkillSummary.CandidateProviderIds),
                StringComparer.Ordinal))
        {
            providerCompatibilityFailures.Add("capabilitySummary.selectedProviderCandidates must match microsoft runtime candidate provider ids.");
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
            boundaryViolations.Add("executionConstraints must preserve the Phase 19 non-execution trust boundary.");
        }
    }

    private static void ValidateReadinessSummary(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        ICollection<string> missingRequiredSections,
        ICollection<string> readinessConsistencyFailures)
    {
        if (manifest.ReadinessSummary is null)
        {
            missingRequiredSections.Add("readinessSummary");
            return;
        }

        if (manifest.ReadinessSummary.PlanningReadiness != planning.Outcome.ReadinessSummary.Status)
        {
            readinessConsistencyFailures.Add("readinessSummary.planningReadiness must match planningOutcome.readinessSummary.status.");
        }

        if (manifest.ReadinessSummary.RuntimeReadiness != runtimeProviderState.Readiness)
        {
            readinessConsistencyFailures.Add("readinessSummary.runtimeReadiness must match runtimeProvider.readiness.");
        }

        if (manifest.ReadinessSummary.ProviderReadiness != providerState.Readiness)
        {
            readinessConsistencyFailures.Add("readinessSummary.providerReadiness must match generationProvider.readiness.");
        }

        if (manifest.ReadinessSummary.GenerationReadiness != executionPlanningState.Readiness)
        {
            readinessConsistencyFailures.Add("readinessSummary.generationReadiness must match generationProviderExecutionPlan.readiness.");
        }
    }

    private static void ValidateApprovalSummary(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        GenerationProviderFrameworkState providerState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
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

        var expectedRuntimeApproval = new GenerationManifestRuntimeApprovalSummary(
            RuntimeProviderId: microsoftRuntimeState.Definition?.ProviderId ?? string.Empty,
            RuntimeReadiness: microsoftRuntimeState.Readiness,
            AcceptsExecutionCandidate: microsoftRuntimeState.AcceptsExecutionCandidate);
        if (manifest.ApprovalSummary.RuntimeApproval != expectedRuntimeApproval)
        {
            readinessConsistencyFailures.Add("approvalSummary.runtimeApproval must match microsoft runtime provider acceptance state.");
        }

        var expectedProviderApproval = new GenerationManifestProviderApprovalSummary(
            ProviderId: providerState.Provider?.ProviderId ?? string.Empty,
            ProviderReadiness: providerState.Readiness,
            ProviderApproved: providerState.Readiness == GenerationProviderReadinessState.ReadyForGenerationProvider);
        if (manifest.ApprovalSummary.ProviderApproval != expectedProviderApproval)
        {
            readinessConsistencyFailures.Add("approvalSummary.providerApproval must match generation provider approval state.");
        }
    }

    private static void ValidateLineage(
        GenerationManifest manifest,
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
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
            runtimeProviderState.Request?.RequestId);

        if (!expectedReferences.All(reference => manifest.Lineage.ImmutableUpstreamLineage.Contains(reference, StringComparer.Ordinal)))
        {
            lineageIntegrityFailures.Add("lineage.immutableUpstreamLineage must contain every required upstream reference.");
        }

        var expectedLineage = BuildExpectedLineageEntries(
            planning.Outcome,
            specificationState.Specification?.SpecificationId,
            providerState.Request?.Metadata.RequestId,
            executionPlanningState.Plan?.Metadata.ExecutionPlanId,
            runtimeProviderState.Request?.RequestId);

        if (!manifest.Lineage.UpstreamLineage.SequenceEqual(expectedLineage))
        {
            lineageIntegrityFailures.Add("lineage.upstreamLineage must preserve complete deterministic upstream lineage.");
        }
    }

    private static void ValidateGenerationSpecification(
        PbirGenerationSpecificationState specificationState,
        ICollection<string> generationSpecificationCompletenessFailures)
    {
        if (specificationState.Specification is null)
        {
            generationSpecificationCompletenessFailures.Add("pbir generation specification is missing.");
            return;
        }

        if (specificationState.Readiness != PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider)
        {
            generationSpecificationCompletenessFailures.Add("pbir generation specification must be readyForGenerationProvider.");
        }
    }

    private static void ValidateUpstreamSchemaVersions(
        PlanningOrchestrationResult planning,
        PbirGenerationSpecificationState specificationState,
        GenerationProviderFrameworkState providerState,
        GenerationProviderExecutionPlanningState executionPlanningState,
        RuntimeProviderFrameworkState runtimeProviderState,
        MicrosoftRuntimeProviderFrameworkState microsoftRuntimeState,
        ICollection<string> unsupportedSchemaVersions)
    {
        ValidateSchemaVersion(planning.Outcome.Metadata.SchemaVersion, PlanningOutcomeContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(specificationState.Specification?.SchemaVersion, PbirGenerationSpecificationContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(providerState.SchemaVersion, GenerationProviderContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(providerState.Request?.SchemaVersion, GenerationProviderRequestContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(providerState.Provider?.SchemaVersion, GenerationProviderDefinitionContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(executionPlanningState.Plan?.Metadata.SchemaVersion, GenerationProviderExecutionPlanContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(runtimeProviderState.Request?.SchemaVersion, RuntimeProviderRequestContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(microsoftRuntimeState.Definition?.SchemaVersion, MicrosoftRuntimeProviderContract.SchemaVersionV1, unsupportedSchemaVersions);
        ValidateSchemaVersion(microsoftRuntimeState.Request?.SchemaVersion, MicrosoftRuntimeRequestContract.SchemaVersionV1, unsupportedSchemaVersions);
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
        return outcome.Lineage.UpstreamLineage
            .Concat(outcome.Lineage.PlanningLineage)
            .Concat(
            [
                new PlanningLineageEntry("generationProviderExecutionPlan", generationProviderExecutionPlanRef ?? string.Empty, "Generation provider execution plan"),
                new PlanningLineageEntry("generationProviderRequest", generationProviderRequestRef ?? string.Empty, "Generation provider request"),
                new PlanningLineageEntry("pbirGenerationSpecification", specificationRef ?? string.Empty, "PBIR generation specification"),
                new PlanningLineageEntry("runtimeProvider", runtimeProviderRef ?? string.Empty, "Runtime provider request"),
            ])
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ReferenceId))
            .Distinct()
            .OrderBy(entry => entry.Stage, StringComparer.Ordinal)
            .ThenBy(entry => entry.ReferenceId, StringComparer.Ordinal)
            .ThenBy(entry => entry.Label, StringComparer.Ordinal)
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

    private static string[] PreserveOrder(IReadOnlyList<string>? values)
    {
        return values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];
    }
}
