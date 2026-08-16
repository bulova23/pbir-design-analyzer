using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationProviderReadinessService
{
    internal GenerationProviderReadinessState Evaluate(
        GenerationProviderValidationResult validation,
        GenerationProviderDefinition provider)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(provider);

        if (validation.Diagnostics.HasBlockingFailures)
        {
            return GenerationProviderReadinessState.Blocked;
        }

        if (validation.Diagnostics.HasUnsupportedFailures || provider.Status == GenerationProviderStatus.Unsupported)
        {
            return GenerationProviderReadinessState.Unsupported;
        }

        return provider.Status == GenerationProviderStatus.Available
            ? GenerationProviderReadinessState.ReadyForGenerationProvider
            : GenerationProviderReadinessState.Candidate;
    }
}
