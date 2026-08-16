using System.Collections;
using System.Reflection;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class DiscoveryDesignStudioAdapterServiceTests
{
    private static readonly Assembly CoreAssembly = typeof(PbirScoringService).Assembly;
    private const string DiscoveryModelsNamespace = "PowerBIModelingService.Services.Discovery.Models";
    private const string DiscoveryServicesNamespace = "PowerBIModelingService.Services.Discovery";

    [Fact(DisplayName = "Discovery Design Studio adapter selects the requested recommendation from primary and alternate candidates")]
    public void CreateStartingPoint_SelectsRequestedRecommendation()
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
                "High"),
            CreateOpportunityCandidate(
                "regional-performance-investigation",
                "Regional Performance Investigation",
                "ComparativePerformanceManagement",
                "Analytical",
                "Investigate regional outliers and drivers.",
                ["AnalyticalInvestigationExperience", "PbirReport"],
                [("Dimension", "Region")],
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
                    ["Strong revenue semantic coverage"],
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
                        ["Date", "Region"]))
                ],
            alternates:
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
                    "Alternate",
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
                        ["Date", "Region"]))
            ]);

        var result = CreateStartingPoint(profile, catalog, recommendations, "regional-performance-investigation", "design-studio:test");

        Assert.Equal("regional-performance-investigation", ReadString(result, "SelectedRecommendationId"));
        var brief = ReadObject(result, "DesignBrief");
        Assert.Equal("Analytical", ReadString(brief, "Audience"));
    }

    [Fact(DisplayName = "Discovery Design Studio adapter creates a discovery-backed Design Brief with populated fields")]
    public void CreateStartingPoint_CreatesDesignBrief()
    {
        var result = CreateStartingPoint(
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
                        ["Strong revenue semantic coverage", "Region support"],
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
                            ["Executive Summary", "Regional Performance", "Territory Detail"],
                            ["Revenue", "Gross Margin", "YoY Growth"],
                            ["Date", "Region", "Territory"]))
                ],
                alternates: []),
            "executive-sales-reporting",
            "design-studio:test");

        var brief = ReadObject(result, "DesignBrief");
        var metadata = ReadObject(brief, "Metadata");
        var provenance = ReadObject(metadata, "Provenance");

        Assert.Equal("Executive", ReadString(brief, "Audience"));
        Assert.Equal("Track revenue trends and leadership-level performance over time.", ReadString(brief, "BusinessObjective"));
        Assert.Contains("Revenue", ReadStringList(brief, "PrimaryKpis"));
        Assert.Contains("Region", ReadStringList(brief, "Dimensions"));
        Assert.Contains("Track revenue trends", ReadString(brief, "IntendedStory"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("summary", ReadString(brief, "NavigationExpectations"), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ExecutiveDashboard", ReadString(brief, "ReportType"));
        Assert.Equal("NotSubmitted", ReadString(metadata, "ApprovalState"));
        Assert.Equal("Draft", ReadString(metadata, "LifecycleState"));
        Assert.Equal("System", ReadString(metadata, "AuthorSource"));
        Assert.Equal("discoveryWizard", ReadString(provenance, "Source"));
    }

    [Fact(DisplayName = "Discovery Design Studio adapter creates concept candidates and preserves full lineage")]
    public void CreateStartingPoint_CreatesConceptCandidates_WithLineage()
    {
        var result = CreateStartingPoint(
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
                        ["Strong revenue semantic coverage"],
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
                            ["Executive Summary", "Regional Performance", "Territory Detail"],
                            ["Revenue", "Gross Margin", "YoY Growth"],
                            ["Date", "Region", "Territory"]))
                ],
                alternates: []),
            "executive-sales-reporting",
            "design-studio:test");

        var concept = ReadObject(result, "Concept");
        var metadata = ReadObject(concept, "Metadata");
        var provenance = ReadObject(metadata, "Provenance");
        var lineage = ReadObjectList(provenance, "Lineage");

        Assert.Equal("NotSubmitted", ReadString(metadata, "ApprovalState"));
        Assert.Equal("Draft", ReadString(metadata, "LifecycleState"));
        Assert.True(ReadObjectList(concept, "AlternateConcepts").Count >= 2);
        Assert.Equal("design-brief:design-studio:test@v1", ReadString(concept, "SourceBriefVersionId"));
        Assert.True(
            new[] { "semanticModel", "discoveryProfile", "opportunity", "recommendation", "experienceBlueprint" }
                .SequenceEqual(lineage.Select(entry => ReadString(entry, "Stage"))));
    }

    [Fact(DisplayName = "Discovery Design Studio adapter creates a draft seed without bypassing approvals, validation, or deployable asset boundaries")]
    public void CreateStartingPoint_CreatesDraftSeed_WithinTrustBoundaries()
    {
        var result = CreateStartingPoint(
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
                        ["Strong revenue semantic coverage"],
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
                            ["Executive Summary", "Regional Performance", "Territory Detail"],
                            ["Revenue", "Gross Margin", "YoY Growth"],
                            ["Date", "Region", "Territory"]))
                ],
                alternates: []),
            "executive-sales-reporting",
            "design-studio:test");

        var draft = ReadObject(result, "Draft");
        var metadata = ReadObject(draft, "Metadata");
        var provenance = ReadObject(metadata, "Provenance");
        var status = ReadObject(draft, "DraftStatus");
        var draftPages = ReadObjectList(result, "DraftPages");
        var draftLayouts = ReadObjectList(result, "DraftLayouts");
        var draftNavigation = ReadObjectList(result, "DraftNavigationArtifacts");

        Assert.Equal("NotSubmitted", ReadString(metadata, "ApprovalState"));
        Assert.Equal("DesignApproval", ReadString(metadata, "ApprovalKind"));
        Assert.Equal("Draft", ReadString(metadata, "LifecycleState"));
        Assert.Null(GetPropertyValue(metadata, "ValidationLinkage"));
        Assert.Equal("NonProduction", ReadString(status, "ProductionState"));
        Assert.Equal("Reviewable", ReadString(status, "Reviewability"));
        Assert.Equal("Isolated", ReadString(status, "Isolation"));
        Assert.NotEmpty(draftPages);
        Assert.NotEmpty(draftLayouts);
        Assert.Single(draftNavigation);
        Assert.True(
            new[] { "semanticModel", "discoveryProfile", "opportunity", "recommendation", "experienceBlueprint" }
                .SequenceEqual(ReadObjectList(provenance, "Lineage").Select(entry => ReadString(entry, "Stage"))));
    }

    [Fact(DisplayName = "Discovery Design Studio adapter preserves upstream semantic model and discovery profile ids in lineage")]
    public void CreateStartingPoint_LineagePreservesUpstreamReferenceIds()
    {
        var result = CreateStartingPoint(
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
                        ["Strong revenue semantic coverage"],
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
                            ["Executive Summary", "Regional Performance", "Territory Detail"],
                            ["Revenue", "Gross Margin", "YoY Growth"],
                            ["Date", "Region", "Territory"]))
                ],
                alternates: []),
            "executive-sales-reporting",
            "design-studio:test");

        var brief = ReadObject(result, "DesignBrief");
        var lineage = ReadObjectList(ReadObject(ReadObject(brief, "Metadata"), "Provenance"), "Lineage");

        Assert.Equal("semantic-model:test", ReadString(lineage[0], "SourceId"));
        Assert.Equal("discovery-profile:test", ReadString(lineage[1], "SourceId"));
    }

    [Fact(DisplayName = "Discovery Design Studio adapter preserves richer experience-specific brief language instead of flattening to generic dashboard framing")]
    public void CreateStartingPoint_BriefPreservesExperienceSpecificity()
    {
        var executiveResult = CreateStartingPoint(
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
                        "Strong forecasting semantic coverage.",
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
                            ["Date", "Region", "Scenario"]))
                ],
                alternates: []),
            "forecast-accuracy-dashboard",
            "design-studio:executive");
        var appResult = CreateStartingPoint(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "service-workflow-orchestration",
                    "Service Workflow Orchestration",
                    "ServiceOperations",
                    "Operations Leadership",
                    "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                    ["FabricApp"],
                    [("Domain", "Service"), ("Dimension", "Technician"), ("Dimension", "Work Order")],
                    [],
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
                        "Strong service semantic coverage.",
                        "Operations Leadership",
                        "Coordinate backlog triage, technician follow-up, and regional handoffs across the service workflow.",
                        ["Service semantic support"],
                        [],
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
                            ["Date", "Region", "Technician"]))
                ],
                alternates: []),
            "service-workflow-orchestration",
            "design-studio:app");

        var executiveBrief = ReadObject(executiveResult, "DesignBrief");
        var appBrief = ReadObject(appResult, "DesignBrief");

        Assert.Contains("planning", ReadString(executiveBrief, "IntendedStory"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weekly", ReadString(executiveBrief, "DecisionCadence"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Application", ReadString(appBrief, "ReportType"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("workflow", ReadString(appBrief, "NavigationExpectations"), StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(ReadString(executiveBrief, "IntendedStory"), ReadString(appBrief, "IntendedStory"));
    }

    [Fact(DisplayName = "Discovery Design Studio adapter creates materially different concept candidates instead of only templated variants")]
    public void CreateStartingPoint_ConceptCandidates_DifferMaterially()
    {
        var result = CreateStartingPoint(
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
                        "Strong forecasting semantic coverage.",
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
                            ["Date", "Region", "Scenario"]))
                ],
                alternates: []),
            "forecast-accuracy-dashboard",
            "design-studio:concept-diversity");

        var concept = ReadObject(result, "Concept");
        var alternates = ReadObjectList(concept, "AlternateConcepts");
        var labels = alternates.Select(item => ReadString(item, "Label")).ToArray();
        var patterns = alternates.Select(item => ReadString(item, "NavigationPattern")).Distinct(StringComparer.Ordinal).ToArray();
        var summaries = alternates.Select(item => ReadString(item, "Summary")).Distinct(StringComparer.Ordinal).ToArray();

        Assert.True(alternates.Count >= 3);
        Assert.True(patterns.Length >= 3);
        Assert.Equal(alternates.Count, labels.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(alternates.Count, summaries.Length);
    }

    [Fact(DisplayName = "Discovery Design Studio adapter draft seeds differ by recommendation type")]
    public void CreateStartingPoint_DraftSeed_DiffersByRecommendationType()
    {
        var executiveResult = CreateStartingPoint(
            CreateDiscoveryProfile(),
            CreateOpportunityCatalog(
                CreateOpportunityCandidate(
                    "executive-sales-reporting",
                    "Executive Sales Reporting",
                    "ExecutiveReporting",
                    "Executive",
                    "Track revenue trends and leadership-level performance over time.",
                    ["ExecutiveDashboard"],
                    [("Domain", "Revenue"), ("Dimension", "Territory")],
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
                        ["Revenue semantic support"],
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
                            ["Executive Summary", "Regional Performance", "Territory Detail"],
                            ["Revenue", "Gross Margin", "YoY Growth"],
                            ["Date", "Region", "Territory"]))
                ],
                alternates: []),
            "executive-sales-reporting",
            "design-studio:draft-executive");
        var operationalResult = CreateStartingPoint(
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
                        "Strong inventory semantic coverage.",
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
                            ["Date", "Warehouse", "Region"]))
                ],
                alternates: []),
            "inventory-operations-monitoring",
            "design-studio:draft-operational");

        var executiveDraftPage = ReadObjectList(executiveResult, "DraftPages").First();
        var operationalDraftPage = ReadObjectList(operationalResult, "DraftPages").First();
        var executiveLayout = ReadObjectList(executiveResult, "DraftLayouts").First();
        var operationalLayout = ReadObjectList(operationalResult, "DraftLayouts").First();

        Assert.Contains("leadership", ReadString(executiveDraftPage, "StructureSummary"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("action", ReadString(operationalDraftPage, "StructureSummary"), StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(ReadString(executiveLayout, "LayoutType"), ReadString(operationalLayout, "LayoutType"));
        Assert.NotEqual(ReadString(executiveLayout, "Title"), ReadString(operationalLayout, "Title"));
    }

    private static object CreateStartingPoint(object profile, object catalog, object recommendations, string recommendationId, string threadId)
    {
        var serviceType = CoreAssembly.GetType($"{DiscoveryServicesNamespace}.DiscoveryDesignStudioAdapterService", throwOnError: false);
        Assert.NotNull(serviceType);

        var service = Activator.CreateInstance(
            serviceType!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: null,
            culture: null);
        Assert.NotNull(service);

        var method = serviceType!.GetMethod("CreateStartingPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(service, [profile, catalog, recommendations, recommendationId, threadId]);
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
                    CreateTypedList(typeof(string), "Executive"))),
            CreateTypedList(typeof(string)),
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
                CreateTypedList(typeof(string)),
                "semantic-model:test",
                "discovery-profile:test"));
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
