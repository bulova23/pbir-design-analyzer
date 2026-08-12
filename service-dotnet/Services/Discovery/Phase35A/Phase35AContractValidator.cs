namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35AContractValidator
{
    internal Phase35AValidation Validate(Phase35ARequest request, Phase35AProviderProfile profile)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(profile);
        var schemas = new List<string>();
        var capabilities = new List<string>();
        var refs = new List<string>();
        var policies = new List<string>();
        var values = new List<string>();
        if (request.SchemaVersion != Phase35AContracts.RequestV1) schemas.Add(request.SchemaVersion);
        if (profile.SchemaVersion != Phase35AContracts.ProviderProfileV1) schemas.Add(profile.SchemaVersion);
        ValidateEnums(request.RequiredCapabilities, capabilities);
        ValidateEnums(profile.Capabilities, capabilities);
        if (request.RequiredCapabilities.Any(capability => !profile.Capabilities.Contains(capability))) capabilities.AddRange(request.RequiredCapabilities.Where(capability => !profile.Capabilities.Contains(capability)).Select(capability => capability.ToString()));
        if (string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.ProviderId) || request.ProviderId != profile.ProviderId) refs.Add("request provider identity does not match profile");
        if (!Phase35ACanonicalJson.IsHash(request.AuthoritativeInputHash) && !request.AuthoritativeInputHash.StartsWith("hash:", StringComparison.Ordinal)) values.Add("authoritativeInputHash");
        if (!Phase35ACanonicalJson.IsHash(request.PolicyHash) && !request.PolicyHash.StartsWith("hash:", StringComparison.Ordinal)) values.Add("policyHash");
        return new(schemas.Distinct(StringComparer.Ordinal).ToArray(), capabilities.Distinct(StringComparer.Ordinal).ToArray(), refs, policies, values);
    }

    internal Phase35AValidation Validate(Phase35AArtifact artifact, Phase35AResult result)
    {
        var policies = new List<string>();
        var refs = new List<string>();
        if (artifact.SchemaVersion != Phase35AContracts.ArtifactV1 || result.SchemaVersion != Phase35AContracts.ResultV1) refs.Add("artifact/result schema version");
        if (artifact.ResultId != result.ResultId || artifact.RequestId != result.RequestId) refs.Add("artifact/result lineage mismatch");
        if (artifact.Quarantine.Reason != Phase35AQuarantineReason.None || !artifact.Quarantine.ReleaseEligible) policies.Add("quarantined artifact is not eligible for acceptance");
        if (artifact.Validation != Phase35AValidationStatus.Valid) policies.Add("artifact is not validated");
        if (!Phase35ACanonicalJson.IsHash(artifact.ContentHash) && !artifact.ContentHash.StartsWith("hash:", StringComparison.Ordinal)) policies.Add("artifact content hash is invalid");
        return new([], [], refs, policies, []);
    }

    internal Phase35AValidation Validate(Phase35AFailure failure, Phase35ARetryPolicy retryPolicy)
    {
        var values = new List<string>();
        if (failure.SchemaVersion != Phase35AContracts.FailureV1 || retryPolicy.SchemaVersion != Phase35AContracts.RetryV1) values.Add("schemaVersion");
        if (string.IsNullOrWhiteSpace(failure.Code) || string.IsNullOrWhiteSpace(failure.Message)) values.Add("failure identity");
        if (failure.Retryable != retryPolicy.IsRetryable(failure.Class)) values.Add("retry classification");
        return new([], [], [], [], values);
    }

    private static void ValidateEnums<T>(IEnumerable<T> values, ICollection<string> failures) where T : struct, Enum
    {
        foreach (var value in values)
        {
            if (!Enum.IsDefined(value)) failures.Add(value.ToString());
        }
    }
}
