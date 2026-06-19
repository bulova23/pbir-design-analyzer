using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class OpportunityIdentificationService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    internal OpportunityCatalog BuildOpportunityCatalog(DiscoveryProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        var candidates = new List<OpportunityCandidate>();

        AddRevenueReportingCandidates(profile, candidates);
        AddCustomerCandidates(profile, candidates);
        AddInventoryCandidates(profile, candidates);
        AddServiceCandidates(profile, candidates);
        AddForecastCandidates(profile, candidates);
        AddAnalyticalInvestigationCandidates(profile, candidates);
        AddComparativePerformanceCandidates(profile, candidates);

        var deduplicated = Deduplicate(candidates);
        return new OpportunityCatalog(deduplicated);
    }

    private static void AddRevenueReportingCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Revenue"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "executive-sales-reporting",
            name: "Executive Sales Reporting",
            category: OpportunityCategory.ExecutiveReporting,
            audience: ChooseAudience(profile, "Executive", "Sales Leadership"),
            businessOutcome: "Track revenue trends and leadership-level performance over time.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.FabricApp
            ],
            requiredSignals:
            [
                ("Domain", "Revenue"),
                ("DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            optionalSignals:
            [
                CreateOptionalSignal(profile, HasGeography(profile), "Dimension", "Geography"),
                CreateOptionalSignal(profile, HasKpiCluster(profile, "Revenue KPIs"), "KpiCluster", "Revenue KPIs")
            ],
            isTimeDependent: true));

        if (HasGeography(profile))
        {
            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "sales-performance-dashboard",
                name: "Sales Performance Dashboard",
                category: OpportunityCategory.SalesPerformance,
                audience: ChooseAudience(profile, "Executive", "Sales Leadership"),
                businessOutcome: "Compare revenue and sales performance across regions and territories.",
                experienceTypes:
                [
                    OpportunityExperienceType.ExecutiveDashboard,
                    OpportunityExperienceType.PbirReport
                ],
                requiredSignals:
                [
                    ("Domain", "Revenue"),
                    ("Dimension", "Geography")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(profile, HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString()),
                    CreateOptionalSignal(profile, HasDimension(profile, "Customer"), "Dimension", "Customer")
                ],
                isTimeDependent: true));

            if (HasDimensionName(profile, "territory"))
            {
                candidates.Add(CreateCandidate(
                    profile,
                    opportunityId: "territory-performance-monitoring",
                    name: "Sales Performance Dashboard",
                    category: OpportunityCategory.SalesPerformance,
                    audience: ChooseAudience(profile, "Executive", "Sales Leadership"),
                    businessOutcome: "Compare revenue and sales performance across regions and territories.",
                    experienceTypes:
                    [
                        OpportunityExperienceType.ExecutiveDashboard,
                        OpportunityExperienceType.PbirReport
                    ],
                    requiredSignals:
                    [
                        ("Domain", "Revenue"),
                        ("Dimension", "Territory")
                    ],
                    optionalSignals:
                    [
                        CreateOptionalSignal(profile, HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
                    ],
                    isTimeDependent: true));
            }
        }
    }

    private static void AddCustomerCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (HasDomain(profile, "Customer") && HasDomain(profile, "Profitability"))
        {
            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "customer-profitability-analysis",
                name: "Customer Profitability Analysis",
                category: OpportunityCategory.ProfitabilityAnalysis,
                audience: ChooseAudience(profile, "Analytical", "Commercial Strategy"),
                businessOutcome: "Identify which customer segments and accounts drive profitable growth.",
                experienceTypes:
                [
                    OpportunityExperienceType.AnalyticalInvestigationExperience,
                    OpportunityExperienceType.PbirReport,
                    OpportunityExperienceType.FabricDataApp
                ],
                requiredSignals:
                [
                    ("Domain", "Customer"),
                    ("Domain", "Profitability")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(profile, HasDimension(profile, "Customer"), "Dimension", "Customer"),
                    CreateOptionalSignal(profile, HasDimensionName(profile, "segment"), "Dimension", "Customer Segment")
                ],
                isTimeDependent: false));
        }
        else if (HasDomain(profile, "Customer"))
        {
            candidates.Add(CreateCandidate(
                profile,
                opportunityId: "customer-segmentation-experience",
                name: "Customer Segmentation Experience",
                category: OpportunityCategory.CustomerAnalysis,
                audience: ChooseAudience(profile, "Analytical", "Commercial Strategy"),
                businessOutcome: "Segment customers for retention, growth, and experience analysis.",
                experienceTypes:
                [
                    OpportunityExperienceType.PbirReport,
                    OpportunityExperienceType.FabricDataApp,
                    OpportunityExperienceType.AnalyticalInvestigationExperience
                ],
                requiredSignals:
                [
                    ("Domain", "Customer")
                ],
                optionalSignals:
                [
                    CreateOptionalSignal(profile, HasDimensionName(profile, "segment"), "Dimension", "Customer Segment"),
                    CreateOptionalSignal(profile, HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
                ],
                isTimeDependent: false));
        }
    }

    private static void AddInventoryCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Inventory"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "inventory-operations-monitoring",
            name: "Inventory Operations Monitoring",
            category: OpportunityCategory.InventoryOptimization,
            audience: ChooseAudience(profile, "Operational", "Supply Chain Operations"),
            businessOutcome: "Monitor stock position, warehouse health, and item-level inventory risk.",
            experienceTypes:
            [
                OpportunityExperienceType.OperationalMonitoringExperience,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.FabricApp
            ],
            requiredSignals:
            [
                ("Domain", "Inventory")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(profile, HasDimension(profile, "Inventory"), "Dimension", "Inventory"),
                CreateOptionalSignal(profile, HasDimension(profile, "Product"), "Dimension", "Product"),
                CreateOptionalSignal(profile, HasMeasureName(profile, "quantity"), "Measure", "Quantity")
            ],
            isTimeDependent: true));
    }

    private static void AddServiceCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Service"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "service-operations-dashboard",
            name: "Service Operations Dashboard",
            category: OpportunityCategory.ServiceOperations,
            audience: ChooseAudience(profile, "Service", "Service Operations"),
            businessOutcome: "Monitor service workload, technician performance, and resolution effectiveness.",
            experienceTypes:
            [
                OpportunityExperienceType.OperationalMonitoringExperience,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.FabricApp
            ],
            requiredSignals:
            [
                ("Domain", "Service")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(profile, HasDimensionName(profile, "technician"), "Dimension", "Technician"),
                CreateOptionalSignal(profile, HasDimensionName(profile, "work order") || HasDimensionName(profile, "ticket"), "Dimension", "Work Order"),
                CreateOptionalSignal(profile, HasMeasureName(profile, "resolution"), "Measure", "Resolution")
            ],
            isTimeDependent: true));
    }

    private static void AddForecastCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!HasDomain(profile, "Forecasting"))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "forecast-accuracy-dashboard",
            name: "Forecast Accuracy Dashboard",
            category: OpportunityCategory.ForecastAccuracy,
            audience: ChooseAudience(profile, "Executive", "Planning Leadership"),
            businessOutcome: "Compare forecast, actuals, and variance to improve planning accuracy.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.AnalyticalInvestigationExperience
            ],
            requiredSignals:
            [
                ("Domain", "Forecasting")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(profile, HasMeasureName(profile, "actual"), "Measure", "Actual"),
                CreateOptionalSignal(profile, HasMeasureName(profile, "variance"), "Measure", "Variance"),
                CreateOptionalSignal(profile, HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            isTimeDependent: true));
    }

    private static void AddAnalyticalInvestigationCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        var hasAnalyticalAudience = HasAudience(profile, "Analytical");
        var hasDrillSignals = profile.Hierarchies.Count > 0 || profile.Dimensions.Count >= 4 || profile.Relationships.Count >= 3;
        var hasRootCauseMeasureSignals = HasMeasureName(profile, "variance") || HasMeasureName(profile, "root cause");

        if (!hasAnalyticalAudience || !hasDrillSignals || !hasRootCauseMeasureSignals)
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "root-cause-analysis-experience",
            name: "Root Cause Analysis Experience",
            category: OpportunityCategory.RootCauseInvestigation,
            audience: "Analytical",
            businessOutcome: "Investigate drivers of variance through drill-based root cause analysis.",
            experienceTypes:
            [
                OpportunityExperienceType.AnalyticalInvestigationExperience,
                OpportunityExperienceType.PbirReport
            ],
            requiredSignals:
            [
                ("Audience", "Analytical"),
                ("Measure", "Variance"),
                ("Drill", "HierarchyRich")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(profile, profile.Relationships.Count >= 3, "RelationshipCount", profile.Relationships.Count.ToString()),
                CreateOptionalSignal(profile, profile.Hierarchies.Count > 0, "HierarchyCount", profile.Hierarchies.Count.ToString())
            ],
            isTimeDependent: false));
    }

    private static void AddComparativePerformanceCandidates(DiscoveryProfile profile, ICollection<OpportunityCandidate> candidates)
    {
        if (!(HasDomain(profile, "Revenue") || HasDomain(profile, "Profitability")) || !HasGeography(profile))
        {
            return;
        }

        candidates.Add(CreateCandidate(
            profile,
            opportunityId: "comparative-performance-management",
            name: "Comparative Performance Management",
            category: OpportunityCategory.ComparativePerformanceManagement,
            audience: ChooseAudience(profile, "Executive", "Performance Management"),
            businessOutcome: "Compare performance across business units, territories, and periods.",
            experienceTypes:
            [
                OpportunityExperienceType.ExecutiveDashboard,
                OpportunityExperienceType.PbirReport,
                OpportunityExperienceType.AnalyticalInvestigationExperience
            ],
            requiredSignals:
            [
                ("Dimension", "Geography")
            ],
            optionalSignals:
            [
                CreateOptionalSignal(profile, HasDomain(profile, "Revenue"), "Domain", "Revenue"),
                CreateOptionalSignal(profile, HasDomain(profile, "Profitability"), "Domain", "Profitability"),
                CreateOptionalSignal(profile, HasDateReadiness(profile), "DateIntelligence", profile.DateIntelligence.Readiness.ToString())
            ],
            isTimeDependent: true));
    }

    private static OpportunityCandidate CreateCandidate(
        DiscoveryProfile profile,
        string opportunityId,
        string name,
        OpportunityCategory category,
        string audience,
        string businessOutcome,
        IReadOnlyList<OpportunityExperienceType> experienceTypes,
        IReadOnlyList<(string SignalType, string Value)> requiredSignals,
        IReadOnlyList<(bool Include, string SignalType, string Value)> optionalSignals,
        bool isTimeDependent)
    {
        var signals = requiredSignals
            .Select(signal => new OpportunitySemanticSignal(signal.SignalType, signal.Value))
            .Concat(optionalSignals
                .Where(signal => signal.Include)
                .Select(signal => new OpportunitySemanticSignal(signal.SignalType, signal.Value)))
            .Distinct()
            .ToList();

        var confidence = CalculateOpportunityConfidence(profile, signals.Count, isTimeDependent);
        return new OpportunityCandidate(
            OpportunityId: opportunityId,
            Name: name,
            Category: category,
            InferredAudience: audience,
            BusinessOutcome: businessOutcome,
            CandidateExperienceTypes: experienceTypes
                .Distinct()
                .OrderBy(type => type.ToString(), NameComparer)
                .ToList(),
            SupportingSemanticSignals: signals,
            LimitingFactors: BuildLimitingFactors(profile, signals.Count, isTimeDependent),
            Confidence: confidence);
    }

    private static IReadOnlyList<OpportunityCandidate> Deduplicate(IReadOnlyList<OpportunityCandidate> candidates)
    {
        var deduplicated = new Dictionary<string, OpportunityCandidate>(NameComparer);

        foreach (var candidate in candidates)
        {
            var key = BuildDeduplicationKey(candidate);
            if (!deduplicated.TryGetValue(key, out var existing))
            {
                deduplicated[key] = candidate;
                continue;
            }

            deduplicated[key] = MergeCandidate(existing, candidate);
        }

        return deduplicated.Values
            .OrderBy(candidate => candidate.Confidence)
            .ThenBy(candidate => candidate.Name, NameComparer)
            .ToList();
    }

    private static OpportunityCandidate MergeCandidate(OpportunityCandidate existing, OpportunityCandidate candidate)
    {
        var mergedConfidence = Max(existing.Confidence, candidate.Confidence);
        var mergedSignals = existing.SupportingSemanticSignals
            .Concat(candidate.SupportingSemanticSignals)
            .Distinct()
            .OrderBy(signal => signal.SignalType, NameComparer)
            .ThenBy(signal => signal.Value, NameComparer)
            .ToList();
        var mergedLimitingFactors = existing.LimitingFactors
            .Concat(candidate.LimitingFactors)
            .Distinct(NameComparer)
            .OrderBy(note => note, NameComparer)
            .ToList();
        var mergedExperienceTypes = existing.CandidateExperienceTypes
            .Concat(candidate.CandidateExperienceTypes)
            .Distinct()
            .OrderBy(type => type.ToString(), NameComparer)
            .ToList();

        return existing with
        {
            CandidateExperienceTypes = mergedExperienceTypes,
            SupportingSemanticSignals = mergedSignals,
            LimitingFactors = mergedLimitingFactors,
            Confidence = mergedConfidence
        };
    }

    private static string BuildDeduplicationKey(OpportunityCandidate candidate)
    {
        return $"{candidate.Category}|{candidate.Name}|{candidate.InferredAudience}";
    }

    private static IReadOnlyList<string> BuildLimitingFactors(DiscoveryProfile profile, int signalCount, bool isTimeDependent)
    {
        var notes = profile.AmbiguityNotes.ToList();

        if (signalCount <= 1)
        {
            notes.Add("Opportunity is inferred from limited semantic evidence.");
        }

        if (isTimeDependent && profile.DateIntelligence.Readiness == DiscoveryDateIntelligenceReadiness.Low)
        {
            notes.Add("Weak date intelligence reduces confidence for time-based monitoring.");
        }

        return notes
            .Distinct(NameComparer)
            .OrderBy(note => note, NameComparer)
            .ToList();
    }

    private static DiscoveryConfidenceLevel CalculateOpportunityConfidence(DiscoveryProfile profile, int signalCount, bool isTimeDependent)
    {
        var score = profile.Confidence switch
        {
            DiscoveryConfidenceLevel.High => 3,
            DiscoveryConfidenceLevel.Medium => 2,
            _ => 1
        };

        score += signalCount switch
        {
            >= 4 => 2,
            3 => 1,
            _ => 0
        };

        if (isTimeDependent)
        {
            score += profile.DateIntelligence.Readiness switch
            {
                DiscoveryDateIntelligenceReadiness.High => 1,
                DiscoveryDateIntelligenceReadiness.Low => -1,
                _ => 0
            };
        }

        score -= Math.Min(profile.AmbiguityNotes.Count, 2);

        return score >= 5
            ? DiscoveryConfidenceLevel.High
            : score >= 3
                ? DiscoveryConfidenceLevel.Medium
                : DiscoveryConfidenceLevel.Low;
    }

    private static DiscoveryConfidenceLevel Max(DiscoveryConfidenceLevel left, DiscoveryConfidenceLevel right)
    {
        return (int)left >= (int)right ? left : right;
    }

    private static (bool Include, string SignalType, string Value) CreateOptionalSignal(
        DiscoveryProfile profile,
        bool include,
        string signalType,
        string value)
    {
        return include && !string.IsNullOrWhiteSpace(value)
            ? (true, signalType, value)
            : (false, string.Empty, string.Empty);
    }

    private static bool HasDomain(DiscoveryProfile profile, string domain)
    {
        return profile.BusinessDomains.Any(signal => string.Equals(signal.Domain, domain, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasAudience(DiscoveryProfile profile, string audience)
    {
        return profile.AudienceSignals.Any(signal => string.Equals(signal.Audience, audience, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDimension(DiscoveryProfile profile, string businessRole)
    {
        return profile.Dimensions.Any(dimension => string.Equals(dimension.BusinessRole, businessRole, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDimensionName(DiscoveryProfile profile, string value)
    {
        return profile.Dimensions.Any(dimension => dimension.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasGeography(DiscoveryProfile profile)
    {
        return HasDimension(profile, "Geography") || HasDimensionName(profile, "region") || HasDimensionName(profile, "territory");
    }

    private static bool HasMeasureName(DiscoveryProfile profile, string value)
    {
        return profile.Measures.Any(measure =>
            measure.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(measure.Description) &&
             measure.Description.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool HasKpiCluster(DiscoveryProfile profile, string clusterName)
    {
        return profile.KpiClusters.Any(cluster => string.Equals(cluster.ClusterName, clusterName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDateReadiness(DiscoveryProfile profile)
    {
        return profile.DateIntelligence.Readiness != DiscoveryDateIntelligenceReadiness.Low;
    }

    private static string ChooseAudience(DiscoveryProfile profile, string preferredAudience, string fallback)
    {
        return HasAudience(profile, preferredAudience)
            ? preferredAudience
            : fallback;
    }
}
