using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ReferenceGenerationSafetyGate
{
    internal ReferenceGenerationSafetyGateResult Validate(
        GenerationManifestState manifestState,
        ArchitectureCertificationState certificationState,
        PbirGenerationSpecificationState specificationState,
        ReferenceGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(manifestState);
        ArgumentNullException.ThrowIfNull(certificationState);
        ArgumentNullException.ThrowIfNull(specificationState);
        ArgumentNullException.ThrowIfNull(options);

        var reasons = new List<string>();
        var manifest = manifestState.Manifest;
        var specification = specificationState.Specification;

        if (!certificationState.IsCertified ||
            certificationState.Certification is null ||
            certificationState.ReadinessReport is null ||
            !string.Equals(certificationState.Certification.SchemaVersion, ArchitectureCertificationContract.SchemaVersionV1, StringComparison.Ordinal) ||
            !string.Equals(certificationState.ReadinessReport.SchemaVersion, ArchitectureReadinessReportContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("architecture certification must exist and be readyForExecutionImplementation.");
        }

        if (manifest is null)
        {
            reasons.Add("generation manifest must exist.");
        }
        else
        {
            if (!string.Equals(manifest.Metadata.SchemaVersion, GenerationManifestContract.SchemaVersionV1, StringComparison.Ordinal))
            {
                reasons.Add("generation manifest schema version must be generation-manifest/v1.");
            }

            if (!manifestState.Validation.IsValid)
            {
                reasons.Add("generation manifest validation must be valid.");
            }

            if (manifestState.Readiness != GenerationManifestReadinessState.ReadyForGenerator)
            {
                reasons.Add("generation manifest must be readyForGenerator.");
            }

            ValidateManifestConstraints(manifest, reasons);
        }

        if (specification is null)
        {
            reasons.Add("PBIR generation specification must exist.");
        }
        else
        {
            if (specificationState.Readiness != PbirGenerationSpecificationReadinessState.ReadyForGenerationProvider ||
                !specificationState.AcceptsGenerationProvider)
            {
                reasons.Add("PBIR generation specification must be readyForGenerationProvider.");
            }

            var specificationValidation = new PbirGenerationSpecificationValidator().Validate(specification);
            if (!specificationValidation.IsValid)
            {
                reasons.Add("PBIR generation specification validation must be valid.");
            }

            if (manifest is not null &&
                !string.Equals(
                    manifest.SourceReferences.PbirGenerationSpecificationRef,
                    specification.SpecificationId,
                    StringComparison.Ordinal))
            {
                reasons.Add("PBIR generation specification reference must match the generation manifest.");
            }
        }

        ValidateOptions(options, reasons);

        return new ReferenceGenerationSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            Reasons: reasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray());
    }

    private static void ValidateManifestConstraints(GenerationManifest manifest, ICollection<string> reasons)
    {
        if (manifest.ExecutionConstraints is null || !manifest.ExecutionConstraints.DryRunOnly)
        {
            reasons.Add("dry-run generation is required.");
        }

        if (manifest.ExecutionConstraints?.DeploymentAllowed == true)
        {
            reasons.Add("deployment must be disabled.");
        }

        if (manifest.ExecutionConstraints?.ProviderInvocationAllowed == true)
        {
            reasons.Add("provider invocation requests are not allowed.");
        }

        if (manifest.ExecutionConstraints?.ApiInvocationAllowed == true)
        {
            reasons.Add("Microsoft API requests are not allowed.");
        }

        if (manifest.ExecutionConstraints?.CliInvocationAllowed == true)
        {
            reasons.Add("CLI requests are not allowed.");
        }
    }

    private static void ValidateOptions(ReferenceGenerationOptions options, ICollection<string> reasons)
    {
        if (!options.DryRun)
        {
            reasons.Add("dry-run generation is required.");
        }

        if (!options.LocalOutputOnly)
        {
            reasons.Add("reference generation output must be local only.");
        }

        if (options.DeploymentRequested)
        {
            reasons.Add("deployment requests are not allowed.");
        }

        if (options.ProviderInvocationRequested)
        {
            reasons.Add("provider invocation requests are not allowed.");
        }

        if (options.MicrosoftApiRequested)
        {
            reasons.Add("Microsoft API requests are not allowed.");
        }

        if (options.CliRequested)
        {
            reasons.Add("CLI requests are not allowed.");
        }

        if (options.NetworkAccessRequested)
        {
            reasons.Add("network access requests are not allowed.");
        }
    }
}
