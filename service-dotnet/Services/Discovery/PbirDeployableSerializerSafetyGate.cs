using System.Text;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirDeployableSerializerSafetyGate
{
    private readonly PbirDeployableSerializerCanonicalJson _canonicalJson;

    internal PbirDeployableSerializerSafetyGate()
        : this(new PbirDeployableSerializerCanonicalJson())
    {
    }

    internal PbirDeployableSerializerSafetyGate(
        PbirDeployableSerializerCanonicalJson canonicalJson)
    {
        _canonicalJson = canonicalJson;
    }

    internal PbirDeployableSerializerSafetyGateResult Validate(
        PbirIntermediateRepresentationState irState,
        PbirSerializerRequest serializerRequest,
        PbirDeployableSerializerRequest request)
    {
        ArgumentNullException.ThrowIfNull(irState);
        ArgumentNullException.ThrowIfNull(serializerRequest);
        ArgumentNullException.ThrowIfNull(request);

        var missing = new List<PbirDeployableDiagnostic>();
        var unsupportedSchemas = new List<PbirDeployableDiagnostic>();
        var invalidPaths = new List<PbirDeployableDiagnostic>();
        var duplicateIdentities = new List<PbirDeployableDiagnostic>();
        var hashViolations = new List<PbirDeployableDiagnostic>();
        var boundaryViolations = new List<PbirDeployableDiagnostic>();
        var nestedContractsComplete =
            request.DatasetReference?.ByPath is not null &&
            request.SemanticModelInventory?.Entries is not null &&
            request.VisualBindings is not null &&
            request.ExecutionPolicy is not null;

        if (!nestedContractsComplete)
        {
            missing.Add(Diagnostic(
                "PBIRDEPLOY-REQUIRED-004",
                "request",
                "Dataset reference, semantic inventory entries, visual bindings, and execution policy are required."));
        }

        if (irState.Ir is null)
        {
            missing.Add(Diagnostic("PBIRDEPLOY-REQUIRED-001", "pbirIr", "Complete canonical PBIR IR is required."));
        }
        else if (!irState.Validation.IsValid ||
                 irState.Readiness != PbirIntermediateRepresentationReadinessState.ReadyForSerializer)
        {
            missing.Add(Diagnostic(
                "PBIRDEPLOY-REQUIRED-002",
                "pbirIr",
                "PBIR IR must be valid and ready for the serializer."));
        }

        if (!IsNfcNonempty(request.RequestId) ||
            !IsNfcNonempty(request.SerializerRequestRef) ||
            !IsNfcNonempty(request.PbirIrRef) ||
            !IsNfcNonempty(request.PbirIrContentHash) ||
            !IsNfcNonempty(request.SemanticModelInventoryRef) ||
            !IsNfcNonempty(request.SemanticModelInventoryContentHash))
        {
            missing.Add(Diagnostic(
                "PBIRDEPLOY-REQUIRED-003",
                "request",
                "Request identities, references, and hashes are required NFC strings."));
        }

        var ir = irState.Ir;
        if (request.SchemaVersion != PbirDeployableSerializerRequestContract.SchemaVersionV1 ||
            request.SerializerRequestSchemaVersion != PbirSerializerRequestContract.SchemaVersionV1 ||
            request.PbirIrSchemaVersion != PbirIntermediateRepresentationContract.SchemaVersionV1 ||
            request.DefinitionPropertiesSchemaVersion != PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion ||
            request.DefinitionSchemaVersion != PbirDeployableSchemaLock.DefinitionSchemaVersion)
        {
            unsupportedSchemas.Add(Diagnostic(
                "PBIRDEPLOY-SCHEMA-001",
                "request",
                "Request and schema versions must match the locked Phase 29 contracts."));
        }

        if (ir is not null &&
            (request.PbirIrRef != ir.Metadata.IrId ||
             request.PbirIrContentHash != ir.Hashes.ContentHash ||
             serializerRequest.PbirIrRef != ir.Metadata.IrId ||
             serializerRequest.PbirIrContentHash != ir.Hashes.ContentHash ||
             request.SerializerRequestRef != serializerRequest.RequestId))
        {
            hashViolations.Add(Diagnostic(
                "PBIRDEPLOY-HASH-001",
                "request",
                "Serializer request and deployable request must reference the exact canonical IR."));
        }

        if (ir is not null &&
            !string.Equals(
                PbirIntermediateRepresentationIntegrity.ComputeContentHash(ir),
                ir.Hashes.ContentHash,
                StringComparison.Ordinal))
        {
            hashViolations.Add(Diagnostic(
                "PBIRDEPLOY-HASH-004",
                "pbirIr.hashes.contentHash",
                "PBIR IR content hash must match the current canonical IR content."));
        }

        if (request.TargetFormat != "modernPbir" ||
            request.LayoutProfileId != "modern-grid-1280x720/v1")
        {
            boundaryViolations.Add(Diagnostic(
                "PBIRDEPLOY-BOUNDARY-002",
                "request",
                "Only modernPbir with modern-grid-1280x720/v1 is supported."));
        }

        if (request.DatasetReference?.ByPath is not null &&
            !IsSafeRelativePath(request.DatasetReference.ByPath.Path))
        {
            invalidPaths.Add(Diagnostic(
                "PBIRDEPLOY-PATH-001",
                "datasetReference.byPath.path",
                "Dataset path must be a normalized safe relative path."));
        }

        if (!serializerRequest.SerializerImplementationAvailable ||
            serializerRequest.ProviderInvocationAllowed ||
            serializerRequest.DeploymentAllowed ||
            serializerRequest.MicrosoftSkillsExecutionAllowed ||
            request.ExecutionPolicy?.HasAuthority == true)
        {
            boundaryViolations.Add(Diagnostic(
                "PBIRDEPLOY-BOUNDARY-001",
                "executionPolicy",
                "Phase 29 carries no filesystem, provider, Skills, API, CLI, deployment, Desktop, or Analyzer authority."));
        }

        if (nestedContractsComplete)
        {
            ValidateInventory(request, duplicateIdentities, hashViolations);
        }

        var diagnostics = new PbirDeployableDiagnostics(
            SchemaVersion: PbirDeployableDiagnosticsContract.SchemaVersionV1,
            MissingRequiredFields: Order(missing),
            UnsupportedSchemaVersions: Order(unsupportedSchemas),
            UnsupportedVisualTypes: [],
            IncompleteSemanticBindings: [],
            InvalidModelReferences: [],
            InvalidPaths: Order(invalidPaths),
            DuplicateIdentities: Order(duplicateIdentities),
            InvalidLayoutDefinitions: [],
            InvalidNavigationDefinitions: [],
            SchemaIncompatibilities: [],
            HashViolations: Order(hashViolations),
            LineageViolations: [],
            BoundaryViolations: Order(boundaryViolations),
            Warnings: [],
            UnsupportedSections: []);
        var isValid = !diagnostics.HasFailures;

        return new PbirDeployableSerializerSafetyGateResult(
            IsValid: isValid,
            Readiness: isValid
                ? PbirDeployableSerializerReadinessState.ReadyForSerialization
                : missing.Count > 0 || unsupportedSchemas.Count > 0
                    ? PbirDeployableSerializerReadinessState.Incomplete
                    : PbirDeployableSerializerReadinessState.Blocked,
            Diagnostics: diagnostics);
    }

    private void ValidateInventory(
        PbirDeployableSerializerRequest request,
        List<PbirDeployableDiagnostic> duplicateIdentities,
        List<PbirDeployableDiagnostic> hashViolations)
    {
        var inventory = request.SemanticModelInventory;
        var duplicate =
            inventory.Entries.Count == 0 ||
            inventory.Entries.Select(entry => entry.EntryId).Distinct(StringComparer.Ordinal).Count() != inventory.Entries.Count ||
            inventory.Entries.Select(entry => entry.Token).Distinct(StringComparer.Ordinal).Count() != inventory.Entries.Count ||
            inventory.Entries
                .Select(entry => $"{entry.Entity}\u001f{entry.Property}\u001f{entry.Kind}")
                .Distinct(StringComparer.Ordinal)
                .Count() != inventory.Entries.Count;

        if (duplicate)
        {
            duplicateIdentities.Add(Diagnostic(
                "PBIRDEPLOY-IDENTITY-001",
                "semanticModelInventory.entries",
                "Semantic inventory entries must have unique identities, tokens, and entity/property/kind tuples."));
            return;
        }

        if (inventory.SchemaVersion != PbirSemanticModelInventoryContract.SchemaVersionV1 ||
            inventory.InventoryRef != request.SemanticModelInventoryRef ||
            inventory.Entries.Any(entry =>
                !IsNfcNonempty(entry.EntryId) ||
                !IsNfcNonempty(entry.Token) ||
                !IsNfcNonempty(entry.Entity) ||
                !IsNfcNonempty(entry.Property)))
        {
            hashViolations.Add(Diagnostic(
                "PBIRDEPLOY-HASH-002",
                "semanticModelInventory",
                "Semantic inventory must use the locked schema, reference, and NFC string contract."));
            return;
        }

        var actualHash = _canonicalJson.ComputeSha256(
            _canonicalJson.SerializeSemanticModelInventory(inventory));
        if (!string.Equals(actualHash, request.SemanticModelInventoryContentHash, StringComparison.Ordinal))
        {
            hashViolations.Add(Diagnostic(
                "PBIRDEPLOY-HASH-003",
                "semanticModelInventoryContentHash",
                "Semantic inventory content hash does not match canonical inventory bytes."));
        }
    }

    private static bool IsSafeRelativePath(string path)
    {
        if (!IsNfcNonempty(path) ||
            Path.IsPathRooted(path) ||
            path.Contains('\\', StringComparison.Ordinal) ||
            path.Contains("://", StringComparison.Ordinal) ||
            path.Any(char.IsControl))
        {
            return false;
        }

        var segments = path.Split('/');
        return segments.All(segment =>
            segment.Length > 0 &&
            segment != "." &&
            segment != ".." &&
            !segment.Contains(':', StringComparison.Ordinal));
    }

    private static bool IsNfcNonempty(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.IsNormalized(NormalizationForm.FormC);
    }

    private static PbirDeployableDiagnostic Diagnostic(string code, string path, string message)
    {
        return new PbirDeployableDiagnostic(code, path, message);
    }

    private static IReadOnlyList<PbirDeployableDiagnostic> Order(
        IEnumerable<PbirDeployableDiagnostic> diagnostics)
    {
        return diagnostics
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToArray();
    }
}
