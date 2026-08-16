using System.IO;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirPreviewSerializerSafetyGate
{
    private static readonly IReadOnlySet<string> DeployableFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "report.json",
        "definition.pbir",
        "model.bim",
    };

    private static readonly IReadOnlySet<string> DeployableExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pbir",
        ".bim",
        ".tmdl",
        ".pbip",
    };

    internal PbirPreviewSerializerSafetyGateResult Validate(
        PbirIntermediateRepresentationState irState,
        PbirSerializerRequest serializerRequest,
        PbirPreviewSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(serializerRequest);
        ArgumentNullException.ThrowIfNull(options);

        var reasons = new List<string>();
        var ir = irState.Ir;

        if (ir is null || !irState.Validation.IsValid || irState.Readiness != PbirIntermediateRepresentationReadinessState.ReadyForSerializer)
        {
            reasons.Add("complete PBIR IR must be provided.");
        }

        if (!string.Equals(serializerRequest.SchemaVersion, PbirSerializerRequestContract.SchemaVersionV1, StringComparison.Ordinal))
        {
            reasons.Add("serializer request schema version must be pbir-serializer-request/v1.");
        }

        if (ir is not null)
        {
            if (!string.Equals(serializerRequest.PbirIrRef, ir.Metadata.IrId, StringComparison.Ordinal))
            {
                reasons.Add("serializer request PBIR IR reference must match the IR id.");
            }

            if (!string.Equals(serializerRequest.PbirIrSchemaVersion, ir.Metadata.SchemaVersion, StringComparison.Ordinal))
            {
                reasons.Add("serializer request PBIR IR schema version must match the IR schema version.");
            }

            if (!string.Equals(serializerRequest.PbirIrContentHash, ir.Hashes.ContentHash, StringComparison.Ordinal))
            {
                reasons.Add("serializer request PBIR IR content hash must match the IR content hash.");
            }
        }

        if (serializerRequest.ProviderInvocationAllowed)
        {
            reasons.Add("provider invocation requests are not allowed.");
        }

        if (serializerRequest.DeploymentAllowed)
        {
            reasons.Add("deployment requests are not allowed.");
        }

        if (serializerRequest.MicrosoftSkillsExecutionAllowed)
        {
            reasons.Add("Microsoft Skills execution requests are not allowed.");
        }

        ValidateOptions(options, reasons);

        return new PbirPreviewSerializerSafetyGateResult(
            IsAllowed: reasons.Count == 0,
            Reasons: reasons
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray());
    }

    private static void ValidateOptions(PbirPreviewSerializerOptions options, ICollection<string> reasons)
    {
        if (!options.LocalOutputOnly)
        {
            reasons.Add("preview output must be local only.");
        }

        if (!IsLocalRelativePath(options.OutputRoot))
        {
            reasons.Add("preview output path must be a local relative path.");
        }

        if (options.OutputTypes.Count == 0)
        {
            reasons.Add("at least one preview output type is required.");
        }

        foreach (var outputType in options.OutputTypes)
        {
            if (!Enum.IsDefined(outputType))
            {
                reasons.Add("preview output type is unsupported.");
            }
        }

        if (options.DeployableOutputRequested)
        {
            reasons.Add("deployable output requests are not allowed.");
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

        if (options.MicrosoftSkillsExecutionRequested)
        {
            reasons.Add("Microsoft Skills execution requests are not allowed.");
        }

        foreach (var requestedFile in options.RequestedOutputFiles)
        {
            ValidateRequestedOutputFile(requestedFile, reasons);
        }
    }

    private static void ValidateRequestedOutputFile(string requestedFile, ICollection<string> reasons)
    {
        if (!IsLocalRelativePath(requestedFile))
        {
            reasons.Add("preview output path must be a local relative path.");
            return;
        }

        var fileName = Path.GetFileName(requestedFile);
        var extension = Path.GetExtension(requestedFile);
        if (DeployableFileNames.Contains(fileName) || DeployableExtensions.Contains(extension))
        {
            reasons.Add($"deployable PBIR file output is not allowed: {fileName}.");
        }

        if (requestedFile.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Any(segment => string.Equals(segment, "tmdl", StringComparison.OrdinalIgnoreCase)))
        {
            reasons.Add($"deployable PBIR file output is not allowed: {fileName}.");
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
