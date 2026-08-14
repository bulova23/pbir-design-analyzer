using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

/// <summary>
/// Converts one admitted envelope and one resolved merge result into bounded
/// fidelity evidence. It does not mutate documents or participate in scoring.
/// </summary>
internal sealed class PbirAuthoringFidelityService
{
    internal PbirRoundTripFidelityResult Compare(
        PbirAuthoringEnvelope envelope,
        PbirResolvedAuthoringRepresentation resolved)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(resolved);

        var source = envelope.Items
            .Where(item => item.Classification != PbirAuthoringPreservationClass.Unsupported)
            .ToDictionary(item => item.OwnedRelativePath, SourceContent, StringComparer.Ordinal);
        var output = resolved.Documents
            .ToDictionary(document => document.RelativePath, document => document.Content, StringComparer.Ordinal);
        var expected = resolved.Documents
            .Where(document => document.Changed)
            .Select(document => document.RelativePath)
            .ToHashSet(StringComparer.Ordinal);
        return new PbirRoundTripFidelityService().Compare(source, output, expected);
    }

    private static string SourceContent(PbirAuthoringEnvelopeItem item) =>
        item.SourceContent ?? item.SourceDocument.GetRawText();
}
