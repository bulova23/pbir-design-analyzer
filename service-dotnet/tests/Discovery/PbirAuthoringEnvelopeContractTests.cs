using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirAuthoringEnvelopeContractTests
{
    [Fact]
    public void EnvelopeItem_RecordsBoundedClassificationAndOwnedSourceDocument()
    {
        using var document = JsonDocument.Parse("{\"$schema\":\"schema\",\"objects\":{\"title\":[]},\"custom\":true}");

        var item = new PbirAuthoringEnvelopeItem(
            PbirAuthoringOwnerKind.Visual,
            "visual-1",
            "definition/pages/page-1/visuals/visual-1/visual.json",
            "schema",
            "1.0.0",
            PbirAuthoringPreservationClass.OpaquePreserved,
            document.RootElement.Clone(),
            "{\"$schema\":\"schema\",\"objects\":{\"title\":[]},\"custom\":true}",
            "hash",
            ["$schema", "objects", "custom"],
            new PbirAuthoringIdentityProvenance("visual-1", "visual-1", null));

        Assert.Equal(PbirAuthoringPreservationClass.OpaquePreserved, item.Classification);
        Assert.Equal("visual-1", item.Identity.ImportedIdentity);
        Assert.Equal("definition/pages/page-1/visuals/visual-1/visual.json", item.OwnedRelativePath);
        Assert.True(item.SourceDocument.GetProperty("custom").GetBoolean());
    }

    [Fact]
    public void EnvelopeIdentity_SeparatesImportedGeneratedAndExplicitValues()
    {
        var identity = new PbirAuthoringIdentityProvenance("imported", "generated", "override");

        Assert.Equal("imported", identity.ImportedIdentity);
        Assert.Equal("generated", identity.GeneratedIdentity);
        Assert.Equal("override", identity.ExplicitOverride);
    }

    [Fact]
    public void EnvelopeClassificationCatalog_DoesNotIncludeGenericMutationClass()
    {
        Assert.Equal(
            new[] { "TypedSupported", "OpaquePreserved", "Unsupported" },
            Enum.GetNames<PbirAuthoringPreservationClass>());
        Assert.DoesNotContain(
            typeof(PbirAuthoringEnvelopeItem).GetProperties(),
            property => property.Name.Contains("Patch", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Contains("JsonPointer", StringComparison.OrdinalIgnoreCase));
    }
}
