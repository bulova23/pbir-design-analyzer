namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35DPackageIdentityResolver
{
    private readonly Phase35ACanonicalJson _canonical = new();

    internal Phase35DPackageIdentity Resolve(string providerId, string providerVersion, string implementationId, Phase35DPackageMetadata metadata)
    {
        var packageId = _canonical.Hash(new { providerId, providerVersion, implementationId, metadata });
        return new(Phase35DContracts.PackageIdentityV1, providerId, providerVersion, implementationId, packageId, metadata, _canonical.Hash(new { schemaVersion = Phase35DContracts.PackageIdentityV1, providerId, providerVersion, implementationId, packageId, metadata }));
    }
}
