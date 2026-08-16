using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase35AContractFoundationTests
{
    [Fact]
    public void ContractValidator_RejectsUnknownSchemaAndCapability()
    {
        var request = Phase35ATestData.Request with
        {
            SchemaVersion = "phase35a-generation-request/v2",
            RequiredCapabilities = [(Phase35ACapability)999]
        };

        var validation = new Phase35AContractValidator().Validate(request, Phase35ATestData.Profile);

        Assert.False(validation.IsValid);
        Assert.Contains("phase35a-generation-request/v2", validation.UnsupportedSchemaVersions);
        Assert.Contains("999", validation.UnsupportedCapabilities);
    }

    [Fact]
    public void CanonicalHash_IsStableAndSensitiveToAuthoritativeRequest()
    {
        var canonical = new Phase35ACanonicalJson();

        Assert.Equal(canonical.Hash(Phase35ATestData.Request), canonical.Hash(Phase35ATestData.Request));
        Assert.NotEqual(canonical.Hash(Phase35ATestData.Request), canonical.Hash(Phase35ATestData.Request with { IntentReference = "intent:changed" }));
    }

    [Fact]
    public void Readiness_FailsClosedForEveryRegisteredCurrentSurface()
    {
        var evaluator = new Phase35AReadinessEvaluator();

        foreach (var profile in Phase35AProviderCatalog.All)
        {
            var result = evaluator.Evaluate(Phase35ATestData.Request, profile, Phase35ATestData.AuthorizationDenied, Phase35ATestData.Policy);
            Assert.Equal(Phase35AReadiness.Unavailable, result.State);
            Assert.Contains("runtime generation provider is available", string.Join("|", result.Reasons), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Lifecycle_RejectsInvalidTransition()
    {
        Assert.Equal(Phase35ALifecycleState.Authorized, Phase35ALifecycle.Transition(
            Phase35ALifecycleState.Requested, Phase35ALifecycleEvent.AuthorizationApproved));
        Assert.Throws<Phase35AContractException>(() => Phase35ALifecycle.Transition(
            Phase35ALifecycleState.Requested, Phase35ALifecycleEvent.ExecutionCompleted));
    }

    [Fact]
    public void AuthorizationAndArtifactValidation_FailClosedForRedactedQuarantinedOutput()
    {
        var artifact = Phase35ATestData.Artifact with
        {
            Quarantine = new Phase35AQuarantine(Phase35AQuarantineReason.PolicyViolation, "policy:blocked", false),
            Redaction = new Phase35ARedaction(Phase35AContracts.RedactionV1, Phase35ARedactionStatus.Applied, "secret omitted", "hash:original")
        };

        var validation = new Phase35AContractValidator().Validate(artifact, Phase35ATestData.Result);

        Assert.False(validation.IsValid);
        Assert.Contains("quarantined", string.Join("|", validation.PolicyViolations), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JsonSerialization_RejectsIntegerEnumValues()
    {
        Assert.Throws<JsonException>(() => new Phase35ACanonicalJson().Deserialize<Phase35AProviderProfile>(
            "{\"schemaVersion\":\"phase35a-provider-profile/v1\",\"providerId\":\"x\",\"displayName\":\"x\",\"category\":99,\"executionClass\":\"nonExecutable\",\"trust\":\"untrusted\",\"capabilities\":[],\"artifactKinds\":[],\"readinessRequirements\":[]}"u8.ToArray()));
    }

    [Fact]
    public void ProviderCatalog_UsesStableClosedIdentities()
    {
        Assert.Equal(new[] { "microsoft-skills.metadata", "offline.reference-materializer", "powerbi-desktop", "powerbi-modeling-mcp", "powerbi-report-author@0.1.4" },
            Phase35AProviderCatalog.All.Select(profile => profile.ProviderId).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Assert.Equal(Phase35AExecutionClass.NonExecutable, Phase35AProviderCatalog.All.Single(profile => profile.ProviderId == "powerbi-report-author@0.1.4").ExecutionClass);
    }

    [Fact]
    public void RetryPolicy_SeparatesRetryableAndNonRetryableFailures()
    {
        var policy = new Phase35ARetryPolicy(Phase35AContracts.RetryV1, 3, [Phase35AFailureClass.ProviderFailure]);
        var retryable = new Phase35AFailure(Phase35AContracts.FailureV1, Phase35AFailureClass.ProviderFailure, "provider.transient", "transient", true);
        var permanent = retryable with { Class = Phase35AFailureClass.Authorization, Retryable = false };

        Assert.True(policy.IsRetryable(retryable.Class));
        Assert.False(policy.IsRetryable(permanent.Class));
        Assert.True(new Phase35AContractValidator().Validate(retryable, policy).IsValid);
        Assert.False(new Phase35AContractValidator().Validate(permanent with { Retryable = true }, policy).IsValid);
    }

    [Fact]
    public void RequestProjector_UsesOnlyAuthoritativeProviderRequest()
    {
        var planning = new PlanningOrchestrationService().Orchestrate(GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage());
        var specification = new PbirGenerationSpecificationService().CreateSpecification(planning);
        var state = new GenerationProviderFrameworkService().CreateProviderState(
            new PbirGenerationSpecificationService().PrepareForGenerationProvider(specification));

        var projector = new Phase35ARequestProjector();
        var first = projector.Project(state);
        var second = projector.Project(state);

        Assert.Equal(new Phase35ACanonicalJson().Hash(first), new Phase35ACanonicalJson().Hash(second));
        Assert.Equal(state.Request!.Metadata.RequestId, first.RequestId);
        Assert.Equal(state.Provider!.ProviderId, first.ProviderId);
        Assert.DoesNotContain("command", System.Text.Encoding.UTF8.GetString(new Phase35ACanonicalJson().Serialize(first)));
    }

    [Fact]
    public void Lineage_ConnectsRequestAndResultHashes()
    {
        var lineage = Phase35ALineage.From(Phase35ATestData.Request, Phase35ATestData.Result);

        Assert.Equal(Phase35ATestData.Request.RequestId, lineage.RequestId);
        Assert.Equal(Phase35ATestData.Result.ResultId, lineage.ResultId);
        Assert.Equal(new Phase35ACanonicalJson().Hash(Phase35ATestData.Request), lineage.RequestHash);
        Assert.Equal(new Phase35ACanonicalJson().Hash(Phase35ATestData.Result), lineage.ResultHash);
    }

    private static class Phase35ATestData
    {
        internal static readonly Phase35AProviderProfile Profile = new(
            Phase35AContracts.ProviderProfileV1, "test.offline", "Offline Test Metadata", Phase35AProviderCategory.OfflineTest,
            Phase35AExecutionClass.NonExecutable, Phase35ATrustClassification.Untrusted, [Phase35ACapability.PbirValidation],
            [Phase35AArtifactKind.PbirReport], [Phase35AReadinessRequirement.ExplicitExecutableRegistration]);

        internal static readonly Phase35ARequest Request = new(
            Phase35AContracts.RequestV1, "request:stable", "intent:business", ["state:authoritative"],
            [Phase35ACapability.PbirValidation], Phase35AArtifactKind.PbirReport, Profile.ProviderId,
            "hash:authoritative", "hash:policy");

        internal static readonly Phase35AExecutionPolicy Policy = Phase35AExecutionPolicy.Denied;
        internal static readonly Phase35AAuthorization AuthorizationDenied = Phase35AAuthorization.Denied;
        internal static readonly Phase35AResult Result = new(
            Phase35AContracts.ResultV1, "result:stable", Request.RequestId, Phase35AResultStatus.Rejected,
            [new Phase35AFailure(Phase35AContracts.FailureV1, Phase35AFailureClass.Authorization, "authorization.denied", "Denied", false)]);
        internal static readonly Phase35AArtifact Artifact = new(
            Phase35AContracts.ArtifactV1, "artifact:stable", Phase35AArtifactKind.PbirReport, Request.RequestId,
            Profile.ProviderId, Phase35AContracts.ContractVersion, "hash:content", Result.ResultId,
            Phase35ALineage.From(Request, Result), Phase35AValidationStatus.Valid, Phase35AQuarantine.None, Phase35ARedaction.None);
    }
}
