using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PowerBIModelingService.Services.Pbir.Scoring;

internal sealed class ReportAnalysisContext
{
    internal ReportAnalysisContext(
        PbirReportLocation location,
        JsonObject reportJson,
        IReadOnlyList<PageData> pages,
        IReadOnlyList<FilterDefinitionData> reportFilters,
        IReadOnlyList<string> themeColors,
        IReadOnlyDictionary<string, double> frameworkWeights,
        NavigationScoringSettings navigationScoring)
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
        _reportJson = reportJson?.DeepClone().AsObject() ?? throw new ArgumentNullException(nameof(reportJson));
        Pages = new ReadOnlyCollection<PageData>((pages ?? throw new ArgumentNullException(nameof(pages))).ToList());
        ReportFilters = new ReadOnlyCollection<FilterDefinitionData>((reportFilters ?? throw new ArgumentNullException(nameof(reportFilters))).ToList());
        ThemeColors = new ReadOnlyCollection<string>((themeColors ?? throw new ArgumentNullException(nameof(themeColors))).ToList());
        FrameworkWeights = new ReadOnlyDictionary<string, double>(
            new Dictionary<string, double>(frameworkWeights ?? throw new ArgumentNullException(nameof(frameworkWeights)), StringComparer.OrdinalIgnoreCase));
        NavigationScoring = navigationScoring;
    }

    internal PbirReportLocation Location { get; }
    internal IReadOnlyList<PageData> Pages { get; }
    internal IReadOnlyList<FilterDefinitionData> ReportFilters { get; }
    internal IReadOnlyList<string> ThemeColors { get; }
    internal IReadOnlyDictionary<string, double> FrameworkWeights { get; }
    internal NavigationScoringSettings NavigationScoring { get; }

    internal JsonObject CreateReportJsonSnapshot() => _reportJson.DeepClone().AsObject();

    private readonly JsonObject _reportJson;
}
