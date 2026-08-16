namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35CResourcePolicyEvaluator
{
    internal Phase35CResourceEvaluation Evaluate(Phase35CResourcePolicy policy) =>
        policy.IsValid ? new(true, []) : new(false, ["resource-policy-invalid-or-unbounded"]);
}
