using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class RecommendationEngineService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;
    private sealed record ExperienceTypeFitEvaluation(
        OpportunityExperienceType ExperienceType,
        double Score);
    private sealed record RecommendationSignalProfile(
        string DecisionCadence,
        string InteractionFrequency,
        double AnalyticalDepth,
        double OperationalActionability,
        IReadOnlyList<string> DimensionSignals,
        IReadOnlyList<string> MeasureSignals);

    private const double TechnicalFitWeight = 0.34d;
    private const double BusinessFitWeight = 0.28d;
    private const double ConsultantJudgmentWeight = 0.38d;

    internal RecommendationSet BuildRecommendations(DiscoveryProfile profile, OpportunityCatalog catalog)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        if (catalog is null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        if (catalog.Opportunities.Count == 0)
        {
            return new RecommendationSet([], []);
        }

        var scored = catalog.Opportunities
            .Select(candidate => ScoreCandidate(profile, candidate))
            .OrderByDescending(candidate => candidate.RankingScore)
            .ThenBy(candidate => candidate.RecommendationName, NameComparer)
            .ToList();

        var deduplicated = Deduplicate(scored);
        var primary = SelectRecommendations(deduplicated, targetCount: 3, RecommendationPlacement.Primary, []);
        var alternates = SelectRecommendations(
            deduplicated.Where(candidate => primary.All(selected => selected.RecommendationId != candidate.RecommendationId)).ToList(),
            targetCount: 2,
            RecommendationPlacement.Alternate,
            primary);

        return new RecommendationSet(primary, alternates);
    }

    private static List<DiscoveryRecommendation> SelectRecommendations(
        IReadOnlyList<DiscoveryRecommendation> candidates,
        int targetCount,
        RecommendationPlacement placement,
        IReadOnlyList<DiscoveryRecommendation> alreadySelected)
    {
        var selected = new List<DiscoveryRecommendation>();
        var remaining = candidates.ToList();
        var existing = alreadySelected.ToList();

        while (selected.Count < targetCount && remaining.Count > 0)
        {
            var next = remaining
                .OrderByDescending(candidate => candidate.RankingScore + CalculateDiversityAdjustment(candidate, existing, placement))
                .ThenByDescending(candidate => candidate.RankingScore)
                .ThenBy(candidate => candidate.RecommendationName, NameComparer)
                .First();

            selected.Add(next with { Placement = placement });
            existing.Add(next);
            remaining.RemoveAll(candidate => candidate.RecommendationId == next.RecommendationId);
        }

        return selected;
    }

    private static double CalculateDiversityAdjustment(
        DiscoveryRecommendation candidate,
        IReadOnlyList<DiscoveryRecommendation> existing,
        RecommendationPlacement placement)
    {
        if (existing.Count == 0)
        {
            return 0d;
        }

        var typeBonus = existing.All(current => current.RecommendedExperienceType != candidate.RecommendedExperienceType)
            ? placement == RecommendationPlacement.Primary ? 6d : 8d
            : 0d;
        var audienceBonus = existing.All(current => !string.Equals(current.ExpectedAudience, candidate.ExpectedAudience, StringComparison.OrdinalIgnoreCase))
            ? placement == RecommendationPlacement.Primary ? 4d : 5d
            : 0d;
        var outcomeBonus = existing.All(current => !OutcomesLookSimilar(current.ExpectedBusinessOutcome, candidate.ExpectedBusinessOutcome))
            ? 2d
            : 0d;
        var themeBonus = existing.All(current => !ThemesLookSimilar(current, candidate))
            ? placement == RecommendationPlacement.Primary ? 3d : 4d
            : 0d;
        var workflowBonus = existing.All(current => !WorkflowLooksSimilar(current, candidate))
            ? placement == RecommendationPlacement.Primary ? 4d : 5d
            : 0d;
        var decisionPatternBonus = existing.All(current => !DecisionPatternsLookSimilar(current, candidate))
            ? placement == RecommendationPlacement.Primary ? 4d : 5d
            : 0d;
        var duplicatePenalty = existing.Any(current =>
            current.RecommendedExperienceType == candidate.RecommendedExperienceType &&
            string.Equals(current.ExpectedAudience, candidate.ExpectedAudience, StringComparison.OrdinalIgnoreCase))
                ? placement == RecommendationPlacement.Primary ? -6d : -8d
                : 0d;
        var familyPenalty = existing.Any(current => BelongsToSameExperienceFamily(current, candidate))
            ? placement == RecommendationPlacement.Primary ? -2d : -3d
            : 0d;
        var workflowPenalty = existing.Any(current => WorkflowLooksSimilar(current, candidate))
            ? placement == RecommendationPlacement.Primary ? -2d : -3d
            : 0d;
        var decisionPatternPenalty = existing.Any(current => DecisionPatternsLookSimilar(current, candidate))
            ? placement == RecommendationPlacement.Primary ? -2d : -3d
            : 0d;

        return typeBonus + audienceBonus + outcomeBonus + themeBonus + workflowBonus + decisionPatternBonus + duplicatePenalty + familyPenalty + workflowPenalty + decisionPatternPenalty;
    }

    private static List<DiscoveryRecommendation> Deduplicate(IReadOnlyList<DiscoveryRecommendation> candidates)
    {
        var deduplicated = new List<DiscoveryRecommendation>();

        foreach (var candidate in candidates.OrderByDescending(item => item.RankingScore))
        {
            var duplicateIndex = deduplicated.FindIndex(existing => AreNearDuplicates(existing, candidate));
            if (duplicateIndex < 0)
            {
                deduplicated.Add(candidate);
                continue;
            }

            deduplicated[duplicateIndex] = MergeRecommendations(deduplicated[duplicateIndex], candidate);
        }

        return deduplicated
            .OrderByDescending(candidate => candidate.RankingScore)
            .ThenBy(candidate => candidate.RecommendationName, NameComparer)
            .ToList();
    }

    private static bool AreNearDuplicates(DiscoveryRecommendation left, DiscoveryRecommendation right)
    {
        if (left.RecommendedExperienceType != right.RecommendedExperienceType)
        {
            return false;
        }

        if (!string.Equals(left.ExpectedAudience, right.ExpectedAudience, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nameSimilarity = CalculateTokenSimilarity(left.RecommendationName, right.RecommendationName);
        var outcomeSimilarity = CalculateTokenSimilarity(left.ExpectedBusinessOutcome, right.ExpectedBusinessOutcome);

        return nameSimilarity >= 0.55d || outcomeSimilarity >= 0.55d;
    }

    private static DiscoveryRecommendation MergeRecommendations(DiscoveryRecommendation primary, DiscoveryRecommendation duplicate)
    {
        var winner = primary.RankingScore >= duplicate.RankingScore ? primary : duplicate;
        var supportingSignals = primary.SupportingSignals
            .Concat(duplicate.SupportingSignals)
            .Distinct(NameComparer)
            .OrderBy(signal => signal, NameComparer)
            .ToList();
        var limitingFactors = primary.LimitingFactors
            .Concat(duplicate.LimitingFactors)
            .Distinct(NameComparer)
            .OrderBy(signal => signal, NameComparer)
            .ToList();

        return winner with
        {
            SupportingSignals = supportingSignals,
            LimitingFactors = limitingFactors,
            WhyWeRecommendIt = winner.WhyWeRecommendIt,
            Confidence = Max(primary.Confidence, duplicate.Confidence),
            BusinessValue = Max(primary.BusinessValue, duplicate.BusinessValue),
            ImplementationComplexity = Max(primary.ImplementationComplexity, duplicate.ImplementationComplexity),
            ConfidenceNote = BuildConfidenceNote(Max(primary.Confidence, duplicate.Confidence)),
            ComplexityNote = BuildComplexityNote(Max(primary.ImplementationComplexity, duplicate.ImplementationComplexity), winner.RecommendedExperienceType),
            RankingScore = Math.Max(primary.RankingScore, duplicate.RankingScore)
        };
    }

    private static DiscoveryRecommendation ScoreCandidate(DiscoveryProfile profile, OpportunityCandidate candidate)
    {
        var experienceEvaluations = EvaluateExperienceTypes(profile, candidate);
        var recommendedExperienceType = experienceEvaluations[0].ExperienceType;
        var supportingSignals = BuildSupportingSignalExplanations(candidate);
        var limitingFactors = profile.AmbiguityNotes
            .Concat(candidate.LimitingFactors)
            .Distinct(NameComparer)
            .OrderBy(factor => factor, NameComparer)
            .ToList();

        var semanticCoverage = NormalizeCount(candidate.SupportingSemanticSignals.Count, 5);
        var businessActionability = CalculateBusinessActionability(candidate);
        var analyticalFit = CalculateAnalyticalFit(profile, candidate, recommendedExperienceType);
        var audienceClarity = CalculateAudienceClarity(profile, candidate.InferredAudience);
        var opportunityCompleteness = CalculateOpportunityCompleteness(candidate);
        var complexityBurden = CalculateComplexityBurden(profile, candidate, recommendedExperienceType);
        var modelConfidence = Average(MapConfidence(profile.Confidence), MapConfidence(candidate.Confidence));
        var experienceFit = NormalizeExperienceFit(experienceEvaluations[0].Score);
        var technicalFit = Average(semanticCoverage, analyticalFit, modelConfidence, experienceFit);
        var businessFit = Average(businessActionability, audienceClarity, opportunityCompleteness, 1d - complexityBurden);
        var consultantDecision = BuildConsultantDecisionAssessment(
            profile,
            candidate,
            recommendedExperienceType,
            experienceEvaluations.Skip(1).Take(2).ToList(),
            supportingSignals,
            limitingFactors,
            technicalFit,
            businessFit,
            complexityBurden);
        var weightedScore =
            (technicalFit * TechnicalFitWeight) +
            (businessFit * BusinessFitWeight) +
            (consultantDecision.ConsultantJudgmentScore * ConsultantJudgmentWeight);

        var scenarioPostureAdjustment = 0d;

        if (consultantDecision.DomainFramework == ConsultantDomainFramework.RevenueSales &&
            recommendedExperienceType == OpportunityExperienceType.AnalyticalInvestigationExperience &&
            HasAudience(profile, "Executive") &&
            HasAudience(profile, "Operational"))
        {
            scenarioPostureAdjustment -= 0.18d;
        }

        if (consultantDecision.DomainFramework == ConsultantDomainFramework.RevenueSales &&
            recommendedExperienceType == OpportunityExperienceType.ExecutiveDashboard &&
            candidate.Category == OpportunityCategory.ExecutiveReporting &&
            HasAudience(profile, "Analytical") &&
            !HasInvestigativeIntent(candidate))
        {
            scenarioPostureAdjustment += 0.14d;
        }

        if (consultantDecision.DomainFramework == ConsultantDomainFramework.Forecasting &&
            recommendedExperienceType == OpportunityExperienceType.AnalyticalInvestigationExperience &&
            !HasInvestigativeIntent(candidate))
        {
            scenarioPostureAdjustment -= 0.12d;
        }

        if (consultantDecision.DomainFramework == ConsultantDomainFramework.Forecasting &&
            recommendedExperienceType == OpportunityExperienceType.ExecutiveDashboard &&
            HasPlanningIntent(candidate))
        {
            scenarioPostureAdjustment += 0.1d;
        }

        if (consultantDecision.DomainFramework == ConsultantDomainFramework.Forecasting &&
            candidate.Category == OpportunityCategory.ForecastAccuracy)
        {
            scenarioPostureAdjustment += recommendedExperienceType switch
            {
                OpportunityExperienceType.ExecutiveDashboard when HasPlanningIntent(candidate) => 0.16d,
                OpportunityExperienceType.FabricApp when HasManagementIntent(candidate) => 0.14d,
                OpportunityExperienceType.AnalyticalInvestigationExperience when HasInvestigativeIntent(candidate) => 0.08d,
                _ => 0d,
            };
        }

        if (consultantDecision.DomainFramework == ConsultantDomainFramework.CustomerProfitability &&
            recommendedExperienceType is OpportunityExperienceType.FabricDataApp or OpportunityExperienceType.AnalyticalInvestigationExperience)
        {
            scenarioPostureAdjustment += 0.18d;
        }

        if (consultantDecision.DomainFramework == ConsultantDomainFramework.RevenueSales &&
            recommendedExperienceType == OpportunityExperienceType.FabricApp &&
            HasAudience(profile, "Operational") &&
            HasManagementIntent(candidate))
        {
            scenarioPostureAdjustment += 0.22d;
        }

        if (candidate.Category == OpportunityCategory.ExecutiveReporting &&
            recommendedExperienceType == OpportunityExperienceType.ExecutiveDashboard)
        {
            if (ProfileHasHighDomain(profile, "Forecasting") && !HasDomain(candidate, "Forecasting"))
            {
                scenarioPostureAdjustment -= 0.18d;
            }

            if (ProfileHasHighDomain(profile, "Profitability") && !HasDomain(candidate, "Profitability"))
            {
                scenarioPostureAdjustment -= 0.2d;
            }

            if (ProfileHasHighDomain(profile, "Customer") && !HasDomain(candidate, "Customer"))
            {
                scenarioPostureAdjustment -= 0.16d;
            }
        }

        weightedScore = Clamp01(weightedScore + scenarioPostureAdjustment);

        var rankingScore = Math.Round(weightedScore * 100d, 2);
        var recommendationConfidence = ClassifyRecommendationConfidence(weightedScore, profile, candidate);
        var businessValue = ClassifyBusinessValue(businessActionability, analyticalFit, semanticCoverage);
        var complexity = ClassifyComplexity(complexityBurden);

        return new DiscoveryRecommendation(
            RecommendationId: candidate.OpportunityId,
            RecommendationName: candidate.Name,
            RecommendedExperienceType: recommendedExperienceType,
            Confidence: recommendationConfidence,
            BusinessValue: businessValue,
            ImplementationComplexity: complexity,
            WhyWeRecommendIt: BuildWhyWeRecommendIt(
                profile,
                candidate,
                recommendedExperienceType,
                consultantDecision,
                supportingSignals,
                limitingFactors),
            ExpectedAudience: candidate.InferredAudience,
            ExpectedBusinessOutcome: candidate.BusinessOutcome,
            SupportingSignals: supportingSignals,
            LimitingFactors: limitingFactors,
            ConfidenceNote: BuildConfidenceNote(recommendationConfidence),
            ComplexityNote: BuildComplexityNote(complexity, recommendedExperienceType),
            Placement: RecommendationPlacement.Primary,
            RankingScore: rankingScore);
    }

    private static OpportunityExperienceType ChooseRecommendedExperienceType(DiscoveryProfile profile, OpportunityCandidate candidate)
    {
        if (candidate.CandidateExperienceTypes.Count == 0)
        {
            return OpportunityExperienceType.PbirReport;
        }

        return EvaluateExperienceTypes(profile, candidate)
            .Select(item => item.ExperienceType)
            .First();
    }

    private static IReadOnlyList<ExperienceTypeFitEvaluation> EvaluateExperienceTypes(
        DiscoveryProfile profile,
        OpportunityCandidate candidate)
    {
        return candidate.CandidateExperienceTypes
            .Select((experienceType, index) => new
            {
                ExperienceType = experienceType,
                Score = ScoreExperienceTypeFit(profile, candidate, experienceType),
                Index = index
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .Select(item => new ExperienceTypeFitEvaluation(item.ExperienceType, item.Score))
            .ToList();
    }

    private static double ScoreExperienceTypeFit(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType experienceType)
    {
        var workflowText = $"{candidate.Name} {candidate.BusinessOutcome}";
        var analyticalDepth = CalculateAnalyticalDepth(profile, candidate);
        var executiveStrength = GetAudienceStrength(profile, candidate, "Executive", "leadership", "executive", "summary", "cadence", "priority");
        var operationalStrength = GetAudienceStrength(profile, candidate, "Operational", "operations", "operational", "queue", "backlog", "action");
        var analyticalStrength = GetAudienceStrength(profile, candidate, "Analytical", "analysis", "investigate", "variance", "driver", "root cause");
        var multiRoleStrength = profile.AudienceSignals.Count >= 2 ? 0.16d : 0d;
        var workflowOrchestration = ContainsAny(workflowText, "coordinate", "handoff", "route", "workflow", "follow-up", "triage")
            ? 0.32d
            : 0d;
        var explorationStrength = ContainsAny(workflowText, "explore", "segment", "cohort", "slice", "discover")
            ? 0.28d
            : 0d;
        var narrativeStrength = ContainsAny(workflowText, "narrative", "story", "brief", "briefing", "review", "readout", "explain", "walkthrough")
            ? 0.26d
            : 0d;
        var executiveCadence = profile.DateIntelligence.Readiness == DiscoveryDateIntelligenceReadiness.High ? 0.12d : 0d;
        var operationalDomain = HasDomain(candidate, "Inventory") || HasDomain(candidate, "Service") ? 0.18d : 0d;
        var serviceWorkflow = HasDomain(candidate, "Service") && (HasSignal(candidate, "Dimension", "Technician") || HasSignal(candidate, "Dimension", "Work Order"))
            ? 0.18d
            : 0d;
        var customerExploration = HasDomain(candidate, "Customer") ? 0.14d : 0d;
        var planningStrength = HasPlanningIntent(candidate) ? 0.22d : 0d;
        var managementStrength = HasManagementIntent(candidate) ? 0.2d : 0d;
        var investigativeStrength = HasInvestigativeIntent(candidate) ? 0.2d : 0d;
        var categoryPrior = GetCategoryPrior(candidate.Category, experienceType);
        var strategicValue = ContainsAny(workflowText, "leadership", "executive", "strategy", "strategic", "board", "priority")
            ? 0.12d
            : 0d;
        var decisionCadence = InferDecisionCadence(candidate);
        var interactionFrequency = InferInteractionFrequency(candidate);
        var operationalActionability = CalculateOperationalActionability(candidate);
        var executiveAnalyticalBlend = HasAudience(profile, "Executive") && HasAudience(profile, "Analytical")
            ? 0.12d
            : 0d;

        var score = experienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => 0.3d + categoryPrior + executiveStrength + executiveCadence + strategicValue + planningStrength + (decisionCadence == "Weekly" || decisionCadence == "Monthly" ? 0.1d : 0d) + (interactionFrequency == "Low" ? 0.08d : 0d) + (1d - analyticalDepth) * 0.1d,
            OpportunityExperienceType.OperationalMonitoringExperience => 0.3d + categoryPrior + operationalStrength + operationalDomain + serviceWorkflow + managementStrength + operationalActionability + (decisionCadence == "Daily" ? 0.12d : 0d) + (interactionFrequency == "High" ? 0.08d : 0d) + (ContainsAny(workflowText, "monitor", "exception", "backlog", "sla", "queue") ? 0.16d : 0d),
            OpportunityExperienceType.AnalyticalInvestigationExperience => 0.28d + categoryPrior + analyticalStrength + (analyticalDepth * 0.34d) + (decisionCadence == "Episodic" ? 0.08d : 0d) + investigativeStrength,
            OpportunityExperienceType.FabricApp => 0.22d + categoryPrior + operationalStrength + multiRoleStrength + workflowOrchestration + serviceWorkflow + managementStrength + operationalActionability + (decisionCadence == "Daily" ? 0.08d : 0d) + (interactionFrequency == "High" ? 0.1d : 0d) + (NormalizeCount(profile.Dimensions.Count, 5) * 0.12d),
            OpportunityExperienceType.FabricDataApp => 0.22d + categoryPrior + analyticalStrength + explorationStrength + customerExploration + (interactionFrequency != "Low" ? 0.08d : 0d) + (NormalizeCount(profile.Dimensions.Count, 5) * 0.1d),
            _ => 0.22d + categoryPrior + narrativeStrength + executiveAnalyticalBlend + (NormalizeCount(candidate.SupportingSemanticSignals.Count, 5) * 0.12d) + (decisionCadence == "Weekly" || decisionCadence == "Monthly" ? 0.08d : 0d) + (interactionFrequency == "Medium" || interactionFrequency == "Low" ? 0.06d : 0d) + (profile.DateIntelligence.Readiness != DiscoveryDateIntelligenceReadiness.Low ? 0.06d : 0d),
        };

        if (experienceType == OpportunityExperienceType.ExecutiveDashboard && analyticalDepth >= 0.7d)
        {
            score -= 0.12d;
        }

        if (experienceType == OpportunityExperienceType.AnalyticalInvestigationExperience && executiveStrength >= 0.2d && analyticalDepth < 0.55d)
        {
            score -= 0.18d;
        }

        if (experienceType == OpportunityExperienceType.AnalyticalInvestigationExperience && !HasInvestigativeIntent(candidate))
        {
            score -= 0.16d;
        }

        if (experienceType == OpportunityExperienceType.FabricApp && workflowOrchestration == 0d)
        {
            score -= 0.1d;
        }

        if (experienceType == OpportunityExperienceType.PbirReport && operationalActionability >= 0.22d && decisionCadence == "Daily")
        {
            score -= 0.16d;
        }

        if (experienceType == OpportunityExperienceType.ExecutiveDashboard && interactionFrequency == "High" && operationalActionability >= 0.18d)
        {
            score -= 0.08d;
        }

        score += GetConsultantExperienceTypeAdjustment(candidate, experienceType, decisionCadence, operationalActionability, analyticalDepth);

        return score;
    }

    private static double GetCategoryPrior(OpportunityCategory category, OpportunityExperienceType experienceType)
    {
        return (category, experienceType) switch
        {
            (OpportunityCategory.ExecutiveReporting, OpportunityExperienceType.ExecutiveDashboard) => 0.16d,
            (OpportunityCategory.SalesPerformance, OpportunityExperienceType.ExecutiveDashboard) => 0.14d,
            (OpportunityCategory.ForecastAccuracy, OpportunityExperienceType.ExecutiveDashboard) => 0.14d,
            (OpportunityCategory.InventoryOptimization, OpportunityExperienceType.OperationalMonitoringExperience) => 0.14d,
            (OpportunityCategory.ServiceOperations, OpportunityExperienceType.OperationalMonitoringExperience) => 0.12d,
            (OpportunityCategory.ServiceOperations, OpportunityExperienceType.FabricApp) => 0.1d,
            (OpportunityCategory.ProfitabilityAnalysis, OpportunityExperienceType.AnalyticalInvestigationExperience) => 0.12d,
            (OpportunityCategory.ProfitabilityAnalysis, OpportunityExperienceType.ExecutiveDashboard) => 0.08d,
            (OpportunityCategory.RootCauseInvestigation, OpportunityExperienceType.AnalyticalInvestigationExperience) => 0.16d,
            (OpportunityCategory.CustomerAnalysis, OpportunityExperienceType.FabricDataApp) => 0.14d,
            (OpportunityCategory.ComparativePerformanceManagement, OpportunityExperienceType.ExecutiveDashboard) => 0.1d,
            _ => experienceType == OpportunityExperienceType.PbirReport ? 0.08d : 0d,
        };
    }

    private static double GetConsultantExperienceTypeAdjustment(
        OpportunityCandidate candidate,
        OpportunityExperienceType experienceType,
        string decisionCadence,
        double operationalActionability,
        double analyticalDepth)
    {
        var workflowOrientation = ResolveWorkflowOrientation(candidate);
        var domainFramework = ResolveConsultantDomainFramework(candidate);

        var adjustment = 0d;

        if (domainFramework == ConsultantDomainFramework.Forecasting && experienceType == OpportunityExperienceType.AnalyticalInvestigationExperience && HasMeasureSignal(candidate, "Forecast"))
        {
            adjustment += HasInvestigativeIntent(candidate) ? 0.1d : -0.18d;
        }

        if (domainFramework == ConsultantDomainFramework.CustomerProfitability && experienceType == OpportunityExperienceType.FabricDataApp)
        {
            adjustment += 0.24d;
        }

        if (domainFramework == ConsultantDomainFramework.CustomerProfitability && experienceType == OpportunityExperienceType.AnalyticalInvestigationExperience)
        {
            adjustment += 0.18d;
        }

        if (domainFramework == ConsultantDomainFramework.ServiceOperations && workflowOrientation == ConsultantWorkflowOrientation.Act && experienceType == OpportunityExperienceType.FabricApp)
        {
            adjustment += 0.22d;
        }

        if (domainFramework == ConsultantDomainFramework.RevenueSales && workflowOrientation == ConsultantWorkflowOrientation.Act && experienceType == OpportunityExperienceType.FabricApp)
        {
            adjustment += 0.2d;
        }

        if (domainFramework == ConsultantDomainFramework.RevenueSales && workflowOrientation == ConsultantWorkflowOrientation.Act && experienceType == OpportunityExperienceType.ExecutiveDashboard)
        {
            adjustment -= 0.16d;
        }

        if (domainFramework == ConsultantDomainFramework.Forecasting && experienceType == OpportunityExperienceType.ExecutiveDashboard && analyticalDepth >= 0.55d)
        {
            adjustment += HasPlanningIntent(candidate) ? 0.16d : -0.08d;
        }

        if (domainFramework == ConsultantDomainFramework.Forecasting && experienceType == OpportunityExperienceType.FabricApp && HasManagementIntent(candidate))
        {
            adjustment += 0.18d;
        }

        if (domainFramework == ConsultantDomainFramework.RevenueSales && experienceType == OpportunityExperienceType.AnalyticalInvestigationExperience && !HasInvestigativeIntent(candidate))
        {
            adjustment -= 0.18d;
        }

        if (domainFramework == ConsultantDomainFramework.CustomerProfitability && experienceType == OpportunityExperienceType.ExecutiveDashboard)
        {
            adjustment -= 0.18d;
        }

        if (decisionCadence == "Daily" && operationalActionability >= 0.18d && experienceType == OpportunityExperienceType.ExecutiveDashboard)
        {
            adjustment -= 0.12d;
        }

        return adjustment;
    }

    private static double GetAudienceStrength(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        string audience,
        params string[] workflowTerms)
    {
        var signal = profile.AudienceSignals.FirstOrDefault(current =>
            string.Equals(current.Audience, audience, StringComparison.OrdinalIgnoreCase));
        var signalStrength = signal is null ? 0d : MapConfidence(signal.Confidence) * 0.18d;
        var outcomeStrength = ContainsAny($"{candidate.Name} {candidate.BusinessOutcome}", workflowTerms) ? 0.12d : 0d;

        return signalStrength + outcomeStrength;
    }

    private static double CalculateAnalyticalDepth(DiscoveryProfile profile, OpportunityCandidate candidate)
    {
        var depth = 0.18d;

        depth += NormalizeCount(profile.Relationships.Count, 4) * 0.28d;
        depth += NormalizeCount(profile.Hierarchies.Count, 3) * 0.2d;
        depth += NormalizeCount(profile.Dimensions.Count, 5) * 0.12d;
        depth += HasSignal(candidate, "Drill") || HasMeasureSignal(candidate, "Variance") ? 0.16d : 0d;
        depth += HasAudience(profile, "Analytical") ? 0.12d : 0d;

        return Clamp01(depth);
    }

    private static RecommendationComplexityLevel ClassifyComplexity(double burden)
    {
        return burden >= 0.67d
            ? RecommendationComplexityLevel.High
            : burden >= 0.4d
                ? RecommendationComplexityLevel.Medium
                : RecommendationComplexityLevel.Low;
    }

    private static RecommendationBusinessValueLevel ClassifyBusinessValue(double actionability, double fit, double coverage)
    {
        var composite = Average(actionability, fit, coverage);
        return composite >= 0.74d
            ? RecommendationBusinessValueLevel.High
            : composite >= 0.5d
                ? RecommendationBusinessValueLevel.Medium
                : RecommendationBusinessValueLevel.Low;
    }

    private static DiscoveryConfidenceLevel ClassifyRecommendationConfidence(double weightedScore, DiscoveryProfile profile, OpportunityCandidate candidate)
    {
        var confidenceScore = Average(weightedScore, MapConfidence(profile.Confidence), MapConfidence(candidate.Confidence));

        if (profile.AmbiguityNotes.Count >= 2)
        {
            confidenceScore -= 0.2d;
        }

        if (candidate.LimitingFactors.Count >= 2)
        {
            confidenceScore -= 0.1d;
        }

        if (profile.Confidence == DiscoveryConfidenceLevel.Low || candidate.Confidence == DiscoveryConfidenceLevel.Low)
        {
            confidenceScore -= 0.1d;
        }

        confidenceScore = Clamp01(confidenceScore);
        return confidenceScore >= 0.74d
            ? DiscoveryConfidenceLevel.High
            : confidenceScore >= 0.5d
                ? DiscoveryConfidenceLevel.Medium
                : DiscoveryConfidenceLevel.Low;
    }

    private static double CalculateOpportunityCompleteness(OpportunityCandidate candidate)
    {
        var signalStrength = NormalizeCount(candidate.SupportingSemanticSignals.Count, 5);
        var limitingPenalty = NormalizeCount(candidate.LimitingFactors.Count, 3);
        return Clamp01((signalStrength * 0.65d) + ((1d - limitingPenalty) * 0.35d));
    }

    private static double CalculateAudienceClarity(DiscoveryProfile profile, string audience)
    {
        var matchedSignal = profile.AudienceSignals.FirstOrDefault(signal =>
            string.Equals(signal.Audience, audience, StringComparison.OrdinalIgnoreCase));

        if (matchedSignal is not null)
        {
            return MapConfidence(matchedSignal.Confidence);
        }

        return string.IsNullOrWhiteSpace(audience) ? 0.3d : 0.55d;
    }

    private static double CalculateAnalyticalFit(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType recommendedExperienceType)
    {
        return recommendedExperienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => Clamp01(
                0.45d +
                (HasSignal(candidate, "KpiCluster") ? 0.2d : 0d) +
                (HasSignal(candidate, "DateIntelligence", "High") ? 0.15d : 0d) +
                (HasAudience(profile, "Executive") ? 0.1d : 0d)),
            OpportunityExperienceType.OperationalMonitoringExperience => Clamp01(
                0.45d +
                (HasDomain(candidate, "Inventory") || HasDomain(candidate, "Service") ? 0.2d : 0d) +
                (HasMeasureSignal(candidate, "Quantity") || HasMeasureSignal(candidate, "Resolution") ? 0.15d : 0d) +
                (HasAudience(profile, "Operational") ? 0.1d : 0d)),
            OpportunityExperienceType.AnalyticalInvestigationExperience => Clamp01(
                0.45d +
                (HasSignal(candidate, "Drill") ? 0.2d : 0d) +
                (HasMeasureSignal(candidate, "Variance") ? 0.15d : 0d) +
                (HasAudience(profile, "Analytical") ? 0.1d : 0d)),
            OpportunityExperienceType.FabricDataApp => Clamp01(
                0.4d +
                (HasDomain(candidate, "Customer") ? 0.18d : 0d) +
                (HasSignal(candidate, "Dimension") ? 0.14d : 0d) +
                (candidate.CandidateExperienceTypes.Contains(OpportunityExperienceType.AnalyticalInvestigationExperience) ? 0.08d : 0d)),
            OpportunityExperienceType.FabricApp => Clamp01(
                0.45d +
                (HasAudience(profile, "Executive") || HasAudience(profile, "Operational") ? 0.15d : 0d) +
                (NormalizeCount(candidate.SupportingSemanticSignals.Count, 5) * 0.2d)),
            _ => Clamp01(
                0.5d +
                (NormalizeCount(candidate.SupportingSemanticSignals.Count, 5) * 0.2d) +
                (HasSignal(candidate, "DateIntelligence") ? 0.1d : 0d)),
        };
    }

    private static double CalculateBusinessActionability(OpportunityCandidate candidate)
    {
        var baseScore = candidate.Category switch
        {
            OpportunityCategory.InventoryOptimization => 0.92d,
            OpportunityCategory.ServiceOperations => 0.9d,
            OpportunityCategory.SalesPerformance => 0.88d,
            OpportunityCategory.ProfitabilityAnalysis => 0.87d,
            OpportunityCategory.ForecastAccuracy => 0.86d,
            OpportunityCategory.ExecutiveReporting => 0.84d,
            OpportunityCategory.RootCauseInvestigation => 0.83d,
            OpportunityCategory.ComparativePerformanceManagement => 0.8d,
            OpportunityCategory.CustomerAnalysis => 0.76d,
            _ => 0.75d,
        };

        if (ContainsAny(candidate.BusinessOutcome, "monitor", "track", "compare", "investigate", "optimize", "improve"))
        {
            baseScore += 0.05d;
        }

        if (HasSignal(candidate, "DateIntelligence") || HasMeasureSignal(candidate, "Variance") || HasSignal(candidate, "Dimension", "Geography"))
        {
            baseScore += 0.04d;
        }

        return Clamp01(baseScore);
    }

    private static double CalculateComplexityBurden(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType recommendedExperienceType)
    {
        var burden = recommendedExperienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => 0.28d,
            OpportunityExperienceType.OperationalMonitoringExperience => 0.42d,
            OpportunityExperienceType.PbirReport => 0.46d,
            OpportunityExperienceType.FabricApp => 0.58d,
            OpportunityExperienceType.FabricDataApp => 0.62d,
            OpportunityExperienceType.AnalyticalInvestigationExperience => 0.66d,
            _ => 0.5d,
        };

        if (candidate.SupportingSemanticSignals.Count >= 4)
        {
            burden += 0.08d;
        }

        if (candidate.CandidateExperienceTypes.Count >= 3)
        {
            burden += 0.06d;
        }

        if (profile.AmbiguityNotes.Count >= 2)
        {
            burden += 0.04d;
        }

        return Clamp01(burden);
    }

    private static IReadOnlyList<string> BuildSupportingSignalExplanations(OpportunityCandidate candidate)
    {
        return candidate.SupportingSemanticSignals
            .Select(BuildSupportingSignalExplanation)
            .Distinct(NameComparer)
            .OrderBy(signal => signal, NameComparer)
            .ToList();
    }

    private static string BuildSupportingSignalExplanation(OpportunitySemanticSignal signal)
    {
        return signal.SignalType switch
        {
            "Domain" => $"Strong {signal.Value} semantic coverage",
            "DateIntelligence" => $"{signal.Value} date intelligence readiness",
            "Dimension" => $"{signal.Value} segmentation support",
            "KpiCluster" => $"{signal.Value} support",
            "Audience" => $"{signal.Value} audience signal",
            "Measure" => $"{signal.Value} measure support",
            "Drill" => $"{signal.Value} drill path support",
            "RelationshipCount" => $"{signal.Value} relationship depth supports investigation",
            "HierarchyCount" => $"{signal.Value} hierarchies support navigation depth",
            _ => $"{signal.SignalType}: {signal.Value}"
        };
    }

    private static ConsultantDecisionAssessment BuildConsultantDecisionAssessment(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        IReadOnlyList<ExperienceTypeFitEvaluation> alternativeEvaluations,
        IReadOnlyList<string> supportingSignals,
        IReadOnlyList<string> limitingFactors,
        double technicalFit,
        double businessFit,
        double complexityBurden)
    {
        var signalProfile = BuildSignalProfile(profile, candidate);
        var domainFramework = ResolveConsultantDomainFramework(candidate);
        var audienceFit = ResolveAudienceFit(candidate);
        var cadence = ParseDecisionCadence(signalProfile.DecisionCadence);
        var workflowOrientation = ResolveWorkflowOrientation(candidate);
        var consumptionPattern = ResolveConsumptionPattern(selectedType);
        var actionability = ResolveActionability(candidate, signalProfile);
        var adoptionLikelihood = ResolveAdoptionLikelihood(selectedType, cadence, workflowOrientation, actionability, signalProfile);
        var maintenanceComplexity = ResolveMaintenanceComplexity(selectedType, complexityBurden);
        var consultantJudgment = CalculateConsultantJudgmentScore(
            profile,
            candidate,
            selectedType,
            domainFramework,
            cadence,
            workflowOrientation,
            actionability,
            adoptionLikelihood,
            maintenanceComplexity,
            signalProfile,
            alternativeEvaluations);
        var whyThisWins = BuildWhyThisWins(profile, candidate, selectedType, signalProfile, supportingSignals);
        var whyAlternativesLose = BuildAlternativeLossReasons(selectedType, alternativeEvaluations, candidate, signalProfile);
        var risks = BuildRiskSummary(selectedType, candidate, limitingFactors);
        var assumptions = BuildAssumptionSummary(candidate, signalProfile, profile);
        var adoptionConsiderations = BuildAdoptionConsiderations(selectedType, candidate, signalProfile, adoptionLikelihood);
        var futureEvolutionPath = BuildFutureEvolutionPath(selectedType, domainFramework, workflowOrientation);

        return new ConsultantDecisionAssessment(
            DomainFramework: domainFramework,
            AudienceFit: audienceFit,
            DecisionCadence: cadence,
            WorkflowOrientation: workflowOrientation,
            ConsumptionPattern: consumptionPattern,
            Actionability: actionability,
            AdoptionLikelihood: adoptionLikelihood,
            MaintenanceComplexity: maintenanceComplexity,
            TechnicalFitScore: technicalFit,
            BusinessFitScore: businessFit,
            ConsultantJudgmentScore: consultantJudgment,
            WhyThisExperienceWins: whyThisWins,
            WhyCompetingExperiencesLose: whyAlternativesLose,
            Risks: risks,
            Assumptions: assumptions,
            AdoptionConsiderations: adoptionConsiderations,
            FutureEvolutionPath: futureEvolutionPath);
    }

    private static string BuildWhyWeRecommendIt(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        ConsultantDecisionAssessment consultantDecision,
        IReadOnlyList<string> supportingSignals,
        IReadOnlyList<string> limitingFactors)
    {
        var signalProfile = BuildSignalProfile(profile, candidate);
        var businessTradeoffs = BuildBusinessTradeoffSummary(selectedType, candidate, limitingFactors);
        var audienceTradeoffs = BuildAudienceTradeoffSummary(profile, candidate, selectedType, signalProfile);
        var operationalTradeoffs = BuildOperationalTradeoffSummary(candidate, selectedType, signalProfile);
        var analyticalTradeoffs = BuildAnalyticalTradeoffSummary(candidate, selectedType, signalProfile);

        return $"Why This Experience Wins (Why This Wins): {consultantDecision.WhyThisExperienceWins} " +
               $"Why Competing Experiences Lose (Why Alternatives Lose): This path is better because {string.Join(" ", consultantDecision.WhyCompetingExperiencesLose)} " +
               $"Experience Posture: {BuildExperiencePostureSummary(candidate, selectedType)} " +
               $"Risks: {string.Join(" ", consultantDecision.Risks)} " +
               $"Assumptions: {string.Join(" ", consultantDecision.Assumptions)} " +
               $"Adoption Considerations: {consultantDecision.AdoptionConsiderations} " +
               $"Future Evolution Path: {consultantDecision.FutureEvolutionPath} " +
               $"Business Tradeoffs: {businessTradeoffs} " +
               $"Audience Tradeoffs: {audienceTradeoffs} " +
               $"Operational Tradeoffs: {operationalTradeoffs} " +
               $"Analytical Tradeoffs: {analyticalTradeoffs}";
    }

    private static RecommendationSignalProfile BuildSignalProfile(
        DiscoveryProfile profile,
        OpportunityCandidate candidate)
    {
        return new RecommendationSignalProfile(
            DecisionCadence: InferDecisionCadence(candidate),
            InteractionFrequency: InferInteractionFrequency(candidate),
            AnalyticalDepth: CalculateAnalyticalDepth(profile, candidate),
            OperationalActionability: CalculateOperationalActionability(candidate),
            DimensionSignals: candidate.SupportingSemanticSignals
                .Where(signal => string.Equals(signal.SignalType, "Dimension", StringComparison.OrdinalIgnoreCase))
                .Select(signal => signal.Value)
                .Distinct(NameComparer)
                .ToList(),
            MeasureSignals: candidate.SupportingSemanticSignals
                .Where(signal => string.Equals(signal.SignalType, "Measure", StringComparison.OrdinalIgnoreCase))
                .Select(signal => signal.Value)
                .Distinct(NameComparer)
                .ToList());
    }

    private static ConsultantDomainFramework ResolveConsultantDomainFramework(OpportunityCandidate candidate)
    {
        if (HasDomain(candidate, "Forecasting") || candidate.Category == OpportunityCategory.ForecastAccuracy)
        {
            return ConsultantDomainFramework.Forecasting;
        }

        if (HasDomain(candidate, "Customer") && HasDomain(candidate, "Profitability"))
        {
            return ConsultantDomainFramework.CustomerProfitability;
        }

        if (HasDomain(candidate, "Inventory") || candidate.Category == OpportunityCategory.InventoryOptimization)
        {
            return ConsultantDomainFramework.Inventory;
        }

        if (HasDomain(candidate, "Service") || candidate.Category == OpportunityCategory.ServiceOperations)
        {
            return ConsultantDomainFramework.ServiceOperations;
        }

        if (candidate.Category == OpportunityCategory.RootCauseInvestigation)
        {
            return ConsultantDomainFramework.AnalyticalInvestigation;
        }

        if (HasDomain(candidate, "Revenue") || candidate.Category == OpportunityCategory.SalesPerformance || candidate.Category == OpportunityCategory.ExecutiveReporting)
        {
            return ConsultantDomainFramework.RevenueSales;
        }

        return ConsultantDomainFramework.General;
    }

    private static ConsultantAudienceFit ResolveAudienceFit(OpportunityCandidate candidate)
    {
        var audience = candidate.InferredAudience ?? string.Empty;

        if (audience.Contains("service manager", StringComparison.OrdinalIgnoreCase) || audience.Contains("service operations", StringComparison.OrdinalIgnoreCase))
        {
            return ConsultantAudienceFit.ServiceManager;
        }

        if (audience.Contains("sales manager", StringComparison.OrdinalIgnoreCase))
        {
            return ConsultantAudienceFit.SalesManager;
        }

        if (audience.Contains("executive", StringComparison.OrdinalIgnoreCase) || audience.Contains("leadership", StringComparison.OrdinalIgnoreCase))
        {
            return ConsultantAudienceFit.Executive;
        }

        if (audience.Contains("anal", StringComparison.OrdinalIgnoreCase) || audience.Contains("strategy", StringComparison.OrdinalIgnoreCase))
        {
            return ConsultantAudienceFit.Analyst;
        }

        if (audience.Contains("operations", StringComparison.OrdinalIgnoreCase) || audience.Contains("operational", StringComparison.OrdinalIgnoreCase))
        {
            return ConsultantAudienceFit.Operational;
        }

        return ConsultantAudienceFit.Mixed;
    }

    private static ConsultantDecisionCadence ParseDecisionCadence(string decisionCadence)
    {
        return decisionCadence switch
        {
            "Daily" => ConsultantDecisionCadence.Daily,
            "Monthly" => ConsultantDecisionCadence.Monthly,
            "Quarterly" => ConsultantDecisionCadence.Quarterly,
            "Episodic" => ConsultantDecisionCadence.Episodic,
            _ => ConsultantDecisionCadence.Weekly,
        };
    }

    private static ConsultantWorkflowOrientation ResolveWorkflowOrientation(OpportunityCandidate candidate)
    {
        var text = $"{candidate.Name} {candidate.BusinessOutcome}";

        if (ContainsAny(text, "assign", "route", "handoff", "follow-up", "triage", "act"))
        {
            return ConsultantWorkflowOrientation.Act;
        }

        if (ContainsAny(text, "investigate", "variance", "driver", "root cause", "why"))
        {
            return ConsultantWorkflowOrientation.Investigate;
        }

        if (ContainsAny(text, "govern", "policy", "standard", "control"))
        {
            return ConsultantWorkflowOrientation.Govern;
        }

        return ConsultantWorkflowOrientation.Monitor;
    }

    private static ConsultantConsumptionPattern ResolveConsumptionPattern(OpportunityExperienceType selectedType)
    {
        return selectedType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => ConsultantConsumptionPattern.Dashboard,
            OpportunityExperienceType.FabricApp => ConsultantConsumptionPattern.App,
            OpportunityExperienceType.FabricDataApp => ConsultantConsumptionPattern.DataApp,
            OpportunityExperienceType.AnalyticalInvestigationExperience => ConsultantConsumptionPattern.InvestigativeExperience,
            _ => ConsultantConsumptionPattern.NarrativeReport,
        };
    }

    private static ConsultantActionability ResolveActionability(
        OpportunityCandidate candidate,
        RecommendationSignalProfile signalProfile)
    {
        if (signalProfile.OperationalActionability >= 0.18d || ContainsAny(candidate.BusinessOutcome, "assign", "route", "handoff", "follow-up"))
        {
            return ConsultantActionability.Operational;
        }

        if (ContainsAny(candidate.BusinessOutcome, "leadership", "strategic", "pricing", "growth", "planning"))
        {
            return ConsultantActionability.Strategic;
        }

        return ConsultantActionability.Informational;
    }

    private static ConsultantAdoptionLikelihood ResolveAdoptionLikelihood(
        OpportunityExperienceType selectedType,
        ConsultantDecisionCadence cadence,
        ConsultantWorkflowOrientation workflowOrientation,
        ConsultantActionability actionability,
        RecommendationSignalProfile signalProfile)
    {
        var score = 0.45d;

        if (cadence == ConsultantDecisionCadence.Daily || cadence == ConsultantDecisionCadence.Weekly)
        {
            score += 0.16d;
        }

        if (workflowOrientation == ConsultantWorkflowOrientation.Act && selectedType == OpportunityExperienceType.FabricApp)
        {
            score += 0.2d;
        }

        if (workflowOrientation == ConsultantWorkflowOrientation.Investigate &&
            (selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience || selectedType == OpportunityExperienceType.FabricDataApp))
        {
            score += 0.16d;
        }

        if (actionability == ConsultantActionability.Strategic && selectedType == OpportunityExperienceType.ExecutiveDashboard)
        {
            score += 0.12d;
        }

        if (signalProfile.DimensionSignals.Count >= 2)
        {
            score += 0.06d;
        }

        return score >= 0.75d
            ? ConsultantAdoptionLikelihood.High
            : score >= 0.58d
                ? ConsultantAdoptionLikelihood.Medium
                : ConsultantAdoptionLikelihood.Low;
    }

    private static ConsultantMaintenanceComplexity ResolveMaintenanceComplexity(
        OpportunityExperienceType selectedType,
        double complexityBurden)
    {
        if (selectedType == OpportunityExperienceType.FabricApp || selectedType == OpportunityExperienceType.FabricDataApp || complexityBurden >= 0.67d)
        {
            return ConsultantMaintenanceComplexity.High;
        }

        if (selectedType == OpportunityExperienceType.OperationalMonitoringExperience ||
            selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience ||
            complexityBurden >= 0.45d)
        {
            return ConsultantMaintenanceComplexity.Medium;
        }

        return ConsultantMaintenanceComplexity.Low;
    }

    private static double CalculateConsultantJudgmentScore(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        ConsultantDomainFramework domainFramework,
        ConsultantDecisionCadence cadence,
        ConsultantWorkflowOrientation workflowOrientation,
        ConsultantActionability actionability,
        ConsultantAdoptionLikelihood adoptionLikelihood,
        ConsultantMaintenanceComplexity maintenanceComplexity,
        RecommendationSignalProfile signalProfile,
        IReadOnlyList<ExperienceTypeFitEvaluation> alternatives)
    {
        var score = 0.38d;

        score += cadence switch
        {
            ConsultantDecisionCadence.Daily when selectedType is OpportunityExperienceType.FabricApp or OpportunityExperienceType.OperationalMonitoringExperience => 0.14d,
            ConsultantDecisionCadence.Monthly or ConsultantDecisionCadence.Quarterly when selectedType is OpportunityExperienceType.ExecutiveDashboard or OpportunityExperienceType.PbirReport => 0.12d,
            ConsultantDecisionCadence.Episodic when selectedType is OpportunityExperienceType.AnalyticalInvestigationExperience or OpportunityExperienceType.FabricDataApp => 0.14d,
            _ => 0.04d,
        };

        score += workflowOrientation switch
        {
            ConsultantWorkflowOrientation.Act when selectedType == OpportunityExperienceType.FabricApp => 0.22d,
            ConsultantWorkflowOrientation.Monitor when selectedType == OpportunityExperienceType.OperationalMonitoringExperience => 0.18d,
            ConsultantWorkflowOrientation.Investigate when selectedType is OpportunityExperienceType.AnalyticalInvestigationExperience or OpportunityExperienceType.FabricDataApp => 0.2d,
            ConsultantWorkflowOrientation.Govern when selectedType is OpportunityExperienceType.ExecutiveDashboard or OpportunityExperienceType.PbirReport => 0.12d,
            _ => -0.06d,
        };

        score += actionability switch
        {
            ConsultantActionability.Operational when selectedType is OpportunityExperienceType.FabricApp or OpportunityExperienceType.OperationalMonitoringExperience => 0.12d,
            ConsultantActionability.Strategic when selectedType == OpportunityExperienceType.ExecutiveDashboard => 0.1d,
            ConsultantActionability.Informational when selectedType == OpportunityExperienceType.PbirReport => 0.08d,
            _ => 0d,
        };

        score += adoptionLikelihood switch
        {
            ConsultantAdoptionLikelihood.High => 0.08d,
            ConsultantAdoptionLikelihood.Medium => 0.03d,
            _ => -0.04d,
        };

        score += maintenanceComplexity switch
        {
            ConsultantMaintenanceComplexity.Low => 0.06d,
            ConsultantMaintenanceComplexity.Medium => 0.02d,
            _ => -0.06d,
        };

        score += GetDomainFrameworkAdjustment(candidate, selectedType, domainFramework, workflowOrientation, signalProfile);
        score -= CalculateDomainDilutionPenalty(profile, candidate, domainFramework);

        if (domainFramework == ConsultantDomainFramework.RevenueSales &&
            selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience &&
            HasAudience(profile, "Executive") &&
            HasAudience(profile, "Operational"))
        {
            score -= 0.34d;
        }

        if (domainFramework == ConsultantDomainFramework.RevenueSales &&
            selectedType == OpportunityExperienceType.ExecutiveDashboard &&
            candidate.Category == OpportunityCategory.ExecutiveReporting &&
            HasAudience(profile, "Operational") &&
            HasAudience(profile, "Analytical") &&
            !HasInvestigativeIntent(candidate))
        {
            score += 0.16d;
        }

        if (domainFramework == ConsultantDomainFramework.RevenueSales &&
            selectedType == OpportunityExperienceType.FabricApp &&
            HasAudience(profile, "Operational") &&
            HasManagementIntent(candidate))
        {
            score += 0.18d;
        }

        if (alternatives.Count > 0)
        {
            score += Clamp01(NormalizeExperienceFit(ScoreGap(selectedType, alternatives)) / 2d) * 0.08d;
        }

        if (profile.AmbiguityNotes.Count >= 2)
        {
            score -= 0.08d;
        }

        return Clamp01(score);
    }

    private static double CalculateDomainDilutionPenalty(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        ConsultantDomainFramework domainFramework)
    {
        if (domainFramework != ConsultantDomainFramework.RevenueSales)
        {
            return 0d;
        }

        var penalty = 0d;

        if (ProfileHasHighDomain(profile, "Forecasting") && !HasDomain(candidate, "Forecasting"))
        {
            penalty += 0.12d;
        }

        if (ProfileHasHighDomain(profile, "Profitability") && !HasDomain(candidate, "Profitability"))
        {
            penalty += 0.14d;
        }

        if (ProfileHasHighDomain(profile, "Customer") && !HasDomain(candidate, "Customer"))
        {
            penalty += 0.1d;
        }

        if (candidate.Category == OpportunityCategory.ExecutiveReporting && penalty > 0d)
        {
            penalty += 0.06d;
        }

        return penalty;
    }

    private static double GetDomainFrameworkAdjustment(
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        ConsultantDomainFramework domainFramework,
        ConsultantWorkflowOrientation workflowOrientation,
        RecommendationSignalProfile signalProfile)
    {
        return domainFramework switch
        {
            ConsultantDomainFramework.RevenueSales when workflowOrientation == ConsultantWorkflowOrientation.Act && selectedType == OpportunityExperienceType.FabricApp => 0.24d,
            ConsultantDomainFramework.RevenueSales when workflowOrientation != ConsultantWorkflowOrientation.Act &&
                selectedType == OpportunityExperienceType.ExecutiveDashboard &&
                candidate.Category == OpportunityCategory.ExecutiveReporting &&
                HasSignal(candidate, "KpiCluster") &&
                HasSignal(candidate, "DateIntelligence", "High") => 0.24d,
            ConsultantDomainFramework.RevenueSales when workflowOrientation != ConsultantWorkflowOrientation.Act && selectedType == OpportunityExperienceType.ExecutiveDashboard && candidate.Category == OpportunityCategory.ExecutiveReporting => 0.24d,
            ConsultantDomainFramework.RevenueSales when workflowOrientation != ConsultantWorkflowOrientation.Act && selectedType == OpportunityExperienceType.ExecutiveDashboard => 0.14d,
            ConsultantDomainFramework.RevenueSales when workflowOrientation == ConsultantWorkflowOrientation.Act && selectedType == OpportunityExperienceType.ExecutiveDashboard => -0.18d,
            ConsultantDomainFramework.RevenueSales when selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience && !HasInvestigativeIntent(candidate) => -0.24d,
            ConsultantDomainFramework.Forecasting when selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience && HasMeasureSignal(candidate, "Forecast") && HasInvestigativeIntent(candidate) => 0.14d,
            ConsultantDomainFramework.Forecasting when selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience && HasMeasureSignal(candidate, "Forecast") && !HasInvestigativeIntent(candidate) => -0.22d,
            ConsultantDomainFramework.Forecasting when selectedType == OpportunityExperienceType.ExecutiveDashboard && HasPlanningIntent(candidate) => 0.24d,
            ConsultantDomainFramework.Forecasting when selectedType == OpportunityExperienceType.ExecutiveDashboard && HasMeasureSignal(candidate, "Forecast") => 0.08d,
            ConsultantDomainFramework.Forecasting when selectedType == OpportunityExperienceType.FabricApp && workflowOrientation == ConsultantWorkflowOrientation.Act => 0.22d,
            ConsultantDomainFramework.CustomerProfitability when selectedType is OpportunityExperienceType.FabricDataApp or OpportunityExperienceType.AnalyticalInvestigationExperience => 0.3d,
            ConsultantDomainFramework.CustomerProfitability when selectedType == OpportunityExperienceType.ExecutiveDashboard => -0.18d,
            ConsultantDomainFramework.Inventory when selectedType == OpportunityExperienceType.OperationalMonitoringExperience => 0.14d,
            ConsultantDomainFramework.ServiceOperations when workflowOrientation == ConsultantWorkflowOrientation.Act && selectedType == OpportunityExperienceType.FabricApp => 0.24d,
            ConsultantDomainFramework.ServiceOperations when workflowOrientation == ConsultantWorkflowOrientation.Monitor && selectedType == OpportunityExperienceType.OperationalMonitoringExperience => 0.18d,
            ConsultantDomainFramework.AnalyticalInvestigation when selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience => 0.24d,
            _ => signalProfile.DimensionSignals.Count >= 2 ? 0.04d : 0d,
        };
    }

    private static IReadOnlyList<string> BuildAlternativeLossReasons(
        OpportunityExperienceType selectedType,
        IReadOnlyList<ExperienceTypeFitEvaluation> alternatives,
        OpportunityCandidate candidate,
        RecommendationSignalProfile signalProfile)
    {
        if (alternatives.Count == 0)
        {
            return ["The selected path has the strongest balance of audience fit, cadence, actionability, and maintainability."];
        }

        return alternatives
            .Take(2)
            .Select(alternative => BuildAlternativeComparison(selectedType, alternative.ExperienceType, candidate, signalProfile))
            .ToList();
    }

    private static IReadOnlyList<string> BuildRiskSummary(
        OpportunityExperienceType selectedType,
        OpportunityCandidate candidate,
        IReadOnlyList<string> limitingFactors)
    {
        var risks = new List<string>();

        if (limitingFactors.Count > 0)
        {
            risks.Add($"The current model still carries constraint risk: {limitingFactors[0]}");
        }

        risks.Add(selectedType switch
        {
            OpportunityExperienceType.FabricApp => "This path asks the team to maintain a richer workflow surface, so under-scoped design effort will hurt adoption.",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "This path can drift into analyst-only usage if the business question is not kept explicit.",
            OpportunityExperienceType.FabricDataApp => "This path can stay too open-ended if the customer segmentation story is not curated tightly.",
            OpportunityExperienceType.ExecutiveDashboard => "This path can hide the follow-through workflow if leaders also need owners and next steps.",
            _ => "This path needs enough semantic discipline to avoid drifting back into a generic template."
        });

        return risks;
    }

    private static IReadOnlyList<string> BuildAssumptionSummary(
        OpportunityCandidate candidate,
        RecommendationSignalProfile signalProfile,
        DiscoveryProfile profile)
    {
        var assumptions = new List<string>
        {
            $"The recommendation assumes a {signalProfile.DecisionCadence.ToLowerInvariant()} decision rhythm and {signalProfile.InteractionFrequency.ToLowerInvariant()} repeat usage.",
            signalProfile.DimensionSignals.Count > 0
                ? $"It assumes {string.Join(" and ", signalProfile.DimensionSignals.Take(2))} remain stable anchors for user navigation."
                : "It assumes the current semantic model exposes stable navigation anchors."
        };

        if (profile.DateIntelligence.Readiness == DiscoveryDateIntelligenceReadiness.Low)
        {
            assumptions.Add("It assumes time-based comparison can be improved later without changing the experience family.");
        }

        return assumptions;
    }

    private static string BuildAdoptionConsiderations(
        OpportunityExperienceType selectedType,
        OpportunityCandidate candidate,
        RecommendationSignalProfile signalProfile,
        ConsultantAdoptionLikelihood adoptionLikelihood)
    {
        var adoptionLabel = adoptionLikelihood.ToString().ToLowerInvariant();
        var audience = string.IsNullOrWhiteSpace(candidate.InferredAudience) ? "the expected audience" : candidate.InferredAudience;

        return selectedType switch
        {
            OpportunityExperienceType.FabricApp => $"{audience} should adopt this with {adoptionLabel} likelihood if the workflow includes owner changes, technician follow-up, or queue routing inside one surface.",
            OpportunityExperienceType.AnalyticalInvestigationExperience => $"{audience} should adopt this with {adoptionLabel} likelihood if investigative questions stay tied to concrete decisions instead of open-ended analysis.",
            OpportunityExperienceType.FabricDataApp => $"{audience} should adopt this with {adoptionLabel} likelihood if customer and segment exploration stays close to pricing, retention, or account action.",
            OpportunityExperienceType.ExecutiveDashboard => $"{audience} should adopt this with {adoptionLabel} likelihood if leadership mainly wants KPI scanning rather than record-level intervention.",
            _ => $"{audience} should adopt this with {adoptionLabel} likelihood if the experience remains focused on the current decision path."
        };
    }

    private static string BuildFutureEvolutionPath(
        OpportunityExperienceType selectedType,
        ConsultantDomainFramework domainFramework,
        ConsultantWorkflowOrientation workflowOrientation)
    {
        if (selectedType == OpportunityExperienceType.ExecutiveDashboard && workflowOrientation == ConsultantWorkflowOrientation.Act)
        {
            return "If operational follow-through becomes first-class later, this should evolve toward a Fabric App or monitoring-led experience rather than adding more dashboard pages.";
        }

        if (selectedType == OpportunityExperienceType.FabricApp)
        {
            return "If the workflow stabilizes, this can later separate orchestration from executive rollups without rewriting the business framing.";
        }

        if (domainFramework == ConsultantDomainFramework.Forecasting)
        {
            return "If forecast process maturity improves, this can later split planning readouts from diagnostic miss analysis without changing the underlying domain story.";
        }

        return "If adoption broadens, this can later add a secondary experience for adjacent audiences without changing the primary recommendation posture.";
    }

    private static double NormalizeExperienceFit(double score)
    {
        return Clamp01(score / 1.25d);
    }

    private static double ScoreGap(
        OpportunityExperienceType selectedType,
        IReadOnlyList<ExperienceTypeFitEvaluation> alternatives)
    {
        var leadAlternative = alternatives.FirstOrDefault();
        return leadAlternative is null ? 0d : Math.Max(0d, 1d - (leadAlternative.Score / 2d));
    }

    private static string BuildWhyThisWins(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        RecommendationSignalProfile signalProfile,
        IReadOnlyList<string> supportingSignals)
    {
        var audience = string.IsNullOrWhiteSpace(candidate.InferredAudience) ? "the expected audience" : candidate.InferredAudience;
        var signalEvidence = BuildSignalEvidence(candidate, signalProfile, supportingSignals);
        var domainContext = BuildDomainContext(candidate);
        var businessFit = selectedType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "the strongest need is a concise KPI readout for leadership review rather than a workflow surface",
            OpportunityExperienceType.OperationalMonitoringExperience => "the strongest need is repeated operational action on queues, exceptions, and next-owner decisions",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "the strongest need is diagnostic depth and root-cause explanation instead of a faster but shallower readout",
            OpportunityExperienceType.FabricApp => "the strongest need is coordinated routing, technician follow-up, and cross-role handoffs rather than passive monitoring alone",
            OpportunityExperienceType.FabricDataApp => "the strongest need is exploratory slicing across segments before locking the user into a fixed story",
            _ => "the strongest need is a guided report sequence that stages context, evidence, and the final decision takeaway"
        };
        var audienceFit = BuildAudienceFit(profile, candidate, selectedType, signalProfile);

        return $"{GetExperienceTypeLabel(selectedType)} fits {audience} because {businessFit} in a {domainContext} context, the decision cadence is {signalProfile.DecisionCadence.ToLowerInvariant()}, usage is {signalProfile.InteractionFrequency.ToLowerInvariant()} frequency, and {audienceFit}. Signal evidence: {signalEvidence}.";
    }

    private static string BuildDomainContext(OpportunityCandidate candidate)
    {
        var domains = candidate.SupportingSemanticSignals
            .Where(signal => string.Equals(signal.SignalType, "Domain", StringComparison.OrdinalIgnoreCase))
            .Select(signal => signal.Value)
            .Distinct(NameComparer)
            .ToList();

        if (domains.Count == 0)
        {
            return "business decision";
        }

        if (domains.Count == 1)
        {
            return domains[0].ToLowerInvariant();
        }

        return string.Join(" and ", domains.Select(domain => domain.ToLowerInvariant()));
    }

    private static string BuildAudienceFit(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        RecommendationSignalProfile signalProfile)
    {
        if (selectedType == OpportunityExperienceType.FabricApp)
        {
            return signalProfile.DimensionSignals.Count > 0
                ? $"{string.Join(" and ", signalProfile.DimensionSignals.Take(2))} signals show who has to act next"
                : "the workflow implies explicit ownership changes";
        }

        if (selectedType == OpportunityExperienceType.ExecutiveDashboard)
        {
            return "leadership can absorb the recommendation quickly without stepping into operational detail";
        }

        if (selectedType == OpportunityExperienceType.OperationalMonitoringExperience)
        {
            return "front-line users need to see which exception to act on first";
        }

        if (selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience)
        {
            return $"analytical depth is {Math.Round(signalProfile.AnalyticalDepth, 2):0.##}, which supports a slower investigative path";
        }

        if (selectedType == OpportunityExperienceType.FabricDataApp)
        {
            return "commercial and analytical users need to move between segment comparison and record inspection";
        }

        return HasAudience(profile, "Executive") || HasAudience(profile, "Analytical")
            ? "the audience mix supports a staged narrative rather than a single landing page"
            : "the recommendation needs a controlled readout instead of an open canvas";
    }

    private static string BuildExperiencePostureSummary(
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType)
    {
        var labels = new List<string>();

        if (selectedType == OpportunityExperienceType.ExecutiveDashboard ||
            candidate.InferredAudience.Contains("executive", StringComparison.OrdinalIgnoreCase) ||
            candidate.InferredAudience.Contains("leadership", StringComparison.OrdinalIgnoreCase))
        {
            labels.Add("Executive-oriented because the recommendation optimizes for leadership review cadence and concise KPI consumption.");
        }

        if (selectedType is OpportunityExperienceType.FabricApp or OpportunityExperienceType.OperationalMonitoringExperience || HasManagementIntent(candidate))
        {
            labels.Add("Operational-oriented because the recommendation keeps owner follow-through, queue pressure, or next-action management visible.");
        }

        if (selectedType == OpportunityExperienceType.AnalyticalInvestigationExperience || HasInvestigativeIntent(candidate))
        {
            labels.Add("Investigative-oriented because the recommendation expects users to test drivers and explain variance before acting.");
        }

        if (selectedType is OpportunityExperienceType.FabricApp or OpportunityExperienceType.FabricDataApp)
        {
            labels.Add("App-oriented because the recommendation needs a guided multi-step surface rather than a static scan-first page set.");
        }

        if (selectedType is OpportunityExperienceType.ExecutiveDashboard or OpportunityExperienceType.OperationalMonitoringExperience)
        {
            labels.Add("Dashboard-oriented because the recommendation is strongest when users can scan status, variance, and priorities quickly.");
        }

        return string.Join(" ", labels.Distinct(NameComparer));
    }

    private static string BuildSignalEvidence(
        OpportunityCandidate candidate,
        RecommendationSignalProfile signalProfile,
        IReadOnlyList<string> supportingSignals)
    {
        var evidence = new List<string>();

        if (signalProfile.DimensionSignals.Count > 0)
        {
            evidence.Add($"{string.Join(" and ", signalProfile.DimensionSignals.Take(2))} dimensions anchor the user path");
        }

        if (signalProfile.MeasureSignals.Count > 0)
        {
            evidence.Add($"{string.Join(" and ", signalProfile.MeasureSignals.Take(2))} measures define the operational or analytical focus");
        }

        evidence.AddRange(supportingSignals.Take(2));

        return string.Join("; ", evidence
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(NameComparer)
            .Take(3));
    }

    private static string BuildWhyAlternativesLose(
        OpportunityExperienceType selectedType,
        IReadOnlyList<ExperienceTypeFitEvaluation> alternatives,
        OpportunityCandidate candidate,
        RecommendationSignalProfile signalProfile)
    {
        if (alternatives.Count == 0)
        {
            return "the selected path has the strongest overall business fit across audience, workflow, and analytical depth";
        }

        return string.Join(" ", alternatives
            .Take(2)
            .Select(alternative => BuildAlternativeComparison(selectedType, alternative.ExperienceType, candidate, signalProfile)));
    }

    private static string BuildAlternativeComparison(
        OpportunityExperienceType selectedType,
        OpportunityExperienceType alternativeType,
        OpportunityCandidate candidate,
        RecommendationSignalProfile signalProfile)
    {
        var alternativeLabel = GetExperienceTypeLabel(alternativeType);

        return alternativeType switch
        {
            OpportunityExperienceType.OperationalMonitoringExperience when selectedType == OpportunityExperienceType.FabricApp =>
                $"{alternativeLabel} would surface queue pressure, but it would not coordinate the handoffs and follow-up steps implied by {string.Join(" and ", signalProfile.DimensionSignals.Take(2))}, rather than the orchestrated path this scenario needs.",
            OpportunityExperienceType.ExecutiveDashboard =>
                $"{alternativeLabel} would compress the decision into KPI scanning too early and would underplay the workflow or narrative nuance this case needs, rather than preserving the stronger decision path.",
            OpportunityExperienceType.AnalyticalInvestigationExperience =>
                $"{alternativeLabel} would give more drill depth, but it would ask the audience to behave like analysts when the decision rhythm is {signalProfile.DecisionCadence.ToLowerInvariant()}, rather than matching the actual stakeholder rhythm.",
            OpportunityExperienceType.FabricApp =>
                $"{alternativeLabel} would add orchestration overhead that this scenario does not need to justify, rather than focusing effort on the current business need.",
            OpportunityExperienceType.FabricDataApp =>
                $"{alternativeLabel} would keep exploration open, but it would weaken the decision path the current recommendation is trying to defend, rather than closing on a specific decision.",
            _ =>
                $"{alternativeLabel} is credible, but it is weaker because it does not align as tightly to the audience, workflow, and operating model signals in this opportunity, rather than reinforcing the strongest fit."
        };
    }

    private static string BuildBusinessTradeoffSummary(
        OpportunityExperienceType selectedType,
        OpportunityCandidate candidate,
        IReadOnlyList<string> limitingFactors)
    {
        var limitation = limitingFactors.FirstOrDefault();

        var tradeoff = selectedType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "it improves fast leadership alignment but gives up some record-level workflow depth",
            OpportunityExperienceType.OperationalMonitoringExperience => "it improves daily actionability but is less effective for broad narrative storytelling",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "it improves diagnostic depth but requires more analytical patience from the audience",
            OpportunityExperienceType.FabricApp => "it improves orchestration and follow-up but costs more implementation and change-management effort",
            OpportunityExperienceType.FabricDataApp => "it improves exploration flexibility but gives stakeholders a less curated narrative",
            _ => "it improves guided storytelling and drill sequencing but is less flexible for free-form exploration"
        };

        return limitation is null ? tradeoff : $"{tradeoff}; known constraint: {limitation}";
    }

    private static string BuildAudienceTradeoffSummary(
        DiscoveryProfile profile,
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        RecommendationSignalProfile signalProfile)
    {
        return selectedType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => $"Primary value goes to {candidate.InferredAudience} because they need speed and strategic emphasis more than record-level control. The adoption pattern is fast leadership review.",
            OpportunityExperienceType.OperationalMonitoringExperience => $"Primary value goes to {candidate.InferredAudience} because operators need action sequencing more than executive polish. The adoption pattern is repeated in-day use.",
            OpportunityExperienceType.AnalyticalInvestigationExperience => $"Primary value goes to {candidate.InferredAudience} because the audience can absorb a deeper investigation with analytical depth {Math.Round(signalProfile.AnalyticalDepth, 2):0.##}. The adoption pattern is focused analytical sessions.",
            OpportunityExperienceType.FabricApp => $"Primary value goes to {candidate.InferredAudience} because multiple owners must move between routing, follow-up, and confirmation without leaving the experience. The adoption pattern is coordinated workflow use.",
            OpportunityExperienceType.FabricDataApp => $"Primary value goes to {candidate.InferredAudience} because exploratory users need freedom to pivot across segments before a decision is framed. The adoption pattern is repeat exploration.",
            _ => $"Primary value goes to {candidate.InferredAudience} because the audience mix benefits from a guided narrative instead of an open experience shell. The adoption pattern is structured review."
        };
    }

    private static string BuildOperationalTradeoffSummary(
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        RecommendationSignalProfile signalProfile)
    {
        return selectedType switch
        {
            OpportunityExperienceType.FabricApp => $"Operationally, this path wins because {signalProfile.DimensionSignals.DefaultIfEmpty("workflow ownership").Take(2).Aggregate((left, right) => $"{left} and {right}")} point to real routing decisions instead of queue visibility alone.",
            OpportunityExperienceType.OperationalMonitoringExperience => $"Operationally, this path wins because actionability is {Math.Round(signalProfile.OperationalActionability, 2):0.##} and the experience can keep exceptions, backlog, and next actions in one loop.",
            OpportunityExperienceType.ExecutiveDashboard => "Operationally, this path gives up some action detail so leadership can hold the KPI line without managing the workflow directly.",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "Operationally, this path is slower because it favors explanation over immediate queue movement.",
            OpportunityExperienceType.FabricDataApp => "Operationally, this path keeps follow-up looser because exploration matters more than codified task flow.",
            _ => "Operationally, this path stages the review sequence clearly but does not try to own the day-to-day workflow."
        };
    }

    private static string BuildAnalyticalTradeoffSummary(
        OpportunityCandidate candidate,
        OpportunityExperienceType selectedType,
        RecommendationSignalProfile signalProfile)
    {
        return selectedType switch
        {
            OpportunityExperienceType.AnalyticalInvestigationExperience => $"Analytically, this path wins because depth is {Math.Round(signalProfile.AnalyticalDepth, 2):0.##} and the user needs to test drivers before acting.",
            OpportunityExperienceType.ExecutiveDashboard => "Analytically, this path stays intentionally shallow so the audience can decide quickly rather than branch into investigation.",
            OpportunityExperienceType.FabricApp => "Analytically, this path keeps just enough evidence in context to support the next action without turning the experience into analyst-first exploration.",
            OpportunityExperienceType.FabricDataApp => "Analytically, this path keeps the model open for segment and cohort exploration before a narrative is fixed.",
            OpportunityExperienceType.OperationalMonitoringExperience => "Analytically, this path focuses on exception diagnosis only to the level needed for the next operational action.",
            _ => "Analytically, this path uses staged narrative evidence so the audience can follow the logic without owning a full investigation workflow."
        };
    }

    private static string InferAdoptionPattern(OpportunityExperienceType experienceType, OpportunityCandidate candidate)
    {
        return experienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "leadership reviews it in recurring KPI check-ins and uses it as the first stop before escalation",
            OpportunityExperienceType.OperationalMonitoringExperience => "front-line operators return to it throughout the day to find exceptions and trigger action",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "analysts open it when a variance or business question needs focused root-cause work",
            OpportunityExperienceType.FabricApp => "cross-functional teams use it when the workflow needs routing, handoff, and follow-up coordination",
            OpportunityExperienceType.FabricDataApp => "commercial and analytical users adopt it as a repeat exploration surface for segment and cohort questions",
            _ => ContainsAny(candidate.BusinessOutcome, "review", "brief", "explain", "narrative")
                ? "stakeholders use it during structured review moments and then drill through the narrative sequence"
                : "stakeholders use it as a curated report path for periodic decision reviews"
        };
    }

    private static string InferDecisionCadence(OpportunityCandidate candidate)
    {
        var text = $"{candidate.Name} {candidate.BusinessOutcome}";

        if (ContainsAny(text, "daily", "queue", "backlog", "sla", "exception", "monitor"))
        {
            return "Daily";
        }

        if (ContainsAny(text, "monthly", "quarterly", "board"))
        {
            return "Monthly";
        }

        if (ContainsAny(text, "investigate", "root cause", "deep dive", "hypothesis"))
        {
            return "Episodic";
        }

        return "Weekly";
    }

    private static string InferInteractionFrequency(OpportunityCandidate candidate)
    {
        var text = $"{candidate.Name} {candidate.BusinessOutcome}";

        if (ContainsAny(text, "daily", "monitor", "queue", "backlog", "triage", "follow-up"))
        {
            return "High";
        }

        if (ContainsAny(text, "investigate", "brief", "review", "leadership"))
        {
            return "Low";
        }

        return "Medium";
    }

    private static double CalculateOperationalActionability(OpportunityCandidate candidate)
    {
        var text = $"{candidate.Name} {candidate.BusinessOutcome}";
        var actionability = 0d;

        if (ContainsAny(text, "action", "assign", "route", "handoff", "triage", "follow-up"))
        {
            actionability += 0.14d;
        }

        if (ContainsAny(text, "monitor", "queue", "exception", "backlog", "sla"))
        {
            actionability += 0.12d;
        }

        if (HasDomain(candidate, "Inventory") || HasDomain(candidate, "Service"))
        {
            actionability += 0.08d;
        }

        return Clamp01(actionability);
    }

    private static bool HasPlanningIntent(OpportunityCandidate candidate)
    {
        return ContainsAny(
            $"{candidate.Name} {candidate.BusinessOutcome}",
            "plan",
            "planning",
            "forecast",
            "variance management",
            "forecast accuracy",
            "performance management",
            "planning cycle");
    }

    private static bool HasManagementIntent(OpportunityCandidate candidate)
    {
        return ContainsAny(
            $"{candidate.Name} {candidate.BusinessOutcome}",
            "manage",
            "management",
            "assign",
            "route",
            "owner",
            "follow-up",
            "triage",
            "pipeline review",
            "coordinate");
    }

    private static bool HasInvestigativeIntent(OpportunityCandidate candidate)
    {
        return ContainsAny(
            $"{candidate.Name} {candidate.BusinessOutcome}",
            "investigate",
            "investigation",
            "root cause",
            "why",
            "driver",
            "diagnostic",
            "miss patterns");
    }

    private static bool WorkflowLooksSimilar(DiscoveryRecommendation left, DiscoveryRecommendation right)
    {
        return string.Equals(ResolveWorkflowShape(left), ResolveWorkflowShape(right), StringComparison.OrdinalIgnoreCase);
    }

    private static bool DecisionPatternsLookSimilar(DiscoveryRecommendation left, DiscoveryRecommendation right)
    {
        return string.Equals(ResolveDecisionPattern(left), ResolveDecisionPattern(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveWorkflowShape(DiscoveryRecommendation recommendation)
    {
        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.FabricApp)
        {
            return "workflow-app";
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.AnalyticalInvestigationExperience)
        {
            return "investigation";
        }

        if (ContainsAny(recommendation.ExpectedBusinessOutcome, "plan", "planning", "forecast", "variance"))
        {
            return "planning";
        }

        if (ContainsAny(recommendation.ExpectedBusinessOutcome, "monitor", "queue", "backlog", "exception"))
        {
            return "monitoring";
        }

        return "summary";
    }

    private static string ResolveDecisionPattern(DiscoveryRecommendation recommendation)
    {
        return recommendation.RecommendedExperienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "executive-consumption",
            OpportunityExperienceType.OperationalMonitoringExperience => "operational-management",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "investigative-analysis",
            OpportunityExperienceType.FabricApp => "workflow-execution",
            OpportunityExperienceType.FabricDataApp => "exploratory-analysis",
            _ => "narrative-review"
        };
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

    private static string BuildConfidenceNote(DiscoveryConfidenceLevel confidence)
    {
        return confidence switch
        {
            DiscoveryConfidenceLevel.High => "High confidence because the semantic model strongly supports this use case.",
            DiscoveryConfidenceLevel.Medium => "Medium confidence because the model supports this use case but still leaves some ambiguity.",
            _ => "Low confidence because the recommendation is inferred from sparse or ambiguous model signals."
        };
    }

    private static string BuildComplexityNote(
        RecommendationComplexityLevel complexity,
        OpportunityExperienceType experienceType)
    {
        var experienceLabel = experienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "a concise executive KPI experience",
            OpportunityExperienceType.OperationalMonitoringExperience => "an operational monitoring flow",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "an analytical drill-based experience",
            OpportunityExperienceType.FabricDataApp => "a data-centric exploration experience",
            OpportunityExperienceType.FabricApp => "a multi-path app experience",
            _ => "a report-oriented experience"
        };

        return complexity switch
        {
            RecommendationComplexityLevel.High => $"High complexity because {experienceLabel} needs broader semantic coordination and design shaping.",
            RecommendationComplexityLevel.Medium => $"Medium complexity because {experienceLabel} spans several semantic signals and design choices.",
            _ => $"Low complexity because {experienceLabel} can be shaped from a relatively focused semantic footprint."
        };
    }

    private static double NormalizeCount(int count, int cap)
    {
        if (cap <= 0)
        {
            return 0d;
        }

        return Clamp01((double)Math.Min(count, cap) / cap);
    }

    private static double MapConfidence(DiscoveryConfidenceLevel confidence)
    {
        return confidence switch
        {
            DiscoveryConfidenceLevel.High => 1d,
            DiscoveryConfidenceLevel.Medium => 0.65d,
            _ => 0.35d
        };
    }

    private static double Average(params double[] values)
    {
        return values.Length == 0 ? 0d : values.Average();
    }

    private static bool OutcomesLookSimilar(string left, string right)
    {
        return CalculateTokenSimilarity(left, right) >= 0.55d;
    }

    private static bool ThemesLookSimilar(DiscoveryRecommendation left, DiscoveryRecommendation right)
    {
        return CalculateTokenSimilarity(
            $"{left.RecommendationName} {left.ExpectedBusinessOutcome} {string.Join(" ", left.SupportingSignals)}",
            $"{right.RecommendationName} {right.ExpectedBusinessOutcome} {string.Join(" ", right.SupportingSignals)}") >= 0.5d;
    }

    private static bool BelongsToSameExperienceFamily(DiscoveryRecommendation left, DiscoveryRecommendation right)
    {
        return GetExperienceFamily(left.RecommendedExperienceType) == GetExperienceFamily(right.RecommendedExperienceType);
    }

    private static string GetExperienceFamily(OpportunityExperienceType experienceType)
    {
        return experienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => "dashboard",
            OpportunityExperienceType.PbirReport => "report",
            OpportunityExperienceType.OperationalMonitoringExperience => "workflow-monitoring",
            OpportunityExperienceType.FabricApp => "workflow-monitoring",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "analysis",
            OpportunityExperienceType.FabricDataApp => "analysis",
            _ => "other"
        };
    }

    private static double CalculateTokenSimilarity(string left, string right)
    {
        var leftTokens = Tokenize(left);
        var rightTokens = Tokenize(right);

        if (leftTokens.Count == 0 || rightTokens.Count == 0)
        {
            return 0d;
        }

        var overlap = leftTokens.Intersect(rightTokens, NameComparer).Count();
        var union = leftTokens.Union(rightTokens, NameComparer).Count();
        return union == 0 ? 0d : (double)overlap / union;
    }

    private static HashSet<string> Tokenize(string value)
    {
        var stopWords = new HashSet<string>(NameComparer)
        {
            "the", "and", "for", "with", "that", "this", "over", "into", "from", "through",
            "executive", "dashboard", "report", "experience", "management"
        };

        return value
            .Split([' ', '-', ',', '.', ';', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToLowerInvariant())
            .Where(token => token.Length > 2 && !stopWords.Contains(token))
            .ToHashSet(NameComparer);
    }

    private static bool HasAudience(DiscoveryProfile profile, string audience)
    {
        return profile.AudienceSignals.Any(signal => string.Equals(signal.Audience, audience, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ProfileHasHighDomain(DiscoveryProfile profile, string domain)
    {
        return profile.BusinessDomains.Any(signal =>
            string.Equals(signal.Domain, domain, StringComparison.OrdinalIgnoreCase) &&
            MapConfidence(signal.Confidence) >= 0.65d);
    }

    private static bool HasDomain(OpportunityCandidate candidate, string value)
    {
        return candidate.SupportingSemanticSignals.Any(signal =>
            string.Equals(signal.SignalType, "Domain", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(signal.Value, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasMeasureSignal(OpportunityCandidate candidate, string value)
    {
        return candidate.SupportingSemanticSignals.Any(signal =>
            string.Equals(signal.SignalType, "Measure", StringComparison.OrdinalIgnoreCase) &&
            signal.Value.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSignal(OpportunityCandidate candidate, string signalType)
    {
        return candidate.SupportingSemanticSignals.Any(signal =>
            string.Equals(signal.SignalType, signalType, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasSignal(OpportunityCandidate candidate, string signalType, string value)
    {
        return candidate.SupportingSemanticSignals.Any(signal =>
            string.Equals(signal.SignalType, signalType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(signal.Value, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static TEnum Max<TEnum>(TEnum left, TEnum right)
        where TEnum : struct, Enum
    {
        return Comparer<int>.Default.Compare(Convert.ToInt32(left), Convert.ToInt32(right)) >= 0 ? left : right;
    }

    private static double Clamp01(double value)
    {
        return Math.Max(0d, Math.Min(1d, value));
    }
}
