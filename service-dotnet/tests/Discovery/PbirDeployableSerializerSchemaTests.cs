using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using Json.Schema;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirDeployableSerializerSchemaTests
{
    private static readonly string FixtureRoot = Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "PbirSchemas");
    private static readonly Lazy<IReadOnlyDictionary<string, JsonSchema>> SchemasById =
        new(LoadSchemasById);

    [Fact(DisplayName = "Modern PBIR serializer locks the approved Microsoft schema and format versions")]
    public void SchemaLock_UsesApprovedVersions()
    {
        Assert.Equal("2.0.0", PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion);
        Assert.Equal("1.0.0", PbirDeployableSchemaLock.DefinitionSchemaVersion);
        Assert.Equal("4.0", PbirDeployableSchemaLock.PbirFileFormatVersion);
        Assert.Equal("1.0.0", PbirDeployableSchemaLock.ReportDefinitionVersion);
    }

    [Fact(DisplayName = "Pinned modern PBIR schemas parse offline and every relative reference resolves locally")]
    public void SchemaFixtures_AreCompleteAndOffline()
    {
        var fixtureFiles = Directory
            .GetFiles(FixtureRoot, "schema.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(8, fixtureFiles.Length);
        Assert.Equal(8, SchemasById.Value.Count);

        foreach (var fixtureFile in fixtureFiles)
        {
            var content = File.ReadAllText(fixtureFile);
            var document = JsonNode.Parse(content);

            Assert.NotNull(document);

            foreach (var reference in FindReferences(document!))
            {
                Assert.False(
                    Uri.TryCreate(reference, UriKind.Absolute, out var absolute) &&
                    absolute.Scheme is "http" or "https",
                    $"Network schema reference is not allowed: {reference}");

                if (reference.StartsWith('#'))
                {
                    continue;
                }

                var relativePath = reference.Split('#', 2)[0];
                var resolvedPath = Path.GetFullPath(
                    Path.Combine(Path.GetDirectoryName(fixtureFile)!, relativePath));

                Assert.StartsWith(Path.GetFullPath(FixtureRoot), resolvedPath, StringComparison.Ordinal);
                Assert.True(File.Exists(resolvedPath), $"Missing local schema reference: {resolvedPath}");
            }
        }
    }

    [Fact(DisplayName = "Pinned modern PBIR schema fixtures have the reviewed exact bytes")]
    public void SchemaFixtures_HashesArePinned()
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["fabric/item/report/definition/formattingObjectDefinitions/1.0.0/schema.json"] =
                "1aaabab101bad35ac9fa28e5e0624512416d5c73b877d8dae5b798efc38c6974",
            ["fabric/item/report/definition/page/1.0.0/schema.json"] =
                "400bfc78e20d980e589d3a4d8e8890e9121e0a0356360ead370f5858f1b6d603",
            ["fabric/item/report/definition/pagesMetadata/1.0.0/schema.json"] =
                "e8a8803daee6d09927c5f4c303bef10cc9a70391db2960e36bba2055bde057ff",
            ["fabric/item/report/definition/report/1.0.0/schema.json"] =
                "d73920133232bc5e8531d5e456d050b9e33469004509bbaa0de1a5a15c814319",
            ["fabric/item/report/definition/semanticQuery/1.0.0/schema.json"] =
                "44ce4c731fbad24461af3735ba94483788e7769e0df23e8b49c387f90ba5b0df",
            ["fabric/item/report/definition/versionMetadata/1.0.0/schema.json"] =
                "06f630c6741ae88dff0d80442295384ef38dca662811ef599a9365b144b3f0ac",
            ["fabric/item/report/definition/visualContainer/1.0.0/schema.json"] =
                "ebac0a74b3c4f1fd5a3497856a9a454eebd97b77ec22ff5c78765f919c8ff69b",
            ["fabric/item/report/definitionProperties/2.0.0/schema.json"] =
                "1ea3450d1321a295abca6a9507548b4e2ec99ab11d3d4526aa73713650296ed0"
        };

        foreach (var pair in expected)
        {
            var bytes = File.ReadAllBytes(Path.Combine(FixtureRoot, pair.Key));
            var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.Equal(pair.Value, actual);
        }
    }

    [Fact(DisplayName = "Every emitted modern PBIR document conforms to its pinned official schema offline")]
    public void EmittedDocuments_ConformToPinnedSchemasOffline()
    {
        var schemasById = SchemasById.Value;

        SchemaRegistry.Global.Fetch = (uri, _) =>
            throw new InvalidOperationException($"Network schema resolution is forbidden: {uri}");

        var inputs = PbirDeployableSerializerServiceTests.CreateReadyInputs();
        var state = new PbirDeployableSerializerService().CreateArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);

        Assert.Equal(PbirDeployableSerializerReadinessState.Serialized, state.Readiness);
        Assert.NotNull(state.Artifact);

        foreach (var file in state.Artifact!.Files)
        {
            Assert.True(schemasById.TryGetValue(file.SchemaUrl, out var schema), file.SchemaUrl);
            using var instance = JsonDocument.Parse(file.Content);
            var results = schema!.Evaluate(
                instance.RootElement,
                new EvaluationOptions
                {
                    OutputFormat = OutputFormat.Hierarchical,
                    RequireFormatValidation = true
                });

            Assert.True(results.IsValid, $"{file.RelativePath}: {results}");
        }
    }

    private static IReadOnlyDictionary<string, JsonSchema> LoadSchemasById()
    {
        return Directory
            .GetFiles(FixtureRoot, "schema.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path =>
            {
                var content = File.ReadAllText(path);
                var id = JsonNode.Parse(content)!["$id"]!.GetValue<string>();
                return new KeyValuePair<string, JsonSchema>(id, JsonSchema.FromText(content));
            })
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal);
    }

    private static IEnumerable<string> FindReferences(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject)
            {
                if (property.Key == "$ref" && property.Value is JsonValue value)
                {
                    yield return value.GetValue<string>();
                }

                if (property.Value is not null)
                {
                    foreach (var reference in FindReferences(property.Value))
                    {
                        yield return reference;
                    }
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is null)
                {
                    continue;
                }

                foreach (var reference in FindReferences(item))
                {
                    yield return reference;
                }
            }
        }
    }
}
