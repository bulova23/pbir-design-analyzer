using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirReviewHandoffSafetyGate
{
    internal PbirReviewHandoffSafetyGateResult Validate(
        PbirPreviewPackage previewPackage,
        GenerationManifestState generationManifestState,
        PbirReviewHandoffRequest request)
    {
        ArgumentNullException.ThrowIfNull(previewPackage);
        ArgumentNullException.ThrowIfNull(generationManifestState);
        ArgumentNullException.ThrowIfNull(request);

        var reasons = new List<string>();
        var manifest = generationManifestState.Manifest;
        if (manifest is null)
        {
            reasons.Add("generation manifest approval context is required.");
        }

        ValidatePreviewPackage(previewPackage, reasons);
        ValidateGenerationManifest(previewPackage, manifest, reasons);
        ValidateRequest(request, reasons);

        return new PbirReviewHandoffSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            Reasons: reasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray());
    }

    private static void ValidatePreviewPackage(PbirPreviewPackage previewPackage, ICollection<string> reasons)
    {
        if (!string.Equals(previewPackage.SchemaVersion, PbirPreviewPackageContract.SchemaVersionV1, StringComparison.Ordinal) ||
            !string.Equals(previewPackage.PackageDescriptor.SchemaVersion, PbirPreviewPackageContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("preview package schema version must be pbir-preview-package/v1.");
        }

        if (!previewPackage.PackageDescriptor.MetadataOnly ||
            !previewPackage.PackageDescriptor.LocalOnly ||
            previewPackage.PackageDescriptor.ContainsPhysicalFileContent ||
            previewPackage.PackageDescriptor.ZipCreated ||
            previewPackage.PackageDescriptor.DeployableArtifactsAllowed)
        {
            reasons.Add("preview package must be metadata-only, local-only, and non-deployable.");
        }

        if (previewPackage.FileInventory.Count == 0)
        {
            reasons.Add("preview package file inventory is required.");
        }

        foreach (var file in previewPackage.FileInventory)
        {
            var forbiddenName = PbirPreviewPackageService.GetForbiddenArtifactName(file.ArtifactType) ??
                PbirPreviewPackageService.GetForbiddenPathName(file.RelativePath) ??
                PbirPreviewPackageService.GetForbiddenPathName(file.IntendedPath);
            if (forbiddenName is not null)
            {
                reasons.Add($"preview package references forbidden deployable artifacts: {PbirPreviewPackageService.FormatForbiddenArtifactName(forbiddenName)}.");
            }

            if (!PbirPreviewPackageService.IsHash(file.HashSha256))
            {
                reasons.Add("preview package file inventory must include complete SHA-256 hashes.");
            }
        }

        foreach (var rejectedArtifact in previewPackage.RejectedArtifacts)
        {
            var forbiddenName = PbirPreviewPackageService.GetForbiddenPathName(rejectedArtifact.RelativePath) ??
                PbirPreviewPackageService.GetForbiddenPathName(rejectedArtifact.IntendedPath);
            if (forbiddenName is not null)
            {
                reasons.Add($"preview package references forbidden deployable artifacts: {PbirPreviewPackageService.FormatForbiddenArtifactName(forbiddenName)}.");
            }
        }

        if (previewPackage.HashInventory.Entries.Count == 0 ||
            previewPackage.HashInventory.Entries.Any(entry => !PbirPreviewPackageService.IsHash(entry.HashSha256)))
        {
            reasons.Add("preview package hash inventory must include complete SHA-256 hashes.");
        }

        if (!PbirPreviewPackageService.IsHash(previewPackage.Hashes.InputHash) ||
            !PbirPreviewPackageService.IsHash(previewPackage.Hashes.InventoryHash) ||
            !PbirPreviewPackageService.IsHash(previewPackage.Hashes.PackageHash))
        {
            reasons.Add("preview package hashes must include complete SHA-256 hashes.");
        }

        if (string.IsNullOrWhiteSpace(previewPackage.Lineage.SourceWriteManifestRef) ||
            string.IsNullOrWhiteSpace(previewPackage.Lineage.GenerationManifestRef) ||
            string.IsNullOrWhiteSpace(previewPackage.Lineage.PbirIrRef) ||
            string.IsNullOrWhiteSpace(previewPackage.Lineage.PreviewManifestRef) ||
            previewPackage.Lineage.ImmutableLineage.Count == 0)
        {
            reasons.Add("preview package lineage must be complete.");
        }

        if (previewPackage.RollbackPlanReference.ActionCount <= 0 ||
            !PbirPreviewPackageService.IsHash(previewPackage.RollbackPlanReference.RollbackPlanHash))
        {
            reasons.Add("preview package rollback metadata reference is required.");
        }
    }

    private static void ValidateGenerationManifest(
        PbirPreviewPackage previewPackage,
        GenerationManifest? manifest,
        ICollection<string> reasons)
    {
        if (manifest is null)
        {
            return;
        }

        if (!string.Equals(manifest.Metadata.SchemaVersion, GenerationManifestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("generation manifest schema version must be generation-manifest/v1.");
        }

        if (!string.Equals(previewPackage.Lineage.GenerationManifestRef, manifest.Metadata.ManifestId, StringComparison.Ordinal))
        {
            reasons.Add("preview package lineage must match the generation manifest.");
        }

        if (string.IsNullOrWhiteSpace(manifest.SourceReferences.DesignPackageRef) ||
            manifest.ApprovalSummary.DesignApproval is null)
        {
            reasons.Add("Design Studio approval context is required.");
        }

        if (!manifest.ExecutionConstraints.DryRunOnly ||
            manifest.ExecutionConstraints.DeploymentAllowed ||
            manifest.ExecutionConstraints.ProviderInvocationAllowed ||
            manifest.ExecutionConstraints.ApiInvocationAllowed ||
            manifest.ExecutionConstraints.CliInvocationAllowed)
        {
            reasons.Add("generation manifest execution constraints must remain dry-run and non-invoking.");
        }
    }

    private static void ValidateRequest(PbirReviewHandoffRequest request, ICollection<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(request.HandoffId))
        {
            reasons.Add("handoff id is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RequiredReviewerAction))
        {
            reasons.Add("required reviewer action is required.");
        }

        if (request.AutomaticAnalyzerValidationRequested || request.WorkspaceLaunchRequested)
        {
            reasons.Add("automatic Analyzer Workspace validation requests are not allowed.");
        }

        if (request.DeploymentRequested)
        {
            reasons.Add("deployment requests are not allowed.");
        }
    }
}
