using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class PbirIntermediateRepresentationReadinessService
{
    internal PbirIntermediateRepresentationReadinessState Evaluate(
        PbirIntermediateRepresentationValidationResult validation,
        bool prepareForSerializer)
    {
        ArgumentNullException.ThrowIfNull(validation);

        if (validation.Diagnostics.HasIncompleteFailures)
        {
            return PbirIntermediateRepresentationReadinessState.Incomplete;
        }

        if (validation.Diagnostics.HasBlockingFailures)
        {
            return PbirIntermediateRepresentationReadinessState.Blocked;
        }

        return prepareForSerializer
            ? PbirIntermediateRepresentationReadinessState.ReadyForSerializer
            : PbirIntermediateRepresentationReadinessState.Canonical;
    }
}
