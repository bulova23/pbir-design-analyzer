namespace PowerBIModelingService.Services.Pbir.Models;

/// <summary>
/// Describes the outcome of a refactoring operation (snap-to-grid, colour variance reduction,
/// flagged chart type replacement, etc.) performed by <see cref="PbirRefactorEngine"/>.
/// </summary>
public sealed class RefactorResult
{
    /// <summary>Gets or sets the human-readable description of each operation that was applied.</summary>
    public List<string> AppliedOperations { get; set; } = [];

    /// <summary>Gets or sets any warnings raised during refactoring (e.g. a visual that could not be snapped).</summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>Gets or sets the report path that was refactored.</summary>
    public string? ReportPath { get; set; }

    /// <summary>Gets whether the refactoring completed without warnings.</summary>
    public bool IsClean => Warnings.Count == 0;
}
