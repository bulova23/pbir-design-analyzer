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
            Pages: BuildPages(recommendation, blueprint),
            Kpis: BuildKpis(profile, recommendation, blueprint),
            Filters: BuildFilters(blueprint),
            VisualRecommendations: BuildVisualRecommendations(blueprint),
            Navigation: BuildNavigation(blueprint),
            AnalyticalFlow: new DesignPackageAnalyticalFlow(
                Question: blueprint.AnalyticalFlow.Question,
                Investigation: blueprint.AnalyticalFlow.Investigation,
                Evidence: blueprint.AnalyticalFlow.Evidence,
                Decision: blueprint.AnalyticalFlow.Decision),
            SuccessCriteria: BuildSuccessCriteria(recommendation, opportunity, blueprint),
            RecommendationRationale: new DesignPackageRecommendationRationale(
                RecommendationExplanation: recommendation.WhyWeRecommendIt,
                SupportingSemanticSignals: recommendation.SupportingSignals.ToList(),
                LimitingFactors: recommendation.LimitingFactors.ToList(),
                AudienceRationale: BuildAudienceRationale(recommendation, profile, opportunity),
                BusinessOutcomeRationale: BuildBusinessOutcomeRationale(recommendation),
                ExperienceTypeRationale: BuildExperienceTypeRationale(recommendation, opportunity, blueprint),
                KpiRationale: BuildKpiRationale(recommendation, blueprint),
                PageRationale: BuildPageRationale(blueprint),
                NavigationRationale: BuildNavigationRationale(recommendation, blueprint),
                AnalyticalFlowRationale: BuildAnalyticalFlowRationale(blueprint),
                ProvenanceNotes: BuildProvenanceNotes(blueprint)),
            ProviderGuidance: BuildProviderGuidance(recommendation, opportunity, blueprint),
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

    private static IReadOnlyList<DesignPackagePage> BuildPages(
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint)
    {
        return blueprint.RecommendedPages
            .Select((page, index) => new DesignPackagePage(
                PageName: page.PageName,
                PagePurpose: $"Exists to {page.PageIntent.ToLowerInvariant()} so {recommendation.ExpectedAudience.ToLowerInvariant()} can keep the intended decision path intact.",
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
                Purpose: BuildKpiPurpose(recommendation, kpi),
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
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint)
    {
        var cadence = InferDecisionCadence(recommendation, opportunity);
        var businessCriteria = blueprint.SuccessCriteriaSeed
            .Concat([recommendation.ExpectedBusinessOutcome])
            .Concat([$"{recommendation.ExpectedAudience} can use the experience confidently within the expected {cadence.ToLowerInvariant()} review rhythm."])
            .Distinct(NameComparer)
            .ToArray();
        var analyticalCriteria = new[]
        {
            $"Follow the workflow path: {string.Join(" -> ", blueprint.NavigationIntent.Sequence)}.",
            $"Support the analytical chain from question to decision across {blueprint.RecommendedPages.Count} pages.",
            $"Keep the primary KPI emphasis on {string.Join(", ", blueprint.PrimaryKpis.Take(3))}.",
            $"Preserve the filter context of {string.Join(", ", blueprint.SuggestedGlobalFilters.Take(3))} so the provider does not have to reconstruct the intended decision scope."
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
            ? $"{recommendation.ExpectedAudience} is the primary audience because the discovery signals point to a {GetExperienceTypeLabel(recommendation.RecommendedExperienceType)} for a {InferDecisionCadence(recommendation, opportunity).ToLowerInvariant()} decision cadence{supportingAudience}. Supporting audiences include {string.Join(", ", secondaryAudiences.Take(2))}, but the package still optimizes for the primary decision-maker first."
            : $"{recommendation.ExpectedAudience} is the primary audience because the discovery signals point to a {GetExperienceTypeLabel(recommendation.RecommendedExperienceType)} for a {InferDecisionCadence(recommendation, opportunity).ToLowerInvariant()} decision cadence{supportingAudience}.";
    }

    private static string BuildBusinessOutcomeRationale(DiscoveryRecommendation recommendation)
    {
        return $"The experience is recommended because {recommendation.ExpectedAudience} needs a delivery shape that can {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()} without losing the business story that led to the recommendation, and because the package must preserve why the decision exists rather than only what content appears on pages.";
    }

    private static string BuildExperienceTypeRationale(
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint)
    {
        var cadence = InferDecisionCadence(recommendation, opportunity).ToLowerInvariant();
        return recommendation.RecommendedExperienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => $"The package stays dashboard-oriented because {recommendation.ExpectedAudience} needs a fast {cadence} scan of KPI movement, variance, and leadership actions before any deeper follow-up path branches off.",
            OpportunityExperienceType.OperationalMonitoringExperience => $"The package stays operational-monitoring-oriented because the primary value is repeated queue, exception, and next-action visibility across {blueprint.RecommendedPages.Count} focused pages, not a slower leadership narrative.",
            OpportunityExperienceType.AnalyticalInvestigationExperience => $"The package stays investigation-oriented because the business question requires a slower question-to-evidence-to-decision path instead of a compressed dashboard readout.",
            OpportunityExperienceType.FabricApp => $"The package stays app-oriented because the workflow needs owners to move between coordination, follow-up, and confirmation inside one guided experience rather than across disconnected pages.",
            OpportunityExperienceType.FabricDataApp => $"The package stays data-app-oriented because users need to pivot across segments and records before the final decision pattern should be fixed.",
            _ => $"The package stays report-oriented because a staged narrative path is the clearest way to walk users from context to evidence to the final decision."
        };
    }

    private static IReadOnlyList<string> BuildKpiRationale(
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint)
    {
        return blueprint.PrimaryKpis
            .Select(kpi => $"{kpi} stays in the primary set because {BuildKpiDecisionReason(recommendation, kpi)}")
            .ToArray();
    }

    private static IReadOnlyList<string> BuildPageRationale(ExperienceBlueprint blueprint)
    {
        return blueprint.RecommendedPages
            .Select((page, index) => $"{page.PageName} belongs in the experience because it {page.PageIntent.ToLowerInvariant()}, it uses filters like {string.Join(", ", page.SuggestedFilters.Take(2))}, and it serves as {DescribePageContribution(index, blueprint.RecommendedPages.Count, page.PageName)}.")
            .ToArray();
    }

    private static string BuildNavigationRationale(
        DiscoveryRecommendation recommendation,
        ExperienceBlueprint blueprint)
    {
        var pageNames = blueprint.RecommendedPages.Select(page => page.PageName).ToList();
        var routeHint = pageNames.Any(name => name.Contains("Routing", StringComparison.OrdinalIgnoreCase))
            ? " The page sequence also preserves route and handoff context that would be lost in a flatter dashboard."
            : string.Empty;
        var paceHint = recommendation.RecommendedExperienceType == OpportunityExperienceType.ExecutiveDashboard
            ? " It stays compressed enough for leadership review while still preserving the planned follow-up step."
            : recommendation.RecommendedExperienceType == OpportunityExperienceType.OperationalMonitoringExperience
                ? " It keeps action selection close to exceptions so operators do not have to reconstruct the next step."
                : string.Empty;

        return $"The navigation is organized this way because the workflow {blueprint.NavigationIntent.Flow} lets users move through the recommended {blueprint.RecommendedPages.Count}-page analytical path without reworking the baseline information architecture.{routeHint}{paceHint}";
    }

    private static string BuildAnalyticalFlowRationale(ExperienceBlueprint blueprint)
    {
        return $"The analytical flow is arranged this way because it starts with the question '{blueprint.AnalyticalFlow.Question}', moves through investigation and evidence, and ends with the decision '{blueprint.AnalyticalFlow.Decision}' so the final recommendation is defensible instead of impressionistic.";
    }

    private static DesignPackageProviderGuidance BuildProviderGuidance(
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        ExperienceBlueprint blueprint)
    {
        var why = $"Why this package exists: so a future provider can create the right {GetExperienceTypeLabel(recommendation.RecommendedExperienceType)} for {recommendation.ExpectedAudience} and {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()} without having to rediscover the business story from scratch.";
        var experience = $"Generate a {GetExperienceTypeLabel(recommendation.RecommendedExperienceType)} with pages shaped around {string.Join(", ", blueprint.RecommendedPages.Select(page => page.PageName))}, preserve the primary filter scope of {string.Join(", ", blueprint.SuggestedGlobalFilters.Take(3))}, keep the audience centered on {recommendation.ExpectedAudience}, and preserve the {InferDecisionCadence(recommendation, opportunity).ToLowerInvariant()} decision posture rather than expanding into a different experience family.";
        var success = $"Success looks like an experience where {recommendation.ExpectedAudience} can follow the path from {blueprint.AnalyticalFlow.Question.ToLowerInvariant()} to {blueprint.AnalyticalFlow.Decision.ToLowerInvariant()}, supported by the primary KPIs {string.Join(", ", blueprint.PrimaryKpis.Take(3))}, with navigation that matches the intended workflow instead of a generic scaffold. Success looks like a provider being able to build the experience without needing external discovery context.";

        return new DesignPackageProviderGuidance(
            WhyThisPackageExists: why,
            ExperienceToGenerate: experience,
            SuccessLooksLike: success);
    }

    private static IReadOnlyList<string> BuildProvenanceNotes(ExperienceBlueprint blueprint)
    {
        var evidence = blueprint.Provenance.SemanticEvidenceReferences
            .Select(DescribeBusinessFriendlyEvidence)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(NameComparer)
            .Take(3)
            .ToList();
        var structures = blueprint.Provenance.InfluencingModelStructures
            .Select(DescribeBusinessFriendlyStructure)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(NameComparer)
            .Take(4)
            .ToList();

        return
        [
            $"Discovery evidence consistently supports a {GetExperienceTypeLabel(blueprint.ExperienceType)} for {blueprint.ExpectedAudience}.",
            $"The recommended story is grounded in business evidence such as {string.Join(", ", evidence.DefaultIfEmpty("the priority measures and filters in the discovery signals"))}.",
            $"The package keeps the business structure visible through {string.Join(", ", structures.DefaultIfEmpty("the model structures that support the recommendation"))}.",
            $"Any remaining ambiguity is already reflected in the selected pages, KPI emphasis, and navigation sequence."
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

    private static string BuildKpiPurpose(DiscoveryRecommendation recommendation, string kpi)
    {
        return $"{BuildKpiDecisionReason(recommendation, kpi)}";
    }

    private static string BuildKpiDecisionReason(DiscoveryRecommendation recommendation, string kpi)
    {
        if (ContainsAny(kpi, "forecast accuracy", "forecast variance", "plan attainment"))
        {
            return $"{kpi} helps {recommendation.ExpectedAudience.ToLowerInvariant()} manage forecast quality and variance before the next planning decision.";
        }

        if (ContainsAny(kpi, "open work orders", "resolution", "sla", "backlog"))
        {
            return $"{kpi} helps {recommendation.ExpectedAudience.ToLowerInvariant()} see workflow pressure and next-owner follow-through clearly.";
        }

        if (ContainsAny(kpi, "open pipeline", "at risk pipeline", "win rate"))
        {
            return $"{kpi} helps {recommendation.ExpectedAudience.ToLowerInvariant()} manage pipeline follow-through rather than just scan topline performance.";
        }

        if (ContainsAny(kpi, "margin", "profit"))
        {
            return $"{kpi} helps {recommendation.ExpectedAudience.ToLowerInvariant()} compare profitable growth drivers instead of relying on generic revenue volume alone.";
        }

        if (ContainsAny(kpi, "revenue", "growth"))
        {
            return $"{kpi} helps {recommendation.ExpectedAudience.ToLowerInvariant()} keep the primary business outcome visible while moving through {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}.";
        }

        return $"{kpi} helps {recommendation.ExpectedAudience.ToLowerInvariant()} make the decision implied by {recommendation.ExpectedBusinessOutcome.ToLowerInvariant()}.";
    }

    private static string GetExperienceTypeLabel(OpportunityExperienceType experienceType)
    {
        return experienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "Executive Dashboard",
            OpportunityExperienceType.OperationalMonitoringExperience => "Operational Monitoring Experience",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "Analytical Investigation Experience",
            OpportunityExperienceType.FabricApp => "Fabric App",
            OpportunityExperienceType.FabricDataApp => "Fabric Data App",
            _ => "PBIR Report"
        };
    }

    private static string DescribeBusinessFriendlyEvidence(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return string.Empty;
        }

        var parts = reference.Split(':', 2, StringSplitOptions.TrimEntries);
        var label = parts.Length == 2 ? parts[1] : parts[0];
        return NormalizeBusinessLabel(label);
    }

    private static string DescribeBusinessFriendlyStructure(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return string.Empty;
        }

        var parts = reference.Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return NormalizeBusinessLabel(reference);
        }

        return parts[0].ToLowerInvariant() switch
        {
            "dimension" => $"{NormalizeBusinessLabel(parts[1])} dimension",
            "measure" => $"{NormalizeBusinessLabel(parts[1])} measure",
            "hierarchy" => $"{NormalizeBusinessLabel(parts[1])} hierarchy",
            "relationship" => NormalizeBusinessLabel(parts[1].Replace("->", " to ", StringComparison.Ordinal)),
            _ => NormalizeBusinessLabel(parts[1])
        };
    }

    private static string NormalizeBusinessLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();

        foreach (var prefix in new[] { "Dim", "Fact", "Tbl", "Table" })
        {
            if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                normalized.Length > prefix.Length &&
                char.IsUpper(normalized[prefix.Length]))
            {
                normalized = normalized[prefix.Length..];
                break;
            }
        }

        normalized = normalized.Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);

        normalized = string.Concat(normalized.Select((character, index) =>
            index > 0 && char.IsUpper(character) && char.IsLetterOrDigit(normalized[index - 1]) && !char.IsUpper(normalized[index - 1])
                ? $" {character}"
                : character.ToString()));

        return string.Join(" ", normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant()));
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string InferDecisionCadence(DiscoveryRecommendation recommendation, OpportunityCandidate? opportunity)
    {
        var text = $"{recommendation.RecommendationName} {recommendation.ExpectedBusinessOutcome} {opportunity?.BusinessOutcome}";

        if (ContainsAny(text, "daily", "queue", "backlog", "sla", "exception", "monitor"))
        {
            return "Daily";
        }

        if (ContainsAny(text, "weekly", "forecast", "planning cycle", "plan"))
        {
            return "Weekly";
        }

        if (ContainsAny(text, "monthly", "quarterly", "board"))
        {
            return "Monthly";
        }

        if (ContainsAny(text, "investigate", "root cause", "deep dive", "hypothesis"))
        {
            return "Episodic";
        }

        return recommendation.RecommendedExperienceType == OpportunityExperienceType.OperationalMonitoringExperience ? "Daily" : "Weekly";
    }
}
