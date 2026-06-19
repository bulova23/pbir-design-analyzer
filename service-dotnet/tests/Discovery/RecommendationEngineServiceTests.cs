using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class RecommendationEngineServiceTests
{
    private static readonly Assembly CoreAssembly = typeof(PbirScoringService).Assembly;
    private const string DiscoveryModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";
    private const string DiscoveryServicesNamespace = "PowerBIModelingService.Services.Discovery";

    [Fact(DisplayName = "Recommendation Engine ranks the strongest opportunities first")]
    public void BuildRecommendations_StrongestOpportunitiesRankHighest()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            audienceSignals:
            [
                ("Executive", "High"),
                ("Operational", "High"),
                ("Analytical", "High")
            ],
            domainSignals:
            [
                ("Revenue", "High"),
                ("Inventory", "High"),
                ("Profitability", "High")
            ]);

        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                opportunityId: "executive-revenue-command-center",
                name: "Executive Revenue Command Center",
                category: "ExecutiveReporting",
                audience: "Executive",
                businessOutcome: "Track revenue trends, target attainment, and leadership KPI performance over time.",
                candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                supportingSignals:
                [
                    ("Domain", "Revenue"),
                    ("DateIntelligence", "High"),
                    ("Dimension", "Geography"),
                    ("KpiCluster", "Revenue KPIs"),
                    ("Audience", "Executive")
                ],
                limitingFactors: [],
                confidence: "High"),
            CreateOpportunityCandidate(
                opportunityId: "inventory-exception-monitor",
                name: "Inventory Exception Monitor",
                category: "InventoryOptimization",
                audience: "Operational",
                businessOutcome: "Monitor stock risks, warehouse issues, and item-level exceptions that require action.",
                candidateExperienceTypes: ["OperationalMonitoringExperience", "PbirReport"],
                supportingSignals:
                [
                    ("Domain", "Inventory"),
                    ("Dimension", "Product"),
                    ("Measure", "Quantity"),
                    ("Audience", "Operational")
                ],
                limitingFactors: [],
                confidence: "High"),
            CreateOpportunityCandidate(
                opportunityId: "root-cause-margin-investigation",
                name: "Root Cause Margin Investigation",
                category: "RootCauseInvestigation",
                audience: "Analytical",
                businessOutcome: "Investigate margin variance drivers through drill-based analysis.",
                candidateExperienceTypes: ["AnalyticalInvestigationExperience", "PbirReport"],
                supportingSignals:
                [
                    ("Domain", "Profitability"),
                    ("Measure", "Variance"),
                    ("Drill", "HierarchyRich"),
                    ("Audience", "Analytical")
                ],
                limitingFactors: [],
                confidence: "Medium"),
            CreateOpportunityCandidate(
                opportunityId: "customer-exploration",
                name: "Customer Exploration",
                category: "CustomerAnalysis",
                audience: "Commercial Strategy",
                businessOutcome: "Explore customer attributes for future segmentation questions.",
                candidateExperienceTypes: ["FabricDataApp", "PbirReport"],
                supportingSignals:
                [
                    ("Domain", "Customer")
                ],
                limitingFactors:
                [
                    "Limited date intelligence reduces trend credibility.",
                    "Customer segmentation dimensions are incomplete."
                ],
                confidence: "Low"));

        var recommendations = BuildRecommendations(profile, catalog);
        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");

        Assert.Equal(3, primary.Count);
        Assert.DoesNotContain("Customer Exploration", primary.Select(recommendation => ReadString(recommendation, "RecommendationName")));
        Assert.Contains("Executive Revenue Command Center", primary.Select(recommendation => ReadString(recommendation, "RecommendationName")));
        Assert.Contains("Inventory Exception Monitor", primary.Select(recommendation => ReadString(recommendation, "RecommendationName")));
        Assert.True(ReadDouble(primary[0], "RankingScore") >= ReadDouble(primary[1], "RankingScore"));
    }

    [Fact(DisplayName = "Recommendation Engine collapses near-duplicate executive opportunities")]
    public void BuildRecommendations_NearDuplicatesCollapse()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            audienceSignals: [("Executive", "High"), ("Operational", "Medium")],
            domainSignals: [("Revenue", "High"), ("Service", "Medium")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "executive-sales-dashboard",
                    name: "Executive Sales Dashboard",
                    category: "ExecutiveReporting",
                    audience: "Executive",
                    businessOutcome: "Track revenue trends and executive KPI coverage over time.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("KpiCluster", "Revenue KPIs")
                    ],
                    limitingFactors: [],
                    confidence: "High"),
                CreateOpportunityCandidate(
                    opportunityId: "sales-executive-dashboard",
                    name: "Sales Executive Dashboard",
                    category: "SalesPerformance",
                    audience: "Executive",
                    businessOutcome: "Track revenue trends and executive KPI coverage over time.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("Dimension", "Territory")
                    ],
                    limitingFactors: [],
                    confidence: "High"),
                CreateOpportunityCandidate(
                    opportunityId: "revenue-executive-dashboard",
                    name: "Revenue Executive Dashboard",
                    category: "ComparativePerformanceManagement",
                    audience: "Executive",
                    businessOutcome: "Track revenue trends and executive KPI coverage over time.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("Dimension", "Geography")
                    ],
                    limitingFactors: [],
                    confidence: "High"),
                CreateOpportunityCandidate(
                    opportunityId: "service-operations",
                    name: "Service Operations Dashboard",
                    category: "ServiceOperations",
                    audience: "Operational",
                    businessOutcome: "Monitor service backlog and technician performance.",
                    candidateExperienceTypes: ["OperationalMonitoringExperience", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Service"),
                        ("Measure", "Resolution")
                    ],
                    limitingFactors: [],
                    confidence: "Medium")));

        var all = ReadAllRecommendations(recommendations);
        var executiveRecommendations = all.Count(recommendation =>
            ReadString(recommendation, "ExpectedAudience") == "Executive");

        Assert.Equal(2, all.Count);
        Assert.Equal(1, executiveRecommendations);
    }

    [Fact(DisplayName = "Recommendation Engine preserves a meaningful executive operational analytical mix when supported")]
    public void BuildRecommendations_DiversityRemainsMeaningful()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            audienceSignals:
            [
                ("Executive", "High"),
                ("Operational", "High"),
                ("Analytical", "High")
            ],
            domainSignals:
            [
                ("Revenue", "High"),
                ("Inventory", "High"),
                ("Profitability", "High"),
                ("Customer", "Medium")
            ]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate("exec-1", "Executive Revenue Overview", "ExecutiveReporting", "Executive", "Track revenue outcomes for leadership.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Revenue"), ("DateIntelligence", "High"), ("Audience", "Executive")], [], "High"),
                CreateOpportunityCandidate("exec-2", "Forecast Leadership Review", "ForecastAccuracy", "Executive", "Compare forecast, actuals, and variance.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Forecasting"), ("Measure", "Variance"), ("DateIntelligence", "High")], [], "High"),
                CreateOpportunityCandidate("ops-1", "Inventory Exception Monitor", "InventoryOptimization", "Operational", "Monitor inventory risk and action queues.", ["OperationalMonitoringExperience", "PbirReport"], [("Domain", "Inventory"), ("Measure", "Quantity"), ("Audience", "Operational")], [], "High"),
                CreateOpportunityCandidate("analytical-1", "Root Cause Investigation", "RootCauseInvestigation", "Analytical", "Investigate variance through drill paths.", ["AnalyticalInvestigationExperience", "PbirReport"], [("Domain", "Profitability"), ("Measure", "Variance"), ("Drill", "HierarchyRich"), ("Audience", "Analytical")], [], "High"),
                CreateOpportunityCandidate("customer-1", "Customer Segment Explorer", "CustomerAnalysis", "Commercial Strategy", "Explore customer segments and cohort behavior.", ["FabricDataApp", "PbirReport"], [("Domain", "Customer"), ("Dimension", "Segment")], ["Limited KPI coverage."], "Medium")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");
        var experienceTypes = primary
            .Select(recommendation => ReadString(recommendation, "RecommendedExperienceType"))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(3, primary.Count);
        Assert.Contains("ExecutiveDashboard", experienceTypes);
        Assert.Contains("OperationalMonitoringExperience", experienceTypes);
        Assert.Contains("AnalyticalInvestigationExperience", experienceTypes);
    }

    [Fact(DisplayName = "Recommendation Engine never returns more than five recommendations")]
    public void BuildRecommendations_NeverReturnsMoreThanFiveRecommendations()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            audienceSignals: [("Executive", "High"), ("Operational", "High"), ("Analytical", "High")],
            domainSignals: [("Revenue", "High"), ("Inventory", "High"), ("Customer", "High"), ("Service", "High"), ("Forecasting", "High")]);

        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate("c1", "Executive Revenue Overview", "ExecutiveReporting", "Executive", "Track revenue trends for leadership.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Revenue"), ("DateIntelligence", "High")], [], "High"),
            CreateOpportunityCandidate("c2", "Territory Sales Review", "SalesPerformance", "Executive", "Compare sales across territories.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Revenue"), ("Dimension", "Territory")], [], "High"),
            CreateOpportunityCandidate("c3", "Inventory Exception Monitor", "InventoryOptimization", "Operational", "Monitor inventory issues.", ["OperationalMonitoringExperience", "PbirReport"], [("Domain", "Inventory"), ("Measure", "Quantity")], [], "High"),
            CreateOpportunityCandidate("c4", "Service Operations Dashboard", "ServiceOperations", "Operational", "Monitor service workload.", ["OperationalMonitoringExperience", "PbirReport"], [("Domain", "Service"), ("Measure", "Resolution")], [], "High"),
            CreateOpportunityCandidate("c5", "Forecast Accuracy Dashboard", "ForecastAccuracy", "Executive", "Compare forecast versus actual.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Forecasting"), ("Measure", "Variance")], [], "High"),
            CreateOpportunityCandidate("c6", "Customer Segment Explorer", "CustomerAnalysis", "Commercial Strategy", "Explore customer segments.", ["FabricDataApp", "PbirReport"], [("Domain", "Customer"), ("Dimension", "Segment")], ["Limited profitability coverage."], "Medium"),
            CreateOpportunityCandidate("c7", "Root Cause Investigation", "RootCauseInvestigation", "Analytical", "Investigate business variance.", ["AnalyticalInvestigationExperience", "PbirReport"], [("Measure", "Variance"), ("Drill", "HierarchyRich"), ("Audience", "Analytical")], [], "High"));

        var recommendations = BuildRecommendations(profile, catalog);
        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");
        var alternates = ReadObjectList(recommendations, "AlternateRecommendations");

        Assert.True(primary.Count <= 3);
        Assert.True(alternates.Count <= 2);
        Assert.True(primary.Count + alternates.Count <= 5);
    }

    [Fact(DisplayName = "Recommendation Engine strengthens recommendation scores for high-confidence models")]
    public void BuildRecommendations_HighConfidenceModelsProduceStrongerRecommendations()
    {
        var highConfidenceProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            audienceSignals: [("Executive", "High")],
            domainSignals: [("Revenue", "High")]);
        var lowConfidenceProfile = CreateDiscoveryProfile(
            confidence: "Low",
            dateReadiness: "Low",
            ambiguityNotes: ["Weak business metadata.", "Date intelligence is missing."],
            audienceSignals: [("Executive", "Low")],
            domainSignals: [("Revenue", "Medium")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                opportunityId: "executive-revenue-overview",
                name: "Executive Revenue Overview",
                category: "ExecutiveReporting",
                audience: "Executive",
                businessOutcome: "Track revenue trends for leadership decision making.",
                candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                supportingSignals:
                [
                    ("Domain", "Revenue"),
                    ("DateIntelligence", "High"),
                    ("KpiCluster", "Revenue KPIs")
                ],
                limitingFactors: [],
                confidence: "High"));

        var highConfidenceRecommendations = BuildRecommendations(highConfidenceProfile, catalog);
        var lowConfidenceRecommendations = BuildRecommendations(lowConfidenceProfile, catalog);

        var highConfidencePrimary = ReadObjectList(highConfidenceRecommendations, "PrimaryRecommendations").Single();
        var lowConfidencePrimary = ReadObjectList(lowConfidenceRecommendations, "PrimaryRecommendations").Single();

        Assert.True(ReadDouble(highConfidencePrimary, "RankingScore") > ReadDouble(lowConfidencePrimary, "RankingScore"));
        Assert.Equal("High", ReadString(highConfidencePrimary, "Confidence"));
        Assert.Equal("Low", ReadString(lowConfidencePrimary, "Confidence"));
    }

    [Fact(DisplayName = "Recommendation Engine degrades gracefully for sparse models while preserving ambiguity")]
    public void BuildRecommendations_SparseModelsPreserveAmbiguity()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "Low",
            dateReadiness: "Low",
            ambiguityNotes:
            [
                "Business domains are weakly inferred from measure names only.",
                "Date intelligence is not well-defined."
            ],
            audienceSignals: [],
            domainSignals: [("Revenue", "Medium")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "sparse-revenue-opportunity",
                    name: "Revenue Monitoring Direction",
                    category: "ExecutiveReporting",
                    audience: "Executive",
                    businessOutcome: "Track basic revenue movement over time.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue")
                    ],
                    limitingFactors: [],
                    confidence: "Low")));

        var all = ReadAllRecommendations(recommendations);

        Assert.Single(all);
        Assert.Equal("Low", ReadString(all[0], "Confidence"));
        Assert.NotEmpty(ReadStringList(all[0], "LimitingFactors"));
        Assert.Contains(ReadStringList(all[0], "LimitingFactors"), factor =>
            factor.Contains("Date intelligence", StringComparison.OrdinalIgnoreCase) ||
            factor.Contains("weakly inferred", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Recommendation Engine generates explanation content from supporting signals")]
    public void BuildRecommendations_ExplanationContentReferencesSupportingSignals()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            audienceSignals: [("Executive", "High")],
            domainSignals: [("Revenue", "High")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "executive-revenue-overview",
                    name: "Executive Revenue Overview",
                    category: "ExecutiveReporting",
                    audience: "Executive",
                    businessOutcome: "Track revenue trends for leadership decision making.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("Dimension", "Territory"),
                        ("KpiCluster", "Revenue KPIs")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations").Single();
        var whyWeRecommendIt = ReadString(primary, "WhyWeRecommendIt");
        var supportingSignals = ReadStringList(primary, "SupportingSignals");

        Assert.False(string.IsNullOrWhiteSpace(whyWeRecommendIt));
        Assert.NotEmpty(supportingSignals);
        Assert.Contains("Revenue", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(supportingSignals, signal => signal.Contains("Revenue", StringComparison.OrdinalIgnoreCase));
        Assert.False(string.IsNullOrWhiteSpace(ReadString(primary, "ConfidenceNote")));
        Assert.False(string.IsNullOrWhiteSpace(ReadString(primary, "ComplexityNote")));
    }

    [Fact(DisplayName = "Recommendation Engine rationale explains why this experience wins over the alternatives")]
    public void BuildRecommendations_RationaleIncludesTradeoffsAndAlternativeRejection()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "YoY Growth"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Territory", "Geography")],
            audienceSignals: [("Executive", "High"), ("Operational", "Low"), ("Analytical", "Low")],
            domainSignals: [("Revenue", "High")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "executive-sales-reporting",
                    name: "Executive Sales Reporting",
                    category: "ExecutiveReporting",
                    audience: "Executive",
                    businessOutcome: "Track KPI movement for weekly leadership review and strategic revenue decisions.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "OperationalMonitoringExperience", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("Dimension", "Territory"),
                        ("KpiCluster", "Revenue KPIs"),
                        ("Audience", "Executive")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations").Single();
        var whyWeRecommendIt = ReadString(primary, "WhyWeRecommendIt");

        Assert.Contains("Executive Dashboard", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("better", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rather than", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Operational Monitoring", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("tradeoff", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("adoption", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cadence", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Recommendation Engine rationale reads like signal-driven consultant tradeoff reasoning")]
    public void BuildRecommendations_RationaleUsesSignalDrivenConsultantSections()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Open Work Orders", "Resolution Time", "Escalation Count"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Technician", "Service"),
                ("Work Order", "Service")
            ],
            relationships: [("Service", "Date"), ("Service", "Technician"), ("Service", "WorkOrder")],
            audienceSignals: [("Operational", "High"), ("Executive", "Medium")],
            domainSignals: [("Service", "High")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "service-workflow-orchestration",
                    name: "Service Workflow Orchestration",
                    category: "ServiceOperations",
                    audience: "Operations Leadership",
                    businessOutcome: "Coordinate daily backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                    candidateExperienceTypes: ["FabricApp", "OperationalMonitoringExperience", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Service"),
                        ("Measure", "Open Work Orders"),
                        ("Dimension", "Technician"),
                        ("Dimension", "Work Order")
                    ],
                    limitingFactors: ["Customer escalation context is still partial."],
                    confidence: "High")));

        var recommendation = ReadObjectList(recommendations, "PrimaryRecommendations").Single();
        var whyWeRecommendIt = ReadString(recommendation, "WhyWeRecommendIt");

        Assert.Contains("Why This Wins", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Why Alternatives Lose", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Business Tradeoffs", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Audience Tradeoffs", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Operational Tradeoffs", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Analytical Tradeoffs", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("handoff", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("technician", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("operational workflow is not the primary business need", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Recommendation Engine explanation fidelity matches the winning ranking signals")]
    public void BuildRecommendations_ExplanationFidelity_MatchesWinningSignals()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Open Work Orders", "Resolution Time", "Escalation Count"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Technician", "Service"),
                ("Work Order", "Service")
            ],
            relationships: [("Service", "Date"), ("Service", "Technician"), ("Service", "WorkOrder")],
            audienceSignals: [("Operational", "High"), ("Executive", "Medium")],
            domainSignals: [("Service", "High")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "service-workflow-orchestration",
                    name: "Service Workflow Orchestration",
                    category: "ServiceOperations",
                    audience: "Operations Leadership",
                    businessOutcome: "Coordinate daily backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                    candidateExperienceTypes: ["FabricApp", "OperationalMonitoringExperience", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Service"),
                        ("Measure", "Open Work Orders"),
                        ("Dimension", "Technician"),
                        ("Dimension", "Work Order")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var recommendation = ReadObjectList(recommendations, "PrimaryRecommendations").Single();
        var whyWeRecommendIt = ReadString(recommendation, "WhyWeRecommendIt");

        Assert.Equal("FabricApp", ReadString(recommendation, "RecommendedExperienceType"));
        Assert.Contains("daily", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("operational", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workflow", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Technician", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Work Order", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Recommendation Engine uses audience and analytical depth to choose among multiple valid experience types")]
    public void BuildRecommendations_ContextAwareExperienceSelection_ChangesOutcomeForTheSameOpportunity()
    {
        var executiveProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "Margin Variance"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Customer Segment", "Customer")],
            audienceSignals: [("Executive", "High")],
            domainSignals: [("Profitability", "High"), ("Revenue", "High")]);
        var analyticalProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "Margin Variance"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Customer Segment", "Customer"), ("Product Category", "Product")],
            hierarchies: [("Geography", ["Region", "Territory"]), ("Product", ["Category", "Subcategory"])],
            relationships: [("Sales", "Customer"), ("Sales", "Product"), ("Sales", "Date")],
            audienceSignals: [("Analytical", "High")],
            domainSignals: [("Profitability", "High"), ("Revenue", "High")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                opportunityId: "profitability-design-direction",
                name: "Profitability Design Direction",
                category: "ProfitabilityAnalysis",
                audience: "Finance Leadership",
                businessOutcome: "Explain margin movement and guide leadership follow-up on the biggest drivers.",
                candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport", "AnalyticalInvestigationExperience"],
                supportingSignals:
                [
                    ("Domain", "Profitability"),
                    ("Measure", "Margin Variance"),
                    ("Dimension", "Customer Segment"),
                    ("Dimension", "Region")
                ],
                limitingFactors: [],
                confidence: "High"));

        var executiveRecommendation = ReadObjectList(BuildRecommendations(executiveProfile, catalog), "PrimaryRecommendations").Single();
        var analyticalRecommendation = ReadObjectList(BuildRecommendations(analyticalProfile, catalog), "PrimaryRecommendations").Single();

        Assert.Equal("ExecutiveDashboard", ReadString(executiveRecommendation, "RecommendedExperienceType"));
        Assert.Equal("AnalyticalInvestigationExperience", ReadString(analyticalRecommendation, "RecommendedExperienceType"));
    }

    [Fact(DisplayName = "Recommendation Engine uses workflow signals instead of category defaults when Fabric App is the better fit")]
    public void BuildRecommendations_WorkflowSignalsCanFavorFabricApp()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Open Work Orders", "Resolution Time", "Escalation Count"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Technician", "Service"),
                ("Work Order", "Service"),
                ("Customer Segment", "Customer")
            ],
            relationships: [("Service", "Customer"), ("Service", "Date"), ("Service", "Technician")],
            audienceSignals: [("Operational", "High"), ("Executive", "Medium")],
            domainSignals: [("Service", "High"), ("Customer", "Medium")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "service-workflow-orchestration",
                    name: "Service Workflow Orchestration",
                    category: "ServiceOperations",
                    audience: "Operations Leadership",
                    businessOutcome: "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                    candidateExperienceTypes: ["OperationalMonitoringExperience", "FabricApp", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Service"),
                        ("Measure", "Open Work Orders"),
                        ("Dimension", "Technician"),
                        ("Dimension", "Work Order")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var recommendation = ReadObjectList(recommendations, "PrimaryRecommendations").Single();

        Assert.Equal("FabricApp", ReadString(recommendation, "RecommendedExperienceType"));
    }

    [Fact(DisplayName = "Recommendation Engine keeps the Top 3 materially diverse when high-scoring opportunities cluster")]
    public void BuildRecommendations_TopThreeAvoidTightClustering()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "Forecast Accuracy", "Open Exceptions", "Customer Margin Variance"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Territory", "Geography"),
                ("Warehouse", "Inventory"),
                ("Customer Segment", "Customer"),
                ("Product Category", "Product")
            ],
            hierarchies: [("Geography", ["Region", "Territory"])],
            relationships: [("Sales", "Customer"), ("Sales", "Product"), ("Inventory", "Date")],
            audienceSignals:
            [
                ("Executive", "High"),
                ("Operational", "High"),
                ("Analytical", "High")
            ],
            domainSignals:
            [
                ("Revenue", "High"),
                ("Inventory", "High"),
                ("Profitability", "High")
            ]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate("exec-1", "Executive Revenue Overview", "ExecutiveReporting", "Executive", "Track weekly revenue KPI movement for leadership.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Revenue"), ("DateIntelligence", "High"), ("Audience", "Executive")], [], "High"),
                CreateOpportunityCandidate("exec-2", "Forecast Leadership Review", "ForecastAccuracy", "Executive", "Review forecast accuracy during the weekly leadership rhythm.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Forecasting"), ("DateIntelligence", "High"), ("Audience", "Executive")], [], "High"),
                CreateOpportunityCandidate("exec-3", "Sales Territory Leadership", "SalesPerformance", "Executive", "Compare territory performance for leadership decisions.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Revenue"), ("Dimension", "Territory"), ("Audience", "Executive")], [], "High"),
                CreateOpportunityCandidate("ops-1", "Inventory Exception Monitor", "InventoryOptimization", "Operational", "Monitor daily inventory exceptions and assign operational action.", ["OperationalMonitoringExperience", "PbirReport"], [("Domain", "Inventory"), ("Measure", "Open Exceptions"), ("Audience", "Operational")], [], "High"),
                CreateOpportunityCandidate("analytical-1", "Customer Margin Investigation", "ProfitabilityAnalysis", "Analytical", "Investigate customer margin variance drivers before pricing action.", ["AnalyticalInvestigationExperience", "PbirReport"], [("Domain", "Profitability"), ("Measure", "Variance"), ("Drill", "HierarchyRich"), ("Audience", "Analytical")], [], "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");
        var experienceTypes = primary
            .Select(recommendation => ReadString(recommendation, "RecommendedExperienceType"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var audiences = primary
            .Select(recommendation => ReadString(recommendation, "ExpectedAudience"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(3, primary.Count);
        Assert.True(experienceTypes.Count >= 2);
        Assert.True(audiences.Count >= 2);
        Assert.DoesNotContain(primary, recommendation =>
            string.Equals(ReadString(recommendation, "RecommendationName"), "Sales Territory Leadership", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Recommendation Engine expands revenue and sales Top 3 recommendations beyond one executive-dashboard cluster when defensible")]
    public void BuildRecommendations_RevenueAndSalesRecommendations_AvoidSingleClusterWhenAlternativesAreCredible()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "Forecast Accuracy", "Customer Margin Variance", "Open Pipeline"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Territory", "Geography"),
                ("Customer Segment", "Customer"),
                ("Product Category", "Product")
            ],
            hierarchies: [("Geography", ["Region", "Territory"])],
            relationships: [("Sales", "Customer"), ("Sales", "Product"), ("Sales", "Date")],
            audienceSignals:
            [
                ("Executive", "High"),
                ("Analytical", "High"),
                ("Operational", "Medium")
            ],
            domainSignals:
            [
                ("Revenue", "High"),
                ("Forecasting", "High"),
                ("Profitability", "High"),
                ("Customer", "High")
            ]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate("sales-executive-1", "Executive Sales Reporting", "ExecutiveReporting", "Executive", "Track weekly revenue KPI movement for leadership decisions.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Revenue"), ("DateIntelligence", "High"), ("Audience", "Executive"), ("KpiCluster", "Revenue KPIs")], [], "High"),
                CreateOpportunityCandidate("sales-executive-2", "Sales Narrative Brief", "SalesPerformance", "Executive", "Guide the monthly revenue story and territory commentary for leadership readouts.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Revenue"), ("DateIntelligence", "High"), ("Dimension", "Territory"), ("Audience", "Executive")], [], "High"),
                CreateOpportunityCandidate("sales-executive-3", "Forecast Accuracy Dashboard", "ForecastAccuracy", "Executive", "Review forecast versus actuals in the weekly leadership rhythm.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Forecasting"), ("DateIntelligence", "High"), ("Measure", "Variance"), ("Audience", "Executive")], [], "High"),
                CreateOpportunityCandidate("sales-analytical-1", "Customer Profitability Experience", "ProfitabilityAnalysis", "Analytical", "Investigate customer margin variance before pricing and account action.", ["AnalyticalInvestigationExperience", "PbirReport", "FabricDataApp"], [("Domain", "Profitability"), ("Dimension", "Customer Segment"), ("Measure", "Variance"), ("Audience", "Analytical")], [], "High"),
                CreateOpportunityCandidate("sales-analytical-2", "Forecast Accuracy Investigation", "RootCauseInvestigation", "Analytical", "Investigate why forecast misses cluster by segment and territory before the next cycle.", ["AnalyticalInvestigationExperience", "PbirReport"], [("Domain", "Forecasting"), ("Measure", "Variance"), ("Drill", "HierarchyRich"), ("Audience", "Analytical")], [], "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");
        var experienceTypes = primary
            .Select(recommendation => ReadString(recommendation, "RecommendedExperienceType"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(3, primary.Count);
        Assert.True(experienceTypes.Count >= 2);
        Assert.Contains(primary, recommendation =>
            !string.Equals(ReadString(recommendation, "RecommendedExperienceType"), "ExecutiveDashboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Recommendation Engine lets a richer operational revenue experience beat an executive dashboard when follow-through is the real need")]
    public void BuildRecommendations_RevenueOperationalWorkflowCanBeatExecutiveDashboard()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Open Pipeline", "At Risk Pipeline", "Win Rate"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Sales Manager", "Sales"),
                ("Account Executive", "Sales"),
                ("Opportunity", "Sales")
            ],
            relationships: [("Pipeline", "Date"), ("Pipeline", "SalesManager"), ("Pipeline", "Opportunity")],
            audienceSignals:
            [
                ("Executive", "High"),
                ("Operational", "High")
            ],
            domainSignals:
            [
                ("Revenue", "High"),
                ("Sales", "High")
            ]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "executive-sales-reporting",
                    name: "Executive Sales Reporting",
                    category: "ExecutiveReporting",
                    audience: "Executive",
                    businessOutcome: "Track monthly revenue performance and leadership KPI movement.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("KpiCluster", "Revenue KPIs"),
                        ("Audience", "Executive")
                    ],
                    limitingFactors: [],
                    confidence: "High"),
                CreateOpportunityCandidate(
                    opportunityId: "revenue-recovery-workflow",
                    name: "Revenue Recovery Workflow",
                    category: "SalesPerformance",
                    audience: "Sales Manager",
                    businessOutcome: "Coordinate daily pipeline triage, assign owner follow-up, and route at-risk opportunities across managers and account executives.",
                    candidateExperienceTypes: ["FabricApp", "ExecutiveDashboard", "OperationalMonitoringExperience"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("Measure", "Open Pipeline"),
                        ("Dimension", "Sales Manager"),
                        ("Dimension", "Account Executive"),
                        ("Dimension", "Opportunity")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");

        Assert.Equal("Revenue Recovery Workflow", ReadString(primary[0], "RecommendationName"));
        Assert.Equal("FabricApp", ReadString(primary[0], "RecommendedExperienceType"));
        Assert.Equal("Executive Sales Reporting", ReadString(primary[1], "RecommendationName"));
    }

    [Fact(DisplayName = "Recommendation Engine keeps forecasting recommendations distinct from generic revenue reporting")]
    public void BuildRecommendations_ForecastingRecommendationsBeatGenericRevenueReportingWhenForecastSignalsLead()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Forecast", "Forecast Variance", "Forecast Accuracy"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Product Category", "Product"),
                ("Customer Segment", "Customer")
            ],
            hierarchies: [("Geography", ["Region", "Territory"])],
            relationships: [("Forecast", "Date"), ("Forecast", "Product"), ("Forecast", "Customer")],
            audienceSignals:
            [
                ("Executive", "High"),
                ("Analytical", "Medium")
            ],
            domainSignals:
            [
                ("Revenue", "High"),
                ("Forecasting", "High")
            ]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "executive-sales-reporting",
                    name: "Executive Sales Reporting",
                    category: "ExecutiveReporting",
                    audience: "Executive",
                    businessOutcome: "Track monthly revenue performance and leadership KPI movement.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("KpiCluster", "Revenue KPIs"),
                        ("Audience", "Executive")
                    ],
                    limitingFactors: [],
                    confidence: "High"),
                CreateOpportunityCandidate(
                    opportunityId: "forecast-accuracy-dashboard",
                    name: "Forecast Accuracy Dashboard",
                    category: "ForecastAccuracy",
                    audience: "Planning Leadership",
                    businessOutcome: "Review weekly forecast accuracy, investigate miss patterns, and improve the next planning cycle.",
                    candidateExperienceTypes: ["AnalyticalInvestigationExperience", "ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Forecasting"),
                        ("Measure", "Forecast Variance"),
                        ("Measure", "Forecast Accuracy"),
                        ("Dimension", "Customer Segment"),
                        ("Audience", "Executive")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");

        Assert.Equal("Forecast Accuracy Dashboard", ReadString(primary[0], "RecommendationName"));
        Assert.NotEqual("Executive Sales Reporting", ReadString(primary[0], "RecommendationName"));
        Assert.Contains("forecast", ReadString(primary[0], "WhyWeRecommendIt"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Recommendation Engine keeps customer profitability recommendations distinct from generic revenue reporting")]
    public void BuildRecommendations_CustomerProfitabilityRecommendationsBeatGenericRevenueReportingWhenProfitabilitySignalsLead()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "Margin Variance", "Customer Lifetime Value"],
            dimensions:
            [
                ("Date", "Date"),
                ("Customer", "Customer"),
                ("Customer Segment", "Customer"),
                ("Region", "Geography"),
                ("Product Category", "Product")
            ],
            hierarchies: [("Customer", ["Customer Segment", "Customer"])],
            relationships: [("Sales", "Customer"), ("Sales", "Product"), ("Sales", "Date")],
            audienceSignals:
            [
                ("Executive", "Medium"),
                ("Analytical", "High")
            ],
            domainSignals:
            [
                ("Revenue", "High"),
                ("Customer", "High"),
                ("Profitability", "High")
            ]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "executive-sales-reporting",
                    name: "Executive Sales Reporting",
                    category: "ExecutiveReporting",
                    audience: "Executive",
                    businessOutcome: "Track monthly revenue performance and leadership KPI movement.",
                    candidateExperienceTypes: ["ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Revenue"),
                        ("DateIntelligence", "High"),
                        ("KpiCluster", "Revenue KPIs"),
                        ("Audience", "Executive")
                    ],
                    limitingFactors: [],
                    confidence: "High"),
                CreateOpportunityCandidate(
                    opportunityId: "customer-profitability-analysis",
                    name: "Customer Profitability Analysis",
                    category: "ProfitabilityAnalysis",
                    audience: "Commercial Strategy",
                    businessOutcome: "Identify which customer segments and accounts drive profitable growth before pricing and account action.",
                    candidateExperienceTypes: ["FabricDataApp", "AnalyticalInvestigationExperience", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Customer"),
                        ("Domain", "Profitability"),
                        ("Dimension", "Customer Segment"),
                        ("Dimension", "Customer"),
                        ("Measure", "Margin Variance")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");

        Assert.Equal("Customer Profitability Analysis", ReadString(primary[0], "RecommendationName"));
        Assert.NotEqual("Executive Sales Reporting", ReadString(primary[0], "RecommendationName"));
        Assert.Contains("customer", ReadString(primary[0], "WhyWeRecommendIt"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("profit", ReadString(primary[0], "WhyWeRecommendIt"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Recommendation Engine distinguishes service workflow orchestration from monitoring-only service recommendations")]
    public void BuildRecommendations_ServiceWorkflowRecommendationsBeatMonitoringWhenWorkflowSignalsLead()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Open Work Orders", "Resolution Time", "Escalation Count"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Technician", "Service"),
                ("Work Order", "Service"),
                ("Service Queue", "Service")
            ],
            relationships: [("Service", "Date"), ("Service", "Technician"), ("Service", "WorkOrder")],
            audienceSignals:
            [
                ("Operational", "High"),
                ("Executive", "Medium")
            ],
            domainSignals:
            [
                ("Service", "High")
            ]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "service-operations-dashboard",
                    name: "Service Operations Dashboard",
                    category: "ServiceOperations",
                    audience: "Service Operations",
                    businessOutcome: "Monitor service backlog, technician performance, and SLA risk.",
                    candidateExperienceTypes: ["OperationalMonitoringExperience", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Service"),
                        ("Dimension", "Technician"),
                        ("Measure", "Resolution")
                    ],
                    limitingFactors: [],
                    confidence: "High"),
                CreateOpportunityCandidate(
                    opportunityId: "service-workflow-orchestration",
                    name: "Service Workflow Orchestration",
                    category: "ServiceOperations",
                    audience: "Service Manager",
                    businessOutcome: "Coordinate daily backlog triage, technician follow-up, work-order routing, and cross-regional handoffs.",
                    candidateExperienceTypes: ["FabricApp", "OperationalMonitoringExperience", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Service"),
                        ("Dimension", "Technician"),
                        ("Dimension", "Work Order"),
                        ("Dimension", "Service Queue"),
                        ("Measure", "Open Work Orders")
                    ],
                    limitingFactors: [],
                    confidence: "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations");

        Assert.Equal("Service Workflow Orchestration", ReadString(primary[0], "RecommendationName"));
        Assert.Equal("FabricApp", ReadString(primary[0], "RecommendedExperienceType"));
        Assert.Contains(primary, recommendation =>
            string.Equals(ReadString(recommendation, "RecommendationName"), "Service Operations Dashboard", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "Recommendation Engine rationale includes consultant decision tradeoffs and future evolution guidance")]
    public void BuildRecommendations_RationaleIncludesConsultantDecisionFrameworkSections()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Forecast Variance", "Forecast Accuracy"],
            dimensions:
            [
                ("Date", "Date"),
                ("Region", "Geography"),
                ("Customer Segment", "Customer")
            ],
            relationships: [("Forecast", "Date"), ("Forecast", "Customer")],
            audienceSignals: [("Executive", "High"), ("Analytical", "Medium")],
            domainSignals: [("Forecasting", "High"), ("Revenue", "High")]);

        var recommendations = BuildRecommendations(
            profile,
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    opportunityId: "forecast-accuracy-dashboard",
                    name: "Forecast Accuracy Dashboard",
                    category: "ForecastAccuracy",
                    audience: "Planning Leadership",
                    businessOutcome: "Review weekly forecast accuracy, investigate miss patterns, and improve the next planning cycle.",
                    candidateExperienceTypes: ["AnalyticalInvestigationExperience", "ExecutiveDashboard", "PbirReport"],
                    supportingSignals:
                    [
                        ("Domain", "Forecasting"),
                        ("Measure", "Forecast Variance"),
                        ("Measure", "Forecast Accuracy"),
                        ("Dimension", "Customer Segment")
                    ],
                    limitingFactors: ["Scenario granularity is still uneven by region."],
                    confidence: "High")));

        var primary = ReadObjectList(recommendations, "PrimaryRecommendations").Single();
        var whyWeRecommendIt = ReadString(primary, "WhyWeRecommendIt");

        Assert.Contains("Why This Experience Wins", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Why Competing Experiences Lose", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Risks", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Assumptions", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Adoption Considerations", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Future Evolution Path", whyWeRecommendIt, StringComparison.OrdinalIgnoreCase);
    }

    private static object BuildRecommendations(object profile, object catalog)
    {
        var recommendationServiceType = CoreAssembly.GetType($"{DiscoveryServicesNamespace}.RecommendationEngineService", throwOnError: false);
        Assert.NotNull(recommendationServiceType);

        var recommendationService = Activator.CreateInstance(
            recommendationServiceType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null);
        Assert.NotNull(recommendationService);

        var buildMethod = recommendationServiceType!.GetMethod("BuildRecommendations", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(buildMethod);

        var recommendations = buildMethod!.Invoke(recommendationService, [profile, catalog]);
        Assert.NotNull(recommendations);
        return recommendations!;
    }

    private static object CreateDiscoveryProfile(
        string confidence,
        string dateReadiness,
        IReadOnlyList<string>? measures = null,
        IReadOnlyList<(string Name, string Role)>? dimensions = null,
        IReadOnlyList<(string Name, IReadOnlyList<string> Levels)>? hierarchies = null,
        IReadOnlyList<(string FromTable, string ToTable)>? relationships = null,
        IReadOnlyList<string>? ambiguityNotes = null,
        IReadOnlyList<(string Audience, string Confidence)>? audienceSignals = null,
        IReadOnlyList<(string Domain, string Confidence)>? domainSignals = null,
        string semanticModelReferenceId = "semantic-model:test",
        string discoveryProfileReferenceId = "discovery-profile:test")
    {
        var measureType = GetType("DiscoveryMeasureProfile");
        var dimensionType = GetType("DiscoveryDimensionProfile");
        var hierarchyType = GetType("DiscoveryHierarchyProfile");
        var relationshipType = GetType("DiscoveryRelationshipProfile");
        var dateIntelligenceType = GetType("DiscoveryDateIntelligenceProfile");
        var audienceSignalType = GetType("DiscoveryAudienceSignal");
        var domainSignalType = GetType("DiscoveryDomainSignal");
        var profileType = GetType("DiscoveryProfile");

        var measureList = CreateTypedList(
            measureType,
            (measures ?? [])
                .Select(name => CreateInstance(measureType, name, null!, null!))
                .ToArray());
        var dimensionList = CreateTypedList(
            dimensionType,
            (dimensions ?? [])
                .Select(dimension => CreateInstance(dimensionType, dimension.Name, "Many", dimension.Role))
                .ToArray());
        var hierarchyList = CreateTypedList(
            hierarchyType,
            (hierarchies ?? [])
                .Select(hierarchy => CreateInstance(hierarchyType, hierarchy.Name, CreateTypedList(typeof(string), hierarchy.Levels.Cast<object>().ToArray()), false))
                .ToArray());
        var relationshipList = CreateTypedList(
            relationshipType,
            (relationships ?? [])
                .Select(relationship => CreateInstance(relationshipType, relationship.FromTable, relationship.ToTable, "ManyToOne", "Single"))
                .ToArray());
        var dateIntelligence = CreateInstance(
            dateIntelligenceType,
            CreateTypedList(typeof(string), (dimensions ?? []).Where(dimension => string.Equals(dimension.Role, "Date", StringComparison.OrdinalIgnoreCase)).Select(dimension => (object)dimension.Name).ToArray()),
            CreateTypedList(typeof(string), (dimensions ?? []).Where(dimension => string.Equals(dimension.Role, "Date", StringComparison.OrdinalIgnoreCase)).Select(dimension => (object)dimension.Name).ToArray()),
            ParseEnum(GetType("DiscoveryDateIntelligenceReadiness"), dateReadiness));

        var audienceList = CreateTypedList(
            audienceSignalType,
            (audienceSignals ?? [])
                .Select(signal => CreateInstance(
                    audienceSignalType,
                    signal.Audience,
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), signal.Confidence),
                    CreateTypedList(typeof(string), signal.Audience)))
                .ToArray());

        var domainList = CreateTypedList(
            domainSignalType,
            (domainSignals ?? [])
                .Select(signal => CreateInstance(
                    domainSignalType,
                    signal.Domain,
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), signal.Confidence),
                    CreateTypedList(typeof(string), signal.Domain)))
                .ToArray());

        return CreateInstance(
            profileType,
            measureList,
            dimensionList,
            hierarchyList,
            dateIntelligence,
            relationshipList,
            domainList,
            CreateTypedList(GetType("DiscoveryKpiCluster")),
            audienceList,
            CreateTypedList(typeof(string), (ambiguityNotes ?? []).Cast<object>().ToArray()),
            ParseEnum(GetType("DiscoveryConfidenceLevel"), confidence),
            semanticModelReferenceId,
            discoveryProfileReferenceId);
    }

    private static object CreateOpportunityCatalog(params object[] opportunities)
    {
        return CreateInstance(
            GetType("OpportunityCatalog"),
            CreateTypedList(GetType("OpportunityCandidate"), opportunities));
    }

    private static object CreateOpportunityCandidate(
        string opportunityId,
        string name,
        string category,
        string audience,
        string businessOutcome,
        IReadOnlyList<string> candidateExperienceTypes,
        IReadOnlyList<(string SignalType, string Value)> supportingSignals,
        IReadOnlyList<string> limitingFactors,
        string confidence)
    {
        var signalType = GetType("OpportunitySemanticSignal");
        var signals = supportingSignals
            .Select(signal => CreateInstance(signalType, signal.SignalType, signal.Value))
            .ToArray();
        var experienceType = GetType("OpportunityExperienceType");

        return CreateInstance(
            GetType("OpportunityCandidate"),
            opportunityId,
            name,
            ParseEnum(GetType("OpportunityCategory"), category),
            audience,
            businessOutcome,
            CreateTypedList(experienceType, candidateExperienceTypes.Select(type => ParseEnum(experienceType, type)).ToArray()),
            CreateTypedList(signalType, signals),
            CreateTypedList(typeof(string), limitingFactors.Cast<object>().ToArray()),
            ParseEnum(GetType("DiscoveryConfidenceLevel"), confidence));
    }

    private static Type GetType(string typeName)
    {
        return CoreAssembly.GetType($"{DiscoveryModelsNamespace}.{typeName}", throwOnError: false)
            ?? throw new InvalidOperationException($"Type '{typeName}' was not found.");
    }

    private static object ParseEnum(Type enumType, string value)
    {
        return Enum.Parse(enumType, value, ignoreCase: true);
    }

    private static object CreateInstance(Type type, params object[] args)
    {
        return Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: args,
            culture: null)
            ?? throw new InvalidOperationException($"Could not create '{type.FullName}'.");
    }

    private static object CreateTypedList(Type elementType, params object[] items)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)(Activator.CreateInstance(listType)
            ?? throw new InvalidOperationException($"Could not create '{listType.FullName}'."));

        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static List<object> ReadAllRecommendations(object result)
    {
        return ReadObjectList(result, "PrimaryRecommendations")
            .Concat(ReadObjectList(result, "AlternateRecommendations"))
            .ToList();
    }

    private static List<object> ReadObjectList(object target, string propertyName)
    {
        var value = GetPropertyValue(target, propertyName);
        Assert.NotNull(value);
        return ((IEnumerable)value!).Cast<object>().ToList();
    }

    private static List<string> ReadStringList(object target, string propertyName)
    {
        var value = GetPropertyValue(target, propertyName);
        Assert.NotNull(value);
        return ((IEnumerable)value!).Cast<object>().Select(item => item?.ToString() ?? string.Empty).ToList();
    }

    private static string ReadString(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString()
            ?? throw new InvalidOperationException($"Property '{propertyName}' was null.");
    }

    private static double ReadDouble(object target, string propertyName)
    {
        return Convert.ToDouble(GetPropertyValue(target, propertyName));
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
    }
}
