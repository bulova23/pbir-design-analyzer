using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationRequestService
{
    private readonly GenerationRequestBuilder _builder = new();
    private readonly GenerationRequestValidator _validator = new();
    private readonly GenerationRequestPromptSegmentOrchestrator _promptSegmentOrchestrator = new();

    internal GenerationRequestCreationResult Create(
        DesignPackageConsumptionResult consumptionResult,
        string schemaVersion = GenerationRequestContract.SchemaVersionV1)
    {
        return _builder.Create(consumptionResult, schemaVersion);
    }

    internal GenerationRequestValidationResult Validate(GenerationRequest request)
    {
        return _validator.Validate(request);
    }

    internal IReadOnlyList<GenerationRequestPromptSegment> BuildPromptSegments(GenerationRequest request)
    {
        return _promptSegmentOrchestrator.BuildPromptSegments(request);
    }
}
