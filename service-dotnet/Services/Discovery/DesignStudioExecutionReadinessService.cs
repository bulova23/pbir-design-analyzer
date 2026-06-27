using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class DesignStudioExecutionReadinessService
{
    private readonly DesignStudioExecutionReadinessSafetyGate _safetyGate;

    internal DesignStudioExecutionReadinessService()
        : this(new DesignStudioExecutionReadinessSafetyGate())
    {
    }

    internal DesignStudioExecutionReadinessService(DesignStudioExecutionReadinessSafetyGate safetyGate)
    {
        _safetyGate = safetyGate;
    }

    internal DesignStudioExecutionReadinessState CreateDashboard(
        DesignStudioExecutionReadinessContext context,
        DesignStudioExecutionReadinessBoundaryRequests boundaryRequests,
        DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(boundaryRequests);

        var safety = _safetyGate.Validate(context, boundaryRequests);
        if (!safety.IsAllowed || context.GenerationManifestState.Manifest is null)
        {
            return new DesignStudioExecutionReadinessState(
                Dashboard: null,
                Safety: safety,
                ReadinessSummary: DesignStudioExecutionReadinessSummary.Blocked);
        }

        var manifest = context.GenerationManifestState.Manifest;
        var readinessSummary = ClassifyReadiness(context);
        var dashboard = new DesignStudioExecutionReadinessDashboard(
            SchemaVersion: DesignStudioExecutionReadinessContract.SchemaVersionV1,
            DashboardId: $"designStudioExecutionReadiness:{manifest.Metadata.ManifestId}",
            CreatedUtc: createdUtc.UtcDateTime,
            ReadinessSummary: readinessSummary,
            StageSummaries: BuildStageSummaries(context, manifest),
            WarningSummaries: BuildWarningSummaries(context),
            ReviewerActionsAvailable: BuildReviewerActions(context),
            LineageReferences: BuildLineageReferences(context, manifest),
            ArchitectureCertificationReference: BuildArchitectureReference(context.ArchitectureCertificationState),
            TrustBoundary: CreateTrustBoundary());

        return new DesignStudioExecutionReadinessState(
            Dashboard: dashboard,
            Safety: safety,
            ReadinessSummary: readinessSummary);
    }

    private static DesignStudioExecutionReadinessSummary ClassifyReadiness(DesignStudioExecutionReadinessContext context)
    {
        if (!context.ArchitectureCertificationState.IsCertified ||
            context.GenerationManifestState.Readiness == GenerationManifestReadinessState.Blocked ||
            !context.PipelineVerificationState.IsVerified ||
            context.ReviewHandoffState.Readiness == PbirReviewHandoffReadinessState.Blocked)
        {
            return DesignStudioExecutionReadinessSummary.Blocked;
        }

        if (context.GenerationManifestState.Readiness == GenerationManifestReadinessState.ReadyForGenerator &&
            context.PreviewPackageState.Package is null)
        {
            return DesignStudioExecutionReadinessSummary.ReadyForGenerationProvider;
        }

        if (context.PreviewPackageState.Package is null ||
            context.ReviewHandoffState.Handoff is null ||
            context.PbirIntermediateRepresentationState.Ir is null)
        {
            return DesignStudioExecutionReadinessSummary.NotReady;
        }

        if (context.PreviewReviewStatus is DesignStudioExecutionPreviewReviewStatus.RevisionRequested or DesignStudioExecutionPreviewReviewStatus.Deferred)
        {
            return DesignStudioExecutionReadinessSummary.NotReady;
        }

        if (context.PreviewReviewStatus is DesignStudioExecutionPreviewReviewStatus.MarkedReviewed or DesignStudioExecutionPreviewReviewStatus.AnalyzerCandidateMetadataPrepared ||
            context.ReviewHandoffState.Readiness == PbirReviewHandoffReadinessState.ReadyForAnalyzerReview)
        {
            return DesignStudioExecutionReadinessSummary.ReadyForAnalyzerReview;
        }

        if (context.ReviewHandoffState.Readiness == PbirReviewHandoffReadinessState.ReadyForDesignReview ||
            context.PreviewReviewStatus == DesignStudioExecutionPreviewReviewStatus.Pending)
        {
            return DesignStudioExecutionReadinessSummary.ReadyForDesignReview;
        }

        return DesignStudioExecutionReadinessSummary.NotReady;
    }

    private static IReadOnlyList<DesignStudioExecutionReadinessStageSummary> BuildStageSummaries(
        DesignStudioExecutionReadinessContext context,
        GenerationManifest manifest)
    {
        return
        [
            new DesignStudioExecutionReadinessStageSummary(
                StageId: "architecture",
                Section: "Architecture",
                Status: context.ArchitectureCertificationState.IsCertified ? "ready" : "blocked",
                Summary: "Architecture certification and readiness classification.",
                Items:
                [
                    Item("Architecture certification status", context.ArchitectureCertificationState.IsCertified ? "Certified" : "Not certified"),
                    Item("Architecture readiness classification", context.ArchitectureCertificationState.ReadinessReport?.Readiness.ToString() ?? "Unavailable"),
                ]),
            new DesignStudioExecutionReadinessStageSummary(
                StageId: "planning",
                Section: "Planning",
                Status: context.GenerationManifestState.Readiness == GenerationManifestReadinessState.ReadyForGenerator && context.PipelineVerificationState.IsVerified ? "ready" : "blocked",
                Summary: "Planning outcome, generation manifest, and pipeline verification.",
                Items:
                [
                    Item("Planning outcome status", manifest.ApprovalSummary.PlanningApproval.OutcomeStatus.ToString()),
                    Item("Generation Manifest status", context.GenerationManifestState.Readiness.ToString()),
                    Item("Pipeline verification status", context.PipelineVerificationState.IsVerified ? "Verified" : "Not verified"),
                ]),
            new DesignStudioExecutionReadinessStageSummary(
                StageId: "generation",
                Section: "Generation",
                Status: context.PbirIntermediateRepresentationState.Readiness == PbirIntermediateRepresentationReadinessState.ReadyForSerializer ? "ready" : "notReady",
                Summary: "PBIR generation specification, canonical IR, preview package, and preview review.",
                Items:
                [
                    Item("PBIR Generation Specification readiness", context.PbirGenerationSpecificationState.Readiness.ToString()),
                    Item("PBIR IR readiness", context.PbirIntermediateRepresentationState.Readiness.ToString()),
                    Item("Preview Package readiness", context.PreviewPackageState.Readiness.ToString()),
                    Item("Preview Review status", context.PreviewReviewStatus.ToString()),
                ]),
            new DesignStudioExecutionReadinessStageSummary(
                StageId: "runtime",
                Section: "Runtime",
                Status: IsRuntimeReady(manifest) ? "ready" : "notReady",
                Summary: "Runtime and provider readiness without invocation.",
                Items:
                [
                    Item("Runtime Provider readiness", manifest.ReadinessSummary.RuntimeReadiness.ToString()),
                    Item("Microsoft Runtime Provider readiness", manifest.ApprovalSummary.RuntimeApproval.RuntimeReadiness.ToString()),
                    Item("Generation Provider readiness", manifest.ReadinessSummary.ProviderReadiness.ToString()),
                ]),
            new DesignStudioExecutionReadinessStageSummary(
                StageId: "skills",
                Section: "Skills",
                Status: manifest.CapabilitySummary.SelectedSkills.Count > 0 ? "ready" : "notReady",
                Summary: "Skill metadata and capability coverage only.",
                Items:
                [
                    Item("Skill readiness", manifest.ApprovalSummary.RuntimeApproval.AcceptsExecutionCandidate ? "ReadyForSkillProviderMetadata" : "NotReady"),
                    Item("Selected provider", manifest.CapabilitySummary.SelectedGenerationProvider.ProviderId),
                    Item("Selected skills", string.Join(", ", manifest.CapabilitySummary.SelectedSkills)),
                    Item("Capability coverage summary", $"{manifest.CapabilitySummary.NegotiatedCapabilities.Count} negotiated capabilities; {manifest.CapabilitySummary.SelectedProviderCandidates.Count} provider candidates."),
                ]),
            new DesignStudioExecutionReadinessStageSummary(
                StageId: "review",
                Section: "Review",
                Status: context.ReviewHandoffState.Readiness is PbirReviewHandoffReadinessState.ReadyForDesignReview or PbirReviewHandoffReadinessState.ReadyForAnalyzerReview ? "ready" : "notReady",
                Summary: "Design approval, preview review, and Analyzer handoff readiness.",
                Items:
                [
                    Item("Design approval status", manifest.ApprovalSummary.DesignApproval.DesignApproved ? "Approved" : "Missing"),
                    Item("Preview review status", context.PreviewReviewStatus.ToString()),
                    Item("Analyzer handoff readiness", context.ReviewHandoffState.Readiness.ToString()),
                ]),
        ];
    }

    private static bool IsRuntimeReady(GenerationManifest manifest)
    {
        return manifest.ReadinessSummary.RuntimeReadiness == RuntimeProviderReadinessState.ReadyForRuntimeProvider &&
            manifest.ApprovalSummary.RuntimeApproval.RuntimeReadiness == MicrosoftRuntimeReadinessState.ReadyForMicrosoftRuntimeProvider &&
            manifest.ReadinessSummary.ProviderReadiness == GenerationProviderReadinessState.ReadyForGenerationProvider;
    }

    private static IReadOnlyList<DesignStudioExecutionReadinessWarningSummary> BuildWarningSummaries(
        DesignStudioExecutionReadinessContext context)
    {
        var warnings = new List<DesignStudioExecutionReadinessWarningSummary>();
        var manifest = context.GenerationManifestState.Manifest;

        if (manifest is null)
        {
            warnings.Add(Warning("blockingIssue", "error", "Generation manifest is missing."));
        }
        else
        {
            if (!manifest.ApprovalSummary.DesignApproval.DesignApproved)
            {
                warnings.Add(Warning("missingApproval", "warning", "Design approval has not been recorded."));
            }

            if (!manifest.ApprovalSummary.DesignApproval.GenerationApproved)
            {
                warnings.Add(Warning("missingApproval", "warning", "Generation approval has not been recorded."));
            }
        }

        if (!context.PipelineVerificationState.IsVerified)
        {
            warnings.Add(Warning("blockingIssue", "error", "Pipeline verification has not passed."));
        }

        warnings.Add(Warning("unsupportedCapability", "info", "Analyzer Workspace automation is not implemented."));
        warnings.Add(Warning("unsupportedCapability", "info", "Deployment is not implemented."));
        warnings.Add(Warning("unsupportedCapability", "info", "Microsoft Skills execution is not implemented."));
        warnings.Add(Warning("unsupportedCapability", "info", "PBIR generation is not implemented."));
        warnings.Add(Warning("unsupportedCapability", "info", "Provider, API, and CLI invocation are not implemented."));

        if (context.PreviewPackageState.Package is not null)
        {
            warnings.AddRange(context.PreviewPackageState.Package.Warnings.Select(message => Warning("previewPackage", "info", message)));
        }

        if (context.ReviewHandoffState.Handoff is not null)
        {
            warnings.AddRange(context.ReviewHandoffState.Handoff.Warnings.Select(message => Warning("reviewHandoff", "info", message)));
        }

        if (context.ArchitectureCertificationState.GapAnalysis is not null)
        {
            warnings.AddRange(context.ArchitectureCertificationState.GapAnalysis.RemainingWork
                .Select(gap => Warning("remainingArchitectureGap", "info", gap.Summary)));
        }

        return warnings
            .Distinct()
            .OrderBy(warning => warning.Category, StringComparer.Ordinal)
            .ThenBy(warning => warning.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> BuildReviewerActions(DesignStudioExecutionReadinessContext context)
    {
        var actions = new List<string>
        {
            "Review readiness dashboard",
            "Request revision",
            "Defer review"
        };

        if (context.PreviewPackageState.Package is not null)
        {
            actions.Add("Mark preview reviewed");
        }

        if (context.ReviewHandoffState.Handoff is not null)
        {
            actions.Add("Prepare Analyzer candidate metadata");
        }

        return actions
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<DesignStudioExecutionReadinessLineageReference> BuildLineageReferences(
        DesignStudioExecutionReadinessContext context,
        GenerationManifest manifest)
    {
        var references = new List<DesignStudioExecutionReadinessLineageReference>
        {
            Reference("generationManifest", manifest.Metadata.ManifestId, manifest.Metadata.SchemaVersion),
            Reference("designPackage", manifest.SourceReferences.DesignPackageRef, "design-package/v1"),
            Reference("planningOutcome", manifest.SourceReferences.PlanningOutcomeRef, "planning-outcome/v1"),
            Reference("pbirGenerationSpecification", manifest.SourceReferences.PbirGenerationSpecificationRef, PbirGenerationSpecificationContract.SchemaVersionV1),
        };

        if (context.PbirIntermediateRepresentationState.Ir is not null)
        {
            references.Add(Reference("pbirIr", context.PbirIntermediateRepresentationState.Ir.Metadata.IrId, context.PbirIntermediateRepresentationState.Ir.Metadata.SchemaVersion));
        }

        if (context.PreviewPackageState.Package is not null)
        {
            references.Add(Reference("previewPackage", context.PreviewPackageState.Package.Metadata.PackageId, context.PreviewPackageState.Package.SchemaVersion));
        }

        if (context.ReviewHandoffState.Handoff is not null)
        {
            references.Add(Reference("reviewHandoff", context.ReviewHandoffState.Handoff.HandoffId, context.ReviewHandoffState.Handoff.SchemaVersion));
        }

        return references
            .Where(reference => !string.IsNullOrWhiteSpace(reference.ReferenceId))
            .Distinct()
            .OrderBy(reference => reference.Stage, StringComparer.Ordinal)
            .ThenBy(reference => reference.ReferenceId, StringComparer.Ordinal)
            .ToArray();
    }

    private static DesignStudioExecutionReadinessArchitectureCertificationReference BuildArchitectureReference(
        ArchitectureCertificationState state)
    {
        return new DesignStudioExecutionReadinessArchitectureCertificationReference(
            CertificationId: state.Certification?.CertificationId ?? "architectureCertification:unavailable",
            ReadinessReportId: state.ReadinessReport?.ReportId ?? "architectureReadiness:unavailable",
            SchemaVersion: state.Certification?.SchemaVersion ?? ArchitectureCertificationContract.SchemaVersionV1,
            Readiness: state.ReadinessReport?.Readiness ?? ArchitectureReadinessState.Incomplete,
            IsCertified: state.IsCertified);
    }

    private static DesignStudioExecutionReadinessTrustBoundary CreateTrustBoundary()
    {
        return new DesignStudioExecutionReadinessTrustBoundary(
            ExecutionAllowed: false,
            ProviderInvocationAllowed: false,
            MicrosoftSkillsExecutionAllowed: false,
            ApiInvocationAllowed: false,
            CliInvocationAllowed: false,
            DeploymentAllowed: false,
            AutomaticAnalyzerValidationAllowed: false,
            AutomaticAnalyzerLaunchAllowed: false);
    }

    private static DesignStudioExecutionReadinessStageItem Item(string label, string value)
    {
        return new DesignStudioExecutionReadinessStageItem(label, string.IsNullOrWhiteSpace(value) ? "Unavailable" : value);
    }

    private static DesignStudioExecutionReadinessWarningSummary Warning(string category, string severity, string message)
    {
        return new DesignStudioExecutionReadinessWarningSummary(category, severity, message);
    }

    private static DesignStudioExecutionReadinessLineageReference Reference(string stage, string referenceId, string schemaVersion)
    {
        return new DesignStudioExecutionReadinessLineageReference(stage, referenceId, schemaVersion);
    }
}
