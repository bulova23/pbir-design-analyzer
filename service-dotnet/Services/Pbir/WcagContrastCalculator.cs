namespace PowerBIModelingService.Services.Pbir;

/// <summary>
/// Static helper for computing WCAG 2.1 colour-contrast ratios.
/// Implements the sRGB linearisation and relative-luminance formulas
/// specified in WCAG 2.1 Success Criterion 1.4.3 and 1.4.6.
/// </summary>
/// <example>
/// <code>
/// bool passes = WcagContrastCalculator.MeetsNormalTextAA("#1F4E79", "#FFFFFF"); // true
/// double ratio = WcagContrastCalculator.ContrastRatio("#1F4E79", "#FFFFFF");    // ≈ 10.4
/// </code>
/// </example>
public static class WcagContrastCalculator
{
    /// <summary>Minimum contrast ratio for WCAG 2.1 AA — normal text (≥ 18px regular or 14px bold).</summary>
    public const double NormalTextAaThreshold = 4.5;

    /// <summary>Minimum contrast ratio for WCAG 2.1 AA — large text (≥ 18px regular or 14px bold).</summary>
    public const double LargeTextAaThreshold = 3.0;

    /// <summary>Minimum contrast ratio for WCAG 2.1 AAA — normal text.</summary>
    public const double NormalTextAaaThreshold = 7.0;

    /// <summary>
    /// Returns the WCAG 2.1 contrast ratio between two hex colours.
    /// </summary>
    /// <param name="hex1">First colour in #RRGGBB or #RGB format.</param>
    /// <param name="hex2">Second colour in #RRGGBB or #RGB format.</param>
    /// <returns>Contrast ratio in the range [1, 21].</returns>
    /// <exception cref="ArgumentException">Thrown when a hex string cannot be parsed.</exception>
    public static double ContrastRatio(string hex1, string hex2)
    {
        var l1 = RelativeLuminance(hex1);
        var l2 = RelativeLuminance(hex2);
        // Ensure lighter colour is in the numerator
        var lighter = Math.Max(l1, l2);
        var darker  = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>
    /// Returns <see langword="true"/> when the contrast between <paramref name="hex1"/> and
    /// <paramref name="hex2"/> meets WCAG 2.1 AA normal-text threshold (≥ 4.5:1).
    /// </summary>
    public static bool MeetsNormalTextAA(string hex1, string hex2) =>
        ContrastRatio(hex1, hex2) >= NormalTextAaThreshold;

    /// <summary>
    /// Returns <see langword="true"/> when the contrast between <paramref name="hex1"/> and
    /// <paramref name="hex2"/> meets WCAG 2.1 AA large-text threshold (≥ 3:1).
    /// </summary>
    public static bool MeetsLargeTextAA(string hex1, string hex2) =>
        ContrastRatio(hex1, hex2) >= LargeTextAaThreshold;

    /// <summary>
    /// Returns <see langword="true"/> when the contrast between <paramref name="hex1"/> and
    /// <paramref name="hex2"/> meets WCAG 2.1 AAA normal-text threshold (≥ 7:1).
    /// </summary>
    public static bool MeetsNormalTextAAA(string hex1, string hex2) =>
        ContrastRatio(hex1, hex2) >= NormalTextAaaThreshold;

    // ── Internal helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Computes the relative luminance of a hex colour per WCAG 2.1 §1.4.3.
    /// Formula: 0.2126 R_lin + 0.7152 G_lin + 0.0722 B_lin.
    /// </summary>
    internal static double RelativeLuminance(string hex)
    {
        var (r, g, b) = ParseHex(hex);
        return 0.2126 * Linearise(r / 255.0)
             + 0.7152 * Linearise(g / 255.0)
             + 0.0722 * Linearise(b / 255.0);
    }

    /// <summary>
    /// Applies sRGB gamma expansion (IEC 61966-2-1).
    /// C_linear = C/12.92 for C ≤ 0.04045; else ((C+0.055)/1.055)^2.4.
    /// </summary>
    private static double Linearise(double c) =>
        c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    /// <summary>Parses a #RRGGBB or #RGB hex string into (R, G, B) byte components.</summary>
    private static (int R, int G, int B) ParseHex(string hex)
    {
        var h = hex.TrimStart('#');
        if (h.Length == 3)
        {
            // Expand shorthand: #RGB → #RRGGBB
            h = new string([h[0], h[0], h[1], h[1], h[2], h[2]]);
        }

        if (h.Length != 6)
        {
            throw new ArgumentException($"Unsupported hex colour format: '{hex}'. Expected #RRGGBB or #RGB.", nameof(hex));
        }

        return (
            Convert.ToInt32(h[..2], 16),
            Convert.ToInt32(h[2..4], 16),
            Convert.ToInt32(h[4..], 16)
        );
    }
}
