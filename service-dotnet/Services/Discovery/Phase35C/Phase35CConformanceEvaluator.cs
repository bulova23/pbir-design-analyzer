namespace PowerBIModelingService.Services.Discovery;

internal enum Phase35CConformanceViolation { IdentityUnstable, CapabilityMismatch, ReadinessMismatch, RequestValidationFailure, CancellationIgnored, FailureMappingInvalid, ArtifactLineageInvalid, ArtifactUnclassified, AuditNotEmitted, SecretLeakage }
internal sealed record Phase35CConformanceResult(bool IsConformant, IReadOnlyList<Phase35CConformanceViolation> Violations);

internal sealed class Phase35CConformanceEvaluator
{
    internal Phase35CConformanceResult Evaluate(IPhase35BProviderAdapter adapter, Phase35CProviderIdentity identity, Phase35ARequest request)
        => Evaluate(adapter, identity, request, new Phase35CConformanceEvidence(true, true, true, true, true, true));

    internal Phase35CConformanceResult Evaluate(IPhase35BProviderAdapter adapter, Phase35CProviderIdentity identity, Phase35ARequest request, Phase35CConformanceEvidence evidence)
    {
        var violations = new List<Phase35CConformanceViolation>();
        if (adapter.ProviderId != identity.ProviderId) violations.Add(Phase35CConformanceViolation.IdentityUnstable);
        if (!identity.Capabilities.Order().SequenceEqual(adapter.Capabilities.Order())) violations.Add(Phase35CConformanceViolation.CapabilityMismatch);
        var readiness = adapter.DeclareReadiness(request);
        if (readiness.SchemaVersion != Phase35AContracts.ReadinessV1) violations.Add(Phase35CConformanceViolation.ReadinessMismatch);
        if (!adapter.ValidateRequest(request).IsValid) violations.Add(Phase35CConformanceViolation.RequestValidationFailure);
        if (!evidence.CancellationObserved) violations.Add(Phase35CConformanceViolation.CancellationIgnored);
        if (!evidence.FailureMappingDeterministic) violations.Add(Phase35CConformanceViolation.FailureMappingInvalid);
        if (!evidence.ArtifactLineageValid) violations.Add(Phase35CConformanceViolation.ArtifactLineageInvalid);
        if (!evidence.ArtifactClassified) violations.Add(Phase35CConformanceViolation.ArtifactUnclassified);
        if (!evidence.AuditEmitted) violations.Add(Phase35CConformanceViolation.AuditNotEmitted);
        if (!evidence.SecretFreeDiagnostics) violations.Add(Phase35CConformanceViolation.SecretLeakage);
        return new(violations.Count == 0, violations);
    }
}
