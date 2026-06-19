using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class SemanticModelDiscoveryServiceTests : IDisposable
{
    private static readonly Assembly CoreAssembly = typeof(PbirScoringService).Assembly;
    private readonly List<string> _tempDirs = [];

    [Fact(DisplayName = "Discovery service builds a rich profile from a semantic model")]
    public void BuildDiscoveryProfile_RichModel_CapturesMeasuresDimensionsHierarchiesAndRelationships()
    {
        var projectRoot = CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactSales",
                  "columns": [
                    { "name": "OrderDateKey", "dataType": "int64" },
                    { "name": "CustomerKey", "dataType": "int64" },
                    { "name": "RevenueAmount", "dataType": "decimal" },
                    { "name": "MarginAmount", "dataType": "decimal" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs", "description": "Net revenue amount" },
                    { "name": "Gross Margin", "displayFolder": "Margin KPIs", "description": "Gross margin amount" },
                    { "name": "Forecast Accuracy", "displayFolder": "Forecast KPIs", "description": "Accuracy versus forecast" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Year", "dataType": "int64" },
                    { "name": "Quarter", "dataType": "string" },
                    { "name": "Month", "dataType": "string" }
                  ],
                  "hierarchies": [
                    { "name": "Calendar", "levels": [ "Year", "Quarter", "Month" ] }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "CustomerKey", "dataType": "int64" },
                    { "name": "Customer Name", "dataType": "string" },
                    { "name": "Customer Segment", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimRegion",
                  "columns": [
                    { "name": "RegionKey", "dataType": "int64" },
                    { "name": "Region", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                {
                  "fromTable": "FactSales",
                  "fromColumn": "OrderDateKey",
                  "toTable": "DimDate",
                  "toColumn": "Date",
                  "cardinality": "ManyToOne",
                  "crossFilteringBehavior": "OneDirection"
                },
                {
                  "fromTable": "FactSales",
                  "fromColumn": "CustomerKey",
                  "toTable": "DimCustomer",
                  "toColumn": "CustomerKey",
                  "cardinality": "ManyToOne",
                  "crossFilteringBehavior": "BothDirections"
                },
                {
                  "fromTable": "FactSales",
                  "fromColumn": "RegionKey",
                  "toTable": "DimRegion",
                  "toColumn": "RegionKey",
                  "cardinality": "ManyToOne",
                  "crossFilteringBehavior": "OneDirection"
                }
              ]
            }
            """,
            pageJson:
            """
            {
              "name": "Page1",
              "displayName": "Executive Overview",
              "visuals": [
                {
                  "id": "s1",
                  "type": "slicer",
                  "x": 0,
                  "y": 0,
                  "width": 200,
                  "height": 80,
                  "title": { "visible": true, "text": "Date" },
                  "fieldRoles": {
                    "category": [
                      { "displayName": "Calendar", "hierarchy": [ "Year", "Quarter", "Month" ] }
                    ]
                  }
                },
                {
                  "id": "v1",
                  "type": "lineChart",
                  "x": 0,
                  "y": 120,
                  "width": 500,
                  "height": 240,
                  "title": { "visible": true, "text": "Revenue Trend" },
                  "fieldRoles": {
                    "category": [ { "displayName": "Month" } ],
                    "measure": [ { "displayName": "Revenue", "description": "Net revenue amount" } ]
                  }
                }
              ]
            }
            """);

        var profile = BuildDiscoveryProfile(projectRoot);

        Assert.Equal(3, GetListCount(profile, "Measures"));
        Assert.Equal(3, GetListCount(profile, "Dimensions"));
        Assert.Equal(3, GetListCount(profile, "Relationships"));
        Assert.Contains(ReadStringList(profile, "BusinessDomains"), value => value == "Revenue");
        Assert.Contains(ReadStringList(profile, "BusinessDomains"), value => value == "Profitability");
        Assert.Contains(ReadStringList(profile, "BusinessDomains"), value => value == "Forecasting");
        Assert.Contains(ReadStringList(profile, "AudienceSignals"), value => value == "Executive");
        Assert.Contains(ReadStringList(profile, "AudienceSignals"), value => value == "Analytical");
        Assert.Equal("High", ReadString(profile, "Confidence"));

        var measures = ReadObjectList(profile, "Measures");
        Assert.Contains(measures, measure => ReadString(measure, "Name") == "Revenue" && ReadString(measure, "Folder") == "Revenue KPIs");
        Assert.Contains(measures, measure => ReadString(measure, "Name") == "Gross Margin" && ReadString(measure, "Description").Contains("margin", StringComparison.OrdinalIgnoreCase));

        var hierarchies = ReadObjectList(profile, "Hierarchies");
        Assert.Contains(hierarchies, hierarchy => ReadString(hierarchy, "Name") == "Calendar" && !ReadBool(hierarchy, "IsInferred"));

        var dateIntelligence = GetPropertyValue(profile, "DateIntelligence");
        Assert.NotNull(dateIntelligence);
        Assert.Contains(ReadStringList(dateIntelligence!, "DateTables"), value => value == "DimDate");
        Assert.Equal("High", ReadString(dateIntelligence!, "Readiness"));
    }

    [Fact(DisplayName = "Discovery service degrades gracefully for sparse metadata")]
    public void BuildDiscoveryProfile_SparseModel_CapturesAmbiguityAndLowConfidence()
    {
        var projectRoot = CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "Sales",
                  "columns": [
                    { "name": "Category", "dataType": "string" }
                  ],
                  "measures": [
                    { "name": "Sales" }
                  ]
                }
              ]
            }
            """,
            pageJson:
            """
            {
              "name": "Page1",
              "displayName": "Snapshot",
              "visuals": [
                {
                  "id": "v1",
                  "type": "card",
                  "x": 0,
                  "y": 0,
                  "width": 180,
                  "height": 90,
                  "title": { "visible": true, "text": "Sales" }
                }
              ]
            }
            """);

        var profile = BuildDiscoveryProfile(projectRoot);

        Assert.Equal(1, GetListCount(profile, "Measures"));
        Assert.Equal("Low", ReadString(profile, "Confidence"));
        Assert.NotEmpty(ReadStringList(profile, "AmbiguityNotes"));
        Assert.Equal("Low", ReadString(GetPropertyValue(profile, "DateIntelligence")!, "Readiness"));
    }

    [Fact(DisplayName = "Discovery service emits explicit ambiguity notes for weak semantics")]
    public void BuildDiscoveryProfile_AmbiguousModel_ProducesExplicitAmbiguityNotes()
    {
        var projectRoot = CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactOps",
                  "columns": [
                    { "name": "Region", "dataType": "string" },
                    { "name": "Bucket", "dataType": "string" }
                  ],
                  "measures": [
                    { "name": "Inventory Value" },
                    { "name": "Open Tickets" }
                  ]
                }
              ]
            }
            """,
            pageJson:
            """
            {
              "name": "Page1",
              "displayName": "Operational Review",
              "visuals": [
                {
                  "id": "v1",
                  "type": "barChart",
                  "x": 0,
                  "y": 100,
                  "width": 480,
                  "height": 220,
                  "title": { "visible": true, "text": "Inventory and Tickets" },
                  "fieldRoles": {
                    "category": [ { "displayName": "Region" } ],
                    "measure": [ { "displayName": "Inventory Value" }, { "displayName": "Open Tickets" } ]
                  }
                }
              ]
            }
            """);

        var profile = BuildDiscoveryProfile(projectRoot);
        var ambiguityNotes = ReadStringList(profile, "AmbiguityNotes");

        Assert.Contains(ambiguityNotes, note => note.Contains("missing customer dimension", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ambiguityNotes, note => note.Contains("weak date intelligence", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ambiguityNotes, note => note.Contains("unclear inventory structure", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Low", ReadString(profile, "Confidence"));
    }

    [Fact(DisplayName = "Discovery service detects the planned business domains and KPI clusters")]
    public void BuildDiscoveryProfile_DomainDetection_CapturesRevenueCustomerInventoryForecastingAndService()
    {
        var projectRoot = CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactBusiness",
                  "columns": [
                    { "name": "CustomerKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" },
                    { "name": "WarehouseKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "displayFolder": "Revenue KPIs" },
                    { "name": "Customer Count", "displayFolder": "Customer KPIs" },
                    { "name": "Inventory On Hand", "displayFolder": "Inventory KPIs" },
                    { "name": "Forecast Accuracy", "displayFolder": "Forecast KPIs" },
                    { "name": "Average Resolution Time", "displayFolder": "Service KPIs" }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "Customer Name", "dataType": "string" },
                    { "name": "Customer Segment", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Year", "dataType": "int64" },
                    { "name": "Month", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimWarehouse",
                  "columns": [
                    { "name": "Warehouse", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactBusiness", "fromColumn": "CustomerKey", "toTable": "DimCustomer", "toColumn": "Customer Name", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactBusiness", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactBusiness", "fromColumn": "WarehouseKey", "toTable": "DimWarehouse", "toColumn": "Warehouse", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: DefaultPageJson);

        var profile = BuildDiscoveryProfile(projectRoot);

        var domains = ReadStringList(profile, "BusinessDomains");
        Assert.Contains("Revenue", domains);
        Assert.Contains("Customer", domains);
        Assert.Contains("Inventory", domains);
        Assert.Contains("Forecasting", domains);
        Assert.Contains("Service", domains);

        var kpiClusters = ReadStringList(profile, "KpiClusters");
        Assert.Contains("Revenue KPIs", kpiClusters);
        Assert.Contains("Inventory KPIs", kpiClusters);
        Assert.Contains("Service KPIs", kpiClusters);
    }

    [Fact(DisplayName = "Discovery service uses high medium and low confidence levels")]
    public void BuildDiscoveryProfile_ConfidenceLevels_SpanHighMediumAndLow()
    {
        var high = BuildDiscoveryProfile(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactSales",
                  "columns": [
                    { "name": "CustomerKey", "dataType": "int64" },
                    { "name": "DateKey", "dataType": "int64" }
                  ],
                  "measures": [
                    { "name": "Revenue", "description": "Net revenue", "displayFolder": "Revenue KPIs" }
                  ]
                },
                {
                  "name": "DimDate",
                  "isDateTable": true,
                  "columns": [
                    { "name": "Date", "dataType": "dateTime" },
                    { "name": "Year", "dataType": "int64" },
                    { "name": "Month", "dataType": "string" }
                  ]
                },
                {
                  "name": "DimCustomer",
                  "columns": [
                    { "name": "Customer", "dataType": "string" }
                  ]
                }
              ],
              "relationships": [
                { "fromTable": "FactSales", "fromColumn": "DateKey", "toTable": "DimDate", "toColumn": "Date", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" },
                { "fromTable": "FactSales", "fromColumn": "CustomerKey", "toTable": "DimCustomer", "toColumn": "Customer", "cardinality": "ManyToOne", "crossFilteringBehavior": "OneDirection" }
              ]
            }
            """,
            pageJson: DefaultPageJson));

        var medium = BuildDiscoveryProfile(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "FactSales",
                  "columns": [
                    { "name": "DateText", "dataType": "string" },
                    { "name": "Region", "dataType": "string" }
                  ],
                  "measures": [
                    { "name": "Revenue" },
                    { "name": "Margin" }
                  ]
                }
              ]
            }
            """,
            pageJson: DefaultPageJson));

        var low = BuildDiscoveryProfile(CreateTempProject(
            modelJson:
            """
            {
              "tables": [
                {
                  "name": "Facts",
                  "measures": [
                    { "name": "Value" }
                  ]
                }
              ]
            }
            """,
            pageJson: DefaultPageJson));

        Assert.Equal("High", ReadString(high, "Confidence"));
        Assert.Equal("Medium", ReadString(medium, "Confidence"));
        Assert.Equal("Low", ReadString(low, "Confidence"));
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static string DefaultPageJson =>
        """
        {
          "name": "Page1",
          "displayName": "Overview",
          "visuals": [
            {
              "id": "v1",
              "type": "barChart",
              "x": 0,
              "y": 100,
              "width": 480,
              "height": 220,
              "title": { "visible": true, "text": "Overview" },
              "fieldRoles": {
                "category": [ { "displayName": "Month" } ],
                "measure": [ { "displayName": "Revenue" } ]
              }
            }
          ]
        }
        """;

    private object BuildDiscoveryProfile(string projectRoot)
    {
        var type = CoreAssembly.GetType("PowerBIModelingService.Services.Discovery.SemanticModelDiscoveryService", throwOnError: false);
        Assert.NotNull(type);

        var instance = Activator.CreateInstance(
            type!,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args:
            [
                new PbirProjectService(NullLogger<PbirProjectService>.Instance),
                NullLogger.Instance
            ],
            culture: null);
        Assert.NotNull(instance);

        var method = type!.GetMethod("BuildDiscoveryProfile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var result = method!.Invoke(instance, [projectRoot]);
        Assert.NotNull(result);
        return result!;
    }

    private string CreateTempProject(string modelJson, string pageJson)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "pbir-discovery-" + Guid.NewGuid().ToString("N"));
        var reportDefinitionRoot = Path.Combine(tempRoot, "TestReport.Report", "definition");
        var reportPagesRoot = Path.Combine(reportDefinitionRoot, "pages", "Page1");
        var semanticModelRoot = Path.Combine(tempRoot, "TestModel.SemanticModel", "definition");

        Directory.CreateDirectory(reportPagesRoot);
        Directory.CreateDirectory(semanticModelRoot);
        _tempDirs.Add(tempRoot);

        File.WriteAllText(
            Path.Combine(reportDefinitionRoot, "report.json"),
            """{"id":"test","name":"TestReport","theme":{"name":"CY24SU10"}}""");
        File.WriteAllText(Path.Combine(reportPagesRoot, "page.json"), pageJson);
        File.WriteAllText(Path.Combine(semanticModelRoot, "model.json"), modelJson);

        return tempRoot;
    }

    private static object? GetPropertyValue(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(target);
    }

    private static int GetListCount(object target, string propertyName)
    {
        return ReadObjectList(target, propertyName).Count;
    }

    private static List<object> ReadObjectList(object target, string propertyName)
    {
        var value = GetPropertyValue(target, propertyName);
        Assert.IsAssignableFrom<IEnumerable>(value);
        return ((IEnumerable)value!).Cast<object>().ToList();
    }

    private static List<string> ReadStringList(object target, string propertyName)
    {
        var value = GetPropertyValue(target, propertyName);
        Assert.IsAssignableFrom<IEnumerable>(value);

        var items = ((IEnumerable)value!).Cast<object>().ToList();
        if (items.Count == 0)
        {
            return [];
        }

        if (items[0] is string)
        {
            return items.Select(item => item?.ToString() ?? string.Empty).ToList();
        }

        if (items[0].GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null)
        {
            return items.Select(item => ReadString(item, "Name")).ToList();
        }

        if (items[0].GetType().GetProperty("Audience", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null)
        {
            return items.Select(item => ReadString(item, "Audience")).ToList();
        }

        if (items[0].GetType().GetProperty("ClusterName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null)
        {
            return items.Select(item => ReadString(item, "ClusterName")).ToList();
        }

        if (items[0].GetType().GetProperty("Domain", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) is not null)
        {
            return items.Select(item => ReadString(item, "Domain")).ToList();
        }

        throw new InvalidOperationException($"Unable to read string list for property '{propertyName}'.");
    }

    private static string ReadString(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName)?.ToString() ?? string.Empty;
    }

    private static bool ReadBool(object target, string propertyName)
    {
        return GetPropertyValue(target, propertyName) is bool value && value;
    }
}
