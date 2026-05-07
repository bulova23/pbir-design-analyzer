using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Services;

/// <summary>
/// Unit tests for <see cref="WcagContrastCalculator"/> and <see cref="PbirThemeService"/>
/// (Phase 9 / T044).
/// </summary>
public sealed class PbirThemeServiceTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    // ── WCAG: black on white ──────────────────────────────────────────────────

    /// <summary>
    /// Black (#000000) on white (#FFFFFF) has a contrast ratio of ~21:1,
    /// which meets WCAG 2.1 AA normal-text threshold (≥4.5:1).
    /// </summary>
    [Fact]
    public void MeetsNormalTextAA_BlackOnWhite_ReturnsTrue()
    {
        Assert.True(WcagContrastCalculator.MeetsNormalTextAA("#000000", "#FFFFFF"));
    }

    // ── WCAG: light grey on white ─────────────────────────────────────────────

    /// <summary>
    /// Light grey (#CCCCCC) on white (#FFFFFF) has a contrast ratio of ~1.6:1,
    /// which fails WCAG 2.1 AA normal-text threshold.
    /// </summary>
    [Fact]
    public void MeetsNormalTextAA_LightGrayOnWhite_ReturnsFalse()
    {
        Assert.False(WcagContrastCalculator.MeetsNormalTextAA("#CCCCCC", "#FFFFFF"));
    }

    // ── ASE import: duplicate hex values deduplicated ─────────────────────────

    /// <summary>
    /// Importing an ASE file that contains two identical RGB(255,0,0) colors must
    /// produce a ThemeDefinition with exactly one deduplicated hex color.
    /// </summary>
    [Fact]
    public void ImportFromFile_AseWithDuplicateHex_DeduplicatesColors()
    {
        // Arrange – build ASE binary with two identical red swatches
        var aseBytes = BuildAseFile((255, 0, 0), (255, 0, 0));
        var tempFile = Path.Combine(Path.GetTempPath(), "test-" + Guid.NewGuid().ToString("N") + ".ase");
        File.WriteAllBytes(tempFile, aseBytes);
        _tempFiles.Add(tempFile);

        var projectSvc = new PbirProjectService(NullLogger<PbirProjectService>.Instance);
        var themeService = new PbirThemeService(
            projectSvc,
            new HttpClient(),
            NullLogger<PbirThemeService>.Instance);

        // Act
        var theme = themeService.ImportFromFile(tempFile);

        // Assert – duplicates must be removed
        Assert.Single(theme.HexColors);
    }

    // ── URL import: network timeout raises HttpRequestException ───────────────

    /// <summary>
    /// Using an HttpClient with a 1 ms timeout against a non-routable address must
    /// cause <see cref="PbirThemeService.ImportFromUrlAsync"/> to throw
    /// <see cref="HttpRequestException"/> (the service wraps any transport exception).
    /// </summary>
    [Fact]
    public async Task ImportFromUrl_NetworkTimeout_ReturnsFailureResult()
    {
        // Arrange – 1 ms timeout guarantees a timeout before any network round-trip
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1) };
        var svc = new PbirThemeService(
            new PbirProjectService(NullLogger<PbirProjectService>.Instance),
            httpClient,
            NullLogger<PbirThemeService>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => svc.ImportFromUrlAsync("http://192.0.2.1/theme.json"));
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var f in _tempFiles)
        {
            try { File.Delete(f); } catch { /* best-effort */ }
        }
    }

    // ── ASE binary builder ────────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal Adobe Swatch Exchange binary with one RGB color block per entry.
    /// Format: 4-byte magic + 4-byte version + 4-byte block-count + N color blocks.
    /// </summary>
    private static byte[] BuildAseFile(params (byte R, byte G, byte B)[] colors)
    {
        using var ms = new MemoryStream();

        // magic "ASEF"
        ms.Write(new byte[] { 0x41, 0x53, 0x45, 0x46 });

        // version 1.0 big-endian: major=1, minor=0
        ms.Write(new byte[] { 0x00, 0x01, 0x00, 0x00 });

        // block count (big-endian)
        int blockCount = colors.Length;
        ms.Write(new byte[]
        {
            (byte)(blockCount >> 24), (byte)(blockCount >> 16),
            (byte)(blockCount >> 8),  (byte)blockCount,
        });

        foreach (var (r, g, b) in colors)
        {
            // block type 0x0001 (color entry)
            ms.Write(new byte[] { 0x00, 0x01 });

            // block data length: 2 (nameLen) + 4 (model "RGB ") + 12 (3 × float32) + 2 (colorType) = 20 = 0x14
            ms.Write(new byte[] { 0x00, 0x00, 0x00, 0x14 });

            // name length = 0 (no name bytes follow)
            ms.Write(new byte[] { 0x00, 0x00 });

            // color model "RGB " as ASCII
            ms.Write(new byte[] { 0x52, 0x47, 0x42, 0x20 });

            // R, G, B as big-endian IEEE 754 single-precision floats, normalised to [0, 1]
            WriteFloat32BE(ms, r / 255.0f);
            WriteFloat32BE(ms, g / 255.0f);
            WriteFloat32BE(ms, b / 255.0f);

            // color type: 0 = global
            ms.Write(new byte[] { 0x00, 0x00 });
        }

        return ms.ToArray();
    }

    private static void WriteFloat32BE(Stream stream, float value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        stream.Write(bytes);
    }
}
