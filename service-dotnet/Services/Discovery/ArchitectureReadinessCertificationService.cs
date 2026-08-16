using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ArchitectureReadinessCertificationService
{
    internal ArchitectureCertificationState Certify(ArchitectureValidationState validationState)
    {
        ArgumentNullException.ThrowIfNull(validationState);

        var readiness = EvaluateReadiness(validationState.Diagnostics);
        if (validationState.Validation is null)
        {
            return new ArchitectureCertificationState(
                Certification: null,
                ReadinessReport: CreateReadinessReport("architectureReadiness:unvalidated", readiness),
                GapAnalysis: CreateGapAnalysis("architectureGapAnalysis:unvalidated"));
        }

        var certificationId = validationState.Validation.ValidationId.Replace("architectureValidation:", "architectureCertification:", StringComparison.Ordinal);
        var reportId = validationState.Validation.ValidationId.Replace("architectureValidation:", "architectureReadiness:", StringComparison.Ordinal);
        var analysisId = validationState.Validation.ValidationId.Replace("architectureValidation:", "architectureGapAnalysis:", StringComparison.Ordinal);

        var certification = new ArchitectureCertification(
            SchemaVersion: ArchitectureCertificationContract.SchemaVersionV1,
            CertificationId: certificationId,
            ArchitectureCoverage: CreateCoverage(validationState.Validation),
            TrustBoundaryVerification: validationState.Validation.TrustBoundaryVerification,
            OwnershipVerification: validationState.Validation.OwnershipVerification,
            ProviderNeutralityVerification: validationState.Validation.ProviderNeutralityVerification);

        return new ArchitectureCertificationState(
            Certification: certification,
            ReadinessReport: CreateReadinessReport(reportId, readiness),
            GapAnalysis: CreateGapAnalysis(analysisId));
    }

    internal ArchitectureReadinessState EvaluateReadiness(ArchitectureValidationDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        if (diagnostics.LayerSeparationViolations.Count > 0 ||
            diagnostics.DeterminismViolations.Count > 0 ||
            diagnostics.LineageViolations.Count > 0 ||
            diagnostics.SchemaVersionViolations.Count > 0 ||
            diagnostics.ReadinessTransitionViolations.Count > 0 ||
            diagnostics.ApprovalTransitionViolations.Count > 0)
        {
            return ArchitectureReadinessState.Incomplete;
        }

        if (diagnostics.TrustBoundaryViolations.Count > 0 ||
            diagnostics.OwnershipBoundaryViolations.Count > 0 ||
            diagnostics.ProviderNeutralityViolations.Count > 0)
        {
            return ArchitectureReadinessState.ConditionallyReady;
        }

        if (diagnostics.DeferredArchitectureGaps.Count > 0)
        {
            return ArchitectureReadinessState.ArchitecturallyComplete;
        }

        return ArchitectureReadinessState.ReadyForExecutionImplementation;
    }

    private static ArchitectureCoverage CreateCoverage(ArchitectureValidation validation)
    {
        return new ArchitectureCoverage(
            CompletedPhases: Enumerable.Range(1, 19).ToArray(),
            ImplementedContracts: validation.FrameworkParticipation
                .Select(framework => framework.Contract)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(contract => contract, StringComparer.Ordinal)
                .ToArray(),
            ImplementedServices: validation.FrameworkParticipation
                .Select(framework => framework.Service)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(service => service, StringComparer.Ordinal)
                .ToArray(),
            ImplementedSchemas: CreateImplementedSchemas(validation));
    }

    private static IReadOnlyList<string> CreateImplementedSchemas(ArchitectureValidation validation)
    {
        var schemas = validation.FrameworkParticipation
            .Select(framework => framework.SchemaVersion)
            .Where(schema => !string.IsNullOrWhiteSpace(schema))
            .Concat(
            [
                ArchitectureCertificationContract.SchemaVersionV1,
                ArchitectureReadinessReportContract.SchemaVersionV1,
                ArchitectureGapAnalysisContract.SchemaVersionV1,
                PbirArtifactSpecificationContract.SchemaVersionV1,
                GenerationProviderDefinitionContract.SchemaVersionV1,
                GenerationProviderRequestContract.SchemaVersionV1,
                GenerationProviderContextContract.SchemaVersionV1,
                GenerationProviderResultContract.SchemaVersionV1,
                RuntimeProviderRequestContract.SchemaVersionV1,
                RuntimeProviderContextContract.SchemaVersionV1,
                RuntimeProviderResultContract.SchemaVersionV1,
                MicrosoftRuntimeRequestContract.SchemaVersionV1,
                MicrosoftRuntimeContextContract.SchemaVersionV1,
                MicrosoftSkillDefinitionContract.SchemaVersionV1,
                MicrosoftSkillProviderContract.SchemaVersionV1,
                SkillProviderSelectionContract.SchemaVersionV1,
                PbirExecutionRequestContract.SchemaVersionV1,
                PbirMockExecutionResultContract.SchemaVersionV1,
            ]);

        return schemas
            .Distinct(StringComparer.Ordinal)
            .OrderBy(schema => schema, StringComparer.Ordinal)
            .ToArray();
    }

    private static ArchitectureReadinessReport CreateReadinessReport(string reportId, ArchitectureReadinessState readiness)
    {
        return new ArchitectureReadinessReport(
            SchemaVersion: ArchitectureReadinessReportContract.SchemaVersionV1,
            ReportId: reportId,
            Readiness: readiness,
            ExecutionCapabilityExists: false,
            Guarantees:
            [
                "The architecture is complete enough to begin implementing execution providers.",
                "The current platform still has no execution capability.",
                "Provider invocation, Microsoft API invocation, CLI invocation, PBIR generation, deployment, and Analyzer Workspace automation remain outside the implemented surface.",
            ],
            Conditions:
            [
                "Future execution providers must implement explicit provider contracts.",
                "Generated artifacts must remain downstream from planning and upstream from Analyzer Workspace validation.",
                "Approval and validation authority must remain separate from provider execution implementation.",
            ]);
    }

    private static ArchitectureGapAnalysis CreateGapAnalysis(string analysisId)
    {
        return new ArchitectureGapAnalysis(
            SchemaVersion: ArchitectureGapAnalysisContract.SchemaVersionV1,
            AnalysisId: analysisId,
            ArchitecturalGaps: [],
            RemainingWork:
            [
                new ArchitectureGap("artifactGeneration", "intentionallyUnimplemented", "No PBIR, Fabric App, or Fabric Data App artifact generation exists."),
                new ArchitectureGap("deployment", "intentionallyUnimplemented", "No deployment or publishing capability exists."),
                new ArchitectureGap("executionImplementation", "intentionallyUnimplemented", "Execution providers can now be designed against the certified architecture but are not implemented."),
                new ArchitectureGap("microsoftSkillsImplementation", "intentionallyUnimplemented", "Microsoft Skills remain catalog and adapter metadata only."),
                new ArchitectureGap("productUxIntegration", "intentionallyUnimplemented", "No product UX starts generation, provider invocation, deployment, or Analyzer Workspace automation from this certification."),
                new ArchitectureGap("providerImplementation", "intentionallyUnimplemented", "Generation and runtime providers remain interchangeable contracts without invocation implementations."),
            ]);
    }
}
