namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35ESandboxEvidenceCollector
{
    internal Phase35ESandboxEvidence Collect(Phase35ESandboxResult result, Phase35EPolicyBinding binding)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new { result.SessionId, result.ProviderId, result.ImplementationId, result.PackageId, result.CertificationId, result.SandboxVersion, result.Platform, result.ExitClassification, result.Failure, result.StdoutBytes, result.StderrBytes, result.Violations, result.CleanupSucceeded, binding });
        return new(payload, Phase35EHashing.Hash(payload));
    }
}

internal sealed class Phase35EAuditProjector(Phase35CDurableAuditStore audit)
{
    internal void Append(string sessionId, string providerId, string requestHash, string name, string outcome) => audit.Append(new Phase35CAuditEvent(sessionId, providerId, name, requestHash, outcome));
}
