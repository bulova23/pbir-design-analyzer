namespace PowerBIModelingService.Services.DesignStudio.Providers;

internal interface IDesignStudioProvider
{
    string ProviderId { get; }

    string DisplayName { get; }

    IReadOnlyList<DesignProviderCapability> Capabilities { get; }
}
