using PowerBIModelingService.Services.Discovery.Models;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class ExperienceBlueprintGenerationService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    internal RecommendationSet BuildRecommendationBlueprints(
        DiscoveryProfile profile,
        OpportunityCatalog catalog,
        RecommendationSet recommendations)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(recommendations);

        var opportunities = catalog.Opportunities.ToDictionary(opportunity => opportunity.OpportunityId, NameComparer);

        return new RecommendationSet(
            EnrichRecommendations(profile, opportunities, recommendations.PrimaryRecommendations),
            EnrichRecommendations(profile, opportunities, recommendations.AlternateRecommendations));
    }

    private static IReadOnlyList<DiscoveryRecommendation> EnrichRecommendations(
        DiscoveryProfile profile,
        IReadOnlyDictionary<string, OpportunityCandidate> opportunities,
        IReadOnlyList<DiscoveryRecommendation> recommendations)
    {
        return recommendations
            .Select(recommendation =>
            {
                opportunities.TryGetValue(recommendation.RecommendationId, out var opportunity);
                return recommendation with
                {
                    ExperienceBlueprint = BuildBlueprint(profile, recommendation, opportunity)
                };
            })
            .ToList();
    }

    private static ExperienceBlueprint BuildBlueprint(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        var primaryKpis = BuildPrimaryKpis(profile, recommendation, opportunity);
        var globalFilters = BuildGlobalFilters(profile);
        var pages = BuildPages(profile, recommendation, opportunity, primaryKpis);
        var analyticalFlow = BuildAnalyticalFlow(profile, recommendation, opportunity);
        var navigationIntent = BuildNavigationIntent(profile, recommendation, recommendation.RecommendedExperienceType, opportunity, pages);
        var successCriteria = BuildSuccessCriteria(recommendation, pages, primaryKpis);
        var provenance = BuildProvenance(profile, recommendation, opportunity, primaryKpis);

        return new ExperienceBlueprint(
            BlueprintId: $"{recommendation.RecommendationId}-blueprint",
            ExperienceType: recommendation.RecommendedExperienceType,
            RecommendedPages: pages,
            PrimaryKpis: primaryKpis,
            SuggestedGlobalFilters: globalFilters,
            AnalyticalFlow: analyticalFlow,
            NavigationIntent: navigationIntent,
            ExpectedAudience: recommendation.ExpectedAudience,
            ExpectedBusinessOutcome: recommendation.ExpectedBusinessOutcome,
            SuccessCriteriaSeed: successCriteria,
            Provenance: provenance);
    }

    private static IReadOnlyList<string> BuildPrimaryKpis(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        var collected = new List<string>();

        foreach (var cluster in profile.KpiClusters.OrderByDescending(cluster => cluster.Confidence))
        {
            collected.AddRange(cluster.MeasureNames);
        }

        collected.AddRange(profile.Measures.Select(measure => measure.Name));

        return collected
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(NameComparer)
            .OrderBy(name => GetKpiPriority(name))
            .ThenBy(name => name, NameComparer)
            .Take(5)
            .ToList();
    }

    private static int GetKpiPriority(string name)
    {
        if (name.Contains("revenue", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.Contains("gross margin", StringComparison.OrdinalIgnoreCase) || name.Contains("margin", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (name.Contains("yoy", StringComparison.OrdinalIgnoreCase) || name.Contains("growth", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (name.Contains("forecast", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (name.Contains("retention", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (name.Contains("variance", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        return 10;
    }

    private static IReadOnlyList<string> BuildGlobalFilters(DiscoveryProfile profile)
    {
        var preferred = new[]
        {
            "Date",
            "Region",
            "Territory",
            "Product Category",
            "Product",
            "Customer Segment",
            "Customer",
            "Warehouse",
            "Technician",
            "Work Order",
            "Forecast Period"
        };

        var available = BuildAvailableFilterLabels(profile);

        var selected = preferred
            .Where(filter => available.Any(value => LabelsMatch(value, filter)))
            .ToList();

        if (selected.Count == 0 && available.Count > 0)
        {
            selected.AddRange(available.Take(3));
        }

        if (selected.Count == 0)
        {
            selected.Add("Date");
        }

        return selected
            .Distinct(NameComparer)
            .ToList();
    }

    private static IReadOnlyList<ExperienceBlueprintPage> BuildPages(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        IReadOnlyList<string> primaryKpis)
    {
        return recommendation.RecommendedExperienceType switch
        {
            OpportunityExperienceType.ExecutiveDashboard => BuildExecutivePages(profile, recommendation, opportunity),
            OpportunityExperienceType.OperationalMonitoringExperience => BuildOperationalPages(profile, recommendation, opportunity),
            OpportunityExperienceType.AnalyticalInvestigationExperience => BuildAnalyticalPages(profile, recommendation, opportunity),
            OpportunityExperienceType.FabricApp => BuildFabricAppPages(profile, opportunity),
            OpportunityExperienceType.FabricDataApp => BuildFabricDataAppPages(profile),
            _ => BuildPbirReportPages(profile, opportunity, primaryKpis),
        };
    }

    private static IReadOnlyList<ExperienceBlueprintPage> BuildExecutivePages(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        if (opportunity?.Category == OpportunityCategory.ForecastAccuracy ||
            HasDomain(opportunity, "Forecasting"))
        {
            if (IsExecutiveForecastNarrative(recommendation, opportunity))
            {
                return
                [
                    CreatePage("Executive Forecast Review", "Open with the leadership forecast posture and confidence signal.", PickFilters(profile, "Forecast Period", "Date", "Region"), ["KPI Cards", "Trend Charts", "Scorecards"]),
                    CreatePage("Confidence and Variance Summary", "Explain where confidence is weakening before leadership follow-up is assigned.", PickFilters(profile, "Region", "Forecast Period", "Scenario"), ["Variance Charts", "Line Charts", "Bar Charts"]),
                    CreatePage("Leadership Follow-Up", "Close on the leadership checkpoint, owner, and timing for the next review.", PickFilters(profile, "Region", "Territory", "Date"), ["Scorecards", "Action Tables", "Detail Tables"])
                ];
            }

            return
            [
                CreatePage("Planning Summary", "Anchor the weekly planning review around forecast accuracy, variance posture, and leadership focus.", PickFilters(profile, "Forecast Period", "Date", "Scenario"), ["KPI Cards", "Trend Charts", "Scorecards"]),
                CreatePage("Variance Review", "Explain where misses concentrate across scenarios, regions, and time horizons before corrective action is set.", PickFilters(profile, "Scenario", "Region", "Forecast Period"), ["Variance Charts", "Line Charts", "Bar Charts"]),
                CreatePage("Regional Follow-Up", "Convert variance patterns into owner-level follow-up and the next planning checkpoint.", PickFilters(profile, "Region", "Territory", "Scenario"), ["Scorecards", "Detail Tables", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.ExecutiveReporting ||
            opportunity?.Category == OpportunityCategory.SalesPerformance ||
            HasDomain(opportunity, "Revenue"))
        {
            return
            [
                CreatePage("Revenue Leadership Summary", "Open with the topline revenue posture, mix movement, and leadership decision frame.", PickFilters(profile, "Date", "Region", "Territory"), ["KPI Cards", "Trend Charts", "Scorecards"]),
                CreatePage("Growth and Mix Review", "Compare growth, margin, and territory mix to isolate the strongest commercial drivers.", PickFilters(profile, "Region", "Territory", "Product Category"), ["Bar Charts", "Line Charts", "Waterfall Charts"]),
                CreatePage("Commercial Follow-Up", "Translate the revenue readout into the next commercial checkpoint across customers, products, and territories.", PickFilters(profile, "Customer Segment", "Product Category", "Territory"), ["Detail Tables", "Scatter Charts", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.CustomerAnalysis ||
            HasDomain(opportunity, "Customer"))
        {
            return
            [
                CreatePage("Customer Portfolio Summary", "Frame the portfolio question around segment performance, concentration, and customer posture.", PickFilters(profile, "Date", "Customer Segment", "Region"), ["KPI Cards", "Trend Charts", "Scorecards"]),
                CreatePage("Segment Driver Review", "Organize segment comparisons around the customer groups that change the decision most.", PickFilters(profile, "Customer Segment", "Product Category", "Region"), ["Bar Charts", "Scatter Charts", "Line Charts"]),
                CreatePage("Account Follow-Up", "Carry the segment story into the accounts or categories that need the next move.", PickFilters(profile, "Customer Segment", "Product Category", "Territory"), ["Detail Tables", "Scorecards", "Action Tables"])
            ];
        }

        var pages = new List<ExperienceBlueprintPage>
        {
            CreatePage("Executive Summary", "Fast KPI-first summary for leadership review.", ["Date", "Region"], ["KPI Cards", "Trend Charts", "Scorecards"])
        };

        pages.Add(CreatePage("Revenue Performance", "Revenue trend and driver analysis.", PickFilters(profile, "Product Category", "Customer Segment", "Date"), ["Bar Charts", "Line Charts", "Decomposition Trees"]));

        if (HasDimension(profile, "Territory") || HasDimension(profile, "Region"))
        {
            pages.Add(CreatePage("Territory Performance", "Regional and territory comparison.", PickFilters(profile, "Region", "Territory"), ["Bar Charts", "Maps", "Trend Charts"]));
        }

        if (HasDimension(profile, "Customer Segment") || HasDomain(opportunity, "Customer"))
        {
            pages.Add(CreatePage("Customer Analysis", "Customer segment performance and behavior.", PickFilters(profile, "Customer Segment", "Product Category"), ["Bar Charts", "Scatter Charts", "Detail Tables"]));
        }

        if (HasDomain(opportunity, "Forecasting") || HasMeasure(profile, "forecast"))
        {
            pages.Add(CreatePage("Forecast Accuracy", "Forecast versus actual performance.", PickFilters(profile, "Forecast Period", "Territory", "Date"), ["Line Charts", "Variance Charts", "Scorecards"]));
        }

        return pages;
    }

    private static IReadOnlyList<ExperienceBlueprintPage> BuildOperationalPages(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        if (HasDomain(opportunity, "Service") || HasDimension(profile, "Technician") || HasDimension(profile, "Work Order"))
        {
            return
            [
                CreatePage("Service Command Center", "Monitor backlog pressure, SLA exposure, and regional service health.", PickFilters(profile, "Date", "Region", "Technician"), ["KPI Cards", "Trend Visuals", "Status Grids"]),
                CreatePage("Backlog and SLA Risk", "Prioritize service queues, escalations, and breach risk.", PickFilters(profile, "Region", "Work Order", "Technician"), ["Exception Tables", "Variance Charts", "Bar Charts"]),
                CreatePage("Technician and Work Order Detail", "Inspect work-order throughput, technician performance, and follow-up actions.", PickFilters(profile, "Technician", "Work Order", "Date"), ["Detail Tables", "Timeline Charts", "Bar Charts"])
            ];
        }

        if (HasDomain(opportunity, "Inventory") || HasDimension(profile, "Warehouse"))
        {
            return
            [
                CreatePage("Overview", "Monitor stock position, warehouse pressure, and inventory health.", PickFilters(profile, "Date", "Region", "Warehouse"), ["KPI Cards", "Trend Visuals", "Status Grids"]),
                CreatePage("Exceptions", "Prioritize stockouts, aging inventory, and exception queues.", PickFilters(profile, "Warehouse", "Product Category", "Region"), ["Exception Tables", "Bar Charts", "Heatmaps"]),
                CreatePage("Detail", "Inspect warehouse and SKU-level recovery actions.", PickFilters(profile, "Warehouse", "Product Category", "Date"), ["Detail Tables", "Trend Visuals", "Bar Charts"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.ForecastAccuracy || HasDomain(opportunity, "Forecasting"))
        {
            return
            [
                CreatePage("Overview", "Monitor forecast posture, threshold movement, and owner attention points.", PickFilters(profile, "Forecast Period", "Date", "Region"), ["Status Grids", "Trend Visuals", "KPI Cards"]),
                CreatePage("Miss Thresholds", "Prioritize the forecast misses that need operational follow-through first.", PickFilters(profile, "Region", "Territory", "Forecast Period"), ["Exception Tables", "Variance Charts", "Bar Charts"]),
                CreatePage("Owner Follow-Up", "Translate misses into owner-level actions and next checkpoints.", PickFilters(profile, "Region", "Scenario", "Date"), ["Action Tables", "Detail Tables", "Scorecards"])
            ];
        }

        return
        [
            CreatePage("Overview", "Operational status review and alert scanning.", PickFilters(profile, "Date", "Region"), ["Status Grids", "Trend Visuals", "KPI Cards"]),
            CreatePage("Exceptions", "Prioritized exceptions and action queues.", PickFilters(profile, "Product Category", "Warehouse", "Territory"), ["Exception Tables", "Status Grids", "Bar Charts"]),
            CreatePage("Detail", "Record-level detail for operational follow-up.", PickFilters(profile, "Product Category", "Customer Segment"), ["Detail Tables", "Trend Visuals", "Bar Charts"])
        ];
    }

    private static IReadOnlyList<ExperienceBlueprintPage> BuildAnalyticalPages(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        if (opportunity?.Category == OpportunityCategory.ForecastAccuracy || HasDomain(opportunity, "Forecasting"))
        {
            return
            [
                CreatePage("Question", "Define the forecast question and the miss pattern hypothesis to test first.", PickFilters(profile, "Forecast Period", "Date", "Region"), ["Question Summary", "KPI Cards", "Trend Charts"]),
                CreatePage("Miss Pattern Investigation", "Branch through the forecast drivers, segments, and outlier patterns that explain the miss.", PickFilters(profile, "Product Category", "Customer Segment", "Territory"), ["Decomposition Trees", "Bar Charts", "Matrix"]),
                CreatePage("Driver Evidence", "Review the strongest evidence behind the forecast hypothesis and competing explanations.", PickFilters(profile, "Product Category", "Customer Segment"), ["Trend Charts", "Detail Tables", "Waterfall Charts"]),
                CreatePage("Correction Hypothesis", "Close on the analytical conclusion and the correction path to validate next.", PickFilters(profile, "Date", "Region"), ["Scorecards", "Narrative Summaries", "Action Tables"])
            ];
        }

        return
        [
            CreatePage("Question", "Define the core question and frame the hypothesis.", PickFilters(profile, "Date", "Region"), ["Question Summary", "KPI Cards", "Trend Charts"]),
            CreatePage("Investigation", "Branch through potential drivers and segments.", PickFilters(profile, "Product Category", "Customer Segment", "Territory"), ["Decomposition Trees", "Bar Charts", "Matrix"]),
            CreatePage("Evidence", "Review the strongest evidence and comparative signals.", PickFilters(profile, "Product Category", "Customer Segment"), ["Trend Charts", "Detail Tables", "Waterfall Charts"]),
            CreatePage("Conclusion", "Summarize the conclusion and next action.", PickFilters(profile, "Date", "Region"), ["Scorecards", "Narrative Summaries", "Action Tables"])
        ];
    }

    private static IReadOnlyList<ExperienceBlueprintPage> BuildPbirReportPages(
        DiscoveryProfile profile,
        OpportunityCandidate? opportunity,
        IReadOnlyList<string> primaryKpis)
    {
        if (opportunity?.Category == OpportunityCategory.RootCauseInvestigation)
        {
            return
            [
                CreatePage("Question Framing", "Frame the investigation question and align the decision stakes.", PickFilters(profile, "Date", "Region"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
                CreatePage("Driver Walkthrough", "Guide the reader through the most credible drivers and comparison paths.", PickFilters(profile, "Product Category", "Customer Segment", "Territory"), ["Decomposition Trees", "Bar Charts", "Matrix"]),
                CreatePage("Evidence Review", "Show the evidence that confirms or rejects the leading hypotheses.", PickFilters(profile, "Product Category", "Customer Segment"), ["Waterfall Charts", "Trend Charts", "Detail Tables"]),
                CreatePage("Decision Brief", "Close the narrative with the implication, confidence, and next action.", PickFilters(profile, "Date", "Region"), ["Scorecards", "Narrative Summaries", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.ProfitabilityAnalysis ||
            HasDomain(opportunity, "Profitability"))
        {
            return
            [
                CreatePage("Leadership Narrative", $"Establish the report story around {string.Join(", ", primaryKpis.Take(2))}.", PickFilters(profile, "Date", "Region"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
                CreatePage("Margin Driver Story", "Progress from headline margin movement into the most credible business drivers.", PickFilters(profile, "Customer Segment", "Product Category", "Region"), ["Waterfall Charts", "Bar Charts", "Decomposition Trees"]),
                CreatePage("Segment Drill Path", "Support controlled drill paths across customers, products, and territories.", PickFilters(profile, "Customer Segment", "Product Category", "Territory"), ["Matrix", "Detail Tables", "Trend Charts"]),
                CreatePage("Decision Brief", "Land the narrative on the recommended commercial or finance decision.", PickFilters(profile, "Date", "Region"), ["Scorecards", "Narrative Summaries", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.InventoryOptimization ||
            HasDomain(opportunity, "Inventory"))
        {
            return
            [
                CreatePage("Inventory Control Narrative", "Frame the inventory control story around stock health, service risk, and the operating question.", PickFilters(profile, "Date", "Warehouse", "Region"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
                CreatePage("Stock Pressure Review", "Walk through stockouts, aging exposure, and warehouse pressure in priority order.", PickFilters(profile, "Warehouse", "Product Category", "Region"), ["Bar Charts", "Heatmaps", "Exception Tables"]),
                CreatePage("Recovery Drill Path", "Guide the reader from warehouse pressure into SKU and exception detail.", PickFilters(profile, "Warehouse", "Product Category", "Date"), ["Matrix", "Detail Tables", "Trend Charts"]),
                CreatePage("Recovery Decision Brief", "Close with the replenishment or recovery action the report supports.", PickFilters(profile, "Date", "Warehouse", "Region"), ["Narrative Summaries", "Scorecards", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.ServiceOperations ||
            HasDomain(opportunity, "Service"))
        {
            return
            [
                CreatePage("Service Leadership Narrative", "Open with backlog pressure, SLA exposure, and the service operating question.", PickFilters(profile, "Date", "Region", "Technician"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
                CreatePage("Queue and SLA Story", "Progress through queue pressure, breach risk, and escalation hotspots in a guided order.", PickFilters(profile, "Region", "Work Order", "Technician"), ["Bar Charts", "Exception Tables", "Timeline Charts"]),
                CreatePage("Technician Follow-Up Detail", "Support deliberate drill into technician workload, work-order context, and follow-up detail.", PickFilters(profile, "Technician", "Work Order", "Date"), ["Matrix", "Detail Tables", "Timeline Charts"]),
                CreatePage("Service Action Brief", "Land the narrative on the next service action, owner, and timing.", PickFilters(profile, "Date", "Region", "Technician"), ["Narrative Summaries", "Scorecards", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.ForecastAccuracy ||
            HasDomain(opportunity, "Forecasting"))
        {
            return
            [
                CreatePage("Forecast Narrative", $"Establish the forecast story around {string.Join(", ", primaryKpis.Take(2))}.", PickFilters(profile, "Forecast Period", "Date", "Region"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
                CreatePage("Miss Pattern Review", "Explain where misses concentrate across periods, territories, and segments.", PickFilters(profile, "Forecast Period", "Territory", "Region"), ["Variance Charts", "Bar Charts", "Line Charts"]),
                CreatePage("Driver Drill Path", "Guide the reader from miss patterns into the drivers that need correction.", PickFilters(profile, "Forecast Period", "Product Category", "Territory"), ["Matrix", "Detail Tables", "Decomposition Trees"]),
                CreatePage("Course-Correction Brief", "Close with the forecast adjustment, owner, and next review checkpoint.", PickFilters(profile, "Forecast Period", "Date", "Region"), ["Narrative Summaries", "Scorecards", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.ExecutiveReporting ||
            opportunity?.Category == OpportunityCategory.SalesPerformance ||
            HasDomain(opportunity, "Revenue"))
        {
            return
            [
                CreatePage("Revenue Leadership Narrative", $"Open with the strategic revenue story around {string.Join(", ", primaryKpis.Take(2))}.", PickFilters(profile, "Date", "Region", "Territory"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
                CreatePage("Growth and Mix Story", "Walk through growth, mix, and territory comparisons in a leadership-ready sequence.", PickFilters(profile, "Region", "Territory", "Product Category"), ["Bar Charts", "Line Charts", "Waterfall Charts"]),
                CreatePage("Commercial Drill Path", "Support controlled drill into customers, products, and territories before action is set.", PickFilters(profile, "Customer Segment", "Product Category", "Territory"), ["Matrix", "Detail Tables", "Trend Charts"]),
                CreatePage("Leadership Action Brief", "Close with the commercial decision, owner, and next review action.", PickFilters(profile, "Date", "Region", "Territory"), ["Narrative Summaries", "Scorecards", "Action Tables"])
            ];
        }

        if (opportunity?.Category == OpportunityCategory.CustomerAnalysis ||
            HasDomain(opportunity, "Customer"))
        {
            return
            [
                CreatePage("Audience Narrative", "Explain the customer story, the key segments, and the review objective.", PickFilters(profile, "Date", "Customer Segment"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
                CreatePage("Segment Progression", "Walk through the major customer segments in a deliberate report sequence.", PickFilters(profile, "Customer Segment", "Region", "Product Category"), ["Bar Charts", "Scatter Charts", "Trend Charts"]),
                CreatePage("Drill Detail", "Provide the supporting detail path needed to validate the segment narrative.", PickFilters(profile, "Customer Segment", "Product Category"), ["Matrix", "Detail Tables", "Bar Charts"]),
                CreatePage("Decision Brief", "Summarize the segment implication and the next business move.", PickFilters(profile, "Date", "Region"), ["Narrative Summaries", "Scorecards", "Action Tables"])
            ];
        }

        return
        [
            CreatePage("Leadership Narrative", "Open with the report story, KPI hierarchy, and decision context.", PickFilters(profile, "Date", "Region"), ["Narrative Summaries", "KPI Cards", "Trend Charts"]),
            CreatePage("Performance Story", "Move from the headline KPIs into the most important performance comparisons.", PickFilters(profile, "Product Category", "Customer Segment", "Territory"), ["Bar Charts", "Line Charts", "Scorecards"]),
            CreatePage("Drill Path", "Support intentional drill strategy across the highest-value dimensions.", PickFilters(profile, "Product Category", "Customer Segment", "Territory"), ["Matrix", "Detail Tables", "Trend Charts"]),
            CreatePage("Decision Brief", "Close with the audience-specific implication, recommendation, and next step.", PickFilters(profile, "Date", "Region"), ["Narrative Summaries", "Scorecards", "Action Tables"])
        ];
    }

    private static IReadOnlyList<ExperienceBlueprintPage> BuildFabricAppPages(DiscoveryProfile profile, OpportunityCandidate? opportunity)
    {
        if (HasDomain(opportunity, "Service") || HasDimension(profile, "Technician") || HasDimension(profile, "Work Order"))
        {
            return
            [
                CreatePage("Service Command Center", "Landing view for service leaders coordinating backlog and handoffs.", PickFilters(profile, "Date", "Region"), ["KPI Cards", "Navigation Tiles", "Trend Charts"]),
                CreatePage("Regional Queue Routing", "Route service queues by region and operational pressure.", PickFilters(profile, "Region", "Work Order"), ["Queue Boards", "Bar Charts", "Maps"]),
                CreatePage("Technician Follow-Up", "Guide technician-level follow-up and escalation review.", PickFilters(profile, "Technician", "Date"), ["Detail Tables", "Timeline Charts", "KPI Cards"])
            ];
        }

        return
        [
            CreatePage("App Overview", "Landing experience for high-level guidance and entry points.", PickFilters(profile, "Date", "Region"), ["KPI Cards", "Navigation Tiles", "Trend Charts"]),
            CreatePage("Regional View", "Audience pathway into regional performance.", PickFilters(profile, "Region", "Territory"), ["Bar Charts", "Maps", "Trend Charts"]),
            CreatePage("Customer View", "Customer-oriented path for deeper analysis.", PickFilters(profile, "Customer Segment", "Product Category"), ["Detail Tables", "Bar Charts", "Scatter Charts"])
        ];
    }

    private static IReadOnlyList<ExperienceBlueprintPage> BuildFabricDataAppPages(DiscoveryProfile profile)
    {
        return
        [
            CreatePage("Data Explorer", "Open exploration entry point over the semantic model.", PickFilters(profile, "Date", "Customer Segment"), ["Matrix", "Detail Tables", "Bar Charts"]),
            CreatePage("Segment Analysis", "Explore grouped segments and cohorts.", PickFilters(profile, "Customer Segment", "Product Category"), ["Bar Charts", "Scatter Charts", "Trend Charts"]),
            CreatePage("Record Detail", "Inspect detailed data slices and follow-up records.", PickFilters(profile, "Region", "Customer Segment"), ["Detail Tables", "Matrix", "KPI Cards"])
        ];
    }

    private static ExperienceBlueprintPage CreatePage(
        string name,
        string intent,
        IReadOnlyList<string> filters,
        IReadOnlyList<string> visuals)
    {
        return new ExperienceBlueprintPage(
            PageName: name,
            PageIntent: intent,
            SuggestedFilters: filters.Distinct(NameComparer).ToList(),
            SuggestedVisualTypes: visuals.Distinct(NameComparer).ToList());
    }

    private static IReadOnlyList<string> PickFilters(DiscoveryProfile profile, params string[] candidates)
    {
        var available = BuildAvailableFilterLabels(profile);

        var selected = candidates
            .Where(candidate => available.Any(value => LabelsMatch(value, candidate)))
            .Select(candidate => available.First(value => LabelsMatch(value, candidate)))
            .ToList();

        if (selected.Count == 0)
        {
            selected.AddRange(available.Take(2));
        }

        if (selected.Count == 0)
        {
            selected.Add("Date");
        }

        return selected;
    }

    private static ExperienceBlueprintAnalyticalFlow BuildAnalyticalFlow(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.OperationalMonitoringExperience &&
            (HasDomain(opportunity, "Service") || HasDimension(profile, "Technician") || HasDimension(profile, "Work Order")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "Which service queues and SLA risks need action first?",
                Investigation: "Investigation follows backlog pressure, technician load, and regional queue movement.",
                Evidence: "Evidence comes from work-order detail, SLA trends, and escalation hotspots.",
                Decision: "Decision should route the next service action to the right owner.");
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.OperationalMonitoringExperience &&
            (HasDomain(opportunity, "Inventory") || HasDimension(profile, "Warehouse")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "Where is inventory pressure building and which stock risks need action first?",
                Investigation: "Investigation follows warehouse pressure, stockout exposure, and SKU-level exception patterns.",
                Evidence: "Evidence comes from inventory trend movement, exception queues, and warehouse detail.",
                Decision: "Decision should identify the next replenishment or operational recovery action.");
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.PbirReport &&
            (opportunity?.Category == OpportunityCategory.ProfitabilityAnalysis || HasDomain(opportunity, "Profitability")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "What is the profitability story the report needs to tell first?",
                Investigation: "Investigation should progress from headline margin movement into the driver hierarchy and controlled segment drill paths.",
                Evidence: "Evidence should accumulate through KPI hierarchy, segment comparison, and supporting detail pages instead of isolated visuals.",
                Decision: "Decision should conclude with the finance or commercial action implied by the report narrative.");
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.PbirReport &&
            (opportunity?.Category == OpportunityCategory.InventoryOptimization || HasDomain(opportunity, "Inventory")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "Where is inventory control pressure building and which recovery decision matters first?",
                Investigation: "Investigation should move from stock health into warehouse pressure, exception concentration, and SKU-level recovery paths.",
                Evidence: "Evidence should build through inventory risk, warehouse comparisons, and supporting exception detail rather than isolated KPI tiles.",
                Decision: "Decision should identify the replenishment or recovery action the report is meant to defend.");
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.PbirReport &&
            (opportunity?.Category == OpportunityCategory.ServiceOperations || HasDomain(opportunity, "Service")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "Which service pressure points need leadership attention and why?",
                Investigation: "Investigation should progress from backlog and SLA exposure into queue routing, technician load, and follow-up detail.",
                Evidence: "Evidence should connect service risk, regional queue movement, and technician context before the action brief.",
                Decision: "Decision should clarify the next service action, owner, and timing implied by the report.");
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.PbirReport &&
            (opportunity?.Category == OpportunityCategory.ForecastAccuracy || HasDomain(opportunity, "Forecasting")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "Where are forecast misses building and what correction should happen next?",
                Investigation: "Investigation should move from the headline miss into period, territory, and segment drivers before a correction path is chosen.",
                Evidence: "Evidence should accumulate through miss patterns, variance analysis, and targeted drill pages that explain the correction logic.",
                Decision: "Decision should end with the forecast adjustment and the next checkpoint the audience should use.");
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.ExecutiveDashboard &&
            (opportunity?.Category == OpportunityCategory.ForecastAccuracy || HasDomain(opportunity, "Forecasting")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "Where is planning confidence weakening before the next review cycle?",
                Investigation: "Investigation should move from headline forecast accuracy into scenario variance, regional miss concentration, and follow-up owners behind the next planning checkpoint.",
                Evidence: "Evidence should connect forecast accuracy, scenario variance, and regional movement without turning the primary experience into an investigation shell.",
                Decision: "Decision should clarify the corrective planning action and the next review checkpoint.");
        }

        if (recommendation.RecommendedExperienceType == OpportunityExperienceType.ExecutiveDashboard &&
            (opportunity?.Category == OpportunityCategory.ExecutiveReporting ||
             opportunity?.Category == OpportunityCategory.SalesPerformance ||
             HasDomain(opportunity, "Revenue")))
        {
            return new ExperienceBlueprintAnalyticalFlow(
                Question: "Which commercial performance shifts need leadership attention first?",
                Investigation: "Investigation should move from topline revenue posture into growth, margin, mix, and territory comparisons before follow-up is assigned.",
                Evidence: "Evidence should connect KPI movement, territory context, and commercial drivers so leadership can act without opening a full analyst-first workflow.",
                Decision: "Decision should identify the commercial action, owner, and next leadership checkpoint.");
        }

        return recommendation.RecommendedExperienceType switch
        {
            OpportunityExperienceType.OperationalMonitoringExperience => new ExperienceBlueprintAnalyticalFlow(
                Question: "Which operational exceptions require attention now?",
                Investigation: "Investigation focuses on exception clusters, backlog movement, and impacted segments.",
                Evidence: "Evidence comes from status trends, exception records, and segment-level breakdowns.",
                Decision: "Decision should identify the next operational action and owner."),
            OpportunityExperienceType.AnalyticalInvestigationExperience => new ExperienceBlueprintAnalyticalFlow(
                Question: "What is driving the performance variance or business question?",
                Investigation: "Investigation branches through hierarchy paths, segments, and comparative drivers.",
                Evidence: "Evidence should isolate the strongest patterns, comparisons, and anomalies.",
                Decision: "Decision should explain the conclusion and the next analytical action."),
            OpportunityExperienceType.FabricDataApp => new ExperienceBlueprintAnalyticalFlow(
                Question: "Which data slices and segments are worth exploring first?",
                Investigation: "Investigation moves from broad segmentation into targeted record exploration.",
                Evidence: "Evidence comes from comparative segment views and detailed records.",
                Decision: "Decision should identify which segments deserve follow-up analysis or workflow action."),
            OpportunityExperienceType.PbirReport => new ExperienceBlueprintAnalyticalFlow(
                Question: "What is the report narrative and which audience decision should it support?",
                Investigation: "Investigation should progress page by page from the headline KPI hierarchy into the most important drill paths.",
                Evidence: "Evidence should be staged so each page adds clarity before the reader reaches detailed drill content.",
                Decision: "Decision should end with an explicit audience takeaway and the next action the report supports."),
            _ => new ExperienceBlueprintAnalyticalFlow(
                Question: "What changed in performance and where should attention focus?",
                Investigation: "Investigation reviews KPI movement, segment comparisons, and the main drivers.",
                Evidence: "Evidence comes from trends, segment views, and supporting detail pages.",
                Decision: "Decision should identify the leadership priority and next action.")
        };
    }

    private static ExperienceBlueprintNavigationIntent BuildNavigationIntent(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityExperienceType experienceType,
        OpportunityCandidate? opportunity,
        IReadOnlyList<ExperienceBlueprintPage> pages)
    {
        if (experienceType == OpportunityExperienceType.OperationalMonitoringExperience &&
            (HasDomain(opportunity, "Service") || HasDimension(profile, "Technician") || HasDimension(profile, "Work Order")))
        {
            return new ExperienceBlueprintNavigationIntent(
                Flow: "service command → queue risk → technician follow-up",
                Sequence: pages.Select(page => page.PageName).ToList());
        }

        if (experienceType == OpportunityExperienceType.OperationalMonitoringExperience &&
            (HasDomain(opportunity, "Inventory") || HasDimension(profile, "Warehouse")))
        {
            return new ExperienceBlueprintNavigationIntent(
                Flow: "monitor → exception → detail with inventory control emphasis",
                Sequence: pages.Select(page => page.PageName).ToList());
        }

        if (experienceType == OpportunityExperienceType.FabricApp &&
            (HasDomain(opportunity, "Service") || HasDimension(profile, "Technician") || HasDimension(profile, "Work Order")))
        {
            return new ExperienceBlueprintNavigationIntent(
                Flow: "service command → regional routing → technician follow-up",
                Sequence: pages.Select(page => page.PageName).ToList());
        }

        if (experienceType == OpportunityExperienceType.PbirReport)
        {
            if (opportunity?.Category == OpportunityCategory.InventoryOptimization || HasDomain(opportunity, "Inventory"))
            {
                return new ExperienceBlueprintNavigationIntent(
                    Flow: "inventory narrative → stock pressure → recovery drill → recovery brief",
                    Sequence: pages.Select(page => page.PageName).ToList());
            }

            if (opportunity?.Category == OpportunityCategory.ServiceOperations || HasDomain(opportunity, "Service"))
            {
                return new ExperienceBlueprintNavigationIntent(
                    Flow: "service narrative → queue and SLA story → technician detail → service action brief",
                    Sequence: pages.Select(page => page.PageName).ToList());
            }

            if (opportunity?.Category == OpportunityCategory.ForecastAccuracy || HasDomain(opportunity, "Forecasting"))
            {
                return new ExperienceBlueprintNavigationIntent(
                    Flow: "forecast narrative → miss pattern review → driver drill → course-correction brief",
                    Sequence: pages.Select(page => page.PageName).ToList());
            }

            if (opportunity?.Category == OpportunityCategory.ExecutiveReporting ||
                opportunity?.Category == OpportunityCategory.SalesPerformance ||
                HasDomain(opportunity, "Revenue"))
            {
                return new ExperienceBlueprintNavigationIntent(
                    Flow: "revenue narrative → growth and mix story → commercial drill → leadership action brief",
                    Sequence: pages.Select(page => page.PageName).ToList());
            }

            return new ExperienceBlueprintNavigationIntent(
                Flow: "narrative opener → KPI progression → guided drill → decision brief",
                Sequence: pages.Select(page => page.PageName).ToList());
        }

        if (experienceType == OpportunityExperienceType.ExecutiveDashboard)
        {
            if (opportunity?.Category == OpportunityCategory.ForecastAccuracy || HasDomain(opportunity, "Forecasting"))
            {
                if (IsExecutiveForecastNarrative(recommendation, opportunity))
                {
                    return new ExperienceBlueprintNavigationIntent(
                        Flow: "executive forecast review → confidence and variance summary → leadership follow-up",
                        Sequence: pages.Select(page => page.PageName).ToList());
                }

                return new ExperienceBlueprintNavigationIntent(
                    Flow: "planning summary → variance review → follow-up",
                    Sequence: pages.Select(page => page.PageName).ToList());
            }

            if (opportunity?.Category == OpportunityCategory.ExecutiveReporting ||
                opportunity?.Category == OpportunityCategory.SalesPerformance ||
                HasDomain(opportunity, "Revenue"))
            {
                return new ExperienceBlueprintNavigationIntent(
                    Flow: "leadership summary → growth and mix → commercial follow-up",
                    Sequence: pages.Select(page => page.PageName).ToList());
            }

            if (opportunity?.Category == OpportunityCategory.CustomerAnalysis || HasDomain(opportunity, "Customer"))
            {
                return new ExperienceBlueprintNavigationIntent(
                    Flow: "portfolio summary → segment drivers → account follow-up",
                    Sequence: pages.Select(page => page.PageName).ToList());
            }
        }

        var flow = experienceType switch
        {
            OpportunityExperienceType.OperationalMonitoringExperience => "monitor → exception → detail",
            OpportunityExperienceType.AnalyticalInvestigationExperience => "question → investigation → evidence → conclusion",
            OpportunityExperienceType.FabricApp => "executive → regional → customer",
            OpportunityExperienceType.FabricDataApp => "explore → segment → detail",
            _ => "summary → drill"
        };

        return new ExperienceBlueprintNavigationIntent(
            Flow: flow,
            Sequence: pages.Select(page => page.PageName).ToList());
    }

    private static bool IsExecutiveForecastNarrative(
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity)
    {
        return (opportunity?.InferredAudience.Contains("executive", StringComparison.OrdinalIgnoreCase) == true ||
                recommendation.ExpectedAudience.Contains("executive", StringComparison.OrdinalIgnoreCase)) &&
               !ContainsAny(
                   $"{recommendation.RecommendationName} {recommendation.ExpectedBusinessOutcome} {opportunity?.BusinessOutcome}",
                   "planning cycle",
                   "re-plan",
                   "planning leadership",
                   "assumption");
    }

    private static IReadOnlyList<string> BuildSuccessCriteria(
        DiscoveryRecommendation recommendation,
        IReadOnlyList<ExperienceBlueprintPage> pages,
        IReadOnlyList<string> primaryKpis)
    {
        var cadence = InferDecisionCadence($"{recommendation.RecommendationName} {recommendation.ExpectedBusinessOutcome}");

        return
        [
            $"{recommendation.ExpectedAudience} can move through the suggested {pages.Count}-page experience without redesigning the baseline information architecture.",
            $"The experience highlights the primary KPI set: {string.Join(", ", primaryKpis.Take(3))}.",
            $"{recommendation.ExpectedBusinessOutcome} within a {cadence.ToLowerInvariant()} decision rhythm."
        ];
    }

    private static ExperienceBlueprintProvenance BuildProvenance(
        DiscoveryProfile profile,
        DiscoveryRecommendation recommendation,
        OpportunityCandidate? opportunity,
        IReadOnlyList<string> primaryKpis)
    {
        var semanticEvidenceReferences = opportunity?.SupportingSemanticSignals
            .Select(signal => $"{signal.SignalType}:{signal.Value}")
            .Distinct(NameComparer)
            .OrderBy(signal => signal, NameComparer)
            .ToList() ?? [];
        var influencingModelStructures = BuildInfluencingModelStructures(profile, opportunity);
        var ambiguityNotes = BuildAmbiguityNotes(profile, opportunity, primaryKpis);

        return new ExperienceBlueprintProvenance(
            RecommendationId: recommendation.RecommendationId,
            OpportunityId: opportunity?.OpportunityId ?? recommendation.RecommendationId,
            OpportunityCategory: opportunity?.Category ?? InferFallbackCategory(recommendation.RecommendedExperienceType),
            ExperienceType: recommendation.RecommendedExperienceType,
            DiscoveryConfidence: profile.Confidence,
            SupportingSignals: recommendation.SupportingSignals.ToList(),
            SemanticEvidenceReferences: semanticEvidenceReferences,
            InfluencingModelStructures: influencingModelStructures,
            AmbiguityNotes: ambiguityNotes,
            SemanticModelReferenceId: profile.SemanticModelReferenceId,
            DiscoveryProfileReferenceId: profile.DiscoveryProfileReferenceId);
    }

    private static IReadOnlyList<string> BuildAmbiguityNotes(
        DiscoveryProfile profile,
        OpportunityCandidate? opportunity,
        IReadOnlyList<string> primaryKpis)
    {
        var notes = profile.AmbiguityNotes
            .Concat(opportunity?.LimitingFactors ?? [])
            .Distinct(NameComparer)
            .ToList();

        if (primaryKpis.Count == 0)
        {
            notes.Add("No supported KPI measures were found in the current semantic model, so the blueprint leaves KPI guidance intentionally ambiguous.");
        }
        else if (primaryKpis.Count < 3)
        {
            notes.Add($"KPI support is limited to {string.Join(", ", primaryKpis)}; the blueprint intentionally avoids unsupported fallback KPIs.");
        }

        return notes
            .Distinct(NameComparer)
            .ToList();
    }

    private static IReadOnlyList<string> BuildAvailableFilterLabels(DiscoveryProfile profile)
    {
        return profile.Dimensions
            .Select(GetConsultantFacingDimensionLabel)
            .Concat(profile.DateIntelligence.DateDimensions.Select(NormalizeFilterLabel))
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(NameComparer)
            .ToList();
    }

    private static string GetConsultantFacingDimensionLabel(DiscoveryDimensionProfile dimension)
    {
        if (string.Equals(dimension.BusinessRole, "Date", StringComparison.OrdinalIgnoreCase))
        {
            return "Date";
        }

        var normalizedName = NormalizeFilterLabel(dimension.Name);

        if (!normalizedName.StartsWith("Dim ", StringComparison.OrdinalIgnoreCase) &&
            !normalizedName.StartsWith("Fact ", StringComparison.OrdinalIgnoreCase) &&
            !normalizedName.StartsWith("Tbl ", StringComparison.OrdinalIgnoreCase) &&
            !normalizedName.StartsWith("Table ", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedName;
        }

        var stripped = StripTechnicalPrefix(dimension.Name);
        var normalizedStripped = NormalizeFilterLabel(stripped);

        if (!string.IsNullOrWhiteSpace(normalizedStripped))
        {
            return normalizedStripped;
        }

        return NormalizeFilterLabel(dimension.BusinessRole);
    }

    private static bool LabelsMatch(string availableLabel, string candidate)
    {
        return string.Equals(NormalizeFilterLabel(availableLabel), NormalizeFilterLabel(candidate), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeFilterLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = StripTechnicalPrefix(value)
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);

        normalized = string.Concat(normalized.Select((character, index) =>
            index > 0 && char.IsUpper(character) && char.IsLetterOrDigit(normalized[index - 1]) && !char.IsUpper(normalized[index - 1])
                ? $" {character}"
                : character.ToString()));

        normalized = string.Join(" ", normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ExpandLabelToken));

        return normalized switch
        {
            "Cust" => "Customer",
            "Prod" => "Product",
            "Whse" => "Warehouse",
            "Wrk Order" => "Work Order",
            _ => normalized
        };
    }

    private static string StripTechnicalPrefix(string value)
    {
        foreach (var prefix in new[] { "Dim", "Fact", "Tbl", "Table" })
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                value.Length > prefix.Length &&
                char.IsUpper(value[prefix.Length]))
            {
                return value[prefix.Length..];
            }
        }

        return value;
    }

    private static string ExpandLabelToken(string token)
    {
        return token.ToLowerInvariant() switch
        {
            "cust" => "Customer",
            "customer" => "Customer",
            "prod" => "Product",
            "product" => "Product",
            "wrk" => "Work",
            "wo" => "Work Order",
            "order" => "Order",
            "whse" => "Warehouse",
            _ => char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant()
        };
    }

    private static IReadOnlyList<string> BuildInfluencingModelStructures(
        DiscoveryProfile profile,
        OpportunityCandidate? opportunity)
    {
        var structures = new List<string>();

        foreach (var signal in opportunity?.SupportingSemanticSignals ?? [])
        {
            if (string.Equals(signal.SignalType, "Measure", StringComparison.OrdinalIgnoreCase))
            {
                structures.Add($"measure:{signal.Value}");
            }
            else if (string.Equals(signal.SignalType, "Dimension", StringComparison.OrdinalIgnoreCase))
            {
                structures.Add($"dimension:{signal.Value}");
            }
        }

        structures.AddRange(profile.Measures.Select(measure => $"measure:{measure.Name}").Take(3));
        structures.AddRange(profile.Dimensions.Select(dimension => $"dimension:{dimension.Name}").Take(3));
        structures.AddRange(profile.Hierarchies.Select(hierarchy => $"hierarchy:{hierarchy.Name}").Take(2));
        structures.AddRange(profile.Relationships.Select(relationship => $"relationship:{relationship.FromTable}->{relationship.ToTable}").Take(2));

        return structures
            .Distinct(NameComparer)
            .OrderBy(value => value, NameComparer)
            .ToList();
    }

    private static OpportunityCategory InferFallbackCategory(OpportunityExperienceType experienceType)
    {
        return experienceType switch
        {
            OpportunityExperienceType.OperationalMonitoringExperience => OpportunityCategory.InventoryOptimization,
            OpportunityExperienceType.AnalyticalInvestigationExperience => OpportunityCategory.RootCauseInvestigation,
            OpportunityExperienceType.FabricDataApp => OpportunityCategory.CustomerAnalysis,
            _ => OpportunityCategory.ExecutiveReporting
        };
    }

    private static bool HasMeasure(DiscoveryProfile profile, string value)
    {
        return profile.Measures.Any(measure => measure.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDimension(DiscoveryProfile profile, string value)
    {
        return profile.Dimensions.Any(dimension => string.Equals(dimension.Name, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasDomain(OpportunityCandidate? opportunity, string value)
    {
        return opportunity?.SupportingSemanticSignals.Any(signal =>
            string.Equals(signal.SignalType, "Domain", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(signal.Value, value, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static string InferDecisionCadence(string text)
    {
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

        return "Weekly";
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
