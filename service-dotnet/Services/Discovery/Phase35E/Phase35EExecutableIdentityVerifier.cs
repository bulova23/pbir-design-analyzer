using System.Security.Cryptography;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35EExecutableIdentityVerifier
{
    internal bool Verify(Phase35EExecutableIdentity certified, Phase35EExecutableIdentity requested)
    {
        if (certified != requested || !Path.IsPathFullyQualified(requested.ExecutablePath) || requested.ExecutablePath.Contains(Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar, StringComparison.Ordinal)) return false;
        if (!File.Exists(requested.ExecutablePath)) return false;
        var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(requested.ExecutablePath))).ToLowerInvariant();
        return string.Equals(actual, requested.ExecutableSha256, StringComparison.OrdinalIgnoreCase);
    }
}
