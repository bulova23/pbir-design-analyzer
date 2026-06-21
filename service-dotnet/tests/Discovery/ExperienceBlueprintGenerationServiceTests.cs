using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class ExperienceBlueprintGenerationServiceTests
{
    private static readonly Assembly CoreAssembly = typeof(PbirScoringService).Assembly;
    private const string DiscoveryModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";
    private const string DiscoveryServicesNamespace = "PowerBIModelingService.Services.Discovery";

    [Fact(DisplayName = "Experience Blueprint generation creates an executive dashboard blueprint")]
    public void BuildRecommendationBlueprints_ExecutiveDashboard_GeneratesExecutiveBlueprint()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "YoY Growth"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Territory", "Geography"), ("Customer Segment", "Customer")],
            kpiClusters: [("Revenue KPIs", "High", ["Revenue", "Gross Margin", "YoY Growth"])],
            audienceSignals: [("Executive", "High")],
            domainSignals: [("Revenue", "High")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "executive-sales-reporting",
                "Executive Sales Reporting",
                "ExecutiveReporting",
                "Executive",
                "Track revenue trends and leadership-level performance over time.",
                ["ExecutiveDashboard", "PbirReport"],
                [("Domain", "Revenue"), ("DateIntelligence", "High"), ("Dimension", "Territory"), ("KpiCluster", "Revenue KPIs")],
                [],
                "High"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "executive-sales-reporting",
                    "Executive Sales Reporting",
                    "ExecutiveDashboard",
                    "High",
                    "High",
                    "Medium",
                    "Strong revenue semantic coverage.",
                    "Executive",
                    "Track revenue trends and leadership-level performance over time.",
                    ["Strong Revenue semantic coverage", "High date intelligence readiness"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because a concise executive KPI experience spans several semantic signals and design choices.",
                    "Primary",
                    91.2)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());

        Assert.Equal("Executive", ReadString(blueprint, "ExpectedAudience"));
        Assert.Contains("Revenue Leadership Summary", ReadPageNames(blueprint));
        Assert.Contains("Growth and Mix Review", ReadPageNames(blueprint));
        Assert.Contains("Commercial Follow-Up", ReadPageNames(blueprint));
        Assert.Contains("leadership summary", ReadString(ReadObject(blueprint, "NavigationIntent"), "Flow"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Experience Blueprint generation creates an operational monitoring blueprint")]
    public void BuildRecommendationBlueprints_OperationalMonitoring_GeneratesOperationalBlueprint()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Quantity On Hand", "Open Exceptions", "Backlog Trend"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Product Category", "Product"), ("Warehouse", "Inventory")],
            audienceSignals: [("Operational", "High")],
            domainSignals: [("Inventory", "High")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "inventory-operations-monitoring",
                "Inventory Operations Monitoring",
                "InventoryOptimization",
                "Operational",
                "Monitor stock position, warehouse health, and item-level inventory risk.",
                ["OperationalMonitoringExperience", "PbirReport", "FabricApp"],
                [("Domain", "Inventory"), ("Measure", "Quantity"), ("Dimension", "Product")],
                [],
                "High"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "inventory-operations-monitoring",
                    "Inventory Operations Monitoring",
                    "OperationalMonitoringExperience",
                    "High",
                    "High",
                    "Medium",
                    "Strong inventory semantic coverage.",
                    "Operational",
                    "Monitor stock position, warehouse health, and item-level inventory risk.",
                    ["Strong Inventory semantic coverage", "Quantity measure support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because an operational monitoring flow spans several semantic signals and design choices.",
                    "Primary",
                    88.5)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());
        var analyticalFlow = ReadObject(blueprint, "AnalyticalFlow");

        Assert.Contains("Overview", ReadPageNames(blueprint));
        Assert.Contains("Exceptions", ReadPageNames(blueprint));
        Assert.Contains("Detail", ReadPageNames(blueprint));
        Assert.Contains("monitor", ReadString(ReadObject(blueprint, "NavigationIntent"), "Flow"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exception", ReadString(ReadObject(blueprint, "NavigationIntent"), "Flow"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action", ReadString(analyticalFlow, "Decision"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Experience Blueprint generation creates an analytical investigation blueprint")]
    public void BuildRecommendationBlueprints_AnalyticalInvestigation_GeneratesAnalyticalBlueprint()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "Medium",
            measures: ["Margin Variance", "Revenue", "Gross Margin"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Product Category", "Product"), ("Customer Segment", "Customer")],
            hierarchies: [("Geography", ["Region", "Territory"])],
            relationships: [("Sales", "Customer"), ("Sales", "Product"), ("Sales", "Date")],
            audienceSignals: [("Analytical", "High")],
            domainSignals: [("Profitability", "High"), ("Revenue", "High")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "root-cause-analysis-experience",
                "Root Cause Analysis Experience",
                "RootCauseInvestigation",
                "Analytical",
                "Investigate drivers of variance through drill-based root cause analysis.",
                ["AnalyticalInvestigationExperience", "PbirReport"],
                [("Audience", "Analytical"), ("Measure", "Variance"), ("Drill", "HierarchyRich"), ("Dimension", "Customer Segment")],
                [],
                "High"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "root-cause-analysis-experience",
                    "Root Cause Analysis Experience",
                    "AnalyticalInvestigationExperience",
                    "High",
                    "High",
                    "High",
                    "Strong variance and drill semantic coverage.",
                    "Analytical",
                    "Investigate drivers of variance through drill-based root cause analysis.",
                    ["Variance measure support", "HierarchyRich drill path support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "High complexity because an analytical drill-based experience needs broader semantic coordination and design shaping.",
                    "Primary",
                    86.4)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());
        var analyticalFlow = ReadObject(blueprint, "AnalyticalFlow");

        Assert.Contains("Question", ReadPageNames(blueprint));
        Assert.Contains("Investigation", ReadPageNames(blueprint));
        Assert.Contains("Evidence", ReadPageNames(blueprint));
        Assert.Contains("Conclusion", ReadPageNames(blueprint));
        Assert.Contains("question", ReadString(ReadObject(blueprint, "NavigationIntent"), "Flow"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("investigation", ReadString(analyticalFlow, "Investigation"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("evidence", ReadString(analyticalFlow, "Evidence"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("decision", ReadString(analyticalFlow, "Decision"), StringComparison.OrdinalIgnoreCase);
    }

    [Theory(DisplayName = "Experience Blueprint generation supports PBIR Report, Fabric App, and Fabric Data App experience types")]
    [InlineData("PbirReport", "Pbir Report", "Narrative")]
    [InlineData("FabricApp", "Fabric App", "App Overview")]
    [InlineData("FabricDataApp", "Fabric Data App", "Data Explorer")]
    public void BuildRecommendationBlueprints_ExperienceTypes_GenerateSupportedBlueprintShapes(
        string experienceType,
        string expectedLabel,
        string expectedPage)
    {
        var profile = CreateDiscoveryProfile(
            confidence: "Medium",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Customer Segment", "Customer")],
            audienceSignals: [("Executive", "Medium"), ("Analytical", "Medium")],
            domainSignals: [("Revenue", "High"), ("Customer", "Medium")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "multi-experience-opportunity",
                $"Recommendation for {expectedLabel}",
                "CustomerAnalysis",
                "Commercial Strategy",
                "Use the semantic model in a differentiated experience shape.",
                [experienceType],
                [("Domain", "Revenue"), ("Dimension", "Customer Segment")],
                [],
                "Medium"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "multi-experience-opportunity",
                    $"Recommendation for {expectedLabel}",
                    experienceType,
                    "Medium",
                    "Medium",
                    "Medium",
                    "Structured semantic support exists.",
                    "Commercial Strategy",
                    "Use the semantic model in a differentiated experience shape.",
                    ["Structured semantic support exists"],
                    [],
                    "Medium confidence because the model supports this use case but still leaves some ambiguity.",
                    "Medium complexity because the experience spans several semantic signals and design choices.",
                    "Primary",
                    72.3)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());

        Assert.Equal(experienceType, ReadString(blueprint, "ExperienceType"));
        Assert.Contains(ReadPageNames(blueprint), page => page.Contains(expectedPage, StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(ReadStringList(blueprint, "SuccessCriteriaSeed"));
    }

    [Fact(DisplayName = "Experience Blueprint generation derives KPIs filters visuals navigation and provenance")]
    public void BuildRecommendationBlueprints_DerivesCoreBlueprintContent()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "YoY Growth", "Forecast Accuracy", "Customer Retention"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Territory", "Geography"), ("Product Category", "Product"), ("Customer Segment", "Customer")],
            audienceSignals: [("Executive", "High")],
            domainSignals: [("Revenue", "High"), ("Forecasting", "High"), ("Customer", "High")],
            kpiClusters:
            [
                ("Revenue KPIs", "High", ["Revenue", "Gross Margin", "YoY Growth"]),
                ("Forecast KPIs", "High", ["Forecast Accuracy"]),
                ("Customer KPIs", "Medium", ["Customer Retention"])
            ]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "executive-sales-reporting",
                "Executive Sales Reporting",
                "ExecutiveReporting",
                "Executive",
                "Track revenue trends and leadership-level performance over time.",
                ["ExecutiveDashboard", "PbirReport"],
                [("Domain", "Revenue"), ("DateIntelligence", "High"), ("Dimension", "Territory"), ("Dimension", "Customer Segment"), ("KpiCluster", "Revenue KPIs")],
                [],
                "High"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "executive-sales-reporting",
                    "Executive Sales Reporting",
                    "ExecutiveDashboard",
                    "High",
                    "High",
                    "Medium",
                    "Strong revenue semantic coverage.",
                    "Executive",
                    "Track revenue trends and leadership-level performance over time.",
                    ["Strong Revenue semantic coverage", "Revenue KPIs support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because a concise executive KPI experience spans several semantic signals and design choices.",
                    "Primary",
                    91.2)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());
        var pages = ReadObjectList(blueprint, "RecommendedPages");
        var firstPage = pages[0];
        var provenance = ReadObject(blueprint, "Provenance");

        Assert.Contains("Revenue", ReadStringList(blueprint, "PrimaryKpis"));
        Assert.Contains("Gross Margin", ReadStringList(blueprint, "PrimaryKpis"));
        Assert.Contains("Date", ReadStringList(blueprint, "SuggestedGlobalFilters"));
        Assert.Contains("Territory", ReadStringList(blueprint, "SuggestedGlobalFilters"));
        Assert.NotEmpty(ReadStringList(firstPage, "SuggestedFilters"));
        Assert.NotEmpty(ReadStringList(firstPage, "SuggestedVisualTypes"));
        Assert.Contains("card", string.Join(" ", ReadStringList(firstPage, "SuggestedVisualTypes")), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("executive-sales-reporting", ReadString(provenance, "RecommendationId"));
        Assert.Equal("executive-sales-reporting", ReadString(provenance, "OpportunityId"));
        Assert.Equal("ExecutiveReporting", ReadString(provenance, "OpportunityCategory"));
        Assert.NotEmpty(ReadStringList(provenance, "SupportingSignals"));
    }

    [Fact(DisplayName = "Experience Blueprint generation degrades gracefully for sparse models")]
    public void BuildRecommendationBlueprints_SparseModel_StillProducesBlueprint()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "Low",
            dateReadiness: "Low",
            measures: ["Revenue"],
            dimensions: [("Date", "Date")],
            ambiguityNotes:
            [
                "Business domains are weakly inferred from measure names only.",
                "Date intelligence is not well-defined."
            ],
            audienceSignals: [],
            domainSignals: [("Revenue", "Medium")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "sparse-revenue-opportunity",
                "Revenue Monitoring Direction",
                "ExecutiveReporting",
                "Executive",
                "Track basic revenue movement over time.",
                ["PbirReport"],
                [("Domain", "Revenue")],
                ["Opportunity is inferred from limited semantic evidence."],
                "Low"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "sparse-revenue-opportunity",
                    "Revenue Monitoring Direction",
                    "PbirReport",
                    "Low",
                    "Medium",
                    "Low",
                    "The model suggests a credible direction, but semantic support is still limited.",
                    "Executive",
                    "Track basic revenue movement over time.",
                    ["Strong Revenue semantic coverage"],
                    ["Opportunity is inferred from limited semantic evidence."],
                    "Low confidence because the recommendation is inferred from sparse or ambiguous model signals.",
                    "Low complexity because a report-oriented experience can be shaped from a relatively focused semantic footprint.",
                    "Primary",
                    51.6)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());

        Assert.NotEmpty(ReadPageNames(blueprint));
        Assert.NotEmpty(ReadStringList(blueprint, "PrimaryKpis"));
        Assert.NotEmpty(ReadStringList(blueprint, "SuccessCriteriaSeed"));
        Assert.Contains(ReadStringList(ReadObject(blueprint, "Provenance"), "AmbiguityNotes"), note =>
            note.Contains("weakly inferred", StringComparison.OrdinalIgnoreCase) ||
            note.Contains("not well-defined", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Experience Blueprint generation never fabricates unsupported KPIs and preserves ambiguity when support is insufficient")]
    public void BuildRecommendationBlueprints_StrictKpiFidelity_PreservesAmbiguityInsteadOfFallbackKpis()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "Medium",
            dateReadiness: "High",
            measures: ["Open Work Orders", "Resolution Time"],
            dimensions: [("DimDate", "Date"), ("DimTechnician", "Service"), ("DimWorkOrder", "Service")],
            audienceSignals: [("Operational", "High")],
            domainSignals: [("Service", "High")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "service-operations-dashboard",
                "Service Operations Dashboard",
                "ServiceOperations",
                "Operations Leadership",
                "Monitor service backlog pressure and technician throughput without inferring unsupported financial KPIs.",
                ["OperationalMonitoringExperience", "PbirReport"],
                [("Domain", "Service"), ("Measure", "Open Work Orders"), ("Measure", "Resolution Time"), ("Dimension", "Technician")],
                ["KPI support is limited to operational service measures."],
                "Medium"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "service-operations-dashboard",
                    "Service Operations Dashboard",
                    "OperationalMonitoringExperience",
                    "Medium",
                    "High",
                    "Medium",
                    "Strong service semantic coverage with intentionally narrow KPI support.",
                    "Operations Leadership",
                    "Monitor service backlog pressure and technician throughput without inferring unsupported financial KPIs.",
                    ["Service semantic support", "Operational KPI support"],
                    ["KPI support is limited to operational service measures."],
                    "Medium confidence because the model supports the service workflow but not a wider KPI layer.",
                    "Medium complexity because an operational monitoring flow still needs deliberate queue design.",
                    "Primary",
                    78.6)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());
        var primaryKpis = ReadStringList(blueprint, "PrimaryKpis");
        var ambiguityNotes = ReadStringList(ReadObject(blueprint, "Provenance"), "AmbiguityNotes");

        Assert.Equal(new[] { "Open Work Orders", "Resolution Time" }, primaryKpis);
        Assert.DoesNotContain(primaryKpis, kpi => kpi.Contains("Revenue", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(primaryKpis, kpi => kpi.Contains("Gross Margin", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(primaryKpis, kpi => kpi.Contains("Backlog Trend", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ambiguityNotes, note =>
            note.Contains("limited", StringComparison.OrdinalIgnoreCase) ||
            note.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Experience Blueprint generation converts technical dimension names into consultant-facing filter labels")]
    public void BuildRecommendationBlueprints_FilterNaming_UsesConsultantFacingLabels()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Inventory Quantity", "Inventory Value", "Stock Variance"],
            dimensions:
            [
                ("DimDate", "Date"),
                ("DimCustomer", "Customer"),
                ("DimProduct", "Product"),
                ("DimWarehouse", "Inventory")
            ],
            audienceSignals: [("Operational", "High")],
            domainSignals: [("Inventory", "High")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "inventory-operations-monitoring",
                "Inventory Operations Monitoring",
                "InventoryOptimization",
                "Operational",
                "Monitor inventory risk across customers, products, and warehouses.",
                ["OperationalMonitoringExperience"],
                [("Domain", "Inventory"), ("Measure", "Inventory Quantity"), ("Dimension", "Warehouse")],
                [],
                "High"));
        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "inventory-operations-monitoring",
                    "Inventory Operations Monitoring",
                    "OperationalMonitoringExperience",
                    "High",
                    "High",
                    "Medium",
                    "Strong inventory semantic coverage.",
                    "Operational",
                    "Monitor inventory risk across customers, products, and warehouses.",
                    ["Inventory semantic support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because an operational monitoring flow spans several semantic signals and design choices.",
                    "Primary",
                    88.2)
            ],
            alternates: []);

        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        var blueprint = ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());

        Assert.Contains("Date", ReadStringList(blueprint, "SuggestedGlobalFilters"));
        Assert.Contains("Customer", ReadStringList(blueprint, "SuggestedGlobalFilters"));
        Assert.Contains("Product", ReadStringList(blueprint, "SuggestedGlobalFilters"));
        Assert.Contains("Warehouse", ReadStringList(blueprint, "SuggestedGlobalFilters"));
        Assert.DoesNotContain(ReadStringList(blueprint, "SuggestedGlobalFilters"), filter => filter.StartsWith("Dim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Experience Blueprint generation differentiates inventory and service operational scenarios")]
    public void BuildRecommendationBlueprints_OperationalScenarios_DifferMateriallyByWorkflow()
    {
        var inventoryProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Quantity On Hand", "Stockout Risk", "Open Exceptions"],
            dimensions: [("Date", "Date"), ("Warehouse", "Inventory"), ("Product Category", "Product"), ("Region", "Geography")],
            audienceSignals: [("Operational", "High")],
            domainSignals: [("Inventory", "High")]);
        var serviceProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Open Work Orders", "Resolution Time", "SLA Breach Risk"],
            dimensions: [("Date", "Date"), ("Technician", "Service"), ("Work Order", "Service"), ("Region", "Geography")],
            audienceSignals: [("Operational", "High")],
            domainSignals: [("Service", "High")]);
        var inventoryCatalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "inventory-operations-monitoring",
                "Inventory Operations Monitoring",
                "InventoryOptimization",
                "Operational",
                "Monitor stock position, warehouse health, and item-level inventory risk.",
                ["OperationalMonitoringExperience", "PbirReport"],
                [("Domain", "Inventory"), ("Measure", "Quantity On Hand"), ("Dimension", "Warehouse")],
                [],
                "High"));
        var serviceCatalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "service-operations-monitoring",
                "Service Operations Monitoring",
                "ServiceOperations",
                "Operational",
                "Monitor service backlog, technician throughput, and SLA risk.",
                ["OperationalMonitoringExperience", "PbirReport"],
                [("Domain", "Service"), ("Measure", "Open Work Orders"), ("Dimension", "Technician")],
                [],
                "High"));
        var inventoryRecommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "inventory-operations-monitoring",
                    "Inventory Operations Monitoring",
                    "OperationalMonitoringExperience",
                    "High",
                    "High",
                    "Medium",
                    "Strong inventory semantic coverage.",
                    "Operational",
                    "Monitor stock position, warehouse health, and item-level inventory risk.",
                    ["Inventory semantic support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because an operational monitoring flow spans several semantic signals and design choices.",
                    "Primary",
                    88.5)
            ],
            alternates: []);
        var serviceRecommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "service-operations-monitoring",
                    "Service Operations Monitoring",
                    "OperationalMonitoringExperience",
                    "High",
                    "High",
                    "Medium",
                    "Strong service semantic coverage.",
                    "Operational",
                    "Monitor service backlog, technician throughput, and SLA risk.",
                    ["Service semantic support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because an operational monitoring flow spans several semantic signals and design choices.",
                    "Primary",
                    88.2)
            ],
            alternates: []);

        var inventoryBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(inventoryProfile, inventoryCatalog, inventoryRecommendations), "PrimaryRecommendations").Single());
        var serviceBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(serviceProfile, serviceCatalog, serviceRecommendations), "PrimaryRecommendations").Single());
        var inventoryFirstPage = ReadObjectList(inventoryBlueprint, "RecommendedPages").First();
        var serviceFirstPage = ReadObjectList(serviceBlueprint, "RecommendedPages").First();

        Assert.NotEqual(ReadPageNames(inventoryBlueprint), ReadPageNames(serviceBlueprint));
        Assert.NotEqual(ReadString(ReadObject(inventoryBlueprint, "NavigationIntent"), "Flow"), ReadString(ReadObject(serviceBlueprint, "NavigationIntent"), "Flow"));
        Assert.NotEqual(
            string.Join("|", ReadStringList(inventoryFirstPage, "SuggestedFilters")),
            string.Join("|", ReadStringList(serviceFirstPage, "SuggestedFilters")));
    }

    [Fact(DisplayName = "Experience Blueprint generation differentiates executive planning and executive revenue scenarios")]
    public void BuildRecommendationBlueprints_ExecutiveScenarios_DifferMateriallyByDecisionIntent()
    {
        var revenueProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "YoY Growth"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Territory", "Geography"), ("Customer Segment", "Customer")],
            audienceSignals: [("Executive", "High")],
            domainSignals: [("Revenue", "High")]);
        var forecastProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Forecast Accuracy", "Forecast Variance", "Plan Attainment"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Territory", "Geography"), ("Scenario", "Planning")],
            audienceSignals: [("Planning Leadership", "High")],
            domainSignals: [("Forecasting", "High")]);
        var revenueCatalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "executive-sales-reporting",
                "Executive Sales Reporting",
                "ExecutiveReporting",
                "Executive",
                "Track revenue trends and leadership-level performance over time.",
                ["ExecutiveDashboard", "PbirReport"],
                [("Domain", "Revenue"), ("Dimension", "Territory"), ("Dimension", "Customer Segment")],
                [],
                "High"));
        var forecastCatalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "forecast-accuracy-dashboard",
                "Forecast Accuracy Dashboard",
                "ForecastAccuracy",
                "Planning Leadership",
                "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                ["ExecutiveDashboard", "PbirReport"],
                [("Domain", "Forecasting"), ("Measure", "Forecast Accuracy"), ("Dimension", "Scenario")],
                [],
                "High"));
        var revenueRecommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "executive-sales-reporting",
                    "Executive Sales Reporting",
                    "ExecutiveDashboard",
                    "High",
                    "High",
                    "Medium",
                    "Strong revenue semantic coverage.",
                    "Executive",
                    "Track revenue trends and leadership-level performance over time.",
                    ["Revenue semantic support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because a concise executive KPI experience spans several semantic signals and design choices.",
                    "Primary",
                    91.2)
            ],
            alternates: []);
        var forecastRecommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "forecast-accuracy-dashboard",
                    "Forecast Accuracy Dashboard",
                    "ExecutiveDashboard",
                    "High",
                    "High",
                    "Medium",
                    "Strong forecasting semantic coverage.",
                    "Planning Leadership",
                    "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                    ["Forecasting semantic support"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because the experience spans planning review and variance management without requiring full workflow orchestration.",
                    "Primary",
                    88.4)
            ],
            alternates: []);

        var revenueBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(revenueProfile, revenueCatalog, revenueRecommendations), "PrimaryRecommendations").Single());
        var forecastBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(forecastProfile, forecastCatalog, forecastRecommendations), "PrimaryRecommendations").Single());

        Assert.NotEqual(ReadPageNames(revenueBlueprint), ReadPageNames(forecastBlueprint));
        Assert.NotEqual(ReadString(ReadObject(revenueBlueprint, "AnalyticalFlow"), "Question"), ReadString(ReadObject(forecastBlueprint, "AnalyticalFlow"), "Question"));
        Assert.NotEqual(
            string.Join("|", ReadStringList(revenueBlueprint, "SuggestedGlobalFilters")),
            string.Join("|", ReadStringList(forecastBlueprint, "SuggestedGlobalFilters")));
    }

    [Fact(DisplayName = "Experience Blueprint generation differentiates PBIR report recommendations from Fabric App and Executive Dashboard")]
    public void BuildRecommendationBlueprints_PbirReport_DiffersMateriallyFromOtherExperienceTypes()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "Revenue Variance", "YoY Growth"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Customer Segment", "Customer"), ("Product Category", "Product")],
            hierarchies: [("Geography", ["Region", "Territory"])],
            relationships: [("Sales", "Customer"), ("Sales", "Product"), ("Sales", "Date")],
            audienceSignals: [("Executive", "Medium"), ("Analytical", "High")],
            domainSignals: [("Revenue", "High"), ("Profitability", "High")]);
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "profitability-story-report",
                "Profitability Story Report",
                "ProfitabilityAnalysis",
                "Finance Leadership",
                "Explain margin movement through a guided report narrative with staged drill paths and decision checkpoints.",
                ["PbirReport", "ExecutiveDashboard", "FabricApp"],
                [("Domain", "Profitability"), ("Measure", "Revenue Variance"), ("Dimension", "Customer Segment"), ("Dimension", "Product Category")],
                [],
                "High"));
        var pbirRecommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "profitability-story-report",
                    "Profitability Story Report",
                    "PbirReport",
                    "High",
                    "High",
                    "Medium",
                    "Guided narrative fit with strong variance support.",
                    "Finance Leadership",
                    "Explain margin movement through a guided report narrative with staged drill paths and decision checkpoints.",
                    ["Variance support", "Narrative review rhythm"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because a report-oriented experience still needs careful narrative shaping.",
                    "Primary",
                    82.4)
            ],
            alternates: []);
        var fabricRecommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "profitability-story-report",
                    "Profitability Story Report",
                    "FabricApp",
                    "High",
                    "High",
                    "High",
                    "Workflow orchestration fit.",
                    "Finance Leadership",
                    "Explain margin movement through a guided report narrative with staged drill paths and decision checkpoints.",
                    ["Variance support", "Workflow routing"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "High complexity because a multi-path app experience requires stronger orchestration.",
                    "Primary",
                    80.1)
            ],
            alternates: []);
        var executiveRecommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    "profitability-story-report",
                    "Profitability Story Report",
                    "ExecutiveDashboard",
                    "High",
                    "High",
                    "Medium",
                    "Executive KPI fit.",
                    "Finance Leadership",
                    "Explain margin movement through a guided report narrative with staged drill paths and decision checkpoints.",
                    ["Variance support", "Executive KPI review"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because a concise executive KPI experience spans several semantic signals and design choices.",
                    "Primary",
                    79.3)
            ],
            alternates: []);

        var pbirBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(profile, catalog, pbirRecommendations), "PrimaryRecommendations").Single());
        var fabricBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(profile, catalog, fabricRecommendations), "PrimaryRecommendations").Single());
        var executiveBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(profile, catalog, executiveRecommendations), "PrimaryRecommendations").Single());

        Assert.NotEqual(ReadPageNames(pbirBlueprint), ReadPageNames(fabricBlueprint));
        Assert.NotEqual(ReadPageNames(pbirBlueprint), ReadPageNames(executiveBlueprint));
        Assert.NotEqual(ReadString(ReadObject(pbirBlueprint, "NavigationIntent"), "Flow"), ReadString(ReadObject(fabricBlueprint, "NavigationIntent"), "Flow"));
        Assert.NotEqual(ReadString(ReadObject(pbirBlueprint, "NavigationIntent"), "Flow"), ReadString(ReadObject(executiveBlueprint, "NavigationIntent"), "Flow"));
        Assert.DoesNotContain("Executive Summary", ReadPageNames(pbirBlueprint));
        Assert.DoesNotContain("Analysis", ReadPageNames(pbirBlueprint));
        Assert.DoesNotContain("Detail", ReadPageNames(pbirBlueprint));
    }

    [Fact(DisplayName = "Experience Blueprint generation creates materially different PBIR report patterns across discovery domains")]
    public void BuildRecommendationBlueprints_PbirReport_DiffersAcrossDomains()
    {
        var revenueProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "YoY Growth"],
            dimensions: [("Date", "Date"), ("Region", "Geography"), ("Territory", "Geography"), ("Customer Segment", "Customer")],
            audienceSignals: [("Executive", "High")],
            domainSignals: [("Revenue", "High")]);
        var profitabilityProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue", "Gross Margin", "Margin Variance"],
            dimensions: [("Date", "Date"), ("Customer Segment", "Customer"), ("Product Category", "Product"), ("Region", "Geography")],
            audienceSignals: [("Executive", "Medium"), ("Analytical", "High")],
            domainSignals: [("Profitability", "High"), ("Customer", "High")]);
        var inventoryProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Open Exceptions", "Stockout Risk", "Backlog Trend"],
            dimensions: [("Date", "Date"), ("Warehouse", "Inventory"), ("Product Category", "Product"), ("Region", "Geography")],
            audienceSignals: [("Operational", "High")],
            domainSignals: [("Inventory", "High")]);
        var serviceProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Open Work Orders", "Resolution Time", "SLA Breach Risk"],
            dimensions: [("Date", "Date"), ("Technician", "Service"), ("Work Order", "Service"), ("Region", "Geography")],
            audienceSignals: [("Operational", "High")],
            domainSignals: [("Service", "High")]);
        var forecastProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Forecast Accuracy", "Actuals", "Variance"],
            dimensions: [("Date", "Date"), ("Forecast Period", "Date"), ("Region", "Geography"), ("Territory", "Geography")],
            audienceSignals: [("Executive", "High"), ("Analytical", "Medium")],
            domainSignals: [("Forecasting", "High")]);
        var investigationProfile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Revenue Variance", "Gross Margin", "Driver Score"],
            dimensions: [("Date", "Date"), ("Customer Segment", "Customer"), ("Product Category", "Product"), ("Region", "Geography")],
            hierarchies: [("Geography", ["Region", "Territory"])],
            relationships: [("Sales", "Customer"), ("Sales", "Product"), ("Sales", "Date")],
            audienceSignals: [("Analytical", "High")],
            domainSignals: [("Profitability", "High")]);

        var revenueBlueprint = BuildPbirBlueprint(
            revenueProfile,
            CreateOpportunityCandidate(
                "revenue-report",
                "Revenue Story Report",
                "ExecutiveReporting",
                "Executive",
                "Guide the revenue story for weekly leadership review.",
                ["PbirReport"],
                [("Domain", "Revenue"), ("DateIntelligence", "High"), ("Dimension", "Territory")],
                [],
                "High"));
        var profitabilityBlueprint = BuildPbirBlueprint(
            profitabilityProfile,
            CreateOpportunityCandidate(
                "profitability-report",
                "Profitability Story Report",
                "ProfitabilityAnalysis",
                "Finance Leadership",
                "Explain margin movement through a guided report narrative.",
                ["PbirReport"],
                [("Domain", "Profitability"), ("Dimension", "Customer Segment"), ("Measure", "Variance")],
                [],
                "High"));
        var inventoryBlueprint = BuildPbirBlueprint(
            inventoryProfile,
            CreateOpportunityCandidate(
                "inventory-report",
                "Inventory Control Brief",
                "InventoryOptimization",
                "Operational",
                "Explain stock pressure and recovery priorities in a structured control brief.",
                ["PbirReport"],
                [("Domain", "Inventory"), ("Dimension", "Warehouse"), ("Measure", "Open Exceptions")],
                [],
                "High"));
        var serviceBlueprint = BuildPbirBlueprint(
            serviceProfile,
            CreateOpportunityCandidate(
                "service-report",
                "Service Narrative Brief",
                "ServiceOperations",
                "Operations Leadership",
                "Explain service backlog pressure and technician follow-up priorities in a structured brief.",
                ["PbirReport"],
                [("Domain", "Service"), ("Dimension", "Technician"), ("Measure", "Open Work Orders")],
                [],
                "High"));
        var forecastBlueprint = BuildPbirBlueprint(
            forecastProfile,
            CreateOpportunityCandidate(
                "forecast-report",
                "Forecast Story Report",
                "ForecastAccuracy",
                "Executive",
                "Explain forecast misses and course-correction priorities before the next cycle.",
                ["PbirReport"],
                [("Domain", "Forecasting"), ("DateIntelligence", "High"), ("Measure", "Variance")],
                [],
                "High"));
        var investigationBlueprint = BuildPbirBlueprint(
            investigationProfile,
            CreateOpportunityCandidate(
                "investigation-report",
                "Investigation Story Report",
                "RootCauseInvestigation",
                "Analytical",
                "Walk through the strongest hypotheses and evidence before a decision is made.",
                ["PbirReport"],
                [("Domain", "Profitability"), ("Measure", "Variance"), ("Drill", "HierarchyRich")],
                [],
                "High"));

        Assert.NotEqual(ReadPageNames(revenueBlueprint), ReadPageNames(profitabilityBlueprint));
        Assert.NotEqual(ReadPageNames(revenueBlueprint), ReadPageNames(inventoryBlueprint));
        Assert.NotEqual(ReadPageNames(inventoryBlueprint), ReadPageNames(serviceBlueprint));
        Assert.NotEqual(ReadPageNames(serviceBlueprint), ReadPageNames(forecastBlueprint));
        Assert.NotEqual(ReadPageNames(forecastBlueprint), ReadPageNames(investigationBlueprint));
        Assert.Contains("Inventory", string.Join(" ", ReadPageNames(inventoryBlueprint)), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Service", string.Join(" ", ReadPageNames(serviceBlueprint)), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Forecast", string.Join(" ", ReadPageNames(forecastBlueprint)), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Question", string.Join(" ", ReadPageNames(investigationBlueprint)), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Experience Blueprint generation separates executive planning follow-through and investigation forecast narratives")]
    public void BuildRecommendationBlueprints_ForecastNarratives_DivergeAcrossStoryTypes()
    {
        var profile = CreateDiscoveryProfile(
            confidence: "High",
            dateReadiness: "High",
            measures: ["Forecast Accuracy", "Forecast Variance", "Plan Attainment", "Actual Revenue"],
            dimensions:
            [
                ("Date", "Date"),
                ("Forecast Period", "Date"),
                ("Region", "Geography"),
                ("Territory", "Geography"),
                ("Scenario", "Planning"),
                ("Customer Segment", "Customer"),
                ("Product Category", "Product")
            ],
            hierarchies: [("Geography", ["Region", "Territory"])],
            relationships: [("Forecast", "Date"), ("Forecast", "Region"), ("Forecast", "Scenario"), ("Forecast", "Customer")],
            audienceSignals:
            [
                ("Executive", "High"),
                ("Operational", "High"),
                ("Analytical", "High")
            ],
            domainSignals:
            [
                ("Forecasting", "High"),
                ("Revenue", "Medium")
            ]);

        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate("executive-forecast-review", "Executive Forecast Review", "ForecastAccuracy", "Executive", "Review forecast confidence and executive forecast posture before the next leadership checkpoint.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Forecasting"), ("Measure", "Forecast Accuracy"), ("DateIntelligence", "High")], [], "High"),
            CreateOpportunityCandidate("forecast-planning-review", "Forecast Planning Review", "ForecastAccuracy", "Planning Leadership", "Review forecast posture, re-plan assumptions, and improve the next planning cycle.", ["ExecutiveDashboard", "PbirReport"], [("Domain", "Forecasting"), ("Measure", "Forecast Variance"), ("Dimension", "Scenario"), ("DateIntelligence", "High")], [], "High"),
            CreateOpportunityCandidate("forecast-follow-through", "Forecast Follow-Through", "ForecastAccuracy", "Operations Leadership", "Monitor forecast miss thresholds and route follow-through actions across regions.", ["OperationalMonitoringExperience", "FabricApp", "PbirReport"], [("Domain", "Forecasting"), ("Measure", "Forecast Variance"), ("Dimension", "Region")], [], "High"),
            CreateOpportunityCandidate("forecast-investigation", "Forecast Investigation", "RootCauseInvestigation", "Analytical", "Investigate why forecast misses cluster by segment and product before the next cycle.", ["AnalyticalInvestigationExperience", "PbirReport"], [("Domain", "Forecasting"), ("Measure", "Variance"), ("Drill", "HierarchyRich"), ("Audience", "Analytical")], [], "High"));

        var executiveBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(
            profile,
            catalog,
            CreateRecommendationSet(
                [
                    CreateRecommendation("executive-forecast-review", "Executive Forecast Review", "ExecutiveDashboard", "High", "High", "Medium", "Executive forecast posture fit.", "Executive", "Review forecast confidence and executive forecast posture before the next leadership checkpoint.", ["Forecasting support"], [], "High confidence because the semantic model strongly supports this use case.", "Medium complexity because the experience spans planning review and variance management.", "Primary", 88.0)
                ],
                [])), "PrimaryRecommendations").Single());
        var planningBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(
            profile,
            catalog,
            CreateRecommendationSet(
                [
                    CreateRecommendation("forecast-planning-review", "Forecast Planning Review", "ExecutiveDashboard", "High", "High", "Medium", "Planning review fit.", "Planning Leadership", "Review forecast posture, re-plan assumptions, and improve the next planning cycle.", ["Forecasting support"], [], "High confidence because the semantic model strongly supports this use case.", "Medium complexity because the experience spans planning review and variance management.", "Primary", 87.8)
                ],
                [])), "PrimaryRecommendations").Single());
        var followThroughBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(
            profile,
            catalog,
            CreateRecommendationSet(
                [
                    CreateRecommendation("forecast-follow-through", "Forecast Follow-Through", "OperationalMonitoringExperience", "High", "High", "Medium", "Operational follow-through fit.", "Operations Leadership", "Monitor forecast miss thresholds and route follow-through actions across regions.", ["Forecasting support"], [], "High confidence because the semantic model strongly supports this use case.", "Medium complexity because the experience spans threshold monitoring and owner follow-through.", "Primary", 87.5)
                ],
                [])), "PrimaryRecommendations").Single());
        var investigationBlueprint = ReadBlueprint(ReadObjectList(BuildRecommendationBlueprints(
            profile,
            catalog,
            CreateRecommendationSet(
                [
                    CreateRecommendation("forecast-investigation", "Forecast Investigation", "AnalyticalInvestigationExperience", "High", "High", "High", "Investigation fit.", "Analytical", "Investigate why forecast misses cluster by segment and product before the next cycle.", ["Forecasting support"], [], "High confidence because the semantic model strongly supports this use case.", "High complexity because an investigation path needs broader drill coordination.", "Primary", 86.9)
                ],
                [])), "PrimaryRecommendations").Single());

        Assert.NotEqual(ReadPageNames(executiveBlueprint), ReadPageNames(planningBlueprint));
        Assert.NotEqual(ReadPageNames(planningBlueprint), ReadPageNames(followThroughBlueprint));
        Assert.NotEqual(ReadPageNames(followThroughBlueprint), ReadPageNames(investigationBlueprint));
        Assert.Contains("Executive", string.Join(" ", ReadPageNames(executiveBlueprint)), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Planning", string.Join(" ", ReadPageNames(planningBlueprint)), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Overview", ReadPageNames(followThroughBlueprint));
        Assert.Contains("Question", ReadPageNames(investigationBlueprint));
    }

    private static object BuildRecommendationBlueprints(object profile, object catalog, object recommendations)
    {
        var serviceType = CoreAssembly.GetType($"{DiscoveryServicesNamespace}.ExperienceBlueprintGenerationService", throwOnError: false);
        Assert.NotNull(serviceType);

        var service = Activator.CreateInstance(
            serviceType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null);
        Assert.NotNull(service);

        var buildMethod = serviceType!.GetMethod("BuildRecommendationBlueprints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(buildMethod);

        var result = buildMethod!.Invoke(service, [profile, catalog, recommendations]);
        Assert.NotNull(result);
        return result!;
    }

    private static object BuildPbirBlueprint(object profile, object opportunity)
    {
        var opportunityId = ReadString(opportunity, "OpportunityId");
        var opportunityName = ReadString(opportunity, "Name");
        var audience = ReadString(opportunity, "InferredAudience");
        var businessOutcome = ReadString(opportunity, "BusinessOutcome");

        var recommendations = CreateRecommendationSet(
            primary:
            [
                CreateRecommendation(
                    opportunityId,
                    opportunityName,
                    "PbirReport",
                    "High",
                    "High",
                    "Medium",
                    "Guided report narrative is the strongest fit.",
                    audience,
                    businessOutcome,
                    ["Narrative review fit"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because a report-oriented experience still needs careful narrative shaping.",
                    "Primary",
                    80.0)
            ],
            alternates: []);

        var catalog = CreateOpportunityCatalog(opportunity);
        var enriched = BuildRecommendationBlueprints(profile, catalog, recommendations);
        return ReadBlueprint(ReadObjectList(enriched, "PrimaryRecommendations").Single());
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
        IReadOnlyList<(string ClusterName, string Confidence, IReadOnlyList<string> Measures)>? kpiClusters = null,
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
        var kpiClusterType = GetType("DiscoveryKpiCluster");
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
            CreateTypedList(typeof(string), ["Date"]),
            CreateTypedList(typeof(string), ["Date"]),
            ParseEnum(GetType("DiscoveryDateIntelligenceReadiness"), dateReadiness));
        var audienceList = CreateTypedList(
            audienceSignalType,
            (audienceSignals ?? [])
                .Select(signal => CreateInstance(
                    audienceSignalType,
                    signal.Audience,
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), signal.Confidence),
                    CreateTypedList(typeof(string), [signal.Audience])))
                .ToArray());
        var domainList = CreateTypedList(
            domainSignalType,
            (domainSignals ?? [])
                .Select(signal => CreateInstance(
                    domainSignalType,
                    signal.Domain,
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), signal.Confidence),
                    CreateTypedList(typeof(string), [signal.Domain])))
                .ToArray());
        var clusterList = CreateTypedList(
            kpiClusterType,
            (kpiClusters ?? [])
                .Select(cluster => CreateInstance(
                    kpiClusterType,
                    cluster.ClusterName,
                    CreateTypedList(typeof(string), cluster.Measures.Cast<object>().ToArray()),
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), cluster.Confidence)))
                .ToArray());

        return CreateInstance(
            profileType,
            measureList,
            dimensionList,
            hierarchyList,
            dateIntelligence,
            relationshipList,
            domainList,
            clusterList,
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

    private static object CreateRecommendationSet(IReadOnlyList<object> primary, IReadOnlyList<object> alternates)
    {
        return CreateInstance(
            GetType("RecommendationSet"),
            CreateTypedList(GetType("DiscoveryRecommendation"), primary.ToArray()),
            CreateTypedList(GetType("DiscoveryRecommendation"), alternates.ToArray()));
    }

    private static object CreateRecommendation(
        string recommendationId,
        string recommendationName,
        string recommendedExperienceType,
        string confidence,
        string businessValue,
        string implementationComplexity,
        string whyWeRecommendIt,
        string expectedAudience,
        string expectedBusinessOutcome,
        IReadOnlyList<string> supportingSignals,
        IReadOnlyList<string> limitingFactors,
        string confidenceNote,
        string complexityNote,
        string placement,
        double rankingScore)
    {
        return CreateInstance(
            GetType("DiscoveryRecommendation"),
            recommendationId,
            recommendationName,
            ParseEnum(GetType("OpportunityExperienceType"), recommendedExperienceType),
            ParseEnum(GetType("DiscoveryConfidenceLevel"), confidence),
            ParseEnum(GetType("RecommendationBusinessValueLevel"), businessValue),
            ParseEnum(GetType("RecommendationComplexityLevel"), implementationComplexity),
            whyWeRecommendIt,
            expectedAudience,
            expectedBusinessOutcome,
            CreateTypedList(typeof(string), supportingSignals.Cast<object>().ToArray()),
            CreateTypedList(typeof(string), limitingFactors.Cast<object>().ToArray()),
            confidenceNote,
            complexityNote,
            ParseEnum(GetType("RecommendationPlacement"), placement),
            rankingScore,
            null!);
    }

    private static object ReadBlueprint(object recommendation)
    {
        return ReadObject(recommendation, "ExperienceBlueprint");
    }

    private static List<string> ReadPageNames(object blueprint)
    {
        return ReadObjectList(blueprint, "RecommendedPages")
            .Select(page => ReadString(page, "PageName"))
            .ToList();
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

    private static object ReadObject(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was null.");
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
    }
}
