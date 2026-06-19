using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class DesignPackageGenerationService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    internal DesignPackage CreatePackage(
        DiscoveryProfile profile,
        OpportunityCatalog catalog,
        RecommendationSet recommendations,
        string selectedRecommendationId)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(recommendations);

        if (string.IsNullOrWhiteSpace(selectedRecommendationId))
        {
            throw new ArgumentException("A selected recommendation identifier is required.", nameof(selectedRecommendationId));
        }

        var recommendation = recommendations.PrimaryRecommendations
            .Concat(recommendations.AlternateRecommendations)
            .FirstOrDefault(candidate => string.Equals(
                candidate.RecommendationId,
                selectedRecommendationId,
                StringComparison.Ordinal));
        if (recommendation is null)
        {
            throw new InvalidOperationException($"Recommendation '{selectedRecommendationId}' was not found.");
        }

        var blueprint = recommendation.ExperienceBlueprint
            ?? throw new InvalidOperationException("Design Package generation requires an attached Experience Blueprint.");
        var opportunity = catalog.Opportunities.FirstOrDefault(candidate =>
            string.Equals(candidate.OpportunityId, blueprint.Provenance.OpportunityId, StringComparison.Ordinal))
            ?? catalog.Opportunities.FirstOrDefault(candidate =>
                string.Equals(candidate.OpportunityId, recommendation.RecommendationId, StringComparison.Ordinal));

        var lineage = BuildLineage(profile, recommendation, blueprint, opportunity);
        var packageId = $"designPackage:{recommendation.RecommendationId}";

        return new DesignPackage(
            PackageId: packageId,
            DiscoveryContext: BuildDiscoveryContext(lineage),
            Audience: BuildAudience(profile, recommendation, opportunity),
            ExperienceDefinition: new DesignPackageExperienceDefinition(
                ExperienceType: recommendation.RecommendedExperienceType,
                BusinessOutcome: recommendation.ExpectedBusinessOutcome,
                Confidence: recommendation.Confidence,
                BusinessValue: recommendation.BusinessValue,
                Complexity: recommendation.ImplementationComplexity),
            Pages: BuildPages(blueprint),
            Kpis: BuildKpis(profile, recommendation, blueprint),
            Filters: BuildFilters(blueprint),
            VisualRecommendations: BuildVisualRecommendations(blueprint),
            Navigation: BuildNavigation(blueprint),
            AnalyticalFlow: new DesignPackageAnalyticalFlow(
                Question: blueprint.AnalyticalFlow.Question,
                Investigation: blueprint.AnalyticalFlow.Investigation,
                Evidence: blueprint.AnalyticalFlow.Evidence,
                Decision: blueprint.AnalyticalFlow.Decision),
            SuccessCriteria: BuildSuccessCriteria(recommendation, blueprint),
            RecommendationRationale: new DesignPackageRecommendationRationale(
                RecommendationExplanation: recommendation.WhyWeRecommendIt,
                SupportingSemanticSignals: recommendation.SupportingSignals.ToList(),
                LimitingFactors: recommendation.LimitingFactors.ToList(),
                AudienceRationale: BuildAudienceRationale(recommendation, profile, opportunity),
                BusinessOutcomeRationale: BuildBusinessOutcomeRationale(recommendation),
                KpiRationale: BuildKpiRationale(recommendation, blueprint),
                PageRationale: BuildPageRationale(blueprint),
                NavigationRationale: BuildNavigationRationale(blueprint),
                AnalyticalFlowRationale: BuildAnalyticalFlowRationale(blueprint),
                ProvenanceNotes: BuildProvenanceNotes(blueprint)),
            Provenance: new DesignPackageProvenance(
                PackageReference: packageId,
                Lineage: lineage.Concat(
                [
                    new DesignPackageReference("designPackage", packageId, recommendation.RecommendationName)
                ]).ToArray()));
    }

    private static IReadOnlyList<DesignPackageReference> BuildLineage(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint,
        OpportunityCandidate? opportunity)
    {
        return
        [
            new("semanticModel", profile.SemanticModelReferenceId, "Semantic model source"),
            new("discoveryProfile", profile.DiscoveryProfileReferenceId, "Discovery Profile"),
            new("opportunity", opportunity?.OpportunityId ?? blueprint.Provenance.OpportunityId, opportunity?.Name ?? "Opportunity"),
            new("recommendation", recommendation.RecommendationId, recommendation.RecommendationName),
            new("experienceBlueprint", blueprint.BlueprintId, recommendation.RecommendationName),
        ];
    }

    private static DesignPackageDiscoveryContext BuildDiscoveryContext(IReadOnlyList<DesignPackageReference> lineage)
    {
        return new DesignPackageDiscoveryContext(
            SemanticModelSource: lineage[0],
            DiscoveryProfileReference: lineage[1],
            OpportunityReference: lineage[2],
            RecommendationReference: lineage[3],
            ExperienceBlueprintReference: lineage[4]);
    }

    private static DesignPackageAudience BuildAudience(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        var primaryAudience = recommendation.ExpectedAudience;
        var secondaryAudiences = profile.AudienceSignals
            .Select(signal => signal.Audience)
            .Concat(opportunity is null ? [] : [opportunity.InferredAudience])
            .Where(audience => !string.IsNullOrWhiteSpace(audience))
            .Distinct(NameComparer)
            .Where(audience => !string.Equals(audience, primaryAudience, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var personas = new List<DesignPackagePersona>
        {
            new(
                Name: primaryAudience,
                Role: "primary",
                Perspective: $"Primary decision-maker for {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}.")
        };

        personas.AddRange(secondaryAudiences.Select(audience => new DesignPackagePersona(
            Name: audience,
            Role: "secondary",
            Perspective: $"Secondary consumer supporting {recommendation.RecommendationName.ToLowerInvariant()}.")));

        return new DesignPackageAudience(
            PrimaryAudience: primaryAudience,
            SecondaryAudiences: secondaryAudiences,
            Personas: personas);
    }

    private static IReadOnlyList<DesignPackagePage> BuildPages(ExperienceBlueprint blueprint)
    {
        return blueprint.RecommendedPages
            .Select((page, index) => new DesignPackagePage(
                PageName: page.PageName,
                PagePurpose: page.PageIntent,
                NavigationIntent: DescribePageNavigationIntent(index, blueprint.RecommendedPages.Count, page.PageName)))
            .ToArray();
    }

    private static IReadOnlyList<DesignPackageKpi> BuildKpis(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint)
    {
        return blueprint.PrimaryKpis
            .Select(kpi => new DesignPackageKpi(
                Name: kpi,
                Purpose: $"Supports {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()} decisions.",
                Grouping: ResolveKpiGrouping(profile, kpi)))
            .ToArray();
    }

    private static DesignPackageFilterSet BuildFilters(ExperienceBlueprint blueprint)
    {
        return new DesignPackageFilterSet(
            GlobalFilters: blueprint.SuggestedGlobalFilters.ToList(),
            PageFilters: blueprint.RecommendedPages
                .Select(page => new DesignPackagePageFilter(
                    PageName: page.PageName,
                    Filters: page.SuggestedFilters.ToList()))
                .ToArray());
    }

    private static IReadOnlyList<DesignPackageVisualRecommendation> BuildVisualRecommendations(ExperienceBlueprint blueprint)
    {
        return blueprint.RecommendedPages
            .SelectMany(page => page.SuggestedVisualTypes.Select(visual => new DesignPackageVisualRecommendation(
                PageName: page.PageName,
                VisualType: visual,
                VisualPurpose: $"Supports {page.PageIntent.ToLowerInvariant()}")))
            .ToArray();
    }

    private static DesignPackageNavigation BuildNavigation(ExperienceBlueprint blueprint)
    {
        return new DesignPackageNavigation(
            Hierarchy: blueprint.RecommendedPages.Select(page => page.PageName).ToArray(),
            WorkflowPath: blueprint.NavigationIntent.Sequence.ToArray());
    }

    private static DesignPackageSuccessCriteria BuildSuccessCriteria(
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint)
    {
        var businessCriteria = blueprint.SuccessCriteriaSeed
            .Concat([recommendation.ExpectedBusinessOutcome])
            .Distinct(NameComparer)
            .ToArray();
        var analyticalCriteria = new[]
        {
            $"Follow the workflow path: {string.Join(" -> ", blueprint.NavigationIntent.Sequence)}.",
            $"Support the analytical chain from question to decision across {blueprint.RecommendedPages.Count} pages.",
            $"Keep the primary KPI emphasis on {string.Join(", ", blueprint.PrimaryKpis.Take(3))}."
        };

        return new DesignPackageSuccessCriteria(
            BusinessSuccessCriteria: businessCriteria,
            AnalyticalSuccessCriteria: analyticalCriteria);
    }

    private static string DescribePageNavigationIntent(int index, int pageCount, string pageName)
    {
        if (index == 0)
        {
            return $"Entry page for {pageName}.";
        }

        if (index == pageCount - 1)
        {
            return $"Decision page for {pageName}.";
        }

        return $"Supporting navigation step for {pageName}.";
    }

    private static string ResolveKpiGrouping(DiscoveryProfile profile, string kpi)
    {
        var cluster = profile.KpiClusters.FirstOrDefault(candidate =>
            candidate.MeasureNames.Any(measure => string.Equals(measure, kpi, StringComparison.OrdinalIgnoreCase)));
        return cluster?.ClusterName ?? "Primary KPI Set";
    }

    private static string BuildAudienceRationale(
        DiscoveryRecommendation recommendation,
        DiscoveryProfile profile,
        OpportunityCandidate? opportunity)
    {
        var secondaryAudiences = profile.AudienceSignals
            .Select(signal => signal.Audience)
            .Where(audience => !string.Equals(audience, recommendation.ExpectedAudience, StringComparison.OrdinalIgnoreCase))
            .Distinct(NameComparer)
            .ToList();
        var supportingAudience = opportunity?.InferredAudience is not null &&
            !string.Equals(opportunity.InferredAudience, recommendation.ExpectedAudience, StringComparison.OrdinalIgnoreCase)
                ? $", with {opportunity.InferredAudience} as a secondary consumer"
                : string.Empty;

        return secondaryAudiences.Count > 0
            ? $"{recommendation.ExpectedAudience} is the primary audience because the discovery signals point to a {recommendation.RecommendedExperienceType} experience for this decision cadence{supportingAudience}. Supporting audiences include {string.Join(", ", secondaryAudiences.Take(2))}."
            : $"{recommendation.ExpectedAudience} is the primary audience because the discovery signals point to a {recommendation.RecommendedExperienceType} experience for this decision cadence{supportingAudience}.";
    }

    private static string BuildBusinessOutcomeRationale(DiscoveryRecommendation recommendation)
    {
        return $"The experience is recommended because {recommendation.ExpectedAudience} needs a delivery shape that can {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()} without changing the underlying semantic-model story.";
    }

    private static IReadOnlyList<string> BuildKpiRationale(
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint)
    {
        return blueprint.PrimaryKpis
            .Select(kpi => $"{kpi} stays in the primary set because it helps {recommendation.ExpectedAudience.ToLowerInvariant()} make the decision implied by {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}.")
            .ToArray();
    }

    private static IReadOnlyList<string> BuildPageRationale(ExperienceBlueprint blueprint)
    {
        return blueprint.RecommendedPages
            .Select((page, index) => $"{page.PageName} belongs in the experience because it {page.PageIntent.ToLowerInvariant()} and it serves as {DescribePageContribution(index, blueprint.RecommendedPages.Count, page.PageName)}.")
            .ToArray();
    }

    private static string BuildNavigationRationale(ExperienceBlueprint blueprint)
    {
        var pageNames = blueprint.RecommendedPages.Select(page => page.PageName).ToList();
        var routeHint = pageNames.Any(name => name.Contains("Routing", StringComparison.OrdinalIgnoreCase))
            ? " The page sequence also preserves route and handoff context that would be lost in a flatter dashboard."
            : string.Empty;

        return $"The navigation is organized this way because the workflow {blueprint.NavigationIntent.Flow} lets users move through the recommended {blueprint.RecommendedPages.Count}-page analytical path without reworking the baseline information architecture.{routeHint}";
    }

    private static string BuildAnalyticalFlowRationale(ExperienceBlueprint blueprint)
    {
        return $"The analytical flow is arranged this way because it starts with the question '{blueprint.AnalyticalFlow.Question}', moves through investigation and evidence, and ends with the decision '{blueprint.AnalyticalFlow.Decision}' so the final recommendation is defensible instead of impressionistic.";
    }

    private static IReadOnlyList<string> BuildProvenanceNotes(ExperienceBlueprint blueprint)
    {
        return
        [
            $"Semantic model reference: {blueprint.Provenance.SemanticModelReferenceId}",
            $"Discovery profile reference: {blueprint.Provenance.DiscoveryProfileReferenceId}",
            $"Semantic evidence: {string.Join(", ", blueprint.Provenance.SemanticEvidenceReferences.Take(4))}",
            $"Influencing structures: {string.Join(", ", blueprint.Provenance.InfluencingModelStructures.Take(4))}"
        ];
    }

    private static string DescribePageContribution(int index, int pageCount, string pageName)
    {
        if (index == 0)
        {
            return $"the opening decision frame for {pageName}";
        }

        if (index == pageCount - 1)
        {
            return $"the closing action checkpoint for {pageName}";
        }

        return $"the middle transition that keeps the story moving toward {pageName}";
    }
}
