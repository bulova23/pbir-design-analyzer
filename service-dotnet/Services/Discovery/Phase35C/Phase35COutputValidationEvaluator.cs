namespace PowerBIModelingService.Services.Discovery;

internal sealed class Phase35COutputValidationEvaluator
{
    internal Phase35COutputValidationResult Evaluate(Phase35COutputCorpusFixture fixture, IReadOnlyList<string> properties)
    {
        var reasons = new List<string>();
        if (!fixture.RequiredProperties.All(properties.Contains)) reasons.Add("required-property-missing");
        if (fixture.ForbiddenProperties.Any(properties.Contains)) reasons.Add("forbidden-property-present");
        var valid = reasons.Count == 0;
        if ((fixture.ExpectedOutcome == Phase35CExpectedValidationOutcome.Valid) != valid) reasons.Add("expected-outcome-mismatch");
        return new(reasons.Count == 0, reasons);
    }
}
