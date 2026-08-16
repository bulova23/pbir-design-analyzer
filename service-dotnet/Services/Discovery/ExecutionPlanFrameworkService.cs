using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ExecutionPlanFrameworkService
{
    private readonly ExecutionPlanBuilder _builder;
    private readonly ExecutionPlanValidator _validator;

    internal ExecutionPlanFrameworkService()
        : this(new ExecutionPlanBuilder(), new ExecutionPlanValidator())
    {
    }

    internal ExecutionPlanFrameworkService(
        ExecutionPlanBuilder builder,
        ExecutionPlanValidator validator)
    {
        _builder = builder;
        _validator = validator;
    }

    internal ExecutionPlanFrameworkState CreateDraft(
        GenerationRequest request,
        string schemaVersion = ExecutionPlanContract.SchemaVersionV1)
    {
        var creationResult = _builder.Create(request, schemaVersion);
        return creationResult.Plan is null
            ? new ExecutionPlanFrameworkState(
                Request: request,
                Plan: null,
                Readiness: ExecutionPlanReadinessState.Blocked,
                Diagnostics: creationResult.Diagnostics)
            : new ExecutionPlanFrameworkState(
                Request: request,
                Plan: creationResult.Plan,
                Readiness: ExecutionPlanReadinessState.Draft,
                Diagnostics: creationResult.Diagnostics);
    }

    internal ExecutionPlanFrameworkState Validate(ExecutionPlanFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.Plan is null)
        {
            return state with
            {
                Readiness = ExecutionPlanReadinessState.Blocked,
            };
        }

        var validation = _validator.Validate(state.Plan);
        return state with
        {
            Readiness = validation.IsValid ? ExecutionPlanReadinessState.Valid : ExecutionPlanReadinessState.Blocked,
            Diagnostics = validation.Diagnostics,
        };
    }

    internal ExecutionPlanFrameworkState PrepareForProviderAdapter(ExecutionPlanFrameworkState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var validated = Validate(state);
        return validated.Plan is null || validated.Readiness == ExecutionPlanReadinessState.Blocked
            ? validated with
            {
                Readiness = ExecutionPlanReadinessState.Blocked,
            }
            : validated with
            {
                Readiness = ExecutionPlanReadinessState.ReadyForProviderAdapter,
            };
    }
}
