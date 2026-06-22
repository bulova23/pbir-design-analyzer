using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class ExecutionProviderContractFrameworkServiceTests
{
    [Fact(DisplayName = "Execution Provider contracts load a valid provider, preserve lineage, and become approved for a future execution provider deterministically")]
    public void EvaluateProvider_ValidInputs_AreDeterministicAndAuditable()
    {
        var service = new ExecutionProviderContractFrameworkService();
        var definition = service.CreateDefaultProviderDefinition();
        var inputs = CreateValidInputs();

        var first = service.EvaluateProvider(
            definition,
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.NegotiationResult,
            CreateApprovalPolicy(designApproved: true, generationApproved: true),
            ExecutionProviderMode.Manual);
        var second = service.EvaluateProvider(
            definition,
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.NegotiationResult,
            CreateApprovalPolicy(designApproved: true, generationApproved: true),
            ExecutionProviderMode.Manual);
        var prepared = service.PrepareForExecutionProvider(first);

        Assert.NotNull(first.ProviderRequest);
        Assert.NotNull(first.ProviderResponse);
        Assert.NotNull(first.AuditRecord);
        Assert.Equal(ExecutionEligibilityStatus.Eligible, first.Eligibility);
        Assert.Equal(ExecutionProviderReadinessState.Eligible, first.Readiness);
        Assert.Equal(ExecutionProviderResponseStatus.Accepted, first.ProviderResponse!.Status);
        Assert.Equal(ExecutionProviderReadinessState.ApprovedForExecutionProvider, prepared.Readiness);
        Assert.Equal(ExecutionProviderReadinessState.ApprovedForExecutionProvider, prepared.ProviderResponse!.ReadinessStatus);
        Assert.Equal(SerializeProviderRequest(first.ProviderRequest!), SerializeProviderRequest(second.ProviderRequest!));
        Assert.Equal(SerializeProviderResponse(first.ProviderResponse!), SerializeProviderResponse(second.ProviderResponse!));
        Assert.Equal(SerializeAuditRecord(first.AuditRecord!), SerializeAuditRecord(second.AuditRecord!));
        Assert.Equal(SerializeDiagnostics(first.Diagnostics), SerializeDiagnostics(second.Diagnostics));
        Assert.Equal(definition.ProviderId, first.ProviderResponse.ProviderId);
        Assert.Equal(inputs.GenerationRequest.RequestId, first.ProviderRequest!.GenerationRequestRef);
        Assert.Equal(inputs.ExecutionPlan.ExecutionPlanId, first.ProviderRequest.ExecutionPlanRef);
        Assert.Equal(inputs.NegotiationResult.NegotiationId, first.ProviderRequest.NegotiationResultRef);
        Assert.Contains(first.ProviderRequest.ExecutionConstraints.RequiredCapabilities, capability => capability == "layoutGeneration");
        Assert.Equal(inputs.GenerationRequest.RequestId, first.AuditRecord!.ExecutionRequestLineage.GenerationRequestRef);
        Assert.Equal(inputs.ExecutionPlan.ExecutionPlanId, first.AuditRecord.ExecutionRequestLineage.ExecutionPlanRef);
        Assert.Equal(inputs.NegotiationResult.NegotiationId, first.AuditRecord.NegotiationLineage.NegotiationResultRef);
        Assert.Equal(definition.ProviderId, first.AuditRecord.ProviderLineage.ProviderId);
        Assert.True(first.AuditRecord.ApprovalLineage.DesignApproved);
        Assert.True(first.AuditRecord.ApprovalLineage.GenerationApproved);
        Assert.True(first.AuditRecord.ApprovalLineage.AnalyzerValidationRequired);
    }

    [Fact(DisplayName = "Execution Provider contracts evaluate pending approval as conditionally eligible without execution")]
    public void EvaluateProvider_PendingGenerationApproval_IsConditionallyEligible()
    {
        var service = new ExecutionProviderContractFrameworkService();
        var definition = service.CreateDefaultProviderDefinition();
        var inputs = CreateValidInputs();

        var state = service.EvaluateProvider(
            definition,
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.NegotiationResult,
            CreateApprovalPolicy(designApproved: true, generationApproved: false),
            ExecutionProviderMode.Manual);

        Assert.NotNull(state.ProviderResponse);
        Assert.Equal(ExecutionEligibilityStatus.ConditionallyEligible, state.Eligibility);
        Assert.Equal(ExecutionProviderReadinessState.ConditionallyEligible, state.Readiness);
        Assert.Equal(ExecutionProviderResponseStatus.Rejected, state.ProviderResponse!.Status);
        Assert.Contains("generation approval has not been satisfied.", state.Diagnostics.ApprovalRequirementFailures);
    }

    [Fact(DisplayName = "Execution Provider contracts reject unsupported providers and execution modes")]
    public void EvaluateProvider_UnsupportedProviderAndMode_FailClosed()
    {
        var service = new ExecutionProviderContractFrameworkService();
        var definition = service.CreateDefaultProviderDefinition() with
        {
            SupportedExecutionModes = [ExecutionProviderMode.Assisted],
            SupportedTargetProfiles = [GenerationRequestContract.FabricDataAppDefaultProfile]
        };
        var inputs = CreateValidInputs();

        var state = service.EvaluateProvider(
            definition,
            inputs.GenerationRequest,
            inputs.ExecutionPlan,
            inputs.NegotiationResult,
            CreateApprovalPolicy(designApproved: true, generationApproved: true),
            ExecutionProviderMode.Manual);

        Assert.NotNull(state.ProviderResponse);
        Assert.Equal(ExecutionEligibilityStatus.Ineligible, state.Eligibility);
        Assert.Equal(ExecutionProviderReadinessState.NotEligible, state.Readiness);
        Assert.Equal(ExecutionProviderResponseStatus.Unsupported, state.ProviderResponse!.Status);
        Assert.Contains(GenerationRequestContract.PbirReportDefaultProfile, state.Diagnostics.UnsupportedProviderDefinitions);
        Assert.Contains(nameof(ExecutionProviderMode.Manual), state.Diagnostics.IncompatibleExecutionModes);
    }

    [Fact(DisplayName = "Execution Provider contracts block invalid lineage, invalid approval chains, and contract version mismatches")]
    public void EvaluateProvider_InvalidLineageApprovalChainAndVersions_Block()
    {
        var service = new ExecutionProviderContractFrameworkService();
        var definition = service.CreateDefaultProviderDefinition() with
        {
            SupportedCapabilityNegotiationSchemaVersions = ["capability-negotiation/v2"]
        };
        var inputs = CreateValidInputs();
        var brokenRequest = inputs.GenerationRequest with
        {
            RequestId = "genreq:broken-lineage",
            SchemaVersion = "generation-request/v2"
        };
        var brokenPlan = inputs.ExecutionPlan with
        {
            SourceReferences = inputs.ExecutionPlan.SourceReferences with
            {
                GenerationRequestRef = "genreq:different-request"
            }
        };
        var brokenNegotiation = inputs.NegotiationResult with
        {
            SchemaVersion = "capability-negotiation/v2",
            NegotiationId = "capneg:broken",
            TargetProfileId = GenerationRequestContract.FabricDataAppDefaultProfile
        };

        var state = service.EvaluateProvider(
            definition,
            brokenRequest,
            brokenPlan,
            brokenNegotiation,
            CreateApprovalPolicy(designApproved: false, generationApproved: true),
            ExecutionProviderMode.Manual);

        Assert.NotNull(state.ProviderResponse);
        Assert.Equal(ExecutionEligibilityStatus.Blocked, state.Eligibility);
        Assert.Equal(ExecutionProviderReadinessState.NotEligible, state.Readiness);
        Assert.Equal(ExecutionProviderResponseStatus.Blocked, state.ProviderResponse!.Status);
        Assert.Contains("executionPlan.sourceReferences.generationRequestRef must match generationRequest.requestId.", state.Diagnostics.InvalidLineage);
        Assert.Contains("capabilityNegotiation.targetProfileId must match generationRequest.targetArtifactProfile.profileId.", state.Diagnostics.InvalidLineage);
        Assert.Contains("generation approval cannot be satisfied before design approval.", state.Diagnostics.InvalidApprovalChains);
        Assert.Contains("generation-request/v2", state.Diagnostics.VersionMismatches);
        Assert.Contains("capability-negotiation/v2", state.Diagnostics.VersionMismatches);
    }

    [Fact(DisplayName = "Execution Provider contract inventory covers provider definition, request, response, approval, and audit field paths")]
    public void ExecutionProviderInventory_CoversEveryFieldPath()
    {
        var inventoryPaths = ExecutionProviderContract.RequiredFieldInventory
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var modelPaths = EnumerateFieldPaths(typeof(ExecutionProviderResponse), prefix: null)
            .Concat(EnumerateFieldPaths(typeof(ExecutionProviderRequest), prefix: null))
            .Concat(EnumerateFieldPaths(typeof(ExecutionProviderDefinition), prefix: null))
            .Concat(EnumerateFieldPaths(typeof(ExecutionApprovalPolicy), prefix: null))
            .Concat(EnumerateFieldPaths(typeof(ExecutionAuditRecord), prefix: null))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Subset(modelPaths.ToHashSet(StringComparer.Ordinal), inventoryPaths.ToHashSet(StringComparer.Ordinal));
    }

    [Fact(DisplayName = "Execution Provider contract definitions fail validation when required identity, modes, or versions are missing")]
    public void ValidateProviderDefinition_InvalidProvider_Fails()
    {
        var service = new ExecutionProviderContractFrameworkService();
        var definition = service.CreateDefaultProviderDefinition() with
        {
            ProviderId = "",
            ProviderVersion = "",
            SupportedExecutionModes = [],
            SupportedCapabilityNegotiationSchemaVersions = []
        };

        var validation = service.ValidateProviderDefinition(definition);

        Assert.False(validation.IsValid);
        Assert.Contains("providerDefinition.providerId", validation.Diagnostics.MissingRequiredFields);
        Assert.Contains("providerDefinition.providerVersion", validation.Diagnostics.MissingRequiredFields);
        Assert.Contains("providerDefinition.supportedExecutionModes", validation.Diagnostics.MissingRequiredSections);
        Assert.Contains("providerDefinition.supportedCapabilityNegotiationSchemaVersions", validation.Diagnostics.MissingRequiredSections);
    }

    [Fact(DisplayName = "Execution Provider remains contract-only and exposes no execution, provider invocation, artifact generation, deployment, or analyzer automation surface")]
    public void ExecutionProviderBoundary_RemainsContractOnly()
    {
        var forbiddenTokens = new[] { "Execute", "Invoke", "Api", "Cli", "GenerateArtifact", "Deploy", "AnalyzerRunner", "MicrosoftSkill" };
        Type[] types =
        [
            typeof(ExecutionProviderContractFrameworkService),
            typeof(ExecutionEligibilityService),
            typeof(ExecutionProviderValidator),
            typeof(ExecutionProviderResponse),
            typeof(ExecutionAuditRecord)
        ];

        foreach (var type in types)
        {
            Assert.DoesNotContain(forbiddenTokens, token => type.Name.Contains(token, StringComparison.OrdinalIgnoreCase));

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(forbiddenTokens, token => method.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static (GenerationRequest GenerationRequest, ExecutionPlan ExecutionPlan, CapabilityNegotiationResult NegotiationResult) CreateValidInputs()
    {
        var generationRequest = new GenerationRequestFrameworkService()
            .CreateDraft(new DesignPackageConsumptionService().Consume(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage()))
            .Request!;
        var executionPlan = new ExecutionPlanFrameworkService()
            .CreateDraft(generationRequest)
            .Plan!;
        var adapterRequest = new ProviderAdapterFrameworkService(new ProviderAdapterRegistry(), new ProviderAdapterCompatibilityService())
            .BuildAdapterRequest(generationRequest, executionPlan)
            .Request!;
        var adapterDefinition = new ProviderAdapterDefinition(
            AdapterId: "provider-neutral/layout",
            AdapterName: "Provider Neutral Layout Adapter",
            AdapterVersion: "1.0.0",
            ProviderCategory: ProviderAdapterContract.ProviderNeutralCategory,
            SupportedTargetProfiles: [GenerationRequestContract.PbirReportDefaultProfile, GenerationRequestContract.FabricDataAppDefaultProfile],
            SupportedCapabilities: ["layoutGeneration", "semanticGeneration"],
            UnsupportedCapabilities: ["artifactGeneration", "validation"],
            SupportedGenerationRequestSchemaVersions: [GenerationRequestContract.SchemaVersionV1],
            SupportedExecutionPlanSchemaVersions: [ExecutionPlanContract.SchemaVersionV1]);
        var negotiation = new CapabilityNegotiationService()
            .PrepareForExecutionProvider(
                new CapabilityNegotiationService().Negotiate(
                    generationRequest,
                    executionPlan,
                    adapterRequest,
                    adapterDefinition,
                    new MicrosoftAdapterSpecificationService().CreateDefaultSpecification()))
            .Result!;

        return (generationRequest, executionPlan, negotiation);
    }

    private static ExecutionApprovalPolicy CreateApprovalPolicy(bool designApproved, bool generationApproved)
    {
        return new ExecutionApprovalPolicy(
            DesignApprovalRequired: true,
            GenerationApprovalRequired: true,
            AnalyzerValidationRequired: true,
            DesignApproved: designApproved,
            GenerationApproved: generationApproved);
    }

    private static string SerializeProviderRequest(ExecutionProviderRequest request)
    {
        return string.Join("|",
            request.SchemaVersion,
            request.RequestId,
            request.GenerationRequestRef,
            request.ExecutionPlanRef,
            request.NegotiationResultRef,
            request.SourceContractVersions.GenerationRequestSchemaVersion,
            request.SourceContractVersions.ExecutionPlanSchemaVersion,
            request.SourceContractVersions.CapabilityNegotiationSchemaVersion,
            string.Join(",", request.ExecutionConstraints.RequiredCapabilities),
            request.ExecutionConstraints.RequiredTargetProfileId,
            request.ExecutionConstraints.RequiredProviderCategory,
            request.RequestedExecutionMode.ToString(),
            request.ApprovalPolicy.DesignApproved,
            request.ApprovalPolicy.GenerationApproved);
    }

    private static string SerializeProviderResponse(ExecutionProviderResponse response)
    {
        return string.Join("|",
            response.ProviderId,
            response.RequestId,
            response.Status,
            response.Eligibility,
            response.ReadinessStatus,
            string.Join(",", response.Reasons));
    }

    private static string SerializeAuditRecord(ExecutionAuditRecord auditRecord)
    {
        return string.Join("|",
            auditRecord.ExecutionRequestLineage.GenerationRequestRef,
            auditRecord.ExecutionRequestLineage.ExecutionPlanRef,
            auditRecord.ExecutionRequestLineage.ProviderRequestRef,
            auditRecord.NegotiationLineage.NegotiationResultRef,
            auditRecord.NegotiationLineage.NegotiationSchemaVersion,
            auditRecord.ProviderLineage.ProviderId,
            auditRecord.ProviderLineage.ProviderVersion,
            auditRecord.ProviderLineage.ProviderCategory,
            auditRecord.ApprovalLineage.DesignApprovalRequired,
            auditRecord.ApprovalLineage.GenerationApprovalRequired,
            auditRecord.ApprovalLineage.AnalyzerValidationRequired,
            auditRecord.ApprovalLineage.DesignApproved,
            auditRecord.ApprovalLineage.GenerationApproved);
    }

    private static string SerializeDiagnostics(ExecutionProviderDiagnostics diagnostics)
    {
        return string.Join("|",
            string.Join(",", diagnostics.MissingRequiredSections),
            string.Join(",", diagnostics.MissingRequiredFields),
            string.Join(",", diagnostics.InvalidLineage),
            string.Join(",", diagnostics.InvalidApprovalChains),
            string.Join(",", diagnostics.UnsupportedProviderDefinitions),
            string.Join(",", diagnostics.IncompatibleExecutionModes),
            string.Join(",", diagnostics.VersionMismatches),
            string.Join(",", diagnostics.CapabilityRequirementFailures),
            string.Join(",", diagnostics.ReadinessRequirementFailures),
            string.Join(",", diagnostics.ApprovalRequirementFailures));
    }

    private static IReadOnlyList<string> EnumerateFieldPaths(Type type, string? prefix)
    {
        var fields = new List<string>();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var propertyType = property.PropertyType;
            var name = prefix is null ? property.Name : $"{prefix}.{property.Name}";
            fields.Add(name);

            if (propertyType == typeof(string) || propertyType.IsEnum || propertyType.IsPrimitive)
            {
                continue;
            }

            if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
            {
                if (propertyType.IsGenericType)
                {
                    var elementType = propertyType.GetGenericArguments()[0];
                    if (elementType != typeof(string) && !elementType.IsEnum && !elementType.IsPrimitive)
                    {
                        fields.AddRange(EnumerateFieldPaths(elementType, name));
                    }
                }

                continue;
            }

            fields.AddRange(EnumerateFieldPaths(propertyType, name));
        }

        return fields;
    }
}
