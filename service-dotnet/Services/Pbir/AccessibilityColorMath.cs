namespace PowerBIModelingService.Services.Pbir;

/// <summary>Pure color helpers used by PBIR accessibility and semantic-color analysis.</summary>
internal static class AccessibilityColorMath
{
    /// <summary>
    /// Returns a normalized #RRGGBB string when the input is a parseable hex colour;
    /// returns <c>null</c> for null, empty, or malformed inputs. Tolerates the # prefix
    /// being optional and the 3-digit shorthand (#RGB).
    /// </summary>
    internal static string? TryNormalizeHex(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        var trimmed = hex.Trim().TrimStart('#');
        if (trimmed.Length == 3)
        {
            trimmed = $"{trimmed[0]}{trimmed[0]}{trimmed[1]}{trimmed[1]}{trimmed[2]}{trimmed[2]}";
        }
        if (trimmed.Length != 6) return null;
        for (int i = 0; i < 6; i++)
        {
            if (!Uri.IsHexDigit(trimmed[i])) return null;
        }
        return "#" + trimmed.ToUpperInvariant();
    }

    /// <summary>
    /// Returns <c>true</c> when one colour reads as predominantly red and the other as predominantly green
    /// (the classic red/green colourblindness failure pattern). Uses a coarse RGB dominance heuristic.
    /// </summary>
    internal static bool LooksLikeRedGreenPair(string a, string b) =>
        (IsRedDominant(a) && IsGreenDominant(b)) || (IsGreenDominant(a) && IsRedDominant(b));

    internal static bool IsRedDominant(string hex)
    {
        var (r, g, bl) = HexToRgb(hex);
        return r > g + 40 && r > bl + 40;
    }

    internal static bool IsGreenDominant(string hex)
    {
        var (r, g, bl) = HexToRgb(hex);
        return g > r + 40 && g > bl + 40;
    }

    /// <summary>
    /// Applies a simple deuteranopia simulation (Brettel/Viénot-style projection collapsed to a
    /// linearised channel mix) and reports whether the two simulated colours fall within a small
    /// perceptual distance. Intentionally conservative — only flag pairs that are clearly at risk.
    /// </summary>
    internal static bool SimulatesToSimilarUnderDeuteranopia(string a, string b)
    {
        var simA = SimulateDeuteranopia(a);
        var simB = SimulateDeuteranopia(b);
        double dr = simA.R - simB.R;
        double dg = simA.G - simB.G;
        double db = simA.B - simB.B;
        double distance = Math.Sqrt(dr * dr + dg * dg + db * db);
        // sRGB values in [0,1]; 0.15 is a coarse perceptual threshold for "looks similar".
        return distance < 0.15;
    }

    internal static (double R, double G, double B) SimulateDeuteranopia(string hex)
    {
        var (rByte, gByte, bByte) = HexToRgb(hex);
        double r = rByte / 255.0;
        double g = gByte / 255.0;
        double b = bByte / 255.0;
        // Approximate deuteranopia projection in sRGB space (linear approximation of the
        // Brettel/Viénot model). Sufficient for "indistinguishable hue" warnings; not a full
        // CIE simulation.
        double simR = 0.625 * r + 0.375 * g + 0.0 * b;
        double simG = 0.700 * r + 0.300 * g + 0.0 * b;
        double simB = 0.0 * r + 0.300 * g + 0.700 * b;
        return (Math.Clamp(simR, 0, 1), Math.Clamp(simG, 0, 1), Math.Clamp(simB, 0, 1));
    }

    internal static (int R, int G, int B) HexToRgb(string hex)
    {
        var h = hex.TrimStart('#');
        return (
            Convert.ToInt32(h[..2], 16),
            Convert.ToInt32(h[2..4], 16),
            Convert.ToInt32(h[4..], 16));
    }
}
