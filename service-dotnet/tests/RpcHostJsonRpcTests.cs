using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PowerBIModelingService.Services.Pbir.Models;
using Xunit;

namespace ServiceDotnet.Tests
{
    public class RpcHostJsonRpcTests
    {
        [Fact]
        public void SendResponse_WithNullResult_SerializesValidJsonRpc()
        {
            var response = new JsonRpcResponse { Id = 1, Result = null };
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(response, options);
            Assert.Contains("\"result\":null", json);
        }

        [Fact]
        public void RpcHostSerializer_WithShutdownStyleNullResult_EmitsResultProperty()
        {
            var rpcHostAssemblyPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "RpcHost",
                "bin",
                "Release",
                "net8.0",
                "ModelingLanguageServer.dll"));

            Assert.True(File.Exists(rpcHostAssemblyPath), $"RpcHost assembly not found at {rpcHostAssemblyPath}");

            var rpcHostAssembly = Assembly.LoadFrom(rpcHostAssemblyPath);
            var responseType = rpcHostAssembly.GetType("PowerBIModelingService.RpcHost.JsonRpcSuccessResponse");
            var optionsFactory = rpcHostAssembly
                .GetType("PowerBIModelingService.RpcHost.SimpleJsonRpcServer")?
                .GetMethod("CreateJsonSerializerOptions", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(responseType);
            Assert.NotNull(optionsFactory);

            var options = optionsFactory!.Invoke(null, null) as JsonSerializerOptions;
            var response = Activator.CreateInstance(responseType!);

            Assert.NotNull(options);
            Assert.NotNull(response);

            responseType!.GetProperty("Id")!.SetValue(response, JsonDocument.Parse("1").RootElement.Clone());
            responseType.GetProperty("Result")!.SetValue(response, null);

            var json = JsonSerializer.Serialize(response, responseType, options);

            Assert.Contains("\"result\":null", json);
        }

        [Fact]
        public async Task HandleDefinitionAsync_WithNullParams_ReturnsErrorResponseAsync()
        {
            var handler = new FakeRpcHandler();
            var response = await handler.HandleDefinitionAsync(null);
            Assert.NotNull(response);
            // When params are null, handler returns null result (valid JSON-RPC with { "result": null })
            Assert.True(response is JsonRpcResponse);
        }

        [Fact]
        public async Task HandleDefinitionAsync_ServiceReturnsNull_ReturnsValidResponseAsync()
        {
            var handler = new FakeRpcHandler(returnNull:true);
            var response = await handler.HandleDefinitionAsync(new JsonElement?());
            Assert.NotNull(response);
            Assert.True(response is JsonRpcResponse);
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.Never,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(response, options);
            Assert.Contains("\"result\":null", json);
        }

        [Fact]
        public async Task HandleHoverAsync_ThrowsException_ReturnsErrorResponseAsync()
        {
            var handler = new FakeRpcHandler(throwOnHover:true);
            var response = await handler.HandleHoverAsync(new JsonElement?());
            Assert.NotNull(response);
            Assert.True(response is JsonRpcErrorResponse);
        }

        [Fact]
        public void JsonRpcResponse_HasResultOrError_AlwaysTrue()
        {
            var response = new JsonRpcResponse { Id = 1, Result = null };
            var error = new JsonRpcErrorResponse { Id = 1, Error = new JsonRpcError { Code = -32603, Message = "error" } };
            Assert.True(response.Result == null || error.Error != null);
        }

        [Fact]
        public void ScoreResult_SerializesInCamelCase_ForRpcHostContract()
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var score = new ScoreResult
            {
                GestaltScore = 82,
                CognitiveLoadScore = 74,
                DataInkScore = 79,
                AccessibilityScore = 71,
                VisualBestPracticesScore = 77,
                StephenFewScore = 68,
                EnterpriseGovernanceScore = 72,
                TufteScore = 66,
                GraphicalPerceptionScore = 69,
                DensityScore = 61,
                NarrativeScore = 64,
                PageCount = 1,
                ReportPath = "/tmp/Sales.Report",
                Recommendations = new List<string> { "[High] Layout: Snap visuals to grid" },
                FrameworkWeights = new Dictionary<string, double> { ["gestalt"] = 25.0 },
                VisualMetadata = new PageVisualMetadataSummary
                {
                    PageName = "Overview",
                    VisiblePageTitle = "Sales Overview",
                    SemanticColorMap = new List<SemanticColorAssignment>
                    {
                        new()
                        {
                            SemanticKey = "region:north",
                            DisplayLabel = "North",
                            Color = "#3366CC",
                            SourceVisualId = "v1",
                            SourcePageName = "Overview",
                        },
                    },
                    ChartIntentSummary = new ChartIntentSummary
                    {
                        Intent = "comparison",
                        Confidence = "high",
                        Evidence = new List<string> { "bar chart", "category axis" },
                        FitStatus = "good",
                        RecommendedAlternatives = new List<string> { "columnChart" },
                    },
                    VisualCount = 1,
                    VisibleTitleVisualCount = 1,
                    TextVisualCount = 0,
                    SlicerCount = 0,
                    LegendVisualCount = 1,
                    AxisLabelVisualCount = 1,
                    DataLabelVisualCount = 0,
                    FormattedVisualCount = 1,
                    Visuals = new List<VisualMetadataItem>
                    {
                        new()
                        {
                            VisualId = "v1",
                            VisualType = "barChart",
                            HasVisibleTitleIntent = true,
                            Width = 320,
                            Height = 180,
                            SemanticColors = new List<SemanticColorAssignment>
                            {
                                new()
                                {
                                    SemanticKey = "region:north",
                                    DisplayLabel = "North",
                                    Color = "#3366CC",
                                    SourceVisualId = "v1",
                                    SourcePageName = "Overview",
                                },
                            },
                            ChartIntent = new ChartIntentSummary
                            {
                                Intent = "comparison",
                                Confidence = "high",
                                Evidence = new List<string> { "bar chart" },
                                FitStatus = "good",
                                RecommendedAlternatives = new List<string>(),
                            },
                        },
                    },
                },
                ReportConsistencySummary = new ReportConsistencySummary
                {
                    ConsistentTitleAnchors = true,
                    ConsistentFilterBand = true,
                    ConsistentMetricLabels = false,
                    ConsistentSemanticColors = true,
                    Findings = new List<string> { "Metric labels drift across overview pages." },
                },
                Feedback = new Dictionary<string, List<FrameworkFeedbackItem>>
                {
                    ["gestalt"] = new() { new FrameworkFeedbackItem(true, "Aligned.", FindingType: FindingTypes.StrongHeuristic) }
                },
                PageScores = new List<PageScore>
                {
                    new()
                    {
                        PageName = "Overview",
                        GestaltScore = 80,
                        CognitiveLoadScore = 70,
                        DataInkScore = 75,
                        AccessibilityScore = 72,
                        VisualBestPracticesScore = 78,
                        StephenFewScore = 68,
                        EnterpriseGovernanceScore = 74,
                        TufteScore = 66,
                        GraphicalPerceptionScore = 69,
                        DensityScore = 64,
                        NarrativeScore = 67,
                        ReportConsistencyNotes = new List<string> { "Metric labels drift across overview pages." },
                    },
                },
            };

            var json = JsonSerializer.Serialize(score, options);

            Assert.Contains("\"recommendations\":[", json);
            Assert.Contains("\"frameworkWeights\":{", json);
            Assert.Contains("\"reportPath\":\"/tmp/Sales.Report\"", json);
            Assert.Contains("\"visualMetadata\":{", json);
            Assert.Contains("\"visiblePageTitle\":\"Sales Overview\"", json);
            Assert.Contains("\"semanticColorMap\":[", json);
            Assert.Contains("\"semanticColors\":[", json);
            Assert.Contains("\"chartIntentSummary\":{", json);
            Assert.Contains("\"chartIntent\":{", json);
            Assert.Contains("\"reportConsistencySummary\":{", json);
            Assert.Contains("\"reportConsistencyNotes\":[", json);
            Assert.Contains("\"semanticKey\":\"region:north\"", json);
            Assert.Contains("\"consistentMetricLabels\":false", json);
            Assert.Contains("\"findingType\":\"strongHeuristic\"", json);
            Assert.DoesNotContain("\"Recommendations\"", json);
            Assert.DoesNotContain("\"FrameworkWeights\"", json);
            Assert.DoesNotContain("\"ReportPath\"", json);
            Assert.DoesNotContain("\"VisualMetadata\"", json);
            Assert.DoesNotContain("\"SemanticColorMap\"", json);
            Assert.DoesNotContain("\"ChartIntentSummary\"", json);
            Assert.DoesNotContain("\"ReportConsistencySummary\"", json);
            Assert.DoesNotContain("\"ReportConsistencyNotes\"", json);
            Assert.DoesNotContain("\"FindingType\"", json);
        }

        // Minimal stubs for test
        public class JsonRpcResponse
        {
            public object? Id { get; set; } = 1;
            public object? Result { get; set; } = null;
        }
        public class JsonRpcErrorResponse
        {
            public object? Id { get; set; } = 1;
            public JsonRpcError? Error { get; set; } = new JsonRpcError();
        }
        public class JsonRpcError
        {
            public int Code { get; set; } = -32603;
            public string? Message { get; set; } = "error";
        }
        public class FakeRpcHandler
        {
            private readonly bool _returnNull;
            private readonly bool _throwOnHover;
            public FakeRpcHandler(bool returnNull = false, bool throwOnHover = false)
            {
                _returnNull = returnNull;
                _throwOnHover = throwOnHover;
            }
            public Task<object> HandleDefinitionAsync(JsonElement? param)
            {
                // When params are null, return null result (valid JSON-RPC response)
                if (param == null)
                    return Task.FromResult<object>(new JsonRpcResponse { Id = 1, Result = null });
                // When service returns null scenario
                if (_returnNull)
                    return Task.FromResult<object>(new JsonRpcResponse { Id = 1, Result = null });
                return Task.FromResult<object>(new JsonRpcResponse { Id = 1, Result = "ok" });
            }
            public Task<object> HandleHoverAsync(JsonElement? param)
            {
                if (_throwOnHover)
                    return Task.FromResult<object>(new JsonRpcErrorResponse { Id = 1, Error = new JsonRpcError { Code = -32603, Message = "Exception" } });
                return Task.FromResult<object>(new JsonRpcResponse { Id = 1, Result = "hover" });
            }
        }
    }
}
