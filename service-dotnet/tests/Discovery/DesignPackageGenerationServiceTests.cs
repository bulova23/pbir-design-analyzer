using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class DesignPackageGenerationServiceTests
{
    private static readonly Assembly CoreAssembly = typeof(PbirScoringService).Assembly;
    private const string DiscoveryModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";
    private const string DiscoveryServicesNamespace = "PowerBIModelingService.Services.Discovery";

    [Fact(DisplayName = "Design Package generation creates a complete provider-neutral package")]
    public void CreatePackage_GeneratesCompletePackage()
    {
        var package = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "executive-sales-reporting",
                    "Executive Sales Reporting",
                    "ExecutiveReporting",
                    "Executive",
                    "Track revenue trends and leadership-level performance over time.",
                    ["ExecutiveDashboard", "PbirReport"],
                    [("Domain", "Revenue"), ("Dimension", "Region")],
                    ["Forecast detail is limited."],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "executive-sales-reporting",
                        "Executive Sales Reporting",
                        "ExecutiveDashboard",
                        "High",
                        "High",
                        "Medium",
                        "Strong revenue semantic coverage and clear executive audience fit.",
                        "Executive",
                        "Track revenue trends and leadership-level performance over time.",
                        ["Revenue domain", "Regional coverage"],
                        ["Forecast detail is limited."],
                        "High confidence because the semantic model strongly supports this use case.",
                        "Medium complexity because a concise executive KPI experience spans several semantic signals and design choices.",
                        "Primary",
                        91.2,
                        CreateBlueprint(
                            "executive-sales-reporting",
                            "executive-sales-reporting",
                            "ExecutiveReporting",
                            "ExecutiveDashboard",
                            "Executive",
                            "Track revenue trends and leadership-level performance over time.",
                            ["Executive Summary", "Regional Performance", "Territory Detail"],
                            ["Revenue", "Gross Margin", "YoY Growth"],
                            ["Date", "Region", "Territory"]))]
                ,
                alternates: []),
            "executive-sales-reporting");

        var discoveryContext = ReadObject(package, "DiscoveryContext");
        var audience = ReadObject(package, "Audience");
        var experienceDefinition = ReadObject(package, "ExperienceDefinition");
        var pages = ReadObjectList(package, "Pages");
        var kpis = ReadObjectList(package, "Kpis");
        var filters = ReadObject(package, "Filters");
        var navigation = ReadObject(package, "Navigation");
        var analyticalFlow = ReadObject(package, "AnalyticalFlow");
        var successCriteria = ReadObject(package, "SuccessCriteria");
        var rationale = ReadObject(package, "RecommendationRationale");

        Assert.Equal("Executive", ReadString(audience, "PrimaryAudience"));
        Assert.NotEmpty(ReadStringList(audience, "SecondaryAudiences"));
        Assert.NotEmpty(ReadObjectList(audience, "Personas"));
        Assert.Equal("ExecutiveDashboard", ReadString(experienceDefinition, "ExperienceType"));
        Assert.Equal("High", ReadString(experienceDefinition, "Confidence"));
        Assert.Equal("High", ReadString(experienceDefinition, "BusinessValue"));
        Assert.Equal("Medium", ReadString(experienceDefinition, "Complexity"));
        Assert.Equal("Track revenue trends and leadership-level performance over time.", ReadString(experienceDefinition, "BusinessOutcome"));
        Assert.NotEmpty(pages);
        Assert.All(pages, page =>
        {
            Assert.False(string.IsNullOrWhiteSpace(ReadString(page, "PageName")));
            Assert.False(string.IsNullOrWhiteSpace(ReadString(page, "PagePurpose")));
            Assert.False(string.IsNullOrWhiteSpace(ReadString(page, "NavigationIntent")));
        });
        Assert.NotEmpty(kpis);
        Assert.All(kpis, kpi =>
        {
            Assert.False(string.IsNullOrWhiteSpace(ReadString(kpi, "Name")));
            Assert.False(string.IsNullOrWhiteSpace(ReadString(kpi, "Purpose")));
            Assert.False(string.IsNullOrWhiteSpace(ReadString(kpi, "Grouping")));
        });
        Assert.NotEmpty(ReadStringList(filters, "GlobalFilters"));
        Assert.NotEmpty(ReadObjectList(filters, "PageFilters"));
        Assert.NotEmpty(ReadObjectList(package, "VisualRecommendations"));
        Assert.NotEmpty(ReadStringList(navigation, "Hierarchy"));
        Assert.NotEmpty(ReadStringList(navigation, "WorkflowPath"));
        Assert.Contains("What changed?", ReadString(analyticalFlow, "Question"), StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(ReadStringList(successCriteria, "BusinessSuccessCriteria"));
        Assert.NotEmpty(ReadStringList(successCriteria, "AnalyticalSuccessCriteria"));
        Assert.Contains("Strong revenue semantic coverage", ReadString(rationale, "RecommendationExplanation"), StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(ReadStringList(rationale, "SupportingSemanticSignals"));
        Assert.NotEmpty(ReadStringList(rationale, "LimitingFactors"));
        Assert.Equal("semanticModel", ReadString(ReadObject(discoveryContext, "SemanticModelSource"), "Stage"));
        Assert.Equal("discoveryProfile", ReadString(ReadObject(discoveryContext, "DiscoveryProfileReference"), "Stage"));
        Assert.Equal("opportunity", ReadString(ReadObject(discoveryContext, "OpportunityReference"), "Stage"));
        Assert.Equal("recommendation", ReadString(ReadObject(discoveryContext, "RecommendationReference"), "Stage"));
        Assert.Equal("experienceBlueprint", ReadString(ReadObject(discoveryContext, "ExperienceBlueprintReference"), "Stage"));
    }

    [Fact(DisplayName = "Design Package generation preserves full lineage from semantic model through package")]
    public void CreatePackage_PreservesFullLineage()
    {
        var package = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "regional-performance-investigation",
                    "Regional Performance Investigation",
                    "ComparativePerformanceManagement",
                    "Analytical",
                    "Investigate regional outliers and drivers.",
                    ["AnalyticalInvestigationExperience", "PbirReport"],
                    [("Dimension", "Region")],
                    [],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "regional-performance-investigation",
                        "Regional Performance Investigation",
                        "AnalyticalInvestigationExperience",
                        "High",
                        "Medium",
                        "High",
                        "Strong region and variance semantic coverage.",
                        "Analytical",
                        "Investigate regional outliers and drivers.",
                        ["Region analysis support"],
                        [],
                        "High confidence because the semantic model strongly supports this use case.",
                        "High complexity because an analytical drill-based experience needs broader semantic coordination and design shaping.",
                        "Primary",
                        84.1,
                        CreateBlueprint(
                            "regional-performance-investigation",
                            "regional-performance-investigation",
                            "ComparativePerformanceManagement",
                            "AnalyticalInvestigationExperience",
                            "Analytical",
                            "Investigate regional outliers and drivers.",
                            ["Question", "Investigation", "Evidence", "Conclusion"],
                            ["Revenue Variance", "Gross Margin"],
                            ["Date", "Region"]))]
                ,
                alternates: []),
            "regional-performance-investigation");

        var provenance = ReadObject(package, "Provenance");
        var lineage = ReadObjectList(provenance, "Lineage");

        var expectedStages = new[]
        {
            "semanticModel",
            "discoveryProfile",
            "opportunity",
            "recommendation",
            "experienceBlueprint",
            "designPackage",
        };

        Assert.Equal(expectedStages, lineage.Select(item => ReadString(item, "Stage")).ToArray());
        Assert.Equal("designPackage", ReadString(provenance, "PackageReference").Split(':')[0]);
    }

    [Fact(DisplayName = "Design Package generation includes rationale for audience pages navigation analytical flow and provenance")]
    public void CreatePackage_RationaleExplainsWhyTheExperienceWasRecommended()
    {
        var package = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "service-workflow-orchestration",
                    "Service Workflow Orchestration",
                    "ServiceOperations",
                    "Operations Leadership",
                    "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                    ["FabricApp", "OperationalMonitoringExperience"],
                    [("Domain", "Service"), ("Dimension", "Technician"), ("Dimension", "Work Order")],
                    ["Customer escalation context is still partial."],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "service-workflow-orchestration",
                        "Service Workflow Orchestration",
                        "FabricApp",
                        "High",
                        "High",
                        "High",
                        "Strong service semantic coverage and multi-role workflow fit.",
                        "Operations Leadership",
                        "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                        ["Service domain", "Technician dimension", "Work Order dimension"],
                        ["Customer escalation context is still partial."],
                        "High confidence because the semantic model strongly supports this use case.",
                        "High complexity because a multi-path app experience requires stronger orchestration.",
                        "Primary",
                        90.4,
                        CreateBlueprint(
                            "service-workflow-orchestration",
                            "service-workflow-orchestration",
                            "ServiceOperations",
                            "FabricApp",
                            "Operations Leadership",
                            "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                            ["Service Command Center", "Regional Queue Routing", "Technician Follow-Up"],
                            ["Open Work Orders", "Resolution Time", "Escalation Count"],
                            ["Date", "Region", "Technician"]))]
                ,
                alternates: []),
            "service-workflow-orchestration");

        var rationale = ReadObject(package, "RecommendationRationale");
        var experienceDefinition = ReadObject(package, "ExperienceDefinition");

        Assert.Contains("Operations Leadership", ReadString(rationale, "AudienceRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("service workflow", ReadString(rationale, "BusinessOutcomeRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(ReadStringList(rationale, "KpiRationale"));
        Assert.NotEmpty(ReadStringList(rationale, "PageRationale"));
        Assert.Contains("workflow", ReadString(rationale, "NavigationRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("question", ReadString(rationale, "AnalyticalFlowRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(ReadStringList(rationale, "ProvenanceNotes"));
        Assert.Equal("FabricApp", ReadString(experienceDefinition, "ExperienceType"));
    }

    [Fact(DisplayName = "Design Package generation produces provider-grade rationale tied to the selected experience logic")]
    public void CreatePackage_RationaleIsProviderGradeAndDecisionDefensible()
    {
        var package = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "service-workflow-orchestration",
                    "Service Workflow Orchestration",
                    "ServiceOperations",
                    "Operations Leadership",
                    "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                    ["FabricApp", "OperationalMonitoringExperience", "PbirReport"],
                    [("Domain", "Service"), ("Dimension", "Technician"), ("Dimension", "Work Order")],
                    ["Customer escalation context is still partial."],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "service-workflow-orchestration",
                        "Service Workflow Orchestration",
                        "FabricApp",
                        "High",
                        "High",
                        "High",
                        "Why This Wins: Fabric App fits the service workflow because regional routing and technician follow-up must stay coordinated. Why Alternatives Lose: Operational Monitoring exposes queues but does not organize handoffs, and PBIR Report slows daily action. Business Tradeoffs: stronger workflow control at the cost of a heavier implementation path. Audience Tradeoffs: operations leadership gets coordination depth while executives receive a secondary summary path. Operational Tradeoffs: queue routing wins over passive monitoring because technicians and work orders drive next actions. Analytical Tradeoffs: the app supports targeted drill paths without turning the whole experience into analyst-first investigation.",
                        "Operations Leadership",
                        "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                        ["Service domain", "Technician dimension", "Work Order dimension"],
                        ["Customer escalation context is still partial."],
                        "High confidence because the semantic model strongly supports this use case.",
                        "High complexity because a multi-path app experience requires stronger orchestration.",
                        "Primary",
                        90.4,
                        CreateBlueprint(
                            "service-workflow-orchestration",
                            "service-workflow-orchestration",
                            "ServiceOperations",
                            "FabricApp",
                            "Operations Leadership",
                            "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                            ["Service Command Center", "Regional Queue Routing", "Technician Follow-Up"],
                            ["Open Work Orders", "Resolution Time", "Escalation Count"],
                            ["Date", "Region", "Technician"]))]
                ,
                alternates: []),
            "service-workflow-orchestration");

        var rationale = ReadObject(package, "RecommendationRationale");

        Assert.Contains("because", ReadString(rationale, "AudienceRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Operations Leadership", ReadString(rationale, "AudienceRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workflow", ReadString(rationale, "BusinessOutcomeRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("because", ReadString(rationale, "NavigationRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("route", ReadString(rationale, "NavigationRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("decision", ReadString(rationale, "AnalyticalFlowRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ReadStringList(rationale, "PageRationale"), item =>
            item.Contains("Service Command Center", StringComparison.OrdinalIgnoreCase) &&
            item.Contains("because", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ReadStringList(rationale, "KpiRationale"), item =>
            item.Contains("Open Work Orders", StringComparison.OrdinalIgnoreCase) &&
            item.Contains("because", StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "Design Package generation includes provider-neutral handoff guidance that explains intent and success")]
    public void CreatePackage_IncludesProviderGuidanceWithoutProviderSpecificExecution()
    {
        var package = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "forecast-accuracy-dashboard",
                    "Forecast Accuracy Dashboard",
                    "ForecastAccuracy",
                    "Planning Leadership",
                    "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                    ["ExecutiveDashboard", "PbirReport", "AnalyticalInvestigationExperience"],
                    [("Domain", "Forecasting"), ("Measure", "Forecast Accuracy"), ("Measure", "Forecast Variance"), ("Dimension", "Scenario")],
                    ["Scenario granularity is still uneven by region."],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "forecast-accuracy-dashboard",
                        "Forecast Accuracy Dashboard",
                        "ExecutiveDashboard",
                        "High",
                        "High",
                        "Medium",
                        "Why This Wins: Executive Dashboard fits the planning rhythm because forecast accuracy and variance management need a planning-first leadership readout before deeper diagnosis. Why Alternatives Lose: Analytical Investigation adds more drill depth than the weekly planning cycle should carry, and PBIR Report slows fast variance review. Business Tradeoffs: strong planning visibility at the cost of less open-ended analysis. Audience Tradeoffs: planning leadership gets a focused variance readout while analysts remain a secondary path. Operational Tradeoffs: owners can follow variance without turning the primary experience into a workflow shell. Analytical Tradeoffs: the experience keeps diagnostic evidence visible without becoming investigation-first.",
                        "Planning Leadership",
                        "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                        ["Forecasting domain", "Scenario planning support", "Forecast variance support"],
                        ["Scenario granularity is still uneven by region."],
                        "High confidence because the semantic model strongly supports this use case.",
                        "Medium complexity because the experience spans planning review and variance management without requiring full workflow orchestration.",
                        "Primary",
                        88.4,
                        CreateBlueprint(
                            "forecast-accuracy-dashboard",
                            "forecast-accuracy-dashboard",
                            "ForecastAccuracy",
                            "ExecutiveDashboard",
                            "Planning Leadership",
                            "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                            ["Planning Summary", "Variance Review", "Regional Follow-Up"],
                            ["Forecast Accuracy", "Forecast Variance", "Plan Attainment"],
                            ["Date", "Region", "Scenario"]))]
                ,
                alternates: []),
            "forecast-accuracy-dashboard");

        var providerGuidance = ReadObject(package, "ProviderGuidance");

        Assert.Contains("why", ReadString(providerGuidance, "WhyThisPackageExists"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("forecast", ReadString(providerGuidance, "WhyThisPackageExists"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Executive Dashboard", ReadString(providerGuidance, "ExperienceToGenerate"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Planning Leadership", ReadString(providerGuidance, "ExperienceToGenerate"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("success", ReadString(providerGuidance, "SuccessLooksLike"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CLI", ReadString(providerGuidance, "ExperienceToGenerate"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("provider payload", ReadString(providerGuidance, "ExperienceToGenerate"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Design Package generation is deterministic for the same blueprint input")]
    public void CreatePackage_SameInput_IsDeterministic()
    {
        var profile = CreateDiscoveryProfile();
        var catalog = CreateOpportunityCatalog(
            CreateOpportunityCandidate(
                "executive-sales-reporting",
                "Executive Sales Reporting",
                "ExecutiveReporting",
                "Executive",
                "Track revenue trends and leadership-level performance over time.",
                ["ExecutiveDashboard", "PbirReport"],
                [("Domain", "Revenue")],
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
                    ["Revenue domain"],
                    [],
                    "High confidence because the semantic model strongly supports this use case.",
                    "Medium complexity because a concise executive KPI experience spans several semantic signals and design choices.",
                    "Primary",
                    91.2,
                    CreateBlueprint(
                        "executive-sales-reporting",
                        "executive-sales-reporting",
                        "ExecutiveReporting",
                        "ExecutiveDashboard",
                        "Executive",
                        "Track revenue trends and leadership-level performance over time.",
                        ["Executive Summary", "Regional Performance"],
                        ["Revenue", "Gross Margin"],
                        ["Date", "Region"]))]
            ,
            alternates: []);

        var first = CreatePackage(profile, catalog, recommendations, "executive-sales-reporting");
        var second = CreatePackage(profile, catalog, recommendations, "executive-sales-reporting");

        Assert.Equal(BuildDeterministicSummary(first), BuildDeterministicSummary(second));
    }

    [Fact(DisplayName = "Design Package generation does not add provider execution or trust-boundary fields")]
    public void CreatePackage_RemainsProviderNeutral_AndAdvisoryOnly()
    {
        var package = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "executive-sales-reporting",
                    "Executive Sales Reporting",
                    "ExecutiveReporting",
                    "Executive",
                    "Track revenue trends and leadership-level performance over time.",
                    ["ExecutiveDashboard"],
                    [("Domain", "Revenue")],
                    [],
                    "High")),
            CreateRecommendationSet(
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
                        ["Revenue domain"],
                        [],
                        "High confidence because the semantic model strongly supports this use case.",
                        "Medium complexity because a concise executive KPI experience spans several semantic signals and design choices.",
                        "Primary",
                        91.2,
                        CreateBlueprint(
                            "executive-sales-reporting",
                            "executive-sales-reporting",
                            "ExecutiveReporting",
                            "ExecutiveDashboard",
                            "Executive",
                            "Track revenue trends and leadership-level performance over time.",
                            ["Executive Summary", "Regional Performance"],
                            ["Revenue", "Gross Margin"],
                            ["Date", "Region"]))]
                ,
                alternates: []),
            "executive-sales-reporting");

        var propertyNames = package.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ProviderPayload", propertyNames);
        Assert.DoesNotContain("CliCommand", propertyNames);
        Assert.DoesNotContain("FabricAppPayload", propertyNames);
        Assert.DoesNotContain("PbirPayload", propertyNames);
        Assert.DoesNotContain("GeneratedAssets", propertyNames);
        Assert.DoesNotContain("ValidationApproval", propertyNames);
        Assert.DoesNotContain("DeploymentPlan", propertyNames);
    }

    [Fact(DisplayName = "Design Package generation gives provider-ready why-language across rationale guidance and success criteria")]
    public void CreatePackage_ProviderReadiness_ExplainsWhyAndSuccessWithoutExternalDiscoveryContext()
    {
        var package = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "forecast-accuracy-dashboard",
                    "Forecast Accuracy Dashboard",
                    "ForecastAccuracy",
                    "Planning Leadership",
                    "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                    ["ExecutiveDashboard", "PbirReport"],
                    [("Domain", "Forecasting"), ("Measure", "Forecast Accuracy"), ("Dimension", "Scenario")],
                    ["Scenario granularity is still uneven by region."],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "forecast-accuracy-dashboard",
                        "Forecast Accuracy Dashboard",
                        "ExecutiveDashboard",
                        "High",
                        "High",
                        "Medium",
                        "Planning-first leadership readout with forecast variance context.",
                        "Planning Leadership",
                        "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                        ["Forecasting semantic support", "Scenario planning support"],
                        ["Scenario granularity is still uneven by region."],
                        "High confidence because the semantic model strongly supports this use case.",
                        "Medium complexity because the experience spans planning review and variance management without requiring full workflow orchestration.",
                        "Primary",
                        88.4,
                        CreateBlueprint(
                            "forecast-accuracy-dashboard",
                            "forecast-accuracy-dashboard",
                            "ForecastAccuracy",
                            "ExecutiveDashboard",
                            "Planning Leadership",
                            "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                            ["Planning Summary", "Variance Review", "Regional Follow-Up"],
                            ["Forecast Accuracy", "Forecast Variance", "Plan Attainment"],
                            ["Date", "Region", "Scenario"]))]
                ,
                alternates: []),
            "forecast-accuracy-dashboard");

        var successCriteria = ReadObject(package, "SuccessCriteria");
        var rationale = ReadObject(package, "RecommendationRationale");
        var providerGuidance = ReadObject(package, "ProviderGuidance");

        Assert.Contains("planning", ReadString(rationale, "AudienceRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weekly", string.Join(" ", ReadStringList(successCriteria, "BusinessSuccessCriteria")), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("because", ReadString(rationale, "ExperienceTypeRationale"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("generate", ReadString(providerGuidance, "ExperienceToGenerate"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Scenario", ReadString(providerGuidance, "ExperienceToGenerate"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("success looks like", ReadString(providerGuidance, "SuccessLooksLike"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "Design Package generation preserves downstream diversity between executive and operational recommendations")]
    public void CreatePackage_DiversityPropagation_DiffersAcrossRecommendationTypes()
    {
        var executivePackage = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "forecast-accuracy-dashboard",
                    "Forecast Accuracy Dashboard",
                    "ForecastAccuracy",
                    "Planning Leadership",
                    "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                    ["ExecutiveDashboard"],
                    [("Domain", "Forecasting"), ("Dimension", "Scenario")],
                    [],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "forecast-accuracy-dashboard",
                        "Forecast Accuracy Dashboard",
                        "ExecutiveDashboard",
                        "High",
                        "High",
                        "Medium",
                        "Planning-first leadership readout with forecast variance context.",
                        "Planning Leadership",
                        "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                        ["Forecasting semantic support"],
                        [],
                        "High confidence because the semantic model strongly supports this use case.",
                        "Medium complexity because the experience spans planning review and variance management without requiring full workflow orchestration.",
                        "Primary",
                        88.4,
                        CreateBlueprint(
                            "forecast-accuracy-dashboard",
                            "forecast-accuracy-dashboard",
                            "ForecastAccuracy",
                            "ExecutiveDashboard",
                            "Planning Leadership",
                            "Review weekly forecast accuracy, manage variance, and improve the next planning cycle.",
                            ["Planning Summary", "Variance Review", "Regional Follow-Up"],
                            ["Forecast Accuracy", "Forecast Variance", "Plan Attainment"],
                            ["Date", "Region", "Scenario"]))]
                ,
                alternates: []),
            "forecast-accuracy-dashboard");
        var operationalPackage = CreatePackage(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "inventory-operations-monitoring",
                    "Inventory Operations Monitoring",
                    "InventoryOptimization",
                    "Operational",
                    "Monitor stock position, warehouse health, and item-level inventory risk.",
                    ["OperationalMonitoringExperience"],
                    [("Domain", "Inventory"), ("Dimension", "Warehouse")],
                    [],
                    "High")),
            CreateRecommendationSet(
                primary:
                [
                    CreateRecommendation(
                        "inventory-operations-monitoring",
                        "Inventory Operations Monitoring",
                        "OperationalMonitoringExperience",
                        "High",
                        "High",
                        "Medium",
                        "Action-first operational monitoring path.",
                        "Operational",
                        "Monitor stock position, warehouse health, and item-level inventory risk.",
                        ["Inventory semantic support"],
                        [],
                        "High confidence because the semantic model strongly supports this use case.",
                        "Medium complexity because an operational monitoring flow spans several semantic signals and design choices.",
                        "Primary",
                        88.5,
                        CreateBlueprint(
                            "inventory-operations-monitoring",
                            "inventory-operations-monitoring",
                            "InventoryOptimization",
                            "OperationalMonitoringExperience",
                            "Operational",
                            "Monitor stock position, warehouse health, and item-level inventory risk.",
                            ["Overview", "Exceptions", "Detail"],
                            ["Open Exceptions", "Backlog Trend", "Stockout Risk"],
                            ["Date", "Warehouse", "Region"]))]
                ,
                alternates: []),
            "inventory-operations-monitoring");

        var executiveRationale = ReadObject(executivePackage, "RecommendationRationale");
        var operationalRationale = ReadObject(operationalPackage, "RecommendationRationale");
        var executiveGuidance = ReadObject(executivePackage, "ProviderGuidance");
        var operationalGuidance = ReadObject(operationalPackage, "ProviderGuidance");

        Assert.NotEqual(ReadString(executiveRationale, "ExperienceTypeRationale"), ReadString(operationalRationale, "ExperienceTypeRationale"));
        Assert.NotEqual(ReadString(executiveRationale, "NavigationRationale"), ReadString(operationalRationale, "NavigationRationale"));
        Assert.NotEqual(ReadString(executiveGuidance, "ExperienceToGenerate"), ReadString(operationalGuidance, "ExperienceToGenerate"));
        Assert.Contains("planning", ReadString(executiveGuidance, "ExperienceToGenerate"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action", ReadString(operationalGuidance, "SuccessLooksLike"), StringComparison.OrdinalIgnoreCase);
    }

    private static object CreatePackage(object profile, object catalog, object recommendations, string recommendationId)
    {
        var serviceType = CoreAssembly.GetType($"{DiscoveryServicesNamespace}.DesignPackageGenerationService", throwOnError: false);
        Assert.NotNull(serviceType);

        var service = Activator.CreateInstance(
            serviceType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null);
        Assert.NotNull(service);

        var method = serviceType!.GetMethod("CreatePackage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(service, [profile, catalog, recommendations, recommendationId]);
        Assert.NotNull(result);
        return result!;
    }

    private static object CreateDiscoveryProfile()
    {
        var measureType = GetType("DiscoveryMeasureProfile");
        var dimensionType = GetType("DiscoveryDimensionProfile");
        var dateIntelligenceType = GetType("DiscoveryDateIntelligenceProfile");
        var domainSignalType = GetType("DiscoveryDomainSignal");
        var audienceSignalType = GetType("DiscoveryAudienceSignal");
        var kpiClusterType = GetType("DiscoveryKpiCluster");
        var profileType = GetType("DiscoveryProfile");

        return CreateInstance(
            profileType,
            CreateTypedList(
                measureType,
                CreateInstance(measureType, "Revenue", null!, null!),
                CreateInstance(measureType, "Gross Margin", null!, null!),
                CreateInstance(measureType, "YoY Growth", null!, null!)),
            CreateTypedList(
                dimensionType,
                CreateInstance(dimensionType, "Date", "Many", "Date"),
                CreateInstance(dimensionType, "Region", "Many", "Geography"),
                CreateInstance(dimensionType, "Territory", "Many", "Geography")),
            CreateTypedList(GetType("DiscoveryHierarchyProfile")),
            CreateInstance(
                dateIntelligenceType,
                CreateTypedList(typeof(string), "Date"),
                CreateTypedList(typeof(string), "Date"),
                ParseEnum(GetType("DiscoveryDateIntelligenceReadiness"), "High")),
            CreateTypedList(GetType("DiscoveryRelationshipProfile")),
            CreateTypedList(
                domainSignalType,
                CreateInstance(
                    domainSignalType,
                    "Revenue",
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), "High"),
                    CreateTypedList(typeof(string), "Revenue"))),
            CreateTypedList(
                kpiClusterType,
                CreateInstance(
                    kpiClusterType,
                    "Revenue KPIs",
                    CreateTypedList(typeof(string), "Revenue", "Gross Margin", "YoY Growth"),
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), "High"))),
            CreateTypedList(
                audienceSignalType,
                CreateInstance(
                    audienceSignalType,
                    "Executive",
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), "High"),
                    CreateTypedList(typeof(string), "Executive")),
                CreateInstance(
                    audienceSignalType,
                    "Regional Manager",
                    ParseEnum(GetType("DiscoveryConfidenceLevel"), "Medium"),
                    CreateTypedList(typeof(string), "Region"))),
            CreateTypedList(typeof(string), "Forecast detail is limited."),
            ParseEnum(GetType("DiscoveryConfidenceLevel"), "High"),
            "semantic-model:test",
            "discovery-profile:test");
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
        double rankingScore,
        object blueprint)
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
            blueprint);
    }

    private static object CreateBlueprint(
        string recommendationId,
        string opportunityId,
        string opportunityCategory,
        string experienceType,
        string audience,
        string businessOutcome,
        IReadOnlyList<string> pageNames,
        IReadOnlyList<string> kpis,
        IReadOnlyList<string> filters)
    {
        var pageType = GetType("ExperienceBlueprintPage");
        var analyticalFlowType = GetType("ExperienceBlueprintAnalyticalFlow");
        var navigationIntentType = GetType("ExperienceBlueprintNavigationIntent");
        var provenanceType = GetType("ExperienceBlueprintProvenance");

        return CreateInstance(
            GetType("ExperienceBlueprint"),
            $"blueprint:{recommendationId}",
            ParseEnum(GetType("OpportunityExperienceType"), experienceType),
            CreateTypedList(
                pageType,
                pageNames.Select((pageName, index) => CreateInstance(
                    pageType,
                    pageName,
                    $"Support {pageName.ToLowerInvariant()} decisions.",
                    CreateTypedList(typeof(string), filters.Take(Math.Max(1, Math.Min(filters.Count, 2))).Cast<object>().ToArray()),
                    CreateTypedList(typeof(string), index == 0 ? ["KpiCard", "TrendLine"] : ["BarChart", "Table"])))
                    .ToArray()),
            CreateTypedList(typeof(string), kpis.Cast<object>().ToArray()),
            CreateTypedList(typeof(string), filters.Cast<object>().ToArray()),
            CreateInstance(
                analyticalFlowType,
                "What changed?",
                "Investigate the highest-variance segments.",
                "Confirm the supporting evidence on the detail pages.",
                "Decide what action to take next."),
            CreateInstance(
                navigationIntentType,
                "Summary to regional detail and decision support flow.",
                CreateTypedList(typeof(string), pageNames.Cast<object>().ToArray())),
            audience,
            businessOutcome,
            CreateTypedList(typeof(string), "Decision can be made quickly", "Story remains aligned to KPI priorities"),
            CreateInstance(
                provenanceType,
                recommendationId,
                opportunityId,
                ParseEnum(GetType("OpportunityCategory"), opportunityCategory),
                ParseEnum(GetType("OpportunityExperienceType"), experienceType),
                ParseEnum(GetType("DiscoveryConfidenceLevel"), "High"),
                CreateTypedList(typeof(string), "Revenue domain", "Regional coverage"),
                CreateTypedList(typeof(string), "measure:Revenue", "dimension:Region"),
                CreateTypedList(typeof(string), "measure:Revenue", "dimension:Region", "hierarchy:Geography"),
                CreateTypedList(typeof(string), "Forecast detail is limited."),
                "semantic-model:test",
                "discovery-profile:test"));
    }

    private static string BuildDeterministicSummary(object package)
    {
        var audience = ReadObject(package, "Audience");
        var experienceDefinition = ReadObject(package, "ExperienceDefinition");
        var navigation = ReadObject(package, "Navigation");
        var rationale = ReadObject(package, "RecommendationRationale");
        var provenance = ReadObject(package, "Provenance");

        return string.Join(
            ";",
            [
                ReadString(package, "PackageId"),
                ReadString(audience, "PrimaryAudience"),
                string.Join("|", ReadStringList(audience, "SecondaryAudiences")),
                ReadString(experienceDefinition, "ExperienceType"),
                ReadString(experienceDefinition, "BusinessOutcome"),
                ReadString(experienceDefinition, "Confidence"),
                ReadString(experienceDefinition, "BusinessValue"),
                ReadString(experienceDefinition, "Complexity"),
                string.Join("|", ReadObjectList(package, "Pages").Select(page =>
                    $"{ReadString(page, "PageName")}:{ReadString(page, "PagePurpose")}:{ReadString(page, "NavigationIntent")}")),
                string.Join("|", ReadObjectList(package, "Kpis").Select(kpi =>
                    $"{ReadString(kpi, "Name")}:{ReadString(kpi, "Purpose")}:{ReadString(kpi, "Grouping")}")),
                string.Join("|", ReadStringList(ReadObject(package, "Filters"), "GlobalFilters")),
                string.Join("|", ReadStringList(navigation, "Hierarchy")),
                string.Join("|", ReadStringList(navigation, "WorkflowPath")),
                ReadString(rationale, "RecommendationExplanation"),
                string.Join("|", ReadObjectList(provenance, "Lineage").Select(item =>
                    $"{ReadString(item, "Stage")}:{ReadString(item, "ReferenceId")}:{ReadString(item, "Label")}"))
            ]);
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

    private static object ReadObject(object target, string propertyName)
    {
        var value = GetPropertyValue(target, propertyName);
        Assert.NotNull(value);
        return value!;
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
        var value = GetPropertyValue(target, propertyName);
        Assert.NotNull(value);
        return value!.ToString() ?? string.Empty;
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        return target.GetType()
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(target);
    }
}
