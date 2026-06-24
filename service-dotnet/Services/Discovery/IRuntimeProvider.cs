using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal interface IRuntimeProvider
{
    bool CanAcceptRequest(RuntimeProviderRequest request);

    RuntimeProviderValidationResult ValidateRequest(RuntimeProviderRequest request, RuntimeProviderContext context);

    RuntimeProviderContext CreateExecutionContext(RuntimeProviderRequest request);

    RuntimeProviderResult EvaluateExecutionReadiness(RuntimeProviderRequest request, RuntimeProviderContext context);
}
