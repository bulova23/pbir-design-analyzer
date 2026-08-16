using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Services.Pbir;

namespace PowerBIModelingService.Services.Discovery;

internal sealed class SemanticModelDiscoveryService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    private readonly PbirProjectService _projectService;
    private readonly ILogger _logger;

    public SemanticModelDiscoveryService(PbirProjectService projectService, ILogger logger)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal DiscoveryProfile BuildDiscoveryProfile(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            throw new ArgumentException("Parameter 'projectPath' is required.", nameof(projectPath));
        }

        var location = _projectService.TryGetReportLocation(projectPath)
            ?? throw new InvalidOperationException($"No PBIR report definition found at '{projectPath}'.");
        var reportModel = new ReportModelLoader(_logger).LoadReportModel(location);
        var semanticModel = LoadSemanticModel(location.ProjectRootPath);
        var semanticModelReferenceId = BuildSemanticModelReferenceId(location.ProjectRootPath);
        var discoveryProfileReferenceId = $"discovery-profile:{semanticModelReferenceId}";

        var measures = BuildMeasures(semanticModel);
        var relationships = BuildRelationships(semanticModel);
        var dimensions = BuildDimensions(semanticModel, relationships);
        var hierarchies = BuildHierarchies(semanticModel, reportModel);
        var dateIntelligence = BuildDateIntelligence(semanticModel, dimensions, hierarchies, relationships);
        var businessDomains = BuildBusinessDomains(semanticModel, measures, dimensions);
        var kpiClusters = BuildKpiClusters(measures, businessDomains);
        var audienceSignals = BuildAudienceSignals(reportModel, businessDomains, dimensions, relationships);
        var ambiguityNotes = BuildAmbiguityNotes(businessDomains, dimensions, dateIntelligence, relationships);
        var confidence = CalculateConfidence(measures, dimensions, relationships, dateIntelligence, ambiguityNotes);

        return new DiscoveryProfile(
            Measures: measures,
            Dimensions: dimensions,
            Hierarchies: hierarchies,
            DateIntelligence: dateIntelligence,
            Relationships: relationships,
            BusinessDomains: businessDomains,
            KpiClusters: kpiClusters,
            AudienceSignals: audienceSignals,
            AmbiguityNotes: ambiguityNotes,
            Confidence: confidence,
            SemanticModelReferenceId: semanticModelReferenceId,
            DiscoveryProfileReferenceId: discoveryProfileReferenceId);
    }

    private static string BuildSemanticModelReferenceId(string projectRootPath)
    {
        var semanticModelDirectory = Directory.Exists(projectRootPath)
            ? Directory.GetDirectories(projectRootPath, "*.SemanticModel", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, NameComparer)
                .FirstOrDefault()
            : null;

        var sourcePath = semanticModelDirectory ?? projectRootPath;
        var normalized = sourcePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/')
            .TrimEnd('/');

        return $"semantic-model:{normalized}";
    }

    private static IReadOnlyList<DiscoveryMeasureProfile> BuildMeasures(SemanticModelSnapshot model)
    {
        return model.Tables
            .SelectMany(table => table.Measures.Select(measure => new DiscoveryMeasureProfile(
                measure.Name,
                measure.Description,
                measure.Folder)))
            .OrderBy(measure => measure.Name, NameComparer)
            .ToList();
    }

    private static IReadOnlyList<DiscoveryRelationshipProfile> BuildRelationships(SemanticModelSnapshot model)
    {
        return model.Relationships
            .Select(relationship => new DiscoveryRelationshipProfile(
                relationship.FromTable,
                relationship.ToTable,
                NormalizeCardinality(relationship.Cardinality),
                NormalizeDirectionality(relationship.Directionality)))
            .OrderBy(relationship => relationship.FromTable, NameComparer)
            .ThenBy(relationship => relationship.ToTable, NameComparer)
            .ToList();
    }

    private static IReadOnlyList<DiscoveryDimensionProfile> BuildDimensions(
        SemanticModelSnapshot model,
        IReadOnlyList<DiscoveryRelationshipProfile> relationships)
    {
        var profiles = new List<DiscoveryDimensionProfile>();
        var knownNames = new HashSet<string>(NameComparer);
        var relationshipTargets = relationships.Select(relationship => relationship.ToTable).ToHashSet(NameComparer);

        foreach (var table in model.Tables)
        {
            if (LooksLikeDimensionTable(table, relationshipTargets))
            {
                AddDimension(
                    table.Name,
                    "High",
                    InferBusinessRole(table.Name),
                    profiles,
                    knownNames);
                continue;
            }

            foreach (var column in table.Columns)
            {
                if (LooksLikeMeasureColumn(column.Name, column.DataType) ||
                    column.Name.EndsWith("Key", StringComparison.OrdinalIgnoreCase) ||
                    column.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var role = InferBusinessRole(column.Name);
                var cardinality = role == "Other" ? "Low" : "Medium";
                AddDimension(column.Name, cardinality, role, profiles, knownNames);
            }
        }

        return profiles
            .OrderBy(profile => profile.Name, NameComparer)
            .ToList();
    }

    private static IReadOnlyList<DiscoveryHierarchyProfile> BuildHierarchies(
        SemanticModelSnapshot model,
        LoadedReportModel reportModel)
    {
        var profiles = new List<DiscoveryHierarchyProfile>();
        var knownNames = new HashSet<string>(NameComparer);

        foreach (var table in model.Tables)
        {
            foreach (var hierarchy in table.Hierarchies)
            {
                if (knownNames.Add(hierarchy.Name))
                {
                    profiles.Add(new DiscoveryHierarchyProfile(hierarchy.Name, hierarchy.Levels, false));
                }
            }

            if (table.IsDateTable && HasDateHierarchyColumns(table.Columns))
            {
                var inferredName = $"{table.Name} Inferred Calendar";
                if (knownNames.Add(inferredName))
                {
                    profiles.Add(new DiscoveryHierarchyProfile(inferredName, ["Year", "Quarter", "Month"], true));
                }
            }
        }

        foreach (var visual in reportModel.Pages.SelectMany(page => page.Visuals))
        {
            if (visual.Filter.HierarchyDepth >= 2 && !string.IsNullOrWhiteSpace(visual.Filter.HierarchyPattern))
            {
                var levels = visual.Filter.HierarchyPattern!
                    .Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                var hierarchyName = visual.VisibleTitleText ?? visual.BestVisibleText ?? "Report Hierarchy";
                if (knownNames.Add(hierarchyName))
                {
                    profiles.Add(new DiscoveryHierarchyProfile(hierarchyName, levels, true));
                }
            }
        }

        return profiles
            .OrderBy(profile => profile.Name, NameComparer)
            .ToList();
    }

    private static DiscoveryDateIntelligenceProfile BuildDateIntelligence(
        SemanticModelSnapshot model,
        IReadOnlyList<DiscoveryDimensionProfile> dimensions,
        IReadOnlyList<DiscoveryHierarchyProfile> hierarchies,
        IReadOnlyList<DiscoveryRelationshipProfile> relationships)
    {
        var dateTables = model.Tables
            .Where(table => table.IsDateTable || table.Columns.Any(column => IsDateLikeName(column.Name)))
            .Select(table => table.Name)
            .Distinct(NameComparer)
            .OrderBy(name => name, NameComparer)
            .ToList();
        var dateDimensions = dimensions
            .Where(dimension => dimension.BusinessRole == "Date")
            .Select(dimension => dimension.Name)
            .Distinct(NameComparer)
            .OrderBy(name => name, NameComparer)
            .ToList();

        var hasDateRelationships = relationships.Any(relationship =>
            IsDateLikeName(relationship.ToTable) || IsDateLikeName(relationship.FromTable));
        var hasDateHierarchy = hierarchies.Any(hierarchy =>
            hierarchy.Levels.Any(level => level.Contains("year", StringComparison.OrdinalIgnoreCase)) &&
            hierarchy.Levels.Any(level => level.Contains("month", StringComparison.OrdinalIgnoreCase)));

        var readiness =
            dateTables.Count > 0 && hasDateRelationships && hasDateHierarchy
                ? DiscoveryDateIntelligenceReadiness.High
                : dateTables.Count > 0 || dateDimensions.Count > 0
                    ? DiscoveryDateIntelligenceReadiness.Medium
                    : DiscoveryDateIntelligenceReadiness.Low;

        return new DiscoveryDateIntelligenceProfile(dateTables, dateDimensions, readiness);
    }

    private static IReadOnlyList<DiscoveryDomainSignal> BuildBusinessDomains(
        SemanticModelSnapshot model,
        IReadOnlyList<DiscoveryMeasureProfile> measures,
        IReadOnlyList<DiscoveryDimensionProfile> dimensions)
    {
        var evidence = new Dictionary<string, List<string>>(NameComparer)
        {
            ["Revenue"] = [],
            ["Profitability"] = [],
            ["Inventory"] = [],
            ["Customer"] = [],
            ["Service"] = [],
            ["Forecasting"] = [],
        };

        foreach (var measure in measures)
        {
            RegisterDomainEvidence(evidence, measure.Name, $"measure:{measure.Name}");
            RegisterDomainEvidence(evidence, measure.Description, $"measure-description:{measure.Name}");
            RegisterDomainEvidence(evidence, measure.Folder, $"measure-folder:{measure.Name}");
        }

        foreach (var dimension in dimensions)
        {
            RegisterDomainEvidence(evidence, dimension.Name, $"dimension:{dimension.Name}");
            RegisterDomainEvidence(evidence, dimension.BusinessRole, $"dimension-role:{dimension.Name}");
        }

        foreach (var table in model.Tables)
        {
            RegisterDomainEvidence(evidence, table.Name, $"table:{table.Name}");
        }

        return evidence
            .Where(entry => entry.Value.Count > 0)
            .Select(entry => new DiscoveryDomainSignal(
                entry.Key,
                entry.Value.Count >= 2 ? DiscoveryConfidenceLevel.High : DiscoveryConfidenceLevel.Medium,
                entry.Value.OrderBy(value => value, NameComparer).ToList()))
            .OrderBy(signal => signal.Domain, NameComparer)
            .ToList();
    }

    private static IReadOnlyList<DiscoveryKpiCluster> BuildKpiClusters(
        IReadOnlyList<DiscoveryMeasureProfile> measures,
        IReadOnlyList<DiscoveryDomainSignal> domains)
    {
        var clusters = new Dictionary<string, List<string>>(NameComparer);

        foreach (var measure in measures)
        {
            var clusterName = DetectClusterName(measure);
            if (clusterName is null)
            {
                continue;
            }

            if (!clusters.TryGetValue(clusterName, out var names))
            {
                names = [];
                clusters[clusterName] = names;
            }

            names.Add(measure.Name);
        }

        foreach (var domain in domains)
        {
            var defaultCluster = domain.Domain switch
            {
                "Revenue" => "Revenue KPIs",
                "Inventory" => "Inventory KPIs",
                "Service" => "Service KPIs",
                "Forecasting" => "Forecast KPIs",
                "Customer" => "Customer KPIs",
                "Profitability" => "Margin KPIs",
                _ => null,
            };

            if (defaultCluster is not null && !clusters.ContainsKey(defaultCluster))
            {
                clusters[defaultCluster] = [];
            }
        }

        return clusters
            .OrderBy(entry => entry.Key, NameComparer)
            .Select(entry => new DiscoveryKpiCluster(
                entry.Key,
                entry.Value.Distinct(NameComparer).OrderBy(name => name, NameComparer).ToList(),
                entry.Value.Count >= 2 ? DiscoveryConfidenceLevel.High : DiscoveryConfidenceLevel.Medium))
            .ToList();
    }

    private static IReadOnlyList<DiscoveryAudienceSignal> BuildAudienceSignals(
        LoadedReportModel reportModel,
        IReadOnlyList<DiscoveryDomainSignal> domains,
        IReadOnlyList<DiscoveryDimensionProfile> dimensions,
        IReadOnlyList<DiscoveryRelationshipProfile> relationships)
    {
        var audiences = new Dictionary<string, List<string>>(NameComparer);
        var pageLabels = reportModel.Pages
            .Select(page => page.DisplayName)
            .Concat(reportModel.Pages.SelectMany(page => page.Visuals.Select(visual => visual.BestVisibleText)))
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Cast<string>()
            .ToList();

        if (pageLabels.Any(label => ContainsAny(label, "executive", "overview", "summary", "kpi")) ||
            domains.Any(domain => domain.Domain is "Revenue" or "Profitability"))
        {
            audiences["Executive"] = ["page-labels", "domain-coverage"];
        }

        if (pageLabels.Any(label => ContainsAny(label, "operations", "operational", "monitor", "review")) ||
            domains.Any(domain => domain.Domain is "Inventory" or "Service"))
        {
            audiences["Operational"] = ["page-labels", "operational-domain"];
        }

        if (relationships.Count >= 2 || dimensions.Count >= 2)
        {
            audiences["Analytical"] = ["relationship-rich", "dimension-rich"];
        }

        if (domains.Any(domain => domain.Domain == "Service"))
        {
            audiences["Service"] = ["service-domain"];
        }

        return audiences
            .OrderBy(entry => entry.Key, NameComparer)
            .Select(entry => new DiscoveryAudienceSignal(
                entry.Key,
                entry.Value.Count >= 2 ? DiscoveryConfidenceLevel.High : DiscoveryConfidenceLevel.Medium,
                entry.Value))
            .ToList();
    }

    private static IReadOnlyList<string> BuildAmbiguityNotes(
        IReadOnlyList<DiscoveryDomainSignal> domains,
        IReadOnlyList<DiscoveryDimensionProfile> dimensions,
        DiscoveryDateIntelligenceProfile dateIntelligence,
        IReadOnlyList<DiscoveryRelationshipProfile> relationships)
    {
        var notes = new List<string>();

        if (!dimensions.Any(dimension => dimension.BusinessRole == "Customer"))
        {
            notes.Add("Missing customer dimension reduces segmentation confidence.");
        }

        if (dateIntelligence.Readiness == DiscoveryDateIntelligenceReadiness.Low)
        {
            notes.Add("Weak date intelligence limits time-based reasoning.");
        }

        if (domains.Any(domain => domain.Domain == "Inventory") &&
            !dimensions.Any(dimension => dimension.BusinessRole is "Inventory" or "Product") &&
            !relationships.Any(relationship => ContainsAny(relationship.ToTable, "inventory", "warehouse", "product")))
        {
            notes.Add("Unclear inventory structure reduces operational recommendation confidence.");
        }

        if (domains.Select(domain => domain.Domain).Intersect(["Inventory", "Service"], NameComparer).Count() >= 2 &&
            relationships.Count == 0)
        {
            notes.Add("Mixed operational domains appear without enough model structure to separate workflows.");
        }

        return notes;
    }

    private static DiscoveryConfidenceLevel CalculateConfidence(
        IReadOnlyList<DiscoveryMeasureProfile> measures,
        IReadOnlyList<DiscoveryDimensionProfile> dimensions,
        IReadOnlyList<DiscoveryRelationshipProfile> relationships,
        DiscoveryDateIntelligenceProfile dateIntelligence,
        IReadOnlyList<string> ambiguityNotes)
    {
        var score = 0;

        if (measures.Count >= 3)
        {
            score += 3;
        }
        else if (measures.Count >= 1)
        {
            score += 2;
        }

        if (dimensions.Count >= 3)
        {
            score += 3;
        }
        else if (dimensions.Count >= 1)
        {
            score += 2;
        }

        if (relationships.Count >= 2)
        {
            score += 2;
        }
        else if (relationships.Count == 1)
        {
            score += 1;
        }

        score += dateIntelligence.Readiness switch
        {
            DiscoveryDateIntelligenceReadiness.High => 2,
            DiscoveryDateIntelligenceReadiness.Medium => 1,
            _ => 0,
        };

        if (measures.Any(measure => !string.IsNullOrWhiteSpace(measure.Description) || !string.IsNullOrWhiteSpace(measure.Folder)))
        {
            score += 1;
        }

        score -= ambiguityNotes.Count;

        return score >= 7
            ? DiscoveryConfidenceLevel.High
            : score >= 4
                ? DiscoveryConfidenceLevel.Medium
                : DiscoveryConfidenceLevel.Low;
    }

    private static SemanticModelSnapshot LoadSemanticModel(string projectRootPath)
    {
        var candidatePaths = Directory.Exists(projectRootPath)
            ? Directory.GetDirectories(projectRootPath, "*.SemanticModel", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, NameComparer)
                .SelectMany(path => new[]
                {
                    Path.Combine(path, "definition", "model.json"),
                    Path.Combine(path, "definition", "model.bim"),
                    Path.Combine(path, "definition", "database.json"),
                    Path.Combine(path, "model.json"),
                    Path.Combine(path, "model.bim"),
                    Path.Combine(path, "database.json"),
                })
            : [];

        foreach (var candidatePath in candidatePaths)
        {
            if (File.Exists(candidatePath))
            {
                var json = JsonNode.Parse(File.ReadAllText(candidatePath)) as JsonObject;
                if (json is not null)
                {
                    return ParseSemanticModel(json);
                }
            }
        }

        return new SemanticModelSnapshot([], []);
    }

    private static SemanticModelSnapshot ParseSemanticModel(JsonObject root)
    {
        var tables = ReadObjectArray(root["tables"])
            .Select(ParseTable)
            .ToList();
        var relationships = ReadObjectArray(root["relationships"])
            .Select(ParseRelationship)
            .ToList();

        return new SemanticModelSnapshot(tables, relationships);
    }

    private static SemanticTableSnapshot ParseTable(JsonObject table)
    {
        return new SemanticTableSnapshot(
            Name: ReadString(table, "name") ?? "Unknown",
            IsDateTable: ReadBool(table, "isDateTable"),
            Columns: ReadObjectArray(table["columns"]).Select(ParseColumn).ToList(),
            Measures: ReadObjectArray(table["measures"]).Select(ParseMeasure).ToList(),
            Hierarchies: ReadObjectArray(table["hierarchies"]).Select(ParseHierarchy).ToList());
    }

    private static SemanticColumnSnapshot ParseColumn(JsonObject column)
    {
        return new SemanticColumnSnapshot(
            Name: ReadString(column, "name") ?? "Unknown",
            DataType: ReadString(column, "dataType"));
    }

    private static SemanticMeasureSnapshot ParseMeasure(JsonObject measure)
    {
        return new SemanticMeasureSnapshot(
            Name: ReadString(measure, "name") ?? "Unknown",
            Description: ReadString(measure, "description"),
            Folder: ReadString(measure, "displayFolder"));
    }

    private static SemanticHierarchySnapshot ParseHierarchy(JsonObject hierarchy)
    {
        return new SemanticHierarchySnapshot(
            Name: ReadString(hierarchy, "name") ?? "Unknown",
            Levels: ReadStringArray(hierarchy["levels"]));
    }

    private static SemanticRelationshipSnapshot ParseRelationship(JsonObject relationship)
    {
        return new SemanticRelationshipSnapshot(
            FromTable: ReadString(relationship, "fromTable") ?? "Unknown",
            ToTable: ReadString(relationship, "toTable") ?? "Unknown",
            Cardinality: ReadString(relationship, "cardinality"),
            Directionality: ReadString(relationship, "crossFilteringBehavior"));
    }

    private static void AddDimension(
        string name,
        string cardinalityIndicator,
        string businessRole,
        ICollection<DiscoveryDimensionProfile> profiles,
        ISet<string> knownNames)
    {
        if (string.IsNullOrWhiteSpace(name) || knownNames.Contains(name))
        {
            return;
        }

        knownNames.Add(name);
        profiles.Add(new DiscoveryDimensionProfile(name, cardinalityIndicator, businessRole));
    }

    private static bool LooksLikeDimensionTable(SemanticTableSnapshot table, ISet<string> relationshipTargets)
    {
        return table.IsDateTable ||
               relationshipTargets.Contains(table.Name) ||
               table.Name.StartsWith("Dim", StringComparison.OrdinalIgnoreCase) ||
               InferBusinessRole(table.Name) != "Other";
    }

    private static bool LooksLikeMeasureColumn(string name, string? dataType)
    {
        return ContainsAny(name, "amount", "sales", "revenue", "margin", "count", "value", "accuracy") ||
               string.Equals(dataType, "decimal", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(dataType, "double", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasDateHierarchyColumns(IReadOnlyList<SemanticColumnSnapshot> columns)
    {
        return columns.Any(column => column.Name.Contains("year", StringComparison.OrdinalIgnoreCase)) &&
               columns.Any(column => column.Name.Contains("month", StringComparison.OrdinalIgnoreCase));
    }

    private static string InferBusinessRole(string value)
    {
        return value switch
        {
            var name when ContainsAny(name, "date", "year", "month", "quarter", "fiscal") => "Date",
            var name when ContainsAny(name, "customer", "segment", "account") => "Customer",
            var name when ContainsAny(name, "inventory", "warehouse", "stock") => "Inventory",
            var name when ContainsAny(name, "product", "sku", "category", "brand") => "Product",
            var name when ContainsAny(name, "service", "ticket", "queue", "resolution") => "Service",
            var name when ContainsAny(name, "region", "territory", "geo") => "Geography",
            var name when ContainsAny(name, "forecast", "plan", "budget") => "Forecasting",
            _ => "Other",
        };
    }

    private static void RegisterDomainEvidence(
        IDictionary<string, List<string>> evidence,
        string? candidate,
        string evidenceLabel)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        if (ContainsAny(candidate, "revenue", "sales"))
        {
            evidence["Revenue"].Add(evidenceLabel);
        }

        if (ContainsAny(candidate, "margin", "profit"))
        {
            evidence["Profitability"].Add(evidenceLabel);
        }

        if (ContainsAny(candidate, "inventory", "warehouse", "stock"))
        {
            evidence["Inventory"].Add(evidenceLabel);
        }

        if (ContainsAny(candidate, "customer", "account", "segment"))
        {
            evidence["Customer"].Add(evidenceLabel);
        }

        if (ContainsAny(candidate, "service", "ticket", "resolution", "queue"))
        {
            evidence["Service"].Add(evidenceLabel);
        }

        if (ContainsAny(candidate, "forecast", "budget", "plan"))
        {
            evidence["Forecasting"].Add(evidenceLabel);
        }
    }

    private static string? DetectClusterName(DiscoveryMeasureProfile measure)
    {
        if (!string.IsNullOrWhiteSpace(measure.Folder))
        {
            return measure.Folder;
        }

        if (ContainsAny(measure.Name, "revenue", "sales"))
        {
            return "Revenue KPIs";
        }

        if (ContainsAny(measure.Name, "margin", "profit"))
        {
            return "Margin KPIs";
        }

        if (ContainsAny(measure.Name, "inventory", "stock"))
        {
            return "Inventory KPIs";
        }

        if (ContainsAny(measure.Name, "service", "ticket", "resolution"))
        {
            return "Service KPIs";
        }

        if (ContainsAny(measure.Name, "forecast", "budget", "plan"))
        {
            return "Forecast KPIs";
        }

        if (ContainsAny(measure.Name, "customer", "retention"))
        {
            return "Customer KPIs";
        }

        return null;
    }

    private static string NormalizeCardinality(string? cardinality)
    {
        if (string.IsNullOrWhiteSpace(cardinality))
        {
            return "Unknown";
        }

        return cardinality.Contains("many", StringComparison.OrdinalIgnoreCase) &&
               cardinality.Contains("one", StringComparison.OrdinalIgnoreCase)
            ? "ManyToOne"
            : cardinality;
    }

    private static string NormalizeDirectionality(string? directionality)
    {
        if (string.IsNullOrWhiteSpace(directionality))
        {
            return "Unknown";
        }

        return directionality.Contains("both", StringComparison.OrdinalIgnoreCase)
            ? "BothDirections"
            : "OneDirection";
    }

    private static bool IsDateLikeName(string value)
    {
        return ContainsAny(value, "date", "year", "month", "quarter", "fiscal");
    }

    private static bool ContainsAny(string candidate, params string[] needles)
    {
        return needles.Any(needle => candidate.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadString(JsonObject obj, string propertyName)
    {
        return obj[propertyName]?.GetValue<string>();
    }

    private static bool ReadBool(JsonObject obj, string propertyName)
    {
        return obj[propertyName]?.GetValue<bool>() == true;
    }

    private static IReadOnlyList<JsonObject> ReadObjectArray(JsonNode? node)
    {
        return node is JsonArray array
            ? array.OfType<JsonObject>().ToList()
            : [];
    }

    private static IReadOnlyList<string> ReadStringArray(JsonNode? node)
    {
        return node is JsonArray array
            ? array.Select(item => item?.GetValue<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Cast<string>()
                .ToList()
            : [];
    }

    private sealed record SemanticModelSnapshot(
        IReadOnlyList<SemanticTableSnapshot> Tables,
        IReadOnlyList<SemanticRelationshipSnapshot> Relationships);

    private sealed record SemanticTableSnapshot(
        string Name,
        bool IsDateTable,
        IReadOnlyList<SemanticColumnSnapshot> Columns,
        IReadOnlyList<SemanticMeasureSnapshot> Measures,
        IReadOnlyList<SemanticHierarchySnapshot> Hierarchies);

    private sealed record SemanticColumnSnapshot(
        string Name,
        string? DataType);

    private sealed record SemanticMeasureSnapshot(
        string Name,
        string? Description,
        string? Folder);

    private sealed record SemanticHierarchySnapshot(
        string Name,
        IReadOnlyList<string> Levels);

    private sealed record SemanticRelationshipSnapshot(
        string FromTable,
        string ToTable,
        string? Cardinality,
        string? Directionality);
}
