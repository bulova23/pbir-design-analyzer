using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35IEvidenceBuilder
{
    internal Phase35IEvidence Build(Phase35IContainmentResult result, Phase35HRequest request, Phase35IWorkerProfile profile, Phase35IRunnerIdentity runner, Phase35IIsolationEvidence isolation, Phase35IJobEvidence job)
    {
        var token = new Phase35ITokenEvidence("unnecessary-privileges-disabled", "administrative-group-removal-not-proven", "restricted-sids-not-added", "integrity-level-not-proven-to-be-VM-isolation");
        var payloadObject = new { schemaVersion = Phase35IContracts.ContractVersion, result, requestHash = Phase35HAuthentication.Hash(request), profile, runner, containmentProfile = Phase35IContracts.ContainmentProfileVersion, restrictedToken = token, job, isolation, phase35HAuditCorrelationHash = result.Phase35HAuditCorrelationHash, proofStatus = result.JobAssigned ? Phase35IProofStatus.ProvenForInertWorkload : Phase35IProofStatus.PartiallyProven };
        var payload = JsonSerializer.Serialize(payloadObject);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return new(payload, hash, result.ExecutionId, result.SessionId, Phase35HAuthentication.Hash(request), profile, runner, Phase35IContracts.ContainmentProfileVersion, token, job, isolation, result.Phase35HAuditCorrelationHash, payloadObject.proofStatus);
    }
}
