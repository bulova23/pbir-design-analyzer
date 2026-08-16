using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableMaterializationSafetyGate
{
    private readonly PbirDeployableSerializerValidator _serializerValidator;
    private readonly PbirDeployableMaterializationSchemaValidator _schemaValidator;

    internal PbirDeployableMaterializationSafetyGate()
        : this(new PbirDeployableSerializerValidator(), new PbirDeployableMaterializationSchemaValidator())
    {
    }

    internal PbirDeployableMaterializationSafetyGate(
        PbirDeployableSerializerValidator serializerValidator,
        PbirDeployableMaterializationSchemaValidator schemaValidator)
    {
        _serializerValidator = serializerValidator;
        _schemaValidator = schemaValidator;
    }

    internal IReadOnlyList<PbirDeployableMaterializationDiagnostic> ValidatePreview(
        PbirDeployableArtifact artifact,
        PbirDeployableManifest manifest,
        PbirDeployableMaterializationPreviewRequest request)
    {
        var diagnostics = new List<PbirDeployableMaterializationDiagnostic>();
        if (request.SchemaVersion != PbirDeployableMaterializationPreviewRequestContract.SchemaVersionV1 ||
            request.RequestedOperation != "preview")
        {
            diagnostics.Add(new("PBIRMAT-CONTRACT-001", "request", "Preview contract or operation is unsupported."));
        }
        if (request.ExecutionPolicy.FilesystemMutationAllowed || request.ExecutionPolicy.HasExternalAuthority)
        {
            diagnostics.Add(new("PBIRMAT-BOUNDARY-001", "executionPolicy", "Preview must be read-only and carries no external authority."));
        }
        if (request.ArtifactRef != artifact.ArtifactId || request.ArtifactHash != artifact.Hashes.ArtifactHash ||
            request.ManifestRef != manifest.ManifestId || request.ManifestHash != manifest.Hashes.ManifestHash)
        {
            diagnostics.Add(new("PBIRMAT-REFERENCE-001", "request", "Phase 29 artifact or manifest reference does not match."));
        }

        var validation = _serializerValidator.ValidateOutput(artifact, manifest);
        if (!validation.IsValid)
        {
            diagnostics.Add(new("PBIRMAT-PHASE29-001", "artifact", "Phase 29 artifact and manifest failed postflight validation."));
        }
        diagnostics.AddRange(_schemaValidator.Validate(artifact));
        return Order(diagnostics);
    }

    internal static IReadOnlyList<PbirDeployableMaterializationDiagnostic> Order(IEnumerable<PbirDeployableMaterializationDiagnostic> diagnostics) => diagnostics
        .OrderBy(value => value.Code, StringComparer.Ordinal)
        .ThenBy(value => value.Path, StringComparer.Ordinal)
        .ThenBy(value => value.Message, StringComparer.Ordinal)
        .ToArray();
}
