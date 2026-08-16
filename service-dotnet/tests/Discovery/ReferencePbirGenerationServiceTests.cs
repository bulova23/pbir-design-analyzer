using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class ReferencePbirGenerationServiceTests
{
    [Fact(DisplayName = "Reference PBIR generator creates deterministic local reference output with stable hashes and preserved lineage")]
    public void CreateReferenceOutput_ReadyManifest_CreatesDeterministicLocalArtifacts()
    {
        var inputs = CreateReadyReferenceInputs();
        var generatedUtc = DateTimeOffset.Parse("2026-06-26T13:00:00+00:00");
        var service = new ReferencePbirGenerationService();

        var first = service.CreateReferenceOutput(
            inputs.ManifestState,
            inputs.CertificationState,
            inputs.SpecificationState,
            ReferenceGenerationOptions.Default,
            generatedUtc);
        var second = service.CreateReferenceOutput(
            inputs.ManifestState,
            inputs.CertificationState,
            inputs.SpecificationState,
            ReferenceGenerationOptions.Default,
            generatedUtc);

        Assert.Equal(ReferenceGenerationReadinessState.Generated, first.Readiness);
        Assert.True(first.Safety.IsAllowed);
        Assert.Empty(first.Safety.Reasons);
        Assert.NotNull(first.Output);
        Assert.Equal(ReferenceGenerationOutputContract.SchemaVersionV1, first.Output!.SchemaVersion);
        Assert.Equal(ReferencePbirGeneratorContract.SchemaVersionV1, first.Output.Metadata.GeneratorSchemaVersion);
        Assert.Equal("referenceGenerationOutput:generationManifest:planningOutcome:designPackage:executive-summary", first.Output.Metadata.OutputId);
        Assert.Equal("localDeterministicReference", first.Output.Metadata.GenerationMode);
        Assert.Equal(generatedUtc.UtcDateTime, first.Output.Metadata.GeneratedUtc);
        Assert.True(first.Output.Metadata.DryRun);
        Assert.True(first.Output.Metadata.LocalOutputOnly);
        Assert.False(first.Output.Metadata.DeploymentEnabled);
        Assert.False(first.Output.Metadata.ProviderInvocationEnabled);
        Assert.False(first.Output.Metadata.MicrosoftApiInvocationEnabled);
        Assert.False(first.Output.Metadata.CliInvocationEnabled);

        Assert.Equal(inputs.ManifestState.Manifest!.Metadata.ManifestId, first.Output.SourceReferences.GenerationManifestRef);
        Assert.Equal(inputs.SpecificationState.Specification!.SpecificationId, first.Output.SourceReferences.PbirGenerationSpecificationRef);
        Assert.Equal(inputs.CertificationState.Certification!.CertificationId, first.Output.SourceReferences.ArchitectureCertificationRef);
        Assert.All(
            inputs.ManifestState.Manifest.Lineage.ImmutableUpstreamLineage,
            reference => Assert.Contains(reference, first.Output.Lineage.ImmutableLineage));
        Assert.Contains(first.Output.Metadata.OutputId, first.Output.Lineage.ImmutableLineage);
        Assert.Equal("pbirIr:generationManifest:planningOutcome:designPackage:executive-summary", first.Output.CanonicalIr.PbirIrRef);
        Assert.Equal(PbirIntermediateRepresentationContract.SchemaVersionV1, first.Output.CanonicalIr.SchemaVersion);
        Assert.Equal(64, first.Output.CanonicalIr.InputHash.Length);
        Assert.Equal(64, first.Output.CanonicalIr.ContentHash.Length);
        Assert.Equal(64, first.Output.CanonicalIr.LineageHash.Length);
        Assert.Contains(first.Output.CanonicalIr.PbirIrRef, first.Output.CanonicalIr.ImmutableIrLineage);
        Assert.Equal(
            new[]
            {
                "reference-pbir-generator/v1/canonical-pbir-ir.json",
                "reference-pbir-generator/v1/lineage.md",
                "reference-pbir-generator/v1/manifest-summary.json"
            },
            first.Output.GeneratedFiles.Select(file => file.RelativePath).OrderBy(path => path, StringComparer.Ordinal).ToArray());
        var irFile = first.Output.GeneratedFiles.Single(file => file.RelativePath == "reference-pbir-generator/v1/canonical-pbir-ir.json");
        var ir = JsonSerializer.Deserialize<PbirIntermediateRepresentation>(irFile.Content)!;
        Assert.Equal(PbirIntermediateRepresentationContract.SchemaVersionV1, ir.Metadata.SchemaVersion);
        Assert.Equal(first.Output.CanonicalIr.PbirIrRef, ir.Metadata.IrId);
        Assert.Equal(first.Output.CanonicalIr.ContentHash, ir.Hashes.ContentHash);
        Assert.Equal(first.Output.CanonicalIr.ImmutableIrLineage, ir.Lineage.ImmutableLineage);
        Assert.All(first.Output.GeneratedFiles, file =>
        {
            Assert.False(Path.IsPathRooted(file.RelativePath));
            Assert.DoesNotContain("://", file.RelativePath, StringComparison.Ordinal);
            Assert.StartsWith("reference-pbir-generator/v1/", file.RelativePath, StringComparison.Ordinal);
            Assert.Equal(ComputeSha256(file.Content), file.HashSha256);
            Assert.Equal(Encoding.UTF8.GetByteCount(file.Content), file.ByteLength);
        });
        Assert.Equal(ComputeFileSetHash(first.Output.GeneratedFiles), first.Output.Hashes.FileSetHash);
        Assert.Equal(ComputeInputHash(inputs.ManifestState.Manifest, inputs.SpecificationState.Specification), first.Output.Hashes.InputHash);
        Assert.Equal(Serialize(first.Output), Serialize(second.Output));
    }

    [Fact(DisplayName = "Reference generation safety gate rejects non-certified architecture, missing manifest, deployment, provider invocation, Microsoft API, CLI, and network requests")]
    public void CreateReferenceOutput_UnsafeInputs_FailsClosed()
    {
        var inputs = CreateReadyReferenceInputs();
        var service = new ReferencePbirGenerationService();
        var generatedUtc = DateTimeOffset.Parse("2026-06-26T13:00:00+00:00");
        var missingManifest = new GenerationManifestState(
            Manifest: null,
            Validation: new GenerationManifestValidationResult(new GenerationManifestValidationDiagnostics(
                MissingRequiredSections: ["manifest"],
                MissingRequiredFields: [],
                InvalidReferences: [],
                UnsupportedSchemaVersions: [],
                LineageIntegrityFailures: [],
                ReadinessConsistencyFailures: [],
                ProviderCompatibilityFailures: [],
                GenerationSpecificationCompletenessFailures: [],
                BoundaryViolations: [])),
            Readiness: GenerationManifestReadinessState.Incomplete);

        AssertRejected(
            service.CreateReferenceOutput(
                inputs.ManifestState,
                new ArchitectureCertificationState(null, null, null),
                inputs.SpecificationState,
                ReferenceGenerationOptions.Default,
                generatedUtc),
            "architecture certification must exist and be readyForExecutionImplementation.");
        AssertRejected(
            service.CreateReferenceOutput(
                missingManifest,
                inputs.CertificationState,
                inputs.SpecificationState,
                ReferenceGenerationOptions.Default,
                generatedUtc),
            "generation manifest must exist.");
        AssertRejected(
            service.CreateReferenceOutput(
                inputs.ManifestState,
                inputs.CertificationState,
                inputs.SpecificationState,
                ReferenceGenerationOptions.Default with { DeploymentRequested = true },
                generatedUtc),
            "deployment requests are not allowed.");
        AssertRejected(
            service.CreateReferenceOutput(
                inputs.ManifestState,
                inputs.CertificationState,
                inputs.SpecificationState,
                ReferenceGenerationOptions.Default with { ProviderInvocationRequested = true },
                generatedUtc),
            "provider invocation requests are not allowed.");
        AssertRejected(
            service.CreateReferenceOutput(
                inputs.ManifestState,
                inputs.CertificationState,
                inputs.SpecificationState,
                ReferenceGenerationOptions.Default with { MicrosoftApiRequested = true },
                generatedUtc),
            "Microsoft API requests are not allowed.");
        AssertRejected(
            service.CreateReferenceOutput(
                inputs.ManifestState,
                inputs.CertificationState,
                inputs.SpecificationState,
                ReferenceGenerationOptions.Default with { CliRequested = true },
                generatedUtc),
            "CLI requests are not allowed.");
        AssertRejected(
            service.CreateReferenceOutput(
                inputs.ManifestState,
                inputs.CertificationState,
                inputs.SpecificationState,
                ReferenceGenerationOptions.Default with { NetworkAccessRequested = true },
                generatedUtc),
            "network access requests are not allowed.");
    }

    [Fact(DisplayName = "Reference generator validates PBIR specification readiness before creating output")]
    public void CreateReferenceOutput_IncompletePbirSpecification_FailsClosed()
    {
        var inputs = CreateReadyReferenceInputs();
        var incompleteSpecification = inputs.SpecificationState with
        {
            Readiness = PbirGenerationSpecificationReadinessState.Incomplete,
            AcceptsGenerationProvider = false
        };
        var service = new ReferencePbirGenerationService();

        var state = service.CreateReferenceOutput(
            inputs.ManifestState,
            inputs.CertificationState,
            incompleteSpecification,
            ReferenceGenerationOptions.Default,
            DateTimeOffset.Parse("2026-06-26T13:00:00+00:00"));

        AssertRejected(state, "PBIR generation specification must be readyForGenerationProvider.");
    }

    [Fact(DisplayName = "Reference generator remains local and deterministic with no Microsoft Skills execution, provider invocation, deployment, CLI, API, or network surface")]
    public void ReferenceGeneratorBoundary_RemainsLocalDeterministicPrototypeOnly()
    {
        var forbiddenTokens = new[]
        {
            "InvokeProvider",
            "InvokeMicrosoftApi",
            "InvokeApi",
            "InvokeCli",
            "Deploy",
            "RunSkill",
            "PublishArtifact",
            "HttpClient",
            "WebRequest",
            "Socket"
        };
        Type[] types =
        [
            typeof(IReferenceGenerationProvider),
            typeof(ReferencePbirGenerationService),
            typeof(ReferenceGenerationSafetyGate),
            typeof(ReferenceGenerationOutput),
            typeof(ReferenceGeneratedFile),
            typeof(ReferenceGenerationState)
        ];

        foreach (var type in types)
        {
            Assert.DoesNotContain(forbiddenTokens, token => type.Name.Contains(token, StringComparison.OrdinalIgnoreCase));

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                Assert.DoesNotContain(forbiddenTokens, token => method.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static void AssertRejected(ReferenceGenerationState state, string expectedReason)
    {
        Assert.Equal(ReferenceGenerationReadinessState.Rejected, state.Readiness);
        Assert.False(state.Safety.IsAllowed);
        Assert.Null(state.Output);
        Assert.Contains(expectedReason, state.Safety.Reasons);
    }

    private static (
        PbirGenerationSpecificationState SpecificationState,
        GenerationManifestState ManifestState,
        ArchitectureCertificationState CertificationState) CreateReadyReferenceInputs()
    {
        var generationInputs = GenerationManifestServiceTests.CreateReadyInputs();
        var createdUtc = DateTimeOffset.Parse("2026-06-26T12:45:00+00:00");
        var manifestState = new GenerationManifestService().CreateManifestState(
            generationInputs.Planning,
            generationInputs.SpecificationState,
            generationInputs.ProviderState,
            generationInputs.ExecutionPlanningState,
            generationInputs.RuntimeProviderState,
            generationInputs.MicrosoftRuntimeState,
            createdUtc);
        var certificationState = new ArchitectureReadinessCertificationService().Certify(
            new ArchitectureValidationService().Validate(
                GenerationRequestFrameworkServiceTestsAccessor.CreateValidPackage(),
                createdUtc));

        return (
            generationInputs.SpecificationState,
            manifestState,
            certificationState);
    }

    private static string ComputeInputHash(GenerationManifest manifest, PbirGenerationSpecification specification)
    {
        return ComputeSha256(Serialize(new
        {
            manifest,
            specification
        }));
    }

    private static string ComputeFileSetHash(IReadOnlyList<ReferenceGeneratedFile> files)
    {
        var material = string.Join(
            "\n",
            files
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => $"{file.RelativePath}:{file.HashSha256}:{file.ByteLength}"));

        return ComputeSha256(material);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}
