namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35DConformanceRunner
{
    internal Phase35DProviderConformanceResult Run(IPhase35BProviderAdapter adapter, Phase35CProviderIdentity identity, Phase35ARequest request, Phase35CConformanceEvidence evidence, Phase35COutputCorpusFixture fixture, IReadOnlyList<string> outputProperties)
    {
        var runtime = new Phase35CConformanceEvaluator().Evaluate(adapter, identity, request, evidence);
        var output = new Phase35COutputValidationEvaluator().Evaluate(fixture, outputProperties);
        var reasons = new List<string>();
        if (!runtime.IsConformant) reasons.Add("runtime-conformance-failed");
        if (!output.IsValid) reasons.Add("output-corpus-failed");
        return new(reasons.Count == 0, runtime, output, reasons);
    }
}
