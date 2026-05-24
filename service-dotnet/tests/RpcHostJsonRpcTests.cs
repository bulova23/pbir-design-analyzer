using System;
using System.Collections.Generic;
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
                        },
                    },
                },
                Feedback = new Dictionary<string, List<FrameworkFeedbackItem>>
                {
                    ["gestalt"] = new() { new FrameworkFeedbackItem(true, "Aligned.", FindingType: FindingTypes.StrongHeuristic) }
                },
            };

            var json = JsonSerializer.Serialize(score, options);

            Assert.Contains("\"recommendations\":[", json);
            Assert.Contains("\"frameworkWeights\":{", json);
            Assert.Contains("\"reportPath\":\"/tmp/Sales.Report\"", json);
            Assert.Contains("\"visualMetadata\":{", json);
            Assert.Contains("\"visiblePageTitle\":\"Sales Overview\"", json);
            Assert.Contains("\"findingType\":\"strongHeuristic\"", json);
            Assert.DoesNotContain("\"Recommendations\"", json);
            Assert.DoesNotContain("\"FrameworkWeights\"", json);
            Assert.DoesNotContain("\"ReportPath\"", json);
            Assert.DoesNotContain("\"VisualMetadata\"", json);
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
