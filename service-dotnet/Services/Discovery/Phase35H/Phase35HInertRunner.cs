using System.Security.Cryptography;
using System.Text;

namespace PowerBIModelingService.Services.Discovery;

internal sealed record Phase35HRunnerOutput(string Outcome, byte[]? ArtifactBytes, string? FailureCode);

internal sealed class Phase35HInertRunner
{
    internal Phase35HRunnerOutput Run(Phase35HWorkloadType workload, Phase35HRequest request)
    {
        return workload switch
        {
            Phase35HWorkloadType.ReturnSuccess => new("succeeded", null, null),
            Phase35HWorkloadType.ReturnDeterministicHash => new("succeeded:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.ExecutionId))).ToLowerInvariant(), null, null),
            Phase35HWorkloadType.CreateBoundedArtifact => new("candidate", Encoding.UTF8.GetBytes("phase35h-artifact:" + request.ExecutionId), null),
            Phase35HWorkloadType.WaitUntilCancelled or Phase35HWorkloadType.WaitUntilTimeout => new("running", null, null),
            Phase35HWorkloadType.ReturnStructuredFailure => new("failed", null, Phase35HFailureCode.ExecutionFailed.ToString()),
            _ => throw new InvalidOperationException("Unsupported inert workload.")
        };
    }
}
