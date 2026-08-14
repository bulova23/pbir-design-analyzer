using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerBIModelingService.Services.Discovery.Models;

internal static class PbirAuthoringEnvelopeContract
{
    internal const string SchemaVersionV1 = "pbir-authoring-envelope/v1";
}

internal enum PbirAuthoringPreservationClass
{
    TypedSupported,
    OpaquePreserved,
    Unsupported
}

internal enum PbirAuthoringOwnerKind
{
    Report,
    PagesMetadata,
    Page,
    Visual,
    Navigation,
    Theme,
    Filter,
    Slicer,
    Layout
}

internal enum PbirAuthoringMutationClassification
{
    TypedAndMergeable,
    PreservedButNotAuthorable,
    Unsupported
}

internal static class PbirAuthoringMutationInventory
{
    internal static PbirAuthoringMutationClassification Classify(LocalPbirMutationOperationKind operation) => operation switch
    {
        LocalPbirMutationOperationKind.ResizeVisual => PbirAuthoringMutationClassification.TypedAndMergeable,
        LocalPbirMutationOperationKind.UpdateBinding or
        LocalPbirMutationOperationKind.UpdateFormatting or
        LocalPbirMutationOperationKind.UpdateTheme or
        LocalPbirMutationOperationKind.UpdateFilter or
        LocalPbirMutationOperationKind.UpdateNavigation or
        LocalPbirMutationOperationKind.UpdateSlicer => PbirAuthoringMutationClassification.PreservedButNotAuthorable,
        _ => PbirAuthoringMutationClassification.Unsupported
    };
}

internal sealed record PbirAuthoringIdentityProvenance(
    [property: JsonPropertyName("importedIdentity")] string? ImportedIdentity,
    [property: JsonPropertyName("generatedIdentity")] string? GeneratedIdentity,
    [property: JsonPropertyName("explicitOverride")] string? ExplicitOverride)
{
    internal string? PreferredIdentity => ExplicitOverride ?? ImportedIdentity ?? GeneratedIdentity;
}

internal sealed record PbirAuthoringEnvelopeItem(
    [property: JsonPropertyName("ownerKind")] PbirAuthoringOwnerKind OwnerKind,
    [property: JsonPropertyName("ownerId")] string OwnerId,
    [property: JsonPropertyName("ownedRelativePath")] string OwnedRelativePath,
    [property: JsonPropertyName("schemaUrl")] string SchemaUrl,
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("classification")] PbirAuthoringPreservationClass Classification,
    [property: JsonPropertyName("sourceDocument")] JsonElement SourceDocument,
    [property: JsonPropertyName("sourceContent")] string? SourceContent,
    [property: JsonPropertyName("sourceHash")] string SourceHash,
    [property: JsonPropertyName("propertyOrder")] IReadOnlyList<string> PropertyOrder,
    [property: JsonPropertyName("identity")] PbirAuthoringIdentityProvenance? Identity = null);

internal sealed record PbirAuthoringEnvelope(
    [property: JsonPropertyName("schemaVersion")] string SchemaVersion,
    [property: JsonPropertyName("items")] IReadOnlyList<PbirAuthoringEnvelopeItem> Items,
    [property: JsonPropertyName("sourceDefinitionHash")] string SourceDefinitionHash)
{
    internal static PbirAuthoringEnvelope Empty(string sourceDefinitionHash = "") =>
        new(PbirAuthoringEnvelopeContract.SchemaVersionV1, [], sourceDefinitionHash);

    internal PbirAuthoringEnvelopeItem? Find(PbirAuthoringOwnerKind ownerKind, string ownerId) =>
        Items.FirstOrDefault(item => item.OwnerKind == ownerKind && string.Equals(item.OwnerId, ownerId, StringComparison.Ordinal));
}
