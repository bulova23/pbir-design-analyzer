using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Pbir.Models;

namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Provides theme import, deduplication, WCAG validation, and application services for
/// PBIR reports.
///
/// <para><b>Supported import sources:</b></para>
/// <list type="bullet">
/// <item>Adobe Swatch Exchange (<c>.ase</c>) — binary RGB block parser.</item>
/// <item>Adobe Photoshop Color Swatches (<c>.aco</c>) — version-1 binary parser.</item>
/// <item>Adobe Color API URL — HTTP GET with 5-second timeout, JSON swatches array.</item>
/// </list>
/// </summary>
public sealed class PbirThemeService
{
    // WCAG reference background — assume white for all contrast checks.
    private const string BackgroundColor = "#FFFFFF";
    private const int    MaxPaletteSize  = 8;

    // ASE magic bytes: "ASEF"
    private static readonly byte[] AseMagic = [0x41, 0x53, 0x45, 0x46];
    private const ushort AseColorBlockType = 0x0001;

    private static readonly Regex HexRegex =
        new(@"^#([A-Fa-f0-9]{6})$", RegexOptions.Compiled);

    private readonly PbirProjectService _projectService;
    private readonly HttpClient         _httpClient;
    private readonly ILogger<PbirThemeService> _logger;

    /// <summary>Initializes a new instance of <see cref="PbirThemeService"/>.</summary>
    public PbirThemeService(
        PbirProjectService projectService,
        HttpClient httpClient,
        ILogger<PbirThemeService> logger)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _httpClient     = httpClient     ?? throw new ArgumentNullException(nameof(httpClient));
        _logger         = logger         ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Validation helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="hex"/> matches
    /// <c>^#([A-Fa-f0-9]{6})$</c>.
    /// </summary>
    /// <example><code>PbirThemeService.ValidateHex("#1F4E79") // true</code></example>
    public static bool ValidateHex(string hex) =>
        !string.IsNullOrWhiteSpace(hex) && HexRegex.IsMatch(hex);

    /// <summary>
    /// Returns a deduplicated list of valid hex colours (case-insensitive comparison).
    /// Entries that do not match the hex pattern are silently discarded.
    /// </summary>
    public static List<string> DeduplicateColors(IEnumerable<string> hexList)
    {
        var seen   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var hex in hexList)
        {
            if (ValidateHex(hex) && seen.Add(hex.ToUpperInvariant()))
            {
                result.Add(hex.ToUpperInvariant());
            }
        }

        return result;
    }

    // ── File import ───────────────────────────────────────────────────────────

    /// <summary>
    /// Imports a <see cref="ThemeDefinition"/> from a local <c>.ase</c> or <c>.aco</c> file.
    /// </summary>
    /// <param name="filePath">Absolute path to the swatch file.</param>
    /// <exception cref="ArgumentException">When the path is blank or the extension is unsupported.</exception>
    /// <exception cref="InvalidOperationException">When the file format is invalid.</exception>
    public ThemeDefinition ImportFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Parameter 'filePath' is required.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new ArgumentException($"File not found: '{filePath}'.", nameof(filePath));
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        _logger.LogInformation("[Theme] Importing from file: {Path} ({Ext})", filePath, ext);

        var colors = ext switch
        {
            ".ase" => ParseAse(filePath),
            ".aco" => ParseAco(filePath),
            _      => throw new ArgumentException(
                           $"Unsupported swatch format '{ext}'. Expected .ase or .aco.", nameof(filePath)),
        };

        var deduplicated = DeduplicateColors(colors);
        if (deduplicated.Count > MaxPaletteSize)
        {
            deduplicated = deduplicated.Take(MaxPaletteSize).ToList();
            _logger.LogWarning("[Theme] Palette truncated to {Max} colours.", MaxPaletteSize);
        }

        var theme = new ThemeDefinition
        {
            Name       = Path.GetFileNameWithoutExtension(filePath),
            HexColors  = deduplicated,
            SourcePath = filePath,
        };

        ValidateWcag(theme);
        return theme;
    }

    // ── URL import ────────────────────────────────────────────────────────────

    /// <summary>
    /// Imports a <see cref="ThemeDefinition"/> from an Adobe Color API URL.
    /// </summary>
    /// <param name="url">URL returning JSON with a <c>swatches</c> or <c>theme.swatches</c> array.</param>
    /// <exception cref="ArgumentException">When <paramref name="url"/> is blank.</exception>
    /// <returns>
    /// A <see cref="ThemeDefinition"/> on success.
    /// On <see cref="HttpRequestException"/> the exception is re-thrown with the original message
    /// preserved so callers can surface it as a <c>ServiceResult.Failure</c>.
    /// </returns>
    public async Task<ThemeDefinition> ImportFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Parameter 'url' is required.", nameof(url));
        }

        _logger.LogInformation("[Theme] Importing from URL: {Url}", url);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new HttpRequestException(
                $"Could not fetch theme from '{url}': {ex.Message}. " +
                "Check the URL or use 'File' import instead.", ex);
        }

        var json    = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
        var swatches = ParseSwatchesJson(json);

        var deduplicated = DeduplicateColors(swatches);
        if (deduplicated.Count > MaxPaletteSize)
        {
            deduplicated = deduplicated.Take(MaxPaletteSize).ToList();
        }

        var theme = new ThemeDefinition
        {
            Name       = "Adobe Color Import",
            HexColors  = deduplicated,
            SourcePath = url,
        };

        ValidateWcag(theme);
        return theme;
    }

    // ── WCAG validation ───────────────────────────────────────────────────────

    /// <summary>
    /// Validates all <c>BackgroundColor × DataColor</c> contrast pairs against WCAG 2.1 AA
    /// (≥ 4.5:1 normal text). Updates <see cref="ThemeDefinition.WcagAaPassCount"/>,
    /// <see cref="ThemeDefinition.WcagAaFailCount"/>, and <see cref="ThemeDefinition.WcagViolations"/>
    /// in-place.
    /// </summary>
    public void ValidateWcag(ThemeDefinition theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        theme.WcagViolations.Clear();
        theme.WcagAaPassCount = 0;
        theme.WcagAaFailCount = 0;

        foreach (var color in theme.HexColors)
        {
            try
            {
                var ratio = WcagContrastCalculator.ContrastRatio(color, BackgroundColor);
                if (ratio >= WcagContrastCalculator.NormalTextAaThreshold)
                {
                    theme.WcagAaPassCount++;
                }
                else
                {
                    theme.WcagAaFailCount++;
                    theme.WcagViolations.Add(
                        $"{color} vs white — ratio {ratio:F2}:1 (fails WCAG 2.1 AA ≥4.5:1)");
                }
            }
            catch (ArgumentException)
            {
                theme.WcagViolations.Add($"{color} — invalid hex value, skipped.");
                theme.WcagAaFailCount++;
            }
        }
    }

    // ── Apply to report ───────────────────────────────────────────────────────

    /// <summary>
    /// Writes (or overwrites) the <c>theme.json</c> file referenced by the report and updates
    /// <c>report.json</c> to point to the new theme.
    /// </summary>
    /// <param name="reportPath">PBIP project root or <c>.Report</c> folder path.</param>
    /// <param name="theme">The validated <see cref="ThemeDefinition"/> to apply.</param>
    /// <exception cref="ArgumentException">When either parameter is invalid.</exception>
    /// <exception cref="InvalidOperationException">When no PBIR report definition is found.</exception>
    public void ApplyToReport(string reportPath, ThemeDefinition theme)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            throw new ArgumentException("Parameter 'reportPath' is required.", nameof(reportPath));
        }

        ArgumentNullException.ThrowIfNull(theme);

        var location = _projectService.TryGetReportLocation(reportPath);
        if (location is null)
        {
            throw new InvalidOperationException(
                $"No PBIR report definition found at '{reportPath}'.");
        }

        // Write theme.json to the report definition folder.
        var themeFileName = $"{SanitizeFileName(theme.Name)}.json";
        var themePath     = Path.Combine(location.DefinitionPath, themeFileName);

        var themeJson = new JsonObject
        {
            ["name"]       = theme.Name,
            ["dataColors"] = new JsonArray(theme.HexColors.Select(c => (JsonNode)JsonValue.Create(c)!).ToArray()),
        };

        PbirFileWriteGuard.CheckAndGuard(themePath);
        File.WriteAllText(themePath,
            themeJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        // Update report.json to reference the new theme by href.
        var reportJson = ReadJsonObject(location.ReportJsonPath);
        var hrefRelative = Path.GetRelativePath(location.WorkspaceRootPath, themePath)
            .Replace('\\', '/');

        reportJson["theme"] = new JsonObject { ["href"] = hrefRelative };

        PbirFileWriteGuard.CheckAndGuard(location.ReportJsonPath);
        File.WriteAllText(location.ReportJsonPath,
            reportJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        _logger.LogInformation("[Theme] Applied theme '{Name}' to report '{Report}'.",
            theme.Name, location.ReportName);
    }

    // ── ASE binary parser ─────────────────────────────────────────────────────

    /// <summary>
    /// Parses an Adobe Swatch Exchange (<c>.ase</c>) binary file and returns hex colour strings.
    /// </summary>
    /// <remarks>
    /// Format: 4-byte magic "ASEF", 2-byte major version, 2-byte minor version,
    /// 4-byte block count, then blocks.
    /// Each block: 2-byte type (0x0001 = colour), 4-byte block-data length (big-endian),
    /// then block data:
    ///   - 2-byte name-length (in UTF-16 code units, including null terminator)
    ///   - name-length × 2 bytes of UTF-16BE name
    ///   - 4-byte colour-model ("RGB ")
    ///   - 12 bytes: 3 × IEEE 754 float (big-endian) for R, G, B in [0, 1]
    ///   - 2-byte colour type (0=global, 1=spot, 2=normal)
    /// </remarks>
    private List<string> ParseAse(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        if (data.Length < 12)
        {
            throw new InvalidOperationException($"File too small to be a valid .ase file: '{filePath}'.");
        }

        // Verify magic bytes "ASEF"
        for (var i = 0; i < AseMagic.Length; i++)
        {
            if (data[i] != AseMagic[i])
            {
                throw new InvalidOperationException($"Invalid .ase magic bytes in '{filePath}'.");
            }
        }

        var blockCount = ReadInt32BigEndian(data, 8);
        var offset     = 12;
        var colors     = new List<string>();

        for (var b = 0; b < blockCount && offset + 6 <= data.Length; b++)
        {
            var blockType   = ReadUInt16BigEndian(data, offset);
            var blockLength = ReadInt32BigEndian(data, offset + 2);
            offset += 6;

            if (blockType == AseColorBlockType && offset + blockLength <= data.Length)
            {
                // Skip name: 2-byte count then count×2 UTF-16 bytes.
                var nameLen = ReadUInt16BigEndian(data, offset);
                var nameEnd = offset + 2 + (nameLen * 2);

                // Colour model: 4-byte string at nameEnd.
                if (nameEnd + 4 + 12 <= data.Length)
                {
                    var model = System.Text.Encoding.ASCII.GetString(data, nameEnd, 4);
                    if (model is "RGB " or "RGB\0")
                    {
                        var r = ReadFloat32BigEndian(data, nameEnd + 4);
                        var g = ReadFloat32BigEndian(data, nameEnd + 8);
                        var bl = ReadFloat32BigEndian(data, nameEnd + 12);
                        colors.Add(FloatsToHex(r, g, bl));
                    }
                }
            }

            offset += blockLength;
        }

        _logger.LogDebug("[Theme] .ase parser: {Count} colour(s) extracted.", colors.Count);
        return colors;
    }

    // ── ACO binary parser ─────────────────────────────────────────────────────

    /// <summary>
    /// Parses an Adobe Photoshop Color Swatches (<c>.aco</c>) binary file (version 1 or 2).
    /// </summary>
    /// <remarks>
    /// Format: 2-byte version (1 or 2), 2-byte colour count,
    /// then per colour: 2-byte colorspace, 4 × 2-byte components (w, x, y, z),
    /// 2-byte padding = 10 bytes per entry.
    /// For RGB (colorspace 0): w = R × 256, x = G × 256, y = B × 256.
    /// </remarks>
    private List<string> ParseAco(string filePath)
    {
        var data = File.ReadAllBytes(filePath);
        if (data.Length < 4)
        {
            throw new InvalidOperationException($"File too small to be a valid .aco file: '{filePath}'.");
        }

        var version = ReadUInt16BigEndian(data, 0);
        var count   = ReadUInt16BigEndian(data, 2);
        var colors  = new List<string>();
        var offset  = 4;

        for (var i = 0; i < count && offset + 10 <= data.Length; i++)
        {
            var colorspace = ReadUInt16BigEndian(data, offset);
            var w          = ReadUInt16BigEndian(data, offset + 2);
            var x          = ReadUInt16BigEndian(data, offset + 4);
            var y          = ReadUInt16BigEndian(data, offset + 6);
            // z / padding at offset + 8 (unused for RGB)

            offset += 10;

            if (version == 2)
            {
                // v2: skip name field — 2-byte count + count×2 UTF-16 chars + null terminator.
                if (offset + 2 <= data.Length)
                {
                    var nameLen = ReadUInt16BigEndian(data, offset);
                    offset += 2 + ((nameLen + 1) * 2);
                }
            }

            // Only handle RGB colorspace (0).
            if (colorspace != 0) continue;

            var r = (byte)(w >> 8);
            var g = (byte)(x >> 8);
            var b = (byte)(y >> 8);
            colors.Add($"#{r:X2}{g:X2}{b:X2}");
        }

        _logger.LogDebug("[Theme] .aco v{Ver} parser: {Count} colour(s) extracted.", version, colors.Count);
        return colors;
    }

    // ── Adobe Color JSON parser ───────────────────────────────────────────────

    /// <summary>
    /// Parses swatches from an Adobe Color API JSON response.
    /// Handles both <c>{ "swatches": [...] }</c> and
    /// <c>{ "theme": { "swatches": [...] } }</c> response shapes.
    /// Each swatch is expected to have <c>"red"</c>, <c>"green"</c>, <c>"blue"</c> fields (0–255).
    /// </summary>
    private List<string> ParseSwatchesJson(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException("Adobe Color API response is not a JSON object.");

        JsonArray? swatches =
            root["swatches"] as JsonArray ??
            (root["theme"] as JsonObject)?["swatches"] as JsonArray;

        if (swatches is null || swatches.Count == 0)
        {
            throw new InvalidOperationException(
                "Adobe Color API response contains no 'swatches' array. " +
                "Check the URL or use File import instead.");
        }

        var colors = new List<string>();
        foreach (var item in swatches)
        {
            if (item is not JsonObject sw) continue;

            var r = TryGetByte(sw, "red")   ?? TryGetByte(sw, "r");
            var g = TryGetByte(sw, "green") ?? TryGetByte(sw, "g");
            var b = TryGetByte(sw, "blue")  ?? TryGetByte(sw, "b");

            if (r is not null && g is not null && b is not null)
            {
                colors.Add($"#{r.Value:X2}{g.Value:X2}{b.Value:X2}");
            }
        }

        return colors;
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static string FloatsToHex(float r, float g, float b)
    {
        var ri = (int)Math.Round(r * 255, MidpointRounding.AwayFromZero);
        var gi = (int)Math.Round(g * 255, MidpointRounding.AwayFromZero);
        var bi = (int)Math.Round(b * 255, MidpointRounding.AwayFromZero);
        return $"#{Math.Clamp(ri, 0, 255):X2}{Math.Clamp(gi, 0, 255):X2}{Math.Clamp(bi, 0, 255):X2}";
    }

    private static byte? TryGetByte(JsonObject obj, string key)
    {
        try
        {
            if (obj[key] is JsonNode n)
            {
                return (byte)Math.Clamp(n.GetValue<int>(), 0, 255);
            }
        }
        catch { /* fall through */ }

        return null;
    }

    private static JsonObject ReadJsonObject(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidOperationException($"File is not a JSON object: {filePath}");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }

    // Binary helpers — all big-endian.
    private static ushort ReadUInt16BigEndian(byte[] data, int offset) =>
        (ushort)((data[offset] << 8) | data[offset + 1]);

    private static int ReadInt32BigEndian(byte[] data, int offset) =>
        (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

    private static float ReadFloat32BigEndian(byte[] data, int offset)
    {
        // IEEE 754 big-endian → little-endian for BitConverter.
        var bytes = new byte[] { data[offset + 3], data[offset + 2], data[offset + 1], data[offset] };
        return BitConverter.ToSingle(bytes, 0);
    }
}
