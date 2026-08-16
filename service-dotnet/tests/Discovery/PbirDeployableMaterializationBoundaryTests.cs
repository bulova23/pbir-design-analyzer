using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirDeployableMaterializationBoundaryTests
{
    [Fact]
    public void Services_ExposeOnlyApprovedLocalMaterializationInputs()
    {
        AssertMethod(
            typeof(PbirDeployableMaterializationPreviewService), "CreatePreview",
            typeof(PbirDeployableArtifact), typeof(PbirDeployableManifest),
            typeof(PbirDeployableMaterializationPreviewRequest), typeof(string));
        AssertMethod(
            typeof(PbirDeployableMaterializationApplyService), "Apply",
            typeof(PbirDeployableArtifact), typeof(PbirDeployableManifest),
            typeof(PbirDeployableMaterializationPreview), typeof(PbirDeployableMaterializationApplyRequest), typeof(string), typeof(CancellationToken));
        AssertMethod(
            typeof(PbirDeployableMaterializationRollbackService), "Rollback",
            typeof(PbirDeployableMaterializationRollbackRequest), typeof(string));
    }

    [Fact]
    public void Services_HaveNoPreviewWriterOrExternalExecutionDependencies()
    {
        var serviceTypes = new[]
        {
            typeof(PbirDeployableMaterializationPreviewService),
            typeof(PbirDeployableMaterializationApplyService),
            typeof(PbirDeployableMaterializationRollbackService),
            typeof(PbirDeployableMaterializationTransactionStore),
            typeof(PbirDeployableMaterializationSafetyGate)
        };
        var forbidden = new[]
        {
            typeof(PbirLocalPreviewFileWriterService),
            typeof(PbirLocalPreviewFileWriterSafetyGate),
            typeof(PbirLocalPreviewFileContentFactory),
            typeof(PbirLocalWriteManifest),
            typeof(HttpClient),
            typeof(Process)
        };

        foreach (var serviceType in serviceTypes)
        {
            var exposedTypes = serviceType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))
                .Concat(serviceType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(field => field.FieldType))
                .ToArray();
            foreach (var forbiddenType in forbidden)
            {
                Assert.DoesNotContain(forbiddenType, exposedTypes);
            }
        }
    }

    [Fact]
    public void RuntimeSchemaGate_UsesEmbeddedPinnedSchemasWithoutNetwork()
    {
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var diagnostics = new PbirDeployableMaterializationSchemaValidator().Validate(inputs.Artifact);

        Assert.Empty(diagnostics);
        Assert.Equal(8, typeof(PbirDeployableMaterializationSchemaValidator).Assembly
            .GetManifestResourceNames()
            .Count(name => name.Contains("PbirSchemas", StringComparison.Ordinal) && name.EndsWith("schema.json", StringComparison.Ordinal)));
    }

    [Fact]
    public void RuntimeSchemaGate_RejectsDocumentOutsidePinnedSchema()
    {
        var inputs = PbirDeployableMaterializationPreviewServiceTests.CreateInputs("Sales.Report");
        var definition = Assert.Single(inputs.Artifact.Files, file => file.RelativePath == "definition.pbir");
        var invalidDefinition = definition with
        {
            Content = definition.Content.TrimEnd()![..^1] + ",\"unexpected\":true}"
        };
        var artifact = inputs.Artifact with
        {
            Files = inputs.Artifact.Files.Select(file => file.RelativePath == "definition.pbir" ? invalidDefinition : file).ToArray()
        };

        var diagnostics = new PbirDeployableMaterializationSchemaValidator().Validate(artifact);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == "PBIRMAT-SCHEMA-002" && diagnostic.Path == "definition.pbir");
    }

    private static void AssertMethod(Type type, string name, params Type[] parameters)
    {
        var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        Assert.Equal(parameters, method!.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
    }
}
