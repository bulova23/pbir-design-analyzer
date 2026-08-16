using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationManifestReadinessService
{
    internal GenerationManifestReadinessState Evaluate(GenerationManifestValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);

        if (validation.Diagnostics.HasIncompleteFailures)
        {
            return GenerationManifestReadinessState.Incomplete;
        }

        if (validation.Diagnostics.HasBlockingFailures)
        {
            return GenerationManifestReadinessState.Blocked;
        }

        return GenerationManifestReadinessState.ReadyForGenerator;
    }
}

