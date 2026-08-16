using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal static class PbirIntermediateRepresentationIntegrity
{
    internal static string ComputeContentHash(PbirIntermediateRepresentation ir)
    {
        ArgumentNullException.ThrowIfNull(ir);

        return ComputeContentHash(
            ir.Metadata,
            ir.References,
            ir.Pages,
            ir.Visuals,
            ir.Semantics,
            ir.Navigation,
            ir.Layout,
            ir.SuccessCriteria,
            ir.Lineage);
    }

    internal static string ComputeContentHash(
        PbirIntermediateRepresentationMetadata metadata,
        PbirIntermediateRepresentationReferences references,
        IReadOnlyList<PbirIntermediateRepresentationPage> pages,
        IReadOnlyList<PbirIntermediateRepresentationVisual> visuals,
        IReadOnlyList<PbirIntermediateRepresentationSemantic> semantics,
        PbirIntermediateRepresentationNavigation navigation,
        PbirIntermediateRepresentationLayout layout,
        PbirIntermediateRepresentationSuccessCriteria successCriteria,
        PbirIntermediateRepresentationLineage lineage)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            metadata,
            references,
            pages,
            visuals,
            semantics,
            navigation,
            layout,
            successCriteria,
            lineage
        }));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
