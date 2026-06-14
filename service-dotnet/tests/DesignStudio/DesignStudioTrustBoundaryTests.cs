using System.Reflection;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace PowerBIModelingService.Tests.DesignStudio;

public sealed class DesignStudioTrustBoundaryTests
{
    private static readonly Assembly CoreAssembly = typeof(ScoreResult).Assembly;
    private const string DesignStudioRootNamespace = "PowerBIModelingService.Services.DesignStudio";
    private const string ModelsNamespace = "PowerBIModelingService.Services.DesignStudio.Models";
    private const string ProvidersNamespace = "PowerBIModelingService.Services.DesignStudio.Providers";
    private const string MaterializationNamespace = "PowerBIModelingService.Services.DesignStudio.Materialization";

    [Fact(DisplayName = "Design Studio workflow models preserve analyzer-owned validation and explicit approval separation")]
    public void DesignStudio_WorkflowModels_PreserveOwnershipBoundaries()
    {
        var approvalStateType = RequireType(ModelsNamespace, "IterationApprovalState");
        var validationCheckpointType = RequireType(ModelsNamespace, "IterationValidationApprovalCheckpoint");
        var iterationType = RequireType(ModelsNamespace, "DesignIterationRecord");

        var approvalProperties = approvalStateType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("DesignApproval", approvalProperties);
        Assert.Contains("MaterializationApproval", approvalProperties);
        Assert.Contains("RefinementApproval", approvalProperties);
        Assert.Contains("ValidationApproval", approvalProperties);
        Assert.DoesNotContain("DeploymentApproval", approvalProperties);

        var validationProperties = validationCheckpointType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("Owner", validationProperties);
        Assert.Contains("AnalyzerRunId", validationProperties);
        Assert.Contains("ResultReference", validationProperties);
        Assert.Contains("SourceCandidateId", validationProperties);
        Assert.Contains("SourceArtifactVersionFingerprint", validationProperties);
        Assert.Contains("ValidationResultStatus", validationProperties);

        var iterationProperties = iterationType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("ApprovalCheckpoint", iterationProperties);
        Assert.Contains("Guardrails", iterationProperties);
        Assert.DoesNotContain("ApprovalState", iterationProperties);
        Assert.DoesNotContain("ApprovalKind", iterationProperties);
    }

    [Fact(DisplayName = "Design Studio namespaces do not expose direct mutation, deployment, auto-approval, or analyzer-run authority")]
    public void DesignStudio_Namespaces_DoNotExposeExecutionBypassMethods()
    {
        string[] forbiddenFragments =
        [
            "Apply",
            "Deploy",
            "Publish",
            "Mutate",
            "GeneratePbir",
            "CreateProduction",
            "AutoApprove",
            "ApproveValidation",
            "RunAnalyzer",
            "ExecuteAnalyzer",
            "AutoPromote",
        ];

        var methods = CoreAssembly
            .GetTypes()
            .Where(type => type.Namespace is not null && type.Namespace.StartsWith(DesignStudioRootNamespace, StringComparison.Ordinal))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(method => method.Name)
            .ToArray();

        foreach (var fragment in forbiddenFragments)
        {
            Assert.DoesNotContain(methods, method => method.Contains(fragment, StringComparison.Ordinal));
        }
    }

    [Fact(DisplayName = "Provider and materialization models remain advisory-only and non-production")]
    public void DesignStudio_ProviderAndMaterializationModels_RemainRestricted()
    {
        var workflowConstraintsType = RequireType(ProvidersNamespace, "DesignProviderWorkflowConstraints");
        var sideEffectsType = RequireType(MaterializationNamespace, "MaterializationSideEffectState");

        var workflowConstraintProperties = workflowConstraintsType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("RequiresApproval", workflowConstraintProperties);
        Assert.Contains("RequiresValidation", workflowConstraintProperties);
        Assert.Contains("AllowsMaterialization", workflowConstraintProperties);
        Assert.Contains("AllowsReportMutation", workflowConstraintProperties);
        Assert.Contains("AllowsPbirAssetGeneration", workflowConstraintProperties);
        Assert.Contains("AllowsAnalyzableSurfaceCreation", workflowConstraintProperties);

        var sideEffectProperties = sideEffectsType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("AnalyzerHandoffExecuted", sideEffectProperties);
        Assert.Contains("AnalyzerWorkspaceOpened", sideEffectProperties);
        Assert.Contains("PbirFilesCreated", sideEffectProperties);
        Assert.Contains("ReportMutationOccurred", sideEffectProperties);
        Assert.Contains("DeliveryTriggered", sideEffectProperties);
        Assert.Contains("ProviderExecutionTriggered", sideEffectProperties);
    }

    private static Type RequireType(string @namespace, string typeName)
    {
        var type = CoreAssembly.GetType($"{@namespace}.{typeName}", throwOnError: false);
        Assert.NotNull(type);
        return type!;
    }
}
