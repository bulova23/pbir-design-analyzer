using Microsoft.Extensions.Logging;

namespace PowerBIModelingService.Services.Pbir;

internal sealed class ReportDiscoveryService
{
    private readonly PbirProjectService _projectService;
    private readonly ILogger _logger;

    public ReportDiscoveryService(PbirProjectService projectService, ILogger logger)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal PbirReportLocation ResolveRequiredReportLocation(string reportPath)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new ArgumentException("Parameter 'reportPath' is required.", nameof(reportPath));
        }

        var location = _projectService.TryGetReportLocation(reportPath);
        if (location is not null)
        {
            return location;
        }

        _logger.LogWarning("[Scoring] No PBIR report definition found at '{ReportPath}'.", reportPath);
        throw new InvalidOperationException($"No PBIR report definition found at '{reportPath}'.");
    }
}
