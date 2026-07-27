using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirDeployableSchemaLock
{
    internal const string DefinitionPropertiesSchemaVersion = "2.0.0";
    internal const string DefinitionSchemaVersion = "1.0.0";
    internal const string PbirFileFormatVersion = "4.0";
    internal const string ReportDefinitionVersion = "1.0.0";

    internal const string DefinitionPropertiesSchemaUrl =
        "https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json";
    internal const string VersionMetadataSchemaUrl =
        "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json";
    internal const string ReportSchemaUrl =
        "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json";
    internal const string PagesMetadataSchemaUrl =
        "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json";
    internal const string PageSchemaUrl =
        "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.0.0/schema.json";
    internal const string VisualContainerSchemaUrl =
        "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json";
}

internal static class PbirDeployableSerializerRequestContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-serializer-request/v1";
}

internal static class PbirSemanticModelInventoryContract
{
    internal const string SchemaVersionV1 = "pbir-semantic-model-inventory/v1";
}

internal static class PbirDeployableArtifactContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-artifact/v1";
}

internal static class PbirDeployableManifestContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-manifest/v1";
}

internal static class PbirDeployableValidationContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-validation/v1";
}

internal static class PbirDeployableReadinessContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-readiness/v1";
}

internal static class PbirDeployableDiagnosticsContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-diagnostics/v1";
}

internal static class PbirDeployableLineageContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-lineage/v1";
}

internal static class PbirDeployableHashesContract
{
    internal const string SchemaVersionV1 = "pbir-deployable-hashes/v1";
}

internal enum PbirSemanticModelEntryKind
{
    Column,
    Measure,
}

internal enum PbirDeployableSerializerReadinessState
{
    Incomplete,
    Blocked,
    ReadyForSerialization,
    Serialized,
}

internal sealed record PbirDatasetReferenceByPath(
    [property: JsonPropertyName("path")] string Path);

internal sealed record PbirDatasetReference(
    [property: JsonPropertyName("byPath")] PbirDatasetReferenceByPath ByPath);

internal sealed record PbirSemanticModelInventoryEntry(
    [property: JsonPropertyName("entryId")] string EntryId,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("entity")] string Entity,
    [property: JsonPropertyName("property")] string Property,
    [property: JsonPropertyName("kind")] PbirSemanticModelEntryKind Kind);

internal sealed record PbirSemanticModelInventory(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("inventoryRef")] string InventoryRef,
    [property: JsonPropertyName("entries")] IReadOnlyList<PbirSemanticModelInventoryEntry> Entries);

internal sealed record PbirRoleProjectionBinding(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("projectionOrder")] int ProjectionOrder,
    [property: JsonPropertyName("sourceSemanticToken")] string SourceSemanticToken,
    [property: JsonPropertyName("semanticModelEntryRef")] string SemanticModelEntryRef,
    [property: JsonPropertyName("queryRef")] string QueryRef,
    [property: JsonPropertyName("nativeQueryRef")] string NativeQueryRef,
    [property: JsonPropertyName("aggregation")] string Aggregation,
    [property: JsonPropertyName("displayName")] string? DisplayName,
    [property: JsonPropertyName("format")] string? Format);

internal sealed record PbirVisualBinding(
    [property: JsonPropertyName("visualId")] string VisualId,
    [property: JsonPropertyName("projections")] IReadOnlyList<PbirRoleProjectionBinding> Projections);

internal sealed record PbirDeployableExecutionPolicy(
    [property: JsonPropertyName("filesystemMaterializationAllowed")] bool FilesystemMaterializationAllowed,
    [property: JsonPropertyName("providerInvocationAllowed")] bool ProviderInvocationAllowed,
    [property: JsonPropertyName("microsoftSkillsExecutionAllowed")] bool MicrosoftSkillsExecutionAllowed,
    [property: JsonPropertyName("apiInvocationAllowed")] bool ApiInvocationAllowed,
    [property: JsonPropertyName("cliInvocationAllowed")] bool CliInvocationAllowed,
    [property: JsonPropertyName("deploymentAllowed")] bool DeploymentAllowed,
    [property: JsonPropertyName("desktopAutomationAllowed")] bool DesktopAutomationAllowed,
    [property: JsonPropertyName("analyzerAutomationAllowed")] bool AnalyzerAutomationAllowed)
{
    internal static PbirDeployableExecutionPolicy NoAuthority { get; } =
        new(false, false, false, false, false, false, false, false);

    internal bool HasAuthority =>
        FilesystemMaterializationAllowed ||
        ProviderInvocationAllowed ||
        MicrosoftSkillsExecutionAllowed ||
        ApiInvocationAllowed ||
        CliInvocationAllowed ||
        DeploymentAllowed ||
        DesktopAutomationAllowed ||
        AnalyzerAutomationAllowed;
}

internal sealed record PbirDeployableSerializerRequest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("requestId")] string RequestId,
    [property: JsonPropertyName("serializerRequestRef")] string SerializerRequestRef,
    [property: JsonPropertyName("serializerRequestSchemaVersion")] string SerializerRequestSchemaVersion,
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("pbirIrSchemaVersion")] string PbirIrSchemaVersion,
    [property: JsonPropertyName("pbirIrContentHash")] string PbirIrContentHash,
    [property: JsonPropertyName("targetFormat")] string TargetFormat,
    [property: JsonPropertyName("definitionPropertiesSchemaVersion")] string DefinitionPropertiesSchemaVersion,
    [property: JsonPropertyName("definitionSchemaVersion")] string DefinitionSchemaVersion,
    [property: JsonPropertyName("datasetReference")] PbirDatasetReference DatasetReference,
    [property: JsonPropertyName("layoutProfileId")] string LayoutProfileId,
    [property: JsonPropertyName("semanticModelInventory")] PbirSemanticModelInventory SemanticModelInventory,
    [property: JsonPropertyName("semanticModelInventoryRef")] string SemanticModelInventoryRef,
    [property: JsonPropertyName("semanticModelInventoryContentHash")] string SemanticModelInventoryContentHash,
    [property: JsonPropertyName("visualBindings")] IReadOnlyList<PbirVisualBinding> VisualBindings,
    [property: JsonPropertyName("executionPolicy")] PbirDeployableExecutionPolicy ExecutionPolicy);

internal sealed record PbirDeployableDiagnostic(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("message")] string Message);

internal sealed record PbirDeployableDiagnostics(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("missingRequiredFields")] IReadOnlyList<PbirDeployableDiagnostic> MissingRequiredFields,
    [property: JsonPropertyName("unsupportedSchemaVersions")] IReadOnlyList<PbirDeployableDiagnostic> UnsupportedSchemaVersions,
    [property: JsonPropertyName("unsupportedVisualTypes")] IReadOnlyList<PbirDeployableDiagnostic> UnsupportedVisualTypes,
    [property: JsonPropertyName("incompleteSemanticBindings")] IReadOnlyList<PbirDeployableDiagnostic> IncompleteSemanticBindings,
    [property: JsonPropertyName("invalidModelReferences")] IReadOnlyList<PbirDeployableDiagnostic> InvalidModelReferences,
    [property: JsonPropertyName("invalidPaths")] IReadOnlyList<PbirDeployableDiagnostic> InvalidPaths,
    [property: JsonPropertyName("duplicateIdentities")] IReadOnlyList<PbirDeployableDiagnostic> DuplicateIdentities,
    [property: JsonPropertyName("invalidLayoutDefinitions")] IReadOnlyList<PbirDeployableDiagnostic> InvalidLayoutDefinitions,
    [property: JsonPropertyName("invalidNavigationDefinitions")] IReadOnlyList<PbirDeployableDiagnostic> InvalidNavigationDefinitions,
    [property: JsonPropertyName("schemaIncompatibilities")] IReadOnlyList<PbirDeployableDiagnostic> SchemaIncompatibilities,
    [property: JsonPropertyName("hashViolations")] IReadOnlyList<PbirDeployableDiagnostic> HashViolations,
    [property: JsonPropertyName("lineageViolations")] IReadOnlyList<PbirDeployableDiagnostic> LineageViolations,
    [property: JsonPropertyName("boundaryViolations")] IReadOnlyList<PbirDeployableDiagnostic> BoundaryViolations,
    [property: JsonPropertyName("warnings")] IReadOnlyList<PbirDeployableDiagnostic> Warnings,
    [property: JsonPropertyName("unsupportedSections")] IReadOnlyList<PbirDeployableDiagnostic> UnsupportedSections)
{
    internal static PbirDeployableDiagnostics Empty { get; } =
        new(
            PbirDeployableDiagnosticsContract.SchemaVersionV1,
            [], [], [], [], [], [], [], [], [], [], [], [], [], [], []);

    internal bool HasFailures =>
        MissingRequiredFields.Count > 0 ||
        UnsupportedSchemaVersions.Count > 0 ||
        UnsupportedVisualTypes.Count > 0 ||
        IncompleteSemanticBindings.Count > 0 ||
        InvalidModelReferences.Count > 0 ||
        InvalidPaths.Count > 0 ||
        DuplicateIdentities.Count > 0 ||
        InvalidLayoutDefinitions.Count > 0 ||
        InvalidNavigationDefinitions.Count > 0 ||
        SchemaIncompatibilities.Count > 0 ||
        HashViolations.Count > 0 ||
        LineageViolations.Count > 0 ||
        BoundaryViolations.Count > 0;
}

internal sealed record PbirDeployableSerializerSafetyGateResult(
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("readiness")] PbirDeployableSerializerReadinessState Readiness,
    [property: JsonPropertyName("diagnostics")] PbirDeployableDiagnostics Diagnostics);

internal sealed record PbirDeployableGeneratedFile(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("byteLength")] int ByteLength,
    [property: JsonPropertyName("hashSha256")] string HashSha256,
    [property: JsonPropertyName("schemaUrl")] string SchemaUrl,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("sourceIrReferences")] IReadOnlyList<string> SourceIrReferences);

internal sealed record PbirDeployableGeneratedFileReference(
    [property: JsonPropertyName("relativePath")] string RelativePath,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("byteLength")] int ByteLength,
    [property: JsonPropertyName("hashSha256")] string HashSha256,
    [property: JsonPropertyName("schemaUrl")] string SchemaUrl,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion);

internal sealed record PbirDeployableLineage(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("pbirIrRef")] string PbirIrRef,
    [property: JsonPropertyName("pbirIrContentHash")] string PbirIrContentHash,
    [property: JsonPropertyName("serializerRequestRef")] string SerializerRequestRef,
    [property: JsonPropertyName("deployableSerializerRequestRef")] string DeployableSerializerRequestRef,
    [property: JsonPropertyName("semanticModelInventoryRef")] string SemanticModelInventoryRef,
    [property: JsonPropertyName("semanticModelInventoryContentHash")] string SemanticModelInventoryContentHash,
    [property: JsonPropertyName("upstreamLineage")] IReadOnlyList<PlanningLineageEntry> UpstreamLineage,
    [property: JsonPropertyName("immutableLineage")] IReadOnlyList<string> ImmutableLineage,
    [property: JsonPropertyName("lineageHash")] string LineageHash);

internal sealed record PbirDeployableHashes(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("inputHash")] string InputHash,
    [property: JsonPropertyName("fileSetHash")] string FileSetHash,
    [property: JsonPropertyName("artifactHash")] string ArtifactHash,
    [property: JsonPropertyName("manifestHash")] string ManifestHash,
    [property: JsonPropertyName("lineageHash")] string LineageHash);

internal sealed record PbirDeployableArtifact(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("artifactId")] string ArtifactId,
    [property: JsonPropertyName("targetFormat")] string TargetFormat,
    [property: JsonPropertyName("files")] IReadOnlyList<PbirDeployableGeneratedFile> Files,
    [property: JsonPropertyName("lineage")] PbirDeployableLineage Lineage,
    [property: JsonPropertyName("hashes")] PbirDeployableHashes Hashes);

internal sealed record PbirDeployableManifest(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("manifestId")] string ManifestId,
    [property: JsonPropertyName("artifactRef")] string ArtifactRef,
    [property: JsonPropertyName("schemaLock")] IReadOnlyList<string> SchemaLock,
    [property: JsonPropertyName("files")] IReadOnlyList<PbirDeployableGeneratedFileReference> Files,
    [property: JsonPropertyName("supportedFeatures")] IReadOnlyList<string> SupportedFeatures,
    [property: JsonPropertyName("warnings")] IReadOnlyList<PbirDeployableDiagnostic> Warnings,
    [property: JsonPropertyName("unsupportedSections")] IReadOnlyList<PbirDeployableDiagnostic> UnsupportedSections,
    [property: JsonPropertyName("lineage")] PbirDeployableLineage Lineage,
    [property: JsonPropertyName("hashes")] PbirDeployableHashes Hashes);

internal sealed record PbirDeployableValidation(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("isValid")] bool IsValid,
    [property: JsonPropertyName("validatedFileCount")] int ValidatedFileCount,
    [property: JsonPropertyName("schemaContractResults")] IReadOnlyList<PbirDeployableDiagnostic> SchemaContractResults,
    [property: JsonPropertyName("structuralValidationResults")] IReadOnlyList<PbirDeployableDiagnostic> StructuralValidationResults,
    [property: JsonPropertyName("crossReferenceValidationResults")] IReadOnlyList<PbirDeployableDiagnostic> CrossReferenceValidationResults,
    [property: JsonPropertyName("hashValidationResults")] IReadOnlyList<PbirDeployableDiagnostic> HashValidationResults);

internal sealed record PbirDeployableSerializerState(
    [property: JsonPropertyName("artifact")] PbirDeployableArtifact? Artifact,
    [property: JsonPropertyName("manifest")] PbirDeployableManifest? Manifest,
    [property: JsonPropertyName("validation")] PbirDeployableValidation Validation,
    [property: JsonPropertyName("readiness")] PbirDeployableSerializerReadinessState Readiness,
    [property: JsonPropertyName("diagnostics")] PbirDeployableDiagnostics Diagnostics);
