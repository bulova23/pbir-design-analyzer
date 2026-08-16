using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class GenerationRequestPromptSegmentOrchestrator
{
    private readonly GenerationRequestValidator _validator = new();

    internal IReadOnlyList<GenerationRequestPromptSegment> BuildPromptSegments(GenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("Prompt segments can only be generated from a valid generation request.");
        }

        return
        [
            new GenerationRequestPromptSegment(
                1,
                "Target Summary",
                string.Join(Environment.NewLine,
                [
                    $"Target artifact profile: {ToContractValue(request.TargetArtifactProfile.ArtifactType)}",
                    $"Target profile id: {request.TargetArtifactProfile.ProfileId}",
                    $"Schema version: {request.SchemaVersion}",
                    $"Request id: {request.RequestId}",
                    $"Source design package: {request.SourceDesignPackageRef}",
                ])),
            new GenerationRequestPromptSegment(
                2,
                "Audience Summary",
                string.Join(Environment.NewLine,
                [
                    $"Primary audience: {request.DesignIntent.PrimaryAudience}",
                    $"Secondary audiences: {FormatList(request.DesignIntent.SecondaryAudiences)}",
                ])),
            new GenerationRequestPromptSegment(
                3,
                "Business Outcome",
                string.Join(Environment.NewLine,
                [
                    $"Business outcome: {request.DesignIntent.BusinessOutcome}",
                    $"Question: {request.DesignIntent.AnalyticalFlow.Question}",
                    $"Investigation: {request.DesignIntent.AnalyticalFlow.Investigation}",
                    $"Evidence: {request.DesignIntent.AnalyticalFlow.Evidence}",
                    $"Decision: {request.DesignIntent.AnalyticalFlow.Decision}",
                ])),
            new GenerationRequestPromptSegment(
                4,
                "Structural Intent",
                string.Join(Environment.NewLine,
                [
                    "Pages:",
                    .. request.StructuralIntent.Pages.Select(page => $"- {page.Name}: {page.Purpose} [{page.NavigationIntent}]"),
                    "Visual hints:",
                    .. request.StructuralIntent.VisualHints.Select(hint => $"- {hint.PageName}: {hint.VisualType} ({hint.VisualPurpose})"),
                ])),
            new GenerationRequestPromptSegment(
                5,
                "Data Intent",
                string.Join(Environment.NewLine,
                [
                    "KPIs:",
                    .. request.DataIntent.Kpis.Select(kpi => $"- {kpi.Name}: {kpi.Purpose} [{kpi.Grouping}]"),
                    $"Global filters: {FormatList(request.DataIntent.Filters.GlobalFilters)}",
                    "Page filters:",
                    .. request.DataIntent.Filters.PageFilters.Select(filter => $"- {filter.PageName}: {FormatList(filter.Filters)}"),
                    $"Semantic binding: {request.DataIntent.SemanticBinding.SemanticModelRef} ({request.DataIntent.SemanticBinding.SemanticModelLabel})",
                ])),
            new GenerationRequestPromptSegment(
                6,
                "Navigation Intent",
                string.Join(Environment.NewLine,
                [
                    $"Hierarchy: {FormatArrowList(request.StructuralIntent.Navigation.Hierarchy)}",
                    $"Workflow path: {FormatArrowList(request.StructuralIntent.Navigation.WorkflowPath)}",
                ])),
            new GenerationRequestPromptSegment(
                7,
                "Success Criteria",
                string.Join(Environment.NewLine,
                [
                    "Business success criteria:",
                    .. request.SuccessContract.BusinessSuccessCriteria.Select(item => $"- {item}"),
                    "Analytical success criteria:",
                    .. request.SuccessContract.AnalyticalSuccessCriteria.Select(item => $"- {item}"),
                    "Validation requirements:",
                    .. request.SuccessContract.ValidationRequirements.Select(item => $"- {item}"),
                ])),
            new GenerationRequestPromptSegment(
                8,
                "Constraints",
                string.Join(Environment.NewLine,
                [
                    $"Authority: {request.GenerationMode.Authority}",
                    $"Review required: {request.GenerationMode.ReviewRequired}",
                    $"Allow partial output: {request.GenerationMode.AllowPartialOutput}",
                    $"Design approval required: {request.ReviewPolicy.DesignApprovalRequired}",
                    $"Generation approval required: {request.ReviewPolicy.GenerationApprovalRequired}",
                    $"Analyzer review required: {request.ReviewPolicy.AnalyzerReviewRequired}",
                    "Do not infer unsupported target semantics or introduce provider-specific behavior.",
                ])),
        ];
    }

    private static string ToContractValue(GenerationRequestArtifactType artifactType)
    {
        return artifactType switch
        {
            GenerationRequestArtifactType.PbirReport => "pbirReport",
            GenerationRequestArtifactType.FabricDataApp => "fabricDataApp",
            GenerationRequestArtifactType.FabricApp => "fabricApp",
            _ => throw new ArgumentOutOfRangeException(nameof(artifactType), artifactType, "Unsupported generation request artifact type."),
        };
    }

    private static string FormatList(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? "none" : string.Join(", ", values);
    }

    private static string FormatArrowList(IReadOnlyList<string> values)
    {
        return values.Count == 0 ? "none" : string.Join(" -> ", values);
    }
}
