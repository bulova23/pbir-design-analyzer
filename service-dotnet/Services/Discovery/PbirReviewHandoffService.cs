using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirReviewHandoffService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly PbirReviewHandoffSafetyGate _safetyGate;

    internal PbirReviewHandoffService()
        : this(new PbirReviewHandoffSafetyGate())
    {
    }

    internal PbirReviewHandoffService(PbirReviewHandoffSafetyGate safetyGate)
    {
        _safetyGate = safetyGate;
    }

    internal PbirReviewHandoffState CreateReviewHandoff(
        PbirPreviewPackage previewPackage,
        GenerationManifestState generationManifestState,
        PbirReviewHandoffRequest request,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(previewPackage);
        ArgumentNullException.ThrowIfNull(generationManifestState);
        ArgumentNullException.ThrowIfNull(request);

        var safety = _safetyGate.Validate(previewPackage, generationManifestState, request);
        if (!safety.IsAllowed || generationManifestState.Manifest is null)
        {
            return new PbirReviewHandoffState(
                Handoff: null,
                Safety: safety,
                Diagnostics: new PbirReviewHandoffDiagnostics(
                    SafetyRejections: safety.Reasons,
                    BoundaryViolations: safety.Reasons),
                Readiness: PbirReviewHandoffReadinessState.Blocked);
        }

        var manifest = generationManifestState.Manifest;
        var readiness = ClassifyReadiness(previewPackage, manifest, request);
        var warnings = CreateWarnings(previewPackage, readiness);
        var lineage = new PbirReviewHandoffLineage(
            PreviewPackageRef: previewPackage.Metadata.PackageId,
            DesignPackageRef: manifest.SourceReferences.DesignPackageRef,
            GenerationManifestRef: manifest.Metadata.ManifestId,
            PbirIrRef: previewPackage.Lineage.PbirIrRef,
            SourceWriteManifestRef: previewPackage.Lineage.SourceWriteManifestRef,
            ImmutableLineage: previewPackage.Lineage.ImmutableLineage
                .Concat(manifest.Lineage.ImmutableUpstreamLineage)
                .Append(previewPackage.Metadata.PackageId)
                .Append(request.HandoffId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray());
        var inputHash = ComputeSha256(Serialize(new
        {
            previewPackage,
            manifest,
            request
        }));
        var handoffWithoutHash = new PbirReviewHandoff(
            HandoffId: request.HandoffId,
            SchemaVersion: PbirReviewHandoffContract.SchemaVersionV1,
            GeneratedUtc: generatedUtc.UtcDateTime,
            PreviewPackageReference: new PbirReviewHandoffPreviewPackageReference(
                PackageId: previewPackage.Metadata.PackageId,
                SchemaVersion: previewPackage.SchemaVersion,
                PackageHash: previewPackage.Hashes.PackageHash),
            DesignPackageReference: new PbirReviewHandoffDesignPackageReference(
                DesignPackageRef: manifest.SourceReferences.DesignPackageRef),
            GenerationManifestReference: new PbirReviewHandoffGenerationManifestReference(
                ManifestId: manifest.Metadata.ManifestId,
                SchemaVersion: manifest.Metadata.SchemaVersion),
            PbirIrReference: new PbirReviewHandoffPbirIrReference(
                IrId: previewPackage.Lineage.PbirIrRef,
                SchemaVersion: previewPackage.Lineage.PbirIrSchemaVersion,
                ContentHash: previewPackage.Lineage.PbirIrContentHash),
            ReviewTarget: new PbirReviewTargetDescriptor(
                Target: request.ReviewTarget,
                ReviewOnly: true),
            ReviewReadiness: readiness,
            RequiredReviewerAction: request.RequiredReviewerAction,
            DesignStudioApprovalContext: manifest.ApprovalSummary.DesignApproval,
            AnalyzerWorkspaceBoundary: new PbirAnalyzerWorkspaceBoundary(
                ValidationOccurred: false,
                AutomaticValidationRequested: false,
                AutomaticValidationAllowed: false,
                WorkspaceLaunchRequested: false,
                ValidationStatus: "No Analyzer Workspace validation has occurred."),
            DeploymentBoundary: new PbirDeploymentBoundary(
                DeploymentRequested: false,
                DeploymentAllowed: false),
            Warnings: warnings,
            Lineage: lineage,
            Hashes: new PbirReviewHandoffHashes(
                InputHash: inputHash,
                HandoffHash: string.Empty));
        var handoff = handoffWithoutHash with
        {
            Hashes = handoffWithoutHash.Hashes with
            {
                HandoffHash = ComputeHandoffHash(handoffWithoutHash)
            }
        };

        return new PbirReviewHandoffState(
            Handoff: handoff,
            Safety: safety,
            Diagnostics: PbirReviewHandoffDiagnostics.Empty,
            Readiness: readiness);
    }

    private static PbirReviewHandoffReadinessState ClassifyReadiness(
        PbirPreviewPackage previewPackage,
        GenerationManifest manifest,
        PbirReviewHandoffRequest request)
    {
        if (previewPackage.FileInventory.Count == 0 ||
            previewPackage.HashInventory.Entries.Count == 0 ||
            !manifest.ApprovalSummary.DesignApproval.DesignApproved)
        {
            return PbirReviewHandoffReadinessState.Incomplete;
        }

        return request.ReviewTarget == PbirReviewTarget.AnalyzerWorkspace
            ? PbirReviewHandoffReadinessState.ReadyForAnalyzerReview
            : PbirReviewHandoffReadinessState.ReadyForDesignReview;
    }

    private static IReadOnlyList<string> CreateWarnings(
        PbirPreviewPackage previewPackage,
        PbirReviewHandoffReadinessState readiness)
    {
        var warnings = previewPackage.Warnings
            .Append("Review handoff is advisory and review-only.")
            .Append("No Analyzer Workspace validation has occurred.")
            .Append("No deployment, provider invocation, Microsoft API invocation, CLI invocation, or Microsoft Skills execution was performed.");

        if (readiness == PbirReviewHandoffReadinessState.ReadyForAnalyzerReview)
        {
            warnings = warnings.Append("readyForAnalyzerReview does not mean Analyzer validation occurred.");
        }

        if (readiness == PbirReviewHandoffReadinessState.ReadyForDesignReview)
        {
            warnings = warnings.Append("readyForDesignReview means a human can review preview outputs; it does not mean validation occurred.");
        }

        return warnings
            .Distinct(StringComparer.Ordinal)
            .OrderBy(warning => warning, StringComparer.Ordinal)
            .ToArray();
    }

    private static string ComputeHandoffHash(PbirReviewHandoff handoff)
    {
        return ComputeSha256(Serialize(new
        {
            handoff.HandoffId,
            handoff.SchemaVersion,
            handoff.GeneratedUtc,
            handoff.PreviewPackageReference,
            handoff.DesignPackageReference,
            handoff.GenerationManifestReference,
            handoff.PbirIrReference,
            handoff.ReviewTarget,
            handoff.ReviewReadiness,
            handoff.RequiredReviewerAction,
            handoff.DesignStudioApprovalContext,
            handoff.AnalyzerWorkspaceBoundary,
            handoff.DeploymentBoundary,
            handoff.Warnings,
            handoff.Lineage,
            handoff.Hashes.InputHash
        }));
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
