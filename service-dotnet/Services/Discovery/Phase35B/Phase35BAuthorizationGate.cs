namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35BAuthorizationGate
{
    internal Phase35BAuthorizationDecision Validate(Phase35ARequest request, Phase35AProviderProfile profile, Phase35AAuthorization authorization, Phase35AExecutionPolicy policy)
    {
        var reasons = new List<string>();
        if (authorization.Status != Phase35AAuthorizationStatus.Approved) reasons.Add("authorization is denied");
        if (authorization.RequestId != request.RequestId) reasons.Add("authorization request scope does not match");
        if (authorization.ProviderId != request.ProviderId || authorization.ProviderId != profile.ProviderId) reasons.Add("authorization provider scope does not match");
        if (!authorization.Capabilities.SequenceEqual(request.RequiredCapabilities)) reasons.Add("authorization capability scope does not match");
        if (authorization.ArtifactKind != request.ArtifactKind) reasons.Add("authorization artifact scope does not match");
        if (authorization.PolicyHash != request.PolicyHash) reasons.Add("authorization policy scope does not match");
        return new(reasons.Count == 0, reasons);
    }
}
