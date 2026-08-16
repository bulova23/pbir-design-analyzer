using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirRoundTripFidelityService
{
    internal PbirRoundTripFidelityResult Compare(
        IReadOnlyDictionary<string, string> sourceFiles,
        IReadOnlyDictionary<string, string> outputFiles,
        IReadOnlySet<string>? expectedChangedPaths = null)
    {
        expectedChangedPaths ??= new HashSet<string>(StringComparer.Ordinal);
        var paths = sourceFiles.Keys.Concat(outputFiles.Keys).Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var files = new List<PbirRoundTripFileFidelity>();
        var preserved = new List<string>();
        var changed = new List<string>();
        var unexpected = new List<string>();
        var authoringIdentical = new List<string>();
        var semanticEquivalent = new List<string>();
        var intentionallyChanged = new List<string>();
        var unsupported = new List<string>();
        foreach (var path in paths)
        {
            sourceFiles.TryGetValue(path, out var source);
            outputFiles.TryGetValue(path, out var output);
            var sourceHash = source is null ? null : Hash(source);
            var outputHash = output is null ? null : Hash(output);
            var classification = source is null
                ? PbirFidelityClassification.ExpectedNormalizedDifference
                : output is null
                    ? PbirFidelityClassification.MissingOutput
                    : sourceHash == outputHash
                        ? PbirFidelityClassification.ByteIdentical
                        : SemanticallyEqual(source, output)
                            ? PbirFidelityClassification.SemanticallyIdentical
                            : expectedChangedPaths.Contains(path)
                                ? PbirFidelityClassification.ExpectedNormalizedDifference
                                : PbirFidelityClassification.UnexpectedDifference;
            files.Add(new(path, classification, sourceHash, outputHash));
            if (classification == PbirFidelityClassification.ByteIdentical)
            {
                preserved.Add(path);
                authoringIdentical.Add(path);
            }
            else if (classification == PbirFidelityClassification.SemanticallyIdentical)
            {
                preserved.Add(path);
                semanticEquivalent.Add(path);
            }
            else if (classification == PbirFidelityClassification.ExpectedNormalizedDifference && expectedChangedPaths.Contains(path))
            {
                intentionallyChanged.Add(path);
            }
            if (classification is not (PbirFidelityClassification.ByteIdentical or PbirFidelityClassification.SemanticallyIdentical)) changed.Add(path);
            if (classification is PbirFidelityClassification.UnexpectedDifference or PbirFidelityClassification.MissingOutput) unexpected.Add(path);
            if (classification == PbirFidelityClassification.Unsupported) unsupported.Add(path);
        }
        return new(files, preserved, changed, unexpected, authoringIdentical, semanticEquivalent, intentionallyChanged, unsupported);
    }

    private static bool SemanticallyEqual(string source, string output)
    {
        try
        {
            using var left = JsonDocument.Parse(source);
            using var right = JsonDocument.Parse(output);
            return Canonical(left.RootElement) == Canonical(right.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string Canonical(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", element.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal).Select(property => JsonSerializer.Serialize(property.Name) + ":" + Canonical(property.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", element.EnumerateArray().Select(Canonical)) + "]",
        _ => element.GetRawText()
    };

    private static string Hash(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
