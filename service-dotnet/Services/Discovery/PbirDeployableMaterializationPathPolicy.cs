using System.Text;
using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed record PbirDeployableMaterializationPathResult(
    bool IsValid,
    string CanonicalOutputBasePath,
    string CanonicalTargetPath,
    string ControlRootPath,
    string TargetControlPath,
    string TargetKey,
    IReadOnlyList<PbirDeployableMaterializationDiagnostic> Diagnostics);

internal sealed class PbirDeployableMaterializationPathPolicy
{
    private readonly IPbirDeployableMaterializationFileSystem _fileSystem;

    internal PbirDeployableMaterializationPathPolicy()
        : this(new PbirDeployableMaterializationFileSystem())
    {
    }

    internal PbirDeployableMaterializationPathPolicy(IPbirDeployableMaterializationFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    internal PbirDeployableMaterializationPathResult Resolve(
        string outputBaseDirectory,
        string targetDirectoryName,
        IReadOnlyList<string> artifactPaths)
    {
        var diagnostics = new List<PbirDeployableMaterializationDiagnostic>();
        var empty = new PbirDeployableMaterializationPathResult(false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, diagnostics);

        if (string.IsNullOrWhiteSpace(outputBaseDirectory) || !Path.IsPathFullyQualified(outputBaseDirectory))
        {
            diagnostics.Add(Diagnostic("PBIRMAT-PATH-001", "outputBaseDirectory", "Output base must be an absolute existing directory."));
            return empty;
        }

        string canonicalBase;
        try
        {
            canonicalBase = _fileSystem.GetFullPath(outputBaseDirectory).Normalize(NormalizationForm.FormC);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            diagnostics.Add(Diagnostic("PBIRMAT-PATH-001", "outputBaseDirectory", "Output base could not be canonicalized."));
            return empty;
        }

        if (!_fileSystem.DirectoryExists(canonicalBase) || IsReparsePoint(canonicalBase))
        {
            diagnostics.Add(Diagnostic("PBIRMAT-PATH-002", canonicalBase, "Output base must exist and may not be a link or reparse point."));
        }

        if (!IsSafeTargetName(targetDirectoryName))
        {
            diagnostics.Add(Diagnostic("PBIRMAT-PATH-003", "targetDirectoryName", "Target must be one safe NFC leaf and may not be reserved, PBIP, or SemanticModel."));
        }

        var safeTargetName = IsSafeTargetName(targetDirectoryName) ? targetDirectoryName : "invalid";
        var target = _fileSystem.GetFullPath(Path.Combine(canonicalBase, safeTargetName));
        if (!IsContained(canonicalBase, target) || (_fileSystem.DirectoryExists(target) && IsReparsePoint(target)))
        {
            diagnostics.Add(Diagnostic("PBIRMAT-PATH-004", target, "Target must remain below the base and may not be a link or reparse point."));
        }

        var comparer = ActivePathComparer;
        var normalizedArtifactPaths = new HashSet<string>(comparer);
        foreach (var artifactPath in artifactPaths)
        {
            if (!IsSafeArtifactPath(artifactPath) || !normalizedArtifactPaths.Add(artifactPath))
            {
                diagnostics.Add(Diagnostic("PBIRMAT-PATH-005", artifactPath, "Artifact path is unsafe, legacy, or collides on this platform."));
                continue;
            }

            var resolved = _fileSystem.GetFullPath(Path.Combine(target, artifactPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!IsContained(target, resolved))
            {
                diagnostics.Add(Diagnostic("PBIRMAT-PATH-006", artifactPath, "Artifact path escapes the authorized target."));
            }
        }

        var controlRoot = Path.Combine(canonicalBase, ".pbir-design-analyzer", "materialization");
        var platformKeyPath = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? target.ToUpperInvariant()
            : target;
        var targetKey = new PbirDeployableMaterializationCanonicalJson().ComputeSha256(platformKeyPath)[..32];
        return new PbirDeployableMaterializationPathResult(
            diagnostics.Count == 0,
            canonicalBase,
            target,
            controlRoot,
            Path.Combine(controlRoot, "targets", targetKey),
            targetKey,
            diagnostics.OrderBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.Path, StringComparer.Ordinal).ToArray());
    }

    internal static StringComparer ActivePathComparer =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    private bool IsReparsePoint(string path) => (_fileSystem.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsSafeTargetName(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.IsNormalized(NormalizationForm.FormC) &&
        value is not "." and not ".." &&
        !value.Equals(".pbir-design-analyzer", StringComparison.OrdinalIgnoreCase) &&
        !value.EndsWith(".pbip", StringComparison.OrdinalIgnoreCase) &&
        !value.EndsWith(".SemanticModel", StringComparison.OrdinalIgnoreCase) &&
        !value.EndsWith('.') && !value.EndsWith(' ') &&
        value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\', ':']) < 0 &&
        !value.Any(char.IsControl) &&
        !Path.IsPathRooted(value);

    private static bool IsSafeArtifactPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "report.json" || value.Contains('\\') || value.Contains(':') || value.Any(char.IsControl))
        {
            return false;
        }

        var segments = value.Split('/');
        return !Path.IsPathRooted(value) && segments.All(segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static bool IsContained(string parent, string child)
    {
        var comparison = ActivePathComparer == StringComparer.OrdinalIgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var prefix = parent.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return child.StartsWith(prefix, comparison);
    }

    private static PbirDeployableMaterializationDiagnostic Diagnostic(string code, string path, string message) => new(code, path, message);
}
