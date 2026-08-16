using System.IO;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirLocalArtifactWriterSafetyGate
{
    private static readonly IReadOnlyDictionary<PbirLocalWriteArtifactType, string> ForbiddenArtifactReasons =
        new Dictionary<PbirLocalWriteArtifactType, string>
        {
            [PbirLocalWriteArtifactType.ReportJson] = "reportJson",
            [PbirLocalWriteArtifactType.DefinitionPbir] = "definitionPbir",
            [PbirLocalWriteArtifactType.ModelBim] = "modelBim",
            [PbirLocalWriteArtifactType.Tmdl] = "tmdl",
            [PbirLocalWriteArtifactType.PbipProject] = "pbipProject",
            [PbirLocalWriteArtifactType.DeployableReport] = "deployableReport",
        };

    private static readonly IReadOnlySet<PbirLocalWriteArtifactType> AllowedArtifactTypes =
        new HashSet<PbirLocalWriteArtifactType>
        {
            PbirLocalWriteArtifactType.PreviewMarkdown,
            PbirLocalWriteArtifactType.PreviewJson,
            PbirLocalWriteArtifactType.IrJson,
            PbirLocalWriteArtifactType.ManifestJson,
            PbirLocalWriteArtifactType.DiagnosticsMarkdown,
        };

    internal PbirLocalArtifactWriterSafetyGateResult Validate(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentationState irState,
        PbirLocalWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(previewManifest);
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(request);

        var reasons = new List<string>();
        var ir = irState.Ir;

        if (!string.Equals(request.SchemaVersion, PbirLocalWriteRequestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("write request schema version must be pbir-local-write-request/v1.");
        }

        if (ir is null || !irState.Validation.IsValid || irState.Readiness != PbirIntermediateRepresentationReadinessState.ReadyForSerializer)
        {
            reasons.Add("complete PBIR IR must be provided.");
        }

        if (!string.Equals(previewManifest.SchemaVersion, PbirPreviewManifestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("source preview manifest schema version must be pbir-preview-manifest/v1.");
        }

        ValidateSourceReferences(previewManifest, ir, request, reasons);
        ValidateRequest(request, reasons);

        return new PbirLocalArtifactWriterSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            Reasons: reasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray());
    }

    internal static bool IsAllowedArtifactType(PbirLocalWriteArtifactType artifactType)
    {
        return AllowedArtifactTypes.Contains(artifactType);
    }

    internal static string? GetForbiddenArtifactName(PbirLocalWriteArtifactType artifactType)
    {
        return ForbiddenArtifactReasons.TryGetValue(artifactType, out var name)
            ? name
            : null;
    }

    private static void ValidateSourceReferences(
        PbirPreviewManifest previewManifest,
        PbirIntermediateRepresentation? ir,
        PbirLocalWriteRequest request,
        ICollection<string> reasons)
    {
        if (ir is not null)
        {
            if (!string.Equals(request.SourceIrRef, ir.Metadata.IrId, StringComparison.Ordinal))
            {
                reasons.Add("write request PBIR IR reference must match the IR id.");
            }

            if (!string.Equals(request.SourceIrSchemaVersion, ir.Metadata.SchemaVersion, StringComparison.Ordinal))
            {
                reasons.Add("write request PBIR IR schema version must match the IR schema version.");
            }

            if (!string.Equals(request.SourceIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal))
            {
                reasons.Add("write request PBIR IR content hash must match the IR content hash.");
            }

            if (!string.Equals(previewManifest.SourceReferences.PbirIrRef, ir.Metadata.IrId, StringComparison.Ordinal) ||
                !string.Equals(previewManifest.SourceReferences.PbirIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal))
            {
                reasons.Add("source preview manifest must reference the supplied PBIR IR.");
            }
        }

        if (!string.Equals(request.SourcePreviewManifestRef, previewManifest.Metadata.ManifestId, StringComparison.Ordinal))
        {
            reasons.Add("write request preview manifest reference must match the preview manifest id.");
        }

        if (!string.Equals(request.SourcePreviewManifestSchemaVersion, previewManifest.SchemaVersion, StringComparison.Ordinal))
        {
            reasons.Add("write request preview manifest schema version must match the preview manifest schema version.");
        }

        if (!string.Equals(request.SourcePreviewManifestHash, previewManifest.Hashes.ManifestHash, StringComparison.Ordinal))
        {
            reasons.Add("write request preview manifest hash must match the preview manifest hash.");
        }
    }

    private static void ValidateRequest(PbirLocalWriteRequest request, ICollection<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId))
        {
            reasons.Add("write request id is required.");
        }

        if (!IsLocalRelativePath(request.TargetOutputRoot))
        {
            reasons.Add("target output root must be a local relative path.");
        }

        if (request.DryRun != true)
        {
            reasons.Add("dry-run flag must be present and true.");
        }

        if (request.RequestedArtifactTypes.Count == 0)
        {
            reasons.Add("at least one local write artifact type is required.");
        }

        foreach (var artifactType in request.RequestedArtifactTypes)
        {
            if (!Enum.IsDefined(artifactType))
            {
                reasons.Add("local write artifact type is unsupported.");
                continue;
            }

            var forbiddenName = GetForbiddenArtifactName(artifactType);
            if (forbiddenName is not null)
            {
                reasons.Add($"deployable PBIR artifact requests are not allowed: {forbiddenName}.");
                continue;
            }

            if (!IsAllowedArtifactType(artifactType))
            {
                reasons.Add("local write artifact type is unsupported.");
            }
        }

        if (!Enum.IsDefined(request.OverwritePolicy) ||
            request.OverwritePolicy == PbirLocalOverwritePolicy.OverwriteExisting)
        {
            reasons.Add("overwrite policy must not allow replacing existing files.");
        }

        if (!Enum.IsDefined(request.RollbackPolicy) ||
            request.RollbackPolicy == PbirLocalRollbackPolicy.None)
        {
            reasons.Add("rollback policy must produce a local rollback plan.");
        }

        if (request.DeploymentRequested)
        {
            reasons.Add("deployment requests are not allowed.");
        }

        if (request.ProviderInvocationRequested)
        {
            reasons.Add("provider invocation requests are not allowed.");
        }

        if (request.MicrosoftApiRequested)
        {
            reasons.Add("Microsoft API requests are not allowed.");
        }

        if (request.CliRequested)
        {
            reasons.Add("CLI requests are not allowed.");
        }

        if (request.MicrosoftSkillsExecutionRequested)
        {
            reasons.Add("Microsoft Skills execution requests are not allowed.");
        }
    }

    private static bool IsLocalRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            Path.IsPathRooted(path) ||
            path.StartsWith("~", StringComparison.Ordinal) ||
            path.Contains("://", StringComparison.Ordinal))
        {
            return false;
        }

        return !path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => string.Equals(segment, "..", StringComparison.Ordinal));
    }
}
