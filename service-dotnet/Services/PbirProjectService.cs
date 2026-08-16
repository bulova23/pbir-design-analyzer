using Microsoft.Extensions.Logging;

namespace PowerBIModelingService.Services;

/// <summary>
/// Locates PBIR report definitions within PBIP project structures.
/// </summary>
public sealed class PbirProjectService
{
    private readonly ILogger<PbirProjectService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PbirProjectService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public PbirProjectService(ILogger<PbirProjectService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to resolve the report definition location for a PBIP project.
    /// </summary>
    /// <param name="projectPath">PBIP project root path or .pbip file path.</param>
    /// <returns>Resolved report location, or null if not found.</returns>
    public PbirReportLocation? TryGetReportLocation(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            _logger.LogWarning("PBIR project path was empty.");
            return null;
        }

        var projectRoot = ResolveProjectRoot(projectPath);
        if (projectRoot is null)
        {
            _logger.LogWarning("PBIR project root could not be resolved for path: {Path}", projectPath);
            return null;
        }

        var reportRoot = ResolveReportRoot(projectRoot);
        if (reportRoot is null)
        {
            _logger.LogInformation("No report folder found under project root: {Path}", projectRoot);
            return null;
        }

        var definitionPath = Path.Combine(reportRoot, "definition");
        var reportJsonPath = Path.Combine(definitionPath, "report.json");
        if (!File.Exists(reportJsonPath))
        {
            _logger.LogWarning("Report definition file not found at {Path}", reportJsonPath);
            return null;
        }

        var workspaceRoot = ResolveWorkspaceRoot(projectRoot);

        return new PbirReportLocation(projectRoot, reportRoot, definitionPath, reportJsonPath, workspaceRoot);
    }

    // Resolve the PBIP project root directory from a file or directory path.
    private string? ResolveProjectRoot(string projectPath)
    {
        if (File.Exists(projectPath) && projectPath.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(projectPath);
        }

        if (Directory.Exists(projectPath))
        {
            return projectPath;
        }

        return null;
    }

    // Locate the report root folder containing the PBIR definition.
    private string? ResolveReportRoot(string projectRoot)
    {
        if (projectRoot.EndsWith(".Report", StringComparison.OrdinalIgnoreCase))
        {
            return projectRoot;
        }

        var definitionPbir = Path.Combine(projectRoot, "definition.pbir");
        if (File.Exists(definitionPbir))
        {
            return projectRoot;
        }

        var reportFolders = Directory.GetDirectories(projectRoot, "*.Report", SearchOption.TopDirectoryOnly)
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal);
        foreach (var reportFolder in reportFolders)
        {
            var reportJsonPath = Path.Combine(reportFolder, "definition", "report.json");
            if (File.Exists(reportJsonPath))
            {
                return reportFolder;
            }
        }

        return null;
    }

    // Determine workspace root for relative path calculation.
    private string ResolveWorkspaceRoot(string projectRoot)
    {
        var workspaceRoot = Environment.GetEnvironmentVariable("WORKSPACE_PATH");
        if (!string.IsNullOrWhiteSpace(workspaceRoot) && Directory.Exists(workspaceRoot))
        {
            return workspaceRoot;
        }

        var current = new DirectoryInfo(projectRoot);
        while (current is not null)
        {
            var gitPath = Path.Combine(current.FullName, ".git");
            if (Directory.Exists(gitPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return projectRoot;
    }
}

/// <summary>
/// Represents the resolved PBIR report definition location.
/// </summary>
public sealed class PbirReportLocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PbirReportLocation"/> class.
    /// </summary>
    /// <param name="projectRootPath">PBIP project root path.</param>
    /// <param name="reportRootPath">Report root folder path.</param>
    /// <param name="definitionPath">Definition folder path.</param>
    /// <param name="reportJsonPath">Report JSON path.</param>
    /// <param name="workspaceRootPath">Workspace root path for relative path calculation.</param>
    public PbirReportLocation(
        string projectRootPath,
        string reportRootPath,
        string definitionPath,
        string reportJsonPath,
        string workspaceRootPath)
    {
        ProjectRootPath = projectRootPath;
        ReportRootPath = reportRootPath;
        DefinitionPath = definitionPath;
        ReportJsonPath = reportJsonPath;
        WorkspaceRootPath = workspaceRootPath;
    }

    /// <summary>
    /// Gets the PBIP project root path.
    /// </summary>
    public string ProjectRootPath { get; }

    /// <summary>
    /// Gets the report root folder path.
    /// </summary>
    public string ReportRootPath { get; }

    /// <summary>
    /// Gets the report definition folder path.
    /// </summary>
    public string DefinitionPath { get; }

    /// <summary>
    /// Gets the report JSON path.
    /// </summary>
    public string ReportJsonPath { get; }

    /// <summary>
    /// Gets the workspace root path for workspace-relative paths.
    /// </summary>
    public string WorkspaceRootPath { get; }

    /// <summary>
    /// Gets the report name derived from the report root folder.
    /// </summary>
    public string ReportName
    {
        get
        {
            var folderName = Path.GetFileName(ReportRootPath);
            return folderName.EndsWith(".Report", StringComparison.OrdinalIgnoreCase)
                ? folderName[..^".Report".Length]
                : folderName;
        }
    }
}
