using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using PowerBIModelingService.Services;
using PowerBIModelingService.Services.Pbir;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class Phase44SemanticProjectionTests
{
    [Fact]
    public void Reader_ProjectsAllSupportedPhase40VisualBindingsIntoTheSharedRoles()
    {
        var request = CreateRequest();
        var generated = new LocalPbirGenerationProviderService().Generate(request);
        Assert.True(generated.Artifact is not null, string.Join("; ", generated.Diagnostics.Select(diagnostic => diagnostic.Message)));

        var directory = Path.Combine(Path.GetTempPath(), $"pbir-phase44-projection-{Guid.NewGuid():N}");
        try
        {
            WriteArtifact(directory, generated.Artifact!);
            var imported = new PbirLocalReportReader().Import(directory);

            Assert.Empty(imported.Diagnostics);
            Assert.NotNull(imported.Performance);
            Assert.True(imported.Performance!.ReaderMilliseconds >= imported.Performance.SemanticProjectionMilliseconds);
            Assert.True(imported.Performance.SemanticProjectionMilliseconds >= 0);
            var expected = request.Visuals.ToDictionary(
                visual => visual.VisualType,
                visual => visual.Bindings.Select(binding => (binding.Role, binding.Kind, binding.Token, binding.Entity, binding.Property)).ToArray());
            foreach (var visual in imported.IrState.Ir!.Visuals)
            {
                var actual = visual.Bindings!.Select(binding => (binding.Role.ToString(), binding.Kind, binding.Token, binding.Entity, binding.Property)).ToArray();
                Assert.Equal(expected[visual.VisualType].Select(binding => (binding.Role.ToString(),
                    Enum.Parse<PbirIntermediateRepresentationBindingKind>(binding.Kind.ToString()),
                    binding.Token, binding.Entity, binding.Property)), actual);
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Reader_PreservesUnsupportedQueryRoleAndReportsItAsUntyped()
    {
        var generated = new LocalPbirGenerationProviderService().Generate(CreateRequest());
        Assert.True(generated.Artifact is not null, string.Join("; ", generated.Diagnostics.Select(diagnostic => diagnostic.Message)));

        var directory = Path.Combine(Path.GetTempPath(), $"pbir-phase44-unsupported-{Guid.NewGuid():N}");
        try
        {
            foreach (var file in generated.Artifact!.Files)
            {
                var content = file.Content;
                if (file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal))
                {
                    var root = JsonNode.Parse(content)!.AsObject();
                    var queryState = root["visual"]!["query"]!["queryState"]!.AsObject();
                    queryState["FutureRole"] = new JsonObject
                    {
                        ["projections"] = new JsonArray(new JsonObject { ["queryRef"] = "Future.Field" })
                    };
                    content = root.ToJsonString();
                }

                var path = Path.Combine(directory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, content);
            }

            var imported = new PbirLocalReportReader().Import(directory);
            var diagnostics = imported.Diagnostics.Where(value => value.Code == "PBIR44-IMPORT-ROLE-001").ToArray();
            Assert.Equal(6, diagnostics.Length);
            Assert.All(diagnostics, diagnostic => Assert.Equal(LocalPbirSemanticProjectionStatus.PreservedButUntyped, diagnostic.ProjectionStatus));
            Assert.DoesNotContain(imported.IrState.Ir!.Visuals.SelectMany(visual => visual.Bindings ?? []), binding => binding.Token == "Future.Field");
            Assert.Contains(imported.IrState.Ir.AuthoringEnvelope!.Items, item => item.OwnerKind == PbirAuthoringOwnerKind.Visual && item.SourceContent!.Contains("FutureRole", StringComparison.Ordinal));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Reader_BlocksInvalidDescriptorKindWithoutDroppingTheEnvelope()
    {
        var generated = new LocalPbirGenerationProviderService().Generate(CreateRequest());
        Assert.True(generated.Artifact is not null, string.Join("; ", generated.Diagnostics.Select(diagnostic => diagnostic.Message)));

        var directory = Path.Combine(Path.GetTempPath(), $"pbir-phase44-invalid-{Guid.NewGuid():N}");
        try
        {
            foreach (var file in generated.Artifact!.Files)
            {
                var content = file.Content;
                if (file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal) && content.Contains("\"visualType\": \"card\"", StringComparison.Ordinal))
                {
                    var root = JsonNode.Parse(content)!.AsObject();
                    var field = root["visual"]!["query"]!["queryState"]!["Fields"]!["projections"]![0]!["field"]!.AsObject();
                    var measure = field["Measure"]!.DeepClone();
                    field.Remove("Measure");
                    field["Column"] = measure;
                    content = root.ToJsonString();
                }

                var path = Path.Combine(directory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, content);
            }

            var imported = new PbirLocalReportReader().Import(directory);
            Assert.Equal(PbirIntermediateRepresentationReadinessState.Blocked, imported.IrState.Readiness);
            Assert.Contains(imported.Diagnostics, diagnostic => diagnostic.Code == "PBIR44-IMPORT-BINDING-002" && diagnostic.ProjectionStatus == LocalPbirSemanticProjectionStatus.Invalid);
            Assert.Contains(imported.IrState.Ir!.AuthoringEnvelope!.Items, item => item.OwnerKind == PbirAuthoringOwnerKind.Visual);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ImportedProjection_MutationAndAnalyzerComparison_UsesSharedIr()
    {
        var generated = new LocalPbirGenerationProviderService().Generate(CreateRequest());
        Assert.True(generated.Artifact is not null, string.Join("; ", generated.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var sourceDirectory = Path.Combine(Path.GetTempPath(), $"pbir-phase44-analyzer-before-{Guid.NewGuid():N}");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"pbir-phase44-analyzer-after-{Guid.NewGuid():N}");
        try
        {
            WriteArtifact(sourceDirectory, generated.Artifact!);
            var imported = new PbirLocalReportReader().Import(sourceDirectory);
            var visual = imported.IrState.Ir!.Visuals.First();
            var changedIr = imported.IrState.Ir with
            {
                Visuals = imported.IrState.Ir.Visuals.Select(value => value.VisualId == visual.VisualId && value.Layout is not null
                    ? value with { Layout = value.Layout with { X = value.Layout.X + 1 } }
                    : value).ToArray()
            };
            var semanticDelta = new PbirSemanticEquivalenceService().Compare(imported.IrState.Ir, changedIr, new HashSet<string> { visual.VisualId });
            Assert.True(semanticDelta.IsEquivalent);
            Assert.Empty(semanticDelta.UnexpectedChanges);

            var resolved = new PbirAuthoringMergeService().Resolve(changedIr);
            CopyDirectory(sourceDirectory, outputDirectory);
            foreach (var document in resolved.Documents)
            {
                var path = Path.Combine(outputDirectory, "definition", document.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, document.Content);
            }

            var scorer = new PbirScoringService(new PbirProjectService(NullLogger<PbirProjectService>.Instance), NullLogger<PbirScoringService>.Instance);
            var before = await scorer.ScoreAsync(sourceDirectory);
            var after = await scorer.ScoreAsync(outputDirectory);
            Assert.NotNull(before);
            Assert.NotNull(after);
            Assert.Equal(before.CompositeScore, after.CompositeScore);
        }
        finally
        {
            if (Directory.Exists(sourceDirectory)) Directory.Delete(sourceDirectory, recursive: true);
            if (Directory.Exists(outputDirectory)) Directory.Delete(outputDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Phase44Pipeline_ReportsNonNegativeStageTimings()
    {
        var generated = new LocalPbirGenerationProviderService().Generate(CreateRequest());
        Assert.True(generated.Artifact is not null, string.Join("; ", generated.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var directory = Path.Combine(Path.GetTempPath(), $"pbir-phase44-timing-{Guid.NewGuid():N}");
        try
        {
            WriteArtifact(directory, generated.Artifact!);
            var import = new PbirLocalReportReader().Import(directory);
            var target = import.IrState.Ir!.Visuals.First();
            var request = new LocalPbirMutationRequest(
                LocalPbirMutationRequestContract.SchemaVersionV1,
                "phase44-timing-mutation",
                directory,
                Path.GetTempPath(),
                "phase44-timing-output",
                [new(LocalPbirMutationOperationKind.ResizeVisual, new(VisualId: target.VisualId), Layout: new(X: 1, Y: null, Width: null, Height: null))]);
            var planner = new PbirMutationPlanner();
            var executor = new PbirMutationExecutor();
            var merge = new PbirAuthoringMergeService();
            var scorer = new PbirScoringService(new PbirProjectService(NullLogger<PbirProjectService>.Instance), NullLogger<PbirScoringService>.Instance);

            var planningTimer = Stopwatch.StartNew();
            var plan = planner.Plan(import, request);
            planningTimer.Stop();
            var executionTimer = Stopwatch.StartNew();
            var execution = executor.Execute(plan);
            executionTimer.Stop();
            var mergeTimer = Stopwatch.StartNew();
            _ = merge.Resolve(execution.IrState.Ir!);
            mergeTimer.Stop();
            var serializationTimer = Stopwatch.StartNew();
            _ = new PbirDeployableSerializerService().CreateArtifacts(
                PbirDeployableSerializerServiceTests.CreateReadyInputs().IrState,
                PbirDeployableSerializerServiceTests.CreateReadyInputs().SerializerRequest,
                PbirDeployableSerializerServiceTests.CreateReadyInputs().DeployableRequest);
            serializationTimer.Stop();
            var analyzerTimer = Stopwatch.StartNew();
            _ = await scorer.ScoreAsync(directory);
            analyzerTimer.Stop();

            Assert.True(import.Performance!.ReaderMilliseconds >= import.Performance.SemanticProjectionMilliseconds);
            Assert.True(planningTimer.ElapsedMilliseconds >= 0);
            Assert.True(executionTimer.ElapsedMilliseconds >= 0);
            Assert.True(mergeTimer.ElapsedMilliseconds >= 0);
            Assert.True(serializationTimer.ElapsedMilliseconds >= 0);
            Assert.True(analyzerTimer.ElapsedMilliseconds >= 0);
            Console.WriteLine($"PHASE44_TIMING readerMs={import.Performance.ReaderMilliseconds} projectionMs={import.Performance.SemanticProjectionMilliseconds} mergeMs={mergeTimer.ElapsedMilliseconds} planningMs={planningTimer.ElapsedMilliseconds} executionMs={executionTimer.ElapsedMilliseconds} serializationMs={serializationTimer.ElapsedMilliseconds} analyzerMs={analyzerTimer.ElapsedMilliseconds}");
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void DescriptorCatalog_ResolvesSlicerCategoryThroughTheSharedRoleVocabulary()
    {
        var mapping = Assert.Single(Phase40VisualDescriptorCatalog.ResolveImportedRoles("slicer", "Category"));

        Assert.Equal(LocalPbirGenerationBindingRole.Category, mapping.BindingRole);
        Assert.Equal(LocalPbirGenerationBindingKind.Dimension, mapping.Kind);
    }

    [Fact]
    public void SemanticEquivalence_UsesCanonicalSharedBindingsAndExpectedVisualChanges()
    {
        var before = new PbirIntermediateRepresentation(
            new("before", PbirIntermediateRepresentationContract.SchemaVersionV1, DateTime.UnixEpoch),
            new("generation", "generation"),
            [],
            [new("visual", "page", "pieChart", "", "", [], 0, null, [
                new("binding", PbirIntermediateRepresentationBindingRole.Legend, PbirIntermediateRepresentationBindingKind.Dimension, "Region", "Sales", "Region", 0)])],
            [],
            new("page", [], [], []), new([], [], [], []), new([], [], []), new([], []), new("", "", ""));
        var after = before with
        {
            Visuals = [before.Visuals[0] with
            {
                Bindings = [before.Visuals[0].Bindings![0] with { Token = "Segment" }]
            }]
        };

        var result = new PbirSemanticEquivalenceService().Compare(before, after, new HashSet<string> { "visual" });

        Assert.False(result.IsEquivalent);
        Assert.Contains("visual/bindings", result.ExpectedChanges);
        Assert.Empty(result.UnexpectedChanges);
    }

    private static LocalPbirGenerationRequestV5 CreateRequest() =>
        new(
            LocalPbirGenerationRequestContract.SchemaVersionV5,
            "phase44-semantic-projection",
            "Phase44",
            "Sales.SemanticModel",
            new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc),
            Path.GetTempPath(),
            "phase44-projection",
            [new("overview", "Overview", 0)],
            [
                new("card", "overview", "card", 0, new(0, 0, 320, 160), [
                    new("card-value", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")]),
                new("table", "overview", "table", 1, new(0, 176, 640, 360), [
                    new("table-region", "Region", LocalPbirGenerationBindingKind.Dimension, LocalPbirGenerationBindingRole.Value, "Sales", "Region"),
                    new("table-value", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")]),
                Chart("column", "clusteredColumnChart", LocalPbirGenerationBindingRole.Category),
                Chart("line", "lineChart", LocalPbirGenerationBindingRole.Category) with
                {
                    Bindings = [
                        .. Chart("line-base", "lineChart", LocalPbirGenerationBindingRole.Category).Bindings,
                        new("line-series", "Segment", LocalPbirGenerationBindingKind.Dimension, LocalPbirGenerationBindingRole.Series, "Sales", "Segment")]
                },
                Chart("bar", "barChart", LocalPbirGenerationBindingRole.Category),
                Chart("pie", "pieChart", LocalPbirGenerationBindingRole.Legend)
            ]);

    private static LocalPbirGenerationVisual Chart(string id, string type, LocalPbirGenerationBindingRole categoryRole) =>
        new(id, "overview", type, id == "column" ? 2 : id == "line" ? 3 : id == "bar" ? 4 : 5,
            new(id == "column" ? 0 : id == "line" ? 320 : id == "bar" ? 640 : 960, 560, 300, 160), [
                new($"{id}-category", "Region", LocalPbirGenerationBindingKind.Dimension, categoryRole, "Sales", "Region"),
                new($"{id}-value", "Revenue", LocalPbirGenerationBindingKind.Measure, LocalPbirGenerationBindingRole.Value, "Sales", "Revenue")]);

    private static void WriteArtifact(string directory, PbirDeployableArtifact artifact)
    {
        foreach (var file in artifact.Files)
        {
            var path = Path.Combine(directory, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.Content);
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
