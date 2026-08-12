namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BProviderRegistry
{
    internal IReadOnlyList<Phase35BProviderRegistration> Registrations { get; }

    internal Phase35BProviderRegistry(IEnumerable<Phase35BProviderRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        Registrations = registrations.ToArray();
    }
}

internal static class Phase35BProductionCatalog
{
    internal static IReadOnlyList<Phase35BProviderRegistration> Registrations { get; } =
        Phase35AProviderCatalog.All.Select(profile => new Phase35BProviderRegistration(profile, null)).ToArray();
}
