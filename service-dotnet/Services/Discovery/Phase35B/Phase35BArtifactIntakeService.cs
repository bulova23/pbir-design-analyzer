namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BArtifactIntakeService
{
    internal Phase35BArtifactReview Review(Phase35AArtifact artifact, Phase35AResult result)
    {
        if (artifact.Quarantine.Reason != Phase35AQuarantineReason.None || !artifact.Quarantine.ReleaseEligible)
            return new(Phase35BArtifactDisposition.Quarantined, ["artifact quarantine policy applies"], artifact);
        var validation = new Phase35AContractValidator().Validate(artifact, result);
        if (!validation.IsValid)
            return new(Phase35BArtifactDisposition.Rejected, validation.InvalidReferences.Concat(validation.PolicyViolations).Concat(validation.InvalidValues).ToArray(), artifact);
        return new(Phase35BArtifactDisposition.Accepted, [], artifact);
    }
}
