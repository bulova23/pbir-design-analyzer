using System.Text.Json;

namespace PowerBIModelingService.Services.Pbir.Scoring;

internal sealed class ReportAnalysisContextFactory
{
    private readonly ReportModelLoader _reportModelLoader;
    private readonly ThemeResolutionService _themeResolutionService;
    private readonly ScoringConfigurationService _scoringConfigurationService;

    internal ReportAnalysisContextFactory(
        ReportModelLoader reportModelLoader,
        ThemeResolutionService themeResolutionService,
        ScoringConfigurationService scoringConfigurationService)
    {
        _reportModelLoader = reportModelLoader ?? throw new ArgumentNullException(nameof(reportModelLoader));
        _themeResolutionService = themeResolutionService ?? throw new ArgumentNullException(nameof(themeResolutionService));
        _scoringConfigurationService = scoringConfigurationService ?? throw new ArgumentNullException(nameof(scoringConfigurationService));
    }

    internal ReportAnalysisContext Create(PbirReportLocation location, JsonElement? config)
    {
        ArgumentNullException.ThrowIfNull(location);
        var reportModel = _reportModelLoader.LoadReportModel(location);
        var themeColors = _themeResolutionService.ResolveThemeColors(reportModel.ReportJson, location);
        var frameworkWeights = _scoringConfigurationService.ExtractFrameworkWeights(config);
        var navigationScoring = _scoringConfigurationService.ExtractNavigationScoringSettings(config);

        return new ReportAnalysisContext(
            location,
            reportModel.ReportJson,
            reportModel.Pages,
            reportModel.ReportFilters,
            themeColors,
            frameworkWeights,
            navigationScoring);
    }
}
