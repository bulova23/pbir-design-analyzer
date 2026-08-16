using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationRequestFrameworkService
{
    private readonly GenerationRequestBuilder _builder;
    private readonly GenerationRequestValidator _validator;
    private readonly GenerationRequestPromptSegmentOrchestrator _promptSegmentOrchestrator;

    internal GenerationRequestFrameworkService()
        : this(new GenerationRequestBuilder(), new GenerationRequestValidator(), new GenerationRequestPromptSegmentOrchestrator())
    {
    }

    internal GenerationRequestFrameworkService(
        GenerationRequestBuilder builder,
        GenerationRequestValidator validator,
        GenerationRequestPromptSegmentOrchestrator promptSegmentOrchestrator)
    {
        _builder = builder;
        _validator = validator;
        _promptSegmentOrchestrator = promptSegmentOrchestrator;
    }

    internal GenerationRequestFrameworkState CreateDraft(
        DesignPackageConsumptionResult consumptionResult,
        string schemaVersion = GenerationRequestContract.SchemaVersionV1)
    {
        var creationResult = _builder.Create(consumptionResult, schemaVersion);
        return creationResult.Request is null
            ? new GenerationRequestFrameworkState(
                Request: null,
                Readiness: GenerationRequestReadinessState.Blocked,
                Diagnostics: creationResult.Diagnostics,
                PromptSegments: [])
            : new GenerationRequestFrameworkState(
                Request: creationResult.Request,
                Readiness: GenerationRequestReadinessState.Draft,
                Diagnostics: creationResult.Diagnostics,
                PromptSegments: []);
    }

    internal GenerationRequestFrameworkState Validate(GenerationRequestFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Request is null)
        {
            return state with
            {
                Readiness = GenerationRequestReadinessState.Blocked,
                PromptSegments = [],
            };
        }

        var validation = _validator.Validate(state.Request);
        return state with
        {
            Readiness = validation.IsValid ? GenerationRequestReadinessState.Valid : GenerationRequestReadinessState.Blocked,
            Diagnostics = validation.Diagnostics,
            PromptSegments = [],
        };
    }

    internal GenerationRequestFrameworkState PrepareForProviderPlanning(GenerationRequestFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var validated = Validate(state);
        if (validated.Request is null || validated.Readiness == GenerationRequestReadinessState.Blocked)
        {
            return validated with
            {
                Readiness = GenerationRequestReadinessState.Blocked,
                PromptSegments = [],
            };
        }

        var promptSegments = _promptSegmentOrchestrator.BuildPromptSegments(validated.Request);
        return validated with
        {
            Readiness = GenerationRequestReadinessState.ReadyForProviderPlanning,
            PromptSegments = promptSegments,
        };
    }
}
