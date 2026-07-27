using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PowerBIModelingService.Services.Discovery;
using PowerBIModelingService.Services.Discovery.Models;
using Xunit;

namespace PowerBIModelingService.Tests.Discovery;

public sealed class PbirDeployableSerializerServiceTests
{
    [Fact(DisplayName = "Deployable serializer exposes the complete versioned Phase 29 contract family")]
    public void Contracts_ExposeVersionedPhase29Inventory()
    {
        Assert.Equal("pbir-deployable-serializer-request/v1", PbirDeployableSerializerRequestContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-artifact/v1", PbirDeployableArtifactContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-manifest/v1", PbirDeployableManifestContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-validation/v1", PbirDeployableValidationContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-readiness/v1", PbirDeployableReadinessContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-diagnostics/v1", PbirDeployableDiagnosticsContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-lineage/v1", PbirDeployableLineageContract.SchemaVersionV1);
        Assert.Equal("pbir-deployable-hashes/v1", PbirDeployableHashesContract.SchemaVersionV1);
    }

    [Fact(DisplayName = "Deployable serializer preflight accepts only complete explicit modern PBIR inputs")]
    public void SafetyGate_CompleteExplicitRequest_IsReadyForSerialization()
    {
        var inputs = CreateReadyInputs();

        var result = new PbirDeployableSerializerSafetyGate().Validate(
            inputs.IrState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);

        Assert.True(result.IsValid);
        Assert.Equal(PbirDeployableSerializerReadinessState.ReadyForSerialization, result.Readiness);
        Assert.False(result.Diagnostics.HasFailures);
    }

    [Theory(DisplayName = "Deployable serializer rejects unsafe dataset paths without partial output")]
    [InlineData("/absolute/report.SemanticModel")]
    [InlineData("C:/report.SemanticModel")]
    [InlineData("../report.SemanticModel")]
    [InlineData("reports//report.SemanticModel")]
    [InlineData("reports\\report.SemanticModel")]
    [InlineData("https://example.test/report.SemanticModel")]
    [InlineData("reports/./report.SemanticModel")]
    public void SafetyGate_UnsafeDatasetPath_IsBlocked(string path)
    {
        var inputs = CreateReadyInputs();
        var request = inputs.DeployableRequest with
        {
            DatasetReference = new PbirDatasetReference(
                ByPath: new PbirDatasetReferenceByPath(path))
        };

        var result = new PbirDeployableSerializerSafetyGate().Validate(
            inputs.IrState,
            inputs.SerializerRequest,
            request);

        Assert.False(result.IsValid);
        Assert.Equal(PbirDeployableSerializerReadinessState.Blocked, result.Readiness);
        Assert.Contains(result.Diagnostics.InvalidPaths, value => value.Code == "PBIRDEPLOY-PATH-001");
    }

    [Theory(DisplayName = "Deployable serializer rejects every execution authority flag without partial output")]
    [MemberData(nameof(ExecutionPolicyMutations))]
    public void SafetyGate_ExecutionAuthority_IsBlocked(object policyObject)
    {
        var inputs = CreateReadyInputs();
        var request = inputs.DeployableRequest with
        {
            ExecutionPolicy = Assert.IsType<PbirDeployableExecutionPolicy>(policyObject)
        };

        var result = new PbirDeployableSerializerSafetyGate().Validate(
            inputs.IrState,
            inputs.SerializerRequest,
            request);

        Assert.False(result.IsValid);
        Assert.Equal(PbirDeployableSerializerReadinessState.Blocked, result.Readiness);
        Assert.Contains(result.Diagnostics.BoundaryViolations, value => value.Code == "PBIRDEPLOY-BOUNDARY-001");
    }

    [Fact(DisplayName = "Semantic model inventory canonicalization has exact bytes, ordering, and SHA-256")]
    public void CanonicalJson_SemanticInventory_IsExact()
    {
        var inventory = CreateReadyInputs().DeployableRequest.SemanticModelInventory with
        {
            Entries = CreateReadyInputs().DeployableRequest.SemanticModelInventory.Entries.Reverse().ToArray()
        };
        var canonicalJson = new PbirDeployableSerializerCanonicalJson();

        var bytes = canonicalJson.SerializeSemanticModelInventory(inventory);
        var content = Encoding.UTF8.GetString(bytes);

        Assert.Equal(
            """{"schemaVersion":"pbir-semantic-model-inventory/v1","inventoryRef":"modelInventory:sales","entries":[{"entryId":"column:Date.Month","token":"Month","entity":"Date","property":"Month","kind":"column"},{"entryId":"measure:Sales.Revenue","token":"Revenue","entity":"Sales","property":"Revenue","kind":"measure"}]}""",
            content);
        Assert.Equal(310, bytes.Length);
        Assert.Equal(
            "bc4f58184e62028614f7867e3927c5591f1b55c0104b3f70a9d85ed4e9516d29",
            canonicalJson.ComputeSha256(bytes));
        Assert.False(content.EndsWith('\n'));
    }

    [Fact(DisplayName = "Deterministic identities and the six-slot layout use the exact approved profile")]
    public void CanonicalJson_IdentityAndLayout_AreStable()
    {
        var canonicalJson = new PbirDeployableSerializerCanonicalJson();
        var firstPage = canonicalJson.CreatePageIdentity("pbirIr:phase29-fixture", "page:overview");
        var secondPage = canonicalJson.CreatePageIdentity("pbirIr:phase29-fixture", "page:overview");
        var visual = canonicalJson.CreateVisualIdentity(
            "pbirIr:phase29-fixture",
            "page:overview",
            "visual:revenue-card");

        Assert.Equal(firstPage, secondPage);
        Assert.Matches(new Regex("^[0-9a-f]{20}$", RegexOptions.CultureInvariant), firstPage);
        Assert.Matches(new Regex("^[0-9a-f]{20}$", RegexOptions.CultureInvariant), visual);
        Assert.Equal(
            new[]
            {
                new PbirDeployableLayoutSlot(1, 24, 24, 400, 328, 0, 0),
                new PbirDeployableLayoutSlot(2, 440, 24, 400, 328, 1000, 1000),
                new PbirDeployableLayoutSlot(3, 856, 24, 400, 328, 2000, 2000),
                new PbirDeployableLayoutSlot(4, 24, 368, 400, 328, 3000, 3000),
                new PbirDeployableLayoutSlot(5, 440, 368, 400, 328, 4000, 4000),
                new PbirDeployableLayoutSlot(6, 856, 368, 400, 328, 5000, 5000)
            },
            Enumerable.Range(1, 6).Select(canonicalJson.GetLayoutSlot).ToArray());
    }

    [Fact(DisplayName = "Modern PBIR serializer emits one coherent deterministic in-memory artifact inventory")]
    public void CreateArtifacts_ReadyInputs_EmitsCoherentDeterministicInventory()
    {
        var inputs = CreateReadyInputs();
        var service = new PbirDeployableSerializerService();

        var first = service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, inputs.DeployableRequest);
        var second = service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, inputs.DeployableRequest);

        Assert.Equal(PbirDeployableSerializerReadinessState.Serialized, first.Readiness);
        Assert.True(first.Validation.IsValid);
        Assert.NotNull(first.Artifact);
        Assert.NotNull(first.Manifest);
        Assert.Equal(Serialize(first.Artifact), Serialize(second.Artifact));
        Assert.Equal(Serialize(first.Manifest), Serialize(second.Manifest));

        var files = first.Artifact!.Files;
        Assert.Equal(9, files.Count);
        Assert.Equal(files.OrderBy(file => file.RelativePath, StringComparer.Ordinal), files);
        Assert.Contains(files, file => file.RelativePath == "definition.pbir");
        Assert.Contains(files, file => file.RelativePath == "definition/version.json");
        Assert.Contains(files, file => file.RelativePath == "definition/report.json");
        Assert.Contains(files, file => file.RelativePath == "definition/pages/pages.json");
        Assert.DoesNotContain(files, file => file.RelativePath == "report.json");
        Assert.DoesNotContain(files, file => file.RelativePath.EndsWith(".pbip", StringComparison.Ordinal));
        Assert.DoesNotContain(files, file => file.RelativePath is "model.bim" or "definition.pbism" or ".platform");

        foreach (var file in files)
        {
            Assert.Equal("application/json", file.ContentType);
            Assert.Equal(Encoding.UTF8.GetByteCount(file.Content), file.ByteLength);
            Assert.Matches("^[0-9a-f]{64}$", file.HashSha256);
            Assert.EndsWith("\n", file.Content, StringComparison.Ordinal);
            Assert.DoesNotContain("\r", file.Content, StringComparison.Ordinal);
        }

        Assert.Matches("^[0-9a-f]{64}$", first.Artifact.Hashes.InputHash);
        Assert.Matches("^[0-9a-f]{64}$", first.Artifact.Hashes.FileSetHash);
        Assert.Matches("^[0-9a-f]{64}$", first.Artifact.Hashes.ArtifactHash);
        Assert.Matches("^[0-9a-f]{64}$", first.Manifest!.Hashes.ManifestHash);
        Assert.Matches("^[0-9a-f]{64}$", first.Artifact.Lineage.LineageHash);
        Assert.Equal(
            inputs.DeployableRequest.RequestId,
            first.Artifact.Lineage.DeployableSerializerRequestRef);
    }

    [Fact(DisplayName = "Modern PBIR serializer emits exact page, visual, layout, and semantic projection mappings")]
    public void CreateArtifacts_ReadyInputs_MapsOnlyExplicitValues()
    {
        var inputs = CreateReadyInputs();
        var state = new PbirDeployableSerializerService().CreateArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);
        var files = state.Artifact!.Files;
        var pageFile = Assert.Single(files, file => file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal));

        using var pageDocument = JsonDocument.Parse(pageFile.Content);
        var pageRoot = pageDocument.RootElement;
        Assert.Equal("Overview", pageRoot.GetProperty("displayName").GetString());
        Assert.Equal(720, pageRoot.GetProperty("height").GetInt32());
        Assert.Equal(1280, pageRoot.GetProperty("width").GetInt32());
        Assert.Equal(
            pageRoot.GetProperty("name").GetString(),
            pageFile.RelativePath.Split('/')[2]);

        var visualFiles = files
            .Where(file => file.RelativePath.EndsWith("/visual.json", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(4, visualFiles.Length);

        foreach (var visualFile in visualFiles)
        {
            using var visualDocument = JsonDocument.Parse(visualFile.Content);
            var root = visualDocument.RootElement;
            Assert.Equal(root.GetProperty("name").GetString(), visualFile.RelativePath.Split('/')[4]);
            Assert.False(root.GetProperty("visual").TryGetProperty("objects", out _));
            Assert.False(root.GetProperty("visual").TryGetProperty("visualContainerObjects", out _));

            foreach (var role in root.GetProperty("visual").GetProperty("query").GetProperty("queryState").EnumerateObject())
            {
                foreach (var projection in role.Value.GetProperty("projections").EnumerateArray())
                {
                    Assert.False(projection.TryGetProperty("displayName", out _));
                    Assert.False(projection.TryGetProperty("format", out _));
                    Assert.True(projection.TryGetProperty("queryRef", out _));
                    Assert.True(projection.TryGetProperty("nativeQueryRef", out _));
                }
            }
        }

        var card = Assert.Single(
            visualFiles,
            file => JsonDocument.Parse(file.Content).RootElement
                .GetProperty("visual").GetProperty("visualType").GetString() == "card");
        using var cardDocument = JsonDocument.Parse(card.Content);
        var cardQueryState = cardDocument.RootElement
            .GetProperty("visual").GetProperty("query").GetProperty("queryState");
        var fields = Assert.Single(cardQueryState.EnumerateObject());
        Assert.Equal("Fields", fields.Name);
        var cardProjection = Assert.Single(fields.Value.GetProperty("projections").EnumerateArray());
        Assert.True(cardProjection.GetProperty("field").TryGetProperty("Measure", out _));
    }

    [Fact(DisplayName = "Unsupported visual or invalid layout returns no deployable artifact or manifest")]
    public void CreateArtifacts_UnsupportedOrInvalidIr_FailsClosed()
    {
        var inputs = CreateReadyInputs();
        var unsupportedVisual = inputs.IrState.Ir!.Visuals[0] with
        {
            VisualType = "customVisual"
        };
        var unsupportedInputs = AlignIr(
            inputs,
            inputs.IrState.Ir with
            {
                Visuals = [unsupportedVisual, .. inputs.IrState.Ir.Visuals.Skip(1)]
            });
        var invalidSlotVisual = inputs.IrState.Ir.Visuals[0] with
        {
            Placement = "page:Overview/slot:7"
        };
        var invalidSlotInputs = AlignIr(
            inputs,
            inputs.IrState.Ir with
            {
                Visuals = [invalidSlotVisual, .. inputs.IrState.Ir.Visuals.Skip(1)]
            });
        var service = new PbirDeployableSerializerService();

        var unsupported = service.CreateArtifacts(
            unsupportedInputs.IrState,
            unsupportedInputs.SerializerRequest,
            unsupportedInputs.DeployableRequest);
        var invalidSlot = service.CreateArtifacts(
            invalidSlotInputs.IrState,
            invalidSlotInputs.SerializerRequest,
            invalidSlotInputs.DeployableRequest);

        Assert.Null(unsupported.Artifact);
        Assert.Null(unsupported.Manifest);
        Assert.Contains(unsupported.Diagnostics.UnsupportedVisualTypes, diagnostic => diagnostic.Code == "PBIRDEPLOY-VISUAL-001");
        Assert.Null(invalidSlot.Artifact);
        Assert.Null(invalidSlot.Manifest);
        Assert.Contains(invalidSlot.Diagnostics.InvalidLayoutDefinitions, diagnostic => diagnostic.Code == "PBIRDEPLOY-LAYOUT-001");
    }

    [Fact(DisplayName = "Incomplete semantic, model, navigation, inventory, or authority inputs produce no partial deployable output")]
    public void CreateArtifacts_IncompleteOrUnsafeInputs_FailClosed()
    {
        var inputs = CreateReadyInputs();
        var firstBinding = inputs.DeployableRequest.VisualBindings[0];
        var firstProjection = firstBinding.Projections[0];
        var invalidAggregation = inputs.DeployableRequest with
        {
            VisualBindings =
            [
                firstBinding with
                {
                    Projections = [firstProjection with { Aggregation = "sum" }]
                },
                .. inputs.DeployableRequest.VisualBindings.Skip(1)
            ]
        };
        var invalidModel = inputs.DeployableRequest with
        {
            VisualBindings =
            [
                firstBinding with
                {
                    Projections =
                    [
                        firstProjection with
                        {
                            SemanticModelEntryRef = "measure:Unknown.Value"
                        }
                    ]
                },
                .. inputs.DeployableRequest.VisualBindings.Skip(1)
            ]
        };
        var filteredIr = inputs.IrState with
        {
            Ir = inputs.IrState.Ir! with
            {
                Semantics =
                [
                    inputs.IrState.Ir.Semantics[0] with
                    {
                        Filters = ["Date.Year = 2026"]
                    }
                ]
            }
        };
        var invalidNavigationIr = inputs.IrState with
        {
            Ir = inputs.IrState.Ir! with
            {
                Navigation = inputs.IrState.Ir.Navigation with
                {
                    DrillPaths = ["Overview->Detail"]
                }
            }
        };
        var invalidInventoryHash = inputs.DeployableRequest with
        {
            SemanticModelInventoryContentHash = new string('0', 64)
        };
        var missingRequestIdentity = inputs.DeployableRequest with
        {
            RequestId = string.Empty
        };
        var invalidIrState = inputs.IrState with
        {
            Validation = new PbirIntermediateRepresentationValidationResult(
                new PbirIntermediateRepresentationValidationDiagnostics(
                    MissingRequiredSections: ["pages"],
                    MissingRequiredFields: [],
                    InvalidReferences: [],
                    InvalidNavigationDefinitions: [],
                    InvalidSemanticDefinitions: [],
                    InvalidLayoutDefinitions: [],
                    UnsupportedSchemaVersions: [],
                    BoundaryViolations: [])),
            Readiness = PbirIntermediateRepresentationReadinessState.Blocked
        };
        var nonNfcBinding = inputs.DeployableRequest with
        {
            VisualBindings =
            [
                firstBinding with
                {
                    Projections =
                    [
                        firstProjection with
                        {
                            QueryRef = "Cafe\u0301.Revenue"
                        }
                    ]
                },
                .. inputs.DeployableRequest.VisualBindings.Skip(1)
            ]
        };
        var service = new PbirDeployableSerializerService();

        PbirDeployableSerializerState[] states =
        [
            service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, invalidAggregation),
            service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, invalidModel),
            service.CreateArtifacts(filteredIr, inputs.SerializerRequest, inputs.DeployableRequest),
            service.CreateArtifacts(invalidNavigationIr, inputs.SerializerRequest, inputs.DeployableRequest),
            service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, invalidInventoryHash),
            service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, missingRequestIdentity),
            service.CreateArtifacts(invalidIrState, inputs.SerializerRequest, inputs.DeployableRequest),
            service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, nonNfcBinding)
        ];

        Assert.All(states, state =>
        {
            Assert.NotEqual(PbirDeployableSerializerReadinessState.Serialized, state.Readiness);
            Assert.Null(state.Artifact);
            Assert.Null(state.Manifest);
            Assert.True(state.Diagnostics.HasFailures);
        });
    }

    [Fact(DisplayName = "Postflight validator detects file-set, artifact, manifest, and lineage hash tampering")]
    public void Validator_HashTampering_IsRejected()
    {
        var inputs = CreateReadyInputs();
        var state = new PbirDeployableSerializerService().CreateArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);
        var validator = new PbirDeployableSerializerValidator();
        var artifact = state.Artifact!;
        var manifest = state.Manifest!;
        var invalidHash = new string('0', 64);

        var validations = new[]
        {
            validator.ValidateOutput(
                artifact with
                {
                    Hashes = artifact.Hashes with { FileSetHash = invalidHash }
                },
                manifest),
            validator.ValidateOutput(
                artifact with
                {
                    Hashes = artifact.Hashes with { ArtifactHash = invalidHash }
                },
                manifest),
            validator.ValidateOutput(
                artifact,
                manifest with
                {
                    Hashes = manifest.Hashes with { ManifestHash = invalidHash }
                }),
            validator.ValidateOutput(
                artifact with
                {
                    Lineage = artifact.Lineage with { LineageHash = invalidHash }
                },
                manifest)
        };

        Assert.All(validations, validation =>
        {
            Assert.False(validation.IsValid);
            Assert.Contains(
                validation.HashValidationResults,
                diagnostic => diagnostic.Code == "PBIRDEPLOY-HASH-OUTPUT-002");
        });
    }

    [Fact(DisplayName = "Artifact and manifest hashes cover every mutable contract field")]
    public void Validator_ContractFieldTampering_IsRejected()
    {
        var inputs = CreateReadyInputs();
        var state = new PbirDeployableSerializerService().CreateArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);
        var validator = new PbirDeployableSerializerValidator();
        var artifact = state.Artifact!;
        var manifest = state.Manifest!;
        var firstFile = artifact.Files[0];

        var validations = new[]
        {
            validator.ValidateOutput(
                artifact with
                {
                    Files =
                    [
                        firstFile with { SourceIrReferences = [.. firstFile.SourceIrReferences, "tampered"] },
                        .. artifact.Files.Skip(1)
                    ]
                },
                manifest),
            validator.ValidateOutput(
                artifact,
                manifest with { SchemaLock = [.. manifest.SchemaLock, "tampered"] }),
            validator.ValidateOutput(
                artifact,
                manifest with { SupportedFeatures = [.. manifest.SupportedFeatures, "tampered"] }),
            validator.ValidateOutput(
                artifact,
                manifest with
                {
                    Warnings =
                    [
                        .. manifest.Warnings,
                        new PbirDeployableDiagnostic("TAMPERED", "manifest", "tampered")
                    ]
                }),
            validator.ValidateOutput(
                artifact,
                manifest with { UnsupportedSections = [] })
        };

        Assert.All(validations, validation =>
        {
            Assert.False(validation.IsValid);
            Assert.Contains(
                validation.HashValidationResults,
                diagnostic => diagnostic.Code == "PBIRDEPLOY-HASH-OUTPUT-002");
        });
    }

    [Fact(DisplayName = "Null nested deployable request contracts fail closed without throwing")]
    public void CreateArtifacts_NullNestedContracts_FailClosed()
    {
        var inputs = CreateReadyInputs();
        PbirDeployableSerializerRequest[] requests =
        [
            inputs.DeployableRequest with { DatasetReference = null! },
            inputs.DeployableRequest with
            {
                DatasetReference = new PbirDatasetReference(null!)
            },
            inputs.DeployableRequest with { SemanticModelInventory = null! },
            inputs.DeployableRequest with
            {
                SemanticModelInventory = inputs.DeployableRequest.SemanticModelInventory with { Entries = null! }
            },
            inputs.DeployableRequest with { VisualBindings = null! },
            inputs.DeployableRequest with { ExecutionPolicy = null! }
        ];

        foreach (var request in requests)
        {
            PbirDeployableSerializerState? state = null;
            var exception = Record.Exception(() =>
                state = new PbirDeployableSerializerService().CreateArtifacts(
                    inputs.IrState,
                    inputs.SerializerRequest,
                    request));

            Assert.Null(exception);
            Assert.NotNull(state);
            Assert.Null(state!.Artifact);
            Assert.Null(state.Manifest);
            Assert.Contains(
                state.Diagnostics.MissingRequiredFields,
                diagnostic => diagnostic.Code == "PBIRDEPLOY-REQUIRED-004");
        }
    }

    [Fact(DisplayName = "Serializer requires exact semantic inventory, projection, and relationship coverage")]
    public void CreateArtifacts_InexactSemanticCoverage_FailsClosed()
    {
        var inputs = CreateReadyInputs();
        var canonicalJson = new PbirDeployableSerializerCanonicalJson();
        var extraEntry = new PbirSemanticModelInventoryEntry(
            "measure:Sales.Unused",
            "Unused",
            "Sales",
            "Unused",
            PbirSemanticModelEntryKind.Measure);
        var expandedInventory = inputs.DeployableRequest.SemanticModelInventory with
        {
            Entries = [.. inputs.DeployableRequest.SemanticModelInventory.Entries, extraEntry]
        };
        var unusedInventoryRequest = inputs.DeployableRequest with
        {
            SemanticModelInventory = expandedInventory,
            SemanticModelInventoryContentHash = canonicalJson.ComputeSha256(
                canonicalJson.SerializeSemanticModelInventory(expandedInventory))
        };
        var unusedSemanticInputs = AlignIr(
            inputs,
            inputs.IrState.Ir! with
            {
                Semantics =
                [
                    inputs.IrState.Ir.Semantics[0] with
                    {
                        Measures = [.. inputs.IrState.Ir.Semantics[0].Measures, "Unused"]
                    }
                ]
            });
        var extraRelationshipInputs = AlignIr(
            inputs,
            inputs.IrState.Ir! with
            {
                Semantics =
                [
                    inputs.IrState.Ir.Semantics[0] with
                    {
                        Relationships =
                        [
                            .. inputs.IrState.Ir.Semantics[0].Relationships,
                            "visual:[ghost]->semantic:[intent:ghost]"
                        ]
                    }
                ]
            });
        var service = new PbirDeployableSerializerService();

        var states = new[]
        {
            service.CreateArtifacts(inputs.IrState, inputs.SerializerRequest, unusedInventoryRequest),
            service.CreateArtifacts(
                unusedSemanticInputs.IrState,
                unusedSemanticInputs.SerializerRequest,
                unusedSemanticInputs.DeployableRequest),
            service.CreateArtifacts(
                extraRelationshipInputs.IrState,
                extraRelationshipInputs.SerializerRequest,
                extraRelationshipInputs.DeployableRequest)
        };

        Assert.All(states, state =>
        {
            Assert.Null(state.Artifact);
            Assert.Null(state.Manifest);
            Assert.True(state.Diagnostics.IncompleteSemanticBindings.Count > 0 ||
                        state.Diagnostics.InvalidModelReferences.Count > 0);
        });
    }

    [Fact(DisplayName = "Serializer rejects mutated IR whose declared content hash and validation are stale")]
    public void CreateArtifacts_StaleIrIntegrity_FailsClosed()
    {
        var inputs = CreateReadyInputs();
        var staleState = inputs.IrState with
        {
            Ir = inputs.IrState.Ir! with
            {
                Pages =
                [
                    inputs.IrState.Ir.Pages[0] with
                    {
                        IntendedPurpose = "Mutated after canonical validation."
                    }
                ]
            }
        };

        var state = new PbirDeployableSerializerService().CreateArtifacts(
            staleState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);

        Assert.Null(state.Artifact);
        Assert.Null(state.Manifest);
        Assert.Contains(
            state.Diagnostics.HashViolations,
            diagnostic => diagnostic.Code == "PBIRDEPLOY-HASH-004");
    }

    [Fact(DisplayName = "Runtime postflight validates required page and visual cross-references")]
    public void Validator_DocumentCrossReferenceTampering_IsRejected()
    {
        var inputs = CreateReadyInputs();
        var state = new PbirDeployableSerializerService().CreateArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);
        var artifact = state.Artifact!;
        var manifest = state.Manifest!;
        var page = artifact.Files.Single(file => file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal));
        var tamperedContent = page.Content.Replace(
            page.RelativePath.Split('/')[2],
            "wrong-page-name",
            StringComparison.Ordinal);
        var tamperedPage = page with
        {
            Content = tamperedContent,
            ByteLength = Encoding.UTF8.GetByteCount(tamperedContent),
            HashSha256 = new PbirDeployableSerializerCanonicalJson().ComputeSha256(tamperedContent)
        };
        var tamperedArtifact = artifact with
        {
            Files = artifact.Files
                .Select(file => file.RelativePath == page.RelativePath ? tamperedPage : file)
                .ToArray()
        };

        var validation = new PbirDeployableSerializerValidator().ValidateOutput(
            tamperedArtifact,
            manifest);

        Assert.False(validation.IsValid);
        Assert.Contains(
            validation.CrossReferenceValidationResults,
            diagnostic => diagnostic.Code == "PBIRDEPLOY-XREF-002");
    }

    [Fact(DisplayName = "Required modern PBIR root documents use exact canonical JSON templates")]
    public void CreateArtifacts_RequiredDocuments_MatchExactCanonicalTemplates()
    {
        var inputs = CreateReadyInputs();
        var state = new PbirDeployableSerializerService().CreateArtifacts(
            inputs.IrState,
            inputs.SerializerRequest,
            inputs.DeployableRequest);
        var files = state.Artifact!.Files;
        var pageIdentity = new PbirDeployableSerializerCanonicalJson()
            .CreatePageIdentity("pbirIr:phase29-fixture", "page:overview");

        Assert.Equal(
            """
            {
              "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json",
              "version": "4.0",
              "datasetReference": {
                "byPath": {
                  "path": "Sales.SemanticModel"
                }
              }
            }

            """,
            files.Single(file => file.RelativePath == "definition.pbir").Content);
        Assert.Equal(
            """
            {
              "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json",
              "version": "1.0.0"
            }

            """,
            files.Single(file => file.RelativePath == "definition/version.json").Content);
        Assert.Equal(
            """
            {
              "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json",
              "layoutOptimization": "None",
              "themeCollection": {}
            }

            """,
            files.Single(file => file.RelativePath == "definition/report.json").Content);
        Assert.Equal(
            $$"""
            {
              "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json",
              "pageOrder": [
                "{{pageIdentity}}"
              ],
              "activePageName": "{{pageIdentity}}"
            }

            """,
            files.Single(file => file.RelativePath == "definition/pages/pages.json").Content);
        Assert.Equal(
            $$"""
            {
              "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.0.0/schema.json",
              "name": "{{pageIdentity}}",
              "displayName": "Overview",
              "displayOption": "FitToPage",
              "height": 720,
              "width": 1280
            }

            """,
            files.Single(file => file.RelativePath.EndsWith("/page.json", StringComparison.Ordinal)).Content);
    }

    [Fact(DisplayName = "Deployable serializer exposes only the approved in-memory callable and dependency surface")]
    public void SerializerBoundary_HasNoWriterProviderOrExecutionSurface()
    {
        var serviceType = typeof(PbirDeployableSerializerService);
        var method = Assert.Single(
            serviceType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
            candidate => candidate.Name == "CreateArtifacts");
        var parameters = method.GetParameters();

        Assert.Equal(typeof(PbirDeployableSerializerState), method.ReturnType);
        Assert.Equal(
            new[]
            {
                typeof(PbirIntermediateRepresentationState),
                typeof(PbirSerializerRequest),
                typeof(PbirDeployableSerializerRequest)
            },
            parameters.Select(parameter => parameter.ParameterType).ToArray());

        var dependencyTypes = serviceType
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Select(field => field.FieldType)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(
            new[]
            {
                typeof(PbirDeployableSerializerCanonicalJson),
                typeof(PbirDeployableSerializerSafetyGate),
                typeof(PbirDeployableSerializerValidator)
            }.OrderBy(type => type.FullName, StringComparer.Ordinal),
            dependencyTypes);

        Type[] forbiddenTypes =
        [
            typeof(FileInfo),
            typeof(DirectoryInfo),
            typeof(FileSystemInfo),
            typeof(Stream),
            typeof(HttpClient),
            typeof(Process)
        ];
        var callableTypes = serviceType
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Concat(parameters.Select(parameter => parameter.ParameterType))
            .Append(method.ReturnType)
            .Concat(dependencyTypes)
            .ToArray();
        Assert.DoesNotContain(callableTypes, type => forbiddenTypes.Contains(type));

        var repoRoot = FindRepositoryRoot();
        var coreProject = File.ReadAllText(
            Path.Combine(repoRoot, "service-dotnet/PbirDesignAnalyzer.Core.csproj"));
        Assert.DoesNotContain("JsonSchema.Net", coreProject, StringComparison.Ordinal);
    }

    public static IEnumerable<object[]> ExecutionPolicyMutations()
    {
        var safe = PbirDeployableExecutionPolicy.NoAuthority;

        yield return [safe with { FilesystemMaterializationAllowed = true }];
        yield return [safe with { ProviderInvocationAllowed = true }];
        yield return [safe with { MicrosoftSkillsExecutionAllowed = true }];
        yield return [safe with { ApiInvocationAllowed = true }];
        yield return [safe with { CliInvocationAllowed = true }];
        yield return [safe with { DeploymentAllowed = true }];
        yield return [safe with { DesktopAutomationAllowed = true }];
        yield return [safe with { AnalyzerAutomationAllowed = true }];
    }

    internal static ReadyDeployableInputs CreateReadyInputs()
    {
        var page = new PbirIntermediateRepresentationPage(
            PageId: "Overview",
            PageIdentity: "page:overview",
            NavigationBehavior: "pageTab",
            IntendedPurpose: "Show revenue over time.",
            Order: 1);
        PbirIntermediateRepresentationVisual[] visuals =
        [
            new(
                VisualId: "visual:revenue-card",
                PageId: "Overview",
                VisualType: "card",
                Placement: "page:Overview/slot:1",
                SemanticIntent: "intent:revenue-card",
                InteractionModel: ["none"],
                Order: 1),
            new(
                VisualId: "visual:revenue-table",
                PageId: "Overview",
                VisualType: "table",
                Placement: "page:Overview/slot:2",
                SemanticIntent: "intent:revenue-table",
                InteractionModel: ["none"],
                Order: 2),
            new(
                VisualId: "visual:revenue-column",
                PageId: "Overview",
                VisualType: "clusteredColumnChart",
                Placement: "page:Overview/slot:3",
                SemanticIntent: "intent:revenue-column",
                InteractionModel: ["none"],
                Order: 3),
            new(
                VisualId: "visual:revenue-line",
                PageId: "Overview",
                VisualType: "lineChart",
                Placement: "page:Overview/slot:4",
                SemanticIntent: "intent:revenue-line",
                InteractionModel: ["none"],
                Order: 4)
        ];
        var semantic = new PbirIntermediateRepresentationSemantic(
            SemanticId: "semantic:overview",
            PageId: "Overview",
            Measures: ["Revenue"],
            Dimensions: ["Month"],
            Kpis:
            [
                "intent:revenue-card",
                "intent:revenue-table",
                "intent:revenue-column",
                "intent:revenue-line"
            ],
            Filters: [],
            DrillBehavior: "none",
            Relationships:
            [
                "visual:[visual:revenue-card]->semantic:[intent:revenue-card]",
                "visual:[visual:revenue-table]->semantic:[intent:revenue-table]",
                "visual:[visual:revenue-column]->semantic:[intent:revenue-column]",
                "visual:[visual:revenue-line]->semantic:[intent:revenue-line]"
            ]);
        var ir = new PbirIntermediateRepresentation(
            Metadata: new PbirIntermediateRepresentationMetadata(
                IrId: "pbirIr:phase29-fixture",
                SchemaVersion: PbirIntermediateRepresentationContract.SchemaVersionV1,
                GeneratedUtc: DateTime.Parse("2026-07-26T12:00:00Z").ToUniversalTime()),
            References: new PbirIntermediateRepresentationReferences(
                GenerationManifestRef: "generationManifest:phase29-fixture",
                PbirGenerationSpecificationRef: "pbirGenerationSpecification:phase29-fixture"),
            Pages: [page],
            Visuals: visuals,
            Semantics: [semantic],
            Navigation: new PbirIntermediateRepresentationNavigation(
                LandingPage: "Overview",
                PageTransitions: [],
                Bookmarks: ["page:Overview", "landing:Overview"],
                DrillPaths: []),
            Layout: new PbirIntermediateRepresentationLayout(
                Containers:
                [
                    new PbirIntermediateRepresentationLayoutContainer(
                        ContainerId: "container:overview",
                        PageId: "Overview",
                        Purpose: "Overview grid",
                        VisualRefs: visuals.Select(visual => visual.VisualId).ToArray())
                ],
                Spacing: ["standard-8px-grid"],
                Alignment: ["deterministic-grid", "visual-placement-preserved"],
                ResponsiveHints:
                [
                    "preserve-page-order",
                    "preserve-visual-intent",
                    "allow-future-serializer-layout-adaptation"
                ]),
            SuccessCriteria: new PbirIntermediateRepresentationSuccessCriteria(
                BusinessIntent: ["Revenue is visible."],
                AnalyticalFlow: ["Overview"],
                SuccessCriteria: ["Revenue is bound explicitly."]),
            Lineage: new PbirIntermediateRepresentationLineage(
                UpstreamLineage:
                [
                    new PlanningLineageEntry(
                        Stage: "generationManifest",
                        ReferenceId: "generationManifest:phase29-fixture",
                        Label: "Phase 29 fixture")
                ],
                ImmutableLineage:
                [
                    "generationManifest:phase29-fixture",
                    "pbirGenerationSpecification:phase29-fixture",
                    "pbirIr:phase29-fixture"
                ]),
            Hashes: new PbirIntermediateRepresentationHashes(
                InputHash: new string('1', 64),
                ContentHash: new string('2', 64),
                LineageHash: new string('3', 64)));
        ir = ir with
        {
            Hashes = ir.Hashes with
            {
                ContentHash = PbirIntermediateRepresentationIntegrity.ComputeContentHash(ir)
            }
        };
        var irState = new PbirIntermediateRepresentationState(
            Ir: ir,
            Validation: new PbirIntermediateRepresentationValidationResult(
                PbirIntermediateRepresentationValidationDiagnostics.Empty),
            Readiness: PbirIntermediateRepresentationReadinessState.ReadyForSerializer);
        var serializerRequest = new PbirSerializerRequest(
            SchemaVersion: PbirSerializerRequestContract.SchemaVersionV1,
            RequestId: "pbirSerializerRequest:phase29-fixture",
            PbirIrRef: ir.Metadata.IrId,
            PbirIrSchemaVersion: ir.Metadata.SchemaVersion,
            PbirIrContentHash: ir.Hashes.ContentHash,
            SerializerImplementationAvailable: true,
            ProviderInvocationAllowed: false,
            DeploymentAllowed: false,
            MicrosoftSkillsExecutionAllowed: false);
        var inventory = new PbirSemanticModelInventory(
            SchemaVersion: PbirSemanticModelInventoryContract.SchemaVersionV1,
            InventoryRef: "modelInventory:sales",
            Entries:
            [
                new PbirSemanticModelInventoryEntry(
                    EntryId: "column:Date.Month",
                    Token: "Month",
                    Entity: "Date",
                    Property: "Month",
                    Kind: PbirSemanticModelEntryKind.Column),
                new PbirSemanticModelInventoryEntry(
                    EntryId: "measure:Sales.Revenue",
                    Token: "Revenue",
                    Entity: "Sales",
                    Property: "Revenue",
                    Kind: PbirSemanticModelEntryKind.Measure)
            ]);
        var request = new PbirDeployableSerializerRequest(
            SchemaVersion: PbirDeployableSerializerRequestContract.SchemaVersionV1,
            RequestId: "pbirDeployableSerializerRequest:phase29-fixture",
            SerializerRequestRef: serializerRequest.RequestId,
            SerializerRequestSchemaVersion: serializerRequest.SchemaVersion,
            PbirIrRef: ir.Metadata.IrId,
            PbirIrSchemaVersion: ir.Metadata.SchemaVersion,
            PbirIrContentHash: ir.Hashes.ContentHash,
            TargetFormat: "modernPbir",
            DefinitionPropertiesSchemaVersion: PbirDeployableSchemaLock.DefinitionPropertiesSchemaVersion,
            DefinitionSchemaVersion: PbirDeployableSchemaLock.DefinitionSchemaVersion,
            DatasetReference: new PbirDatasetReference(
                ByPath: new PbirDatasetReferenceByPath("Sales.SemanticModel")),
            LayoutProfileId: "modern-grid-1280x720/v1",
            SemanticModelInventory: inventory,
            SemanticModelInventoryRef: inventory.InventoryRef,
            SemanticModelInventoryContentHash:
                "bc4f58184e62028614f7867e3927c5591f1b55c0104b3f70a9d85ed4e9516d29",
            VisualBindings:
            [
                CreateBinding(
                    "visual:revenue-card",
                    new PbirRoleProjectionBinding(
                        Role: "Fields",
                        ProjectionOrder: 1,
                        SourceSemanticToken: "Revenue",
                        SemanticModelEntryRef: "measure:Sales.Revenue",
                        QueryRef: "Sales.Revenue",
                        NativeQueryRef: "Revenue",
                        Aggregation: "none",
                        DisplayName: null,
                        Format: null)),
                CreateBinding(
                    "visual:revenue-table",
                    new PbirRoleProjectionBinding(
                        Role: "Values",
                        ProjectionOrder: 1,
                        SourceSemanticToken: "Month",
                        SemanticModelEntryRef: "column:Date.Month",
                        QueryRef: "Date.Month",
                        NativeQueryRef: "Month",
                        Aggregation: "none",
                        DisplayName: null,
                        Format: null),
                    new PbirRoleProjectionBinding(
                        Role: "Values",
                        ProjectionOrder: 2,
                        SourceSemanticToken: "Revenue",
                        SemanticModelEntryRef: "measure:Sales.Revenue",
                        QueryRef: "Sales.Revenue",
                        NativeQueryRef: "Revenue",
                        Aggregation: "none",
                        DisplayName: null,
                        Format: null)),
                CreateChartBinding("visual:revenue-column"),
                CreateChartBinding("visual:revenue-line")
            ],
            ExecutionPolicy: PbirDeployableExecutionPolicy.NoAuthority);

        return new ReadyDeployableInputs(irState, serializerRequest, request);
    }

    private static ReadyDeployableInputs AlignIr(
        ReadyDeployableInputs inputs,
        PbirIntermediateRepresentation ir)
    {
        var contentHash = PbirIntermediateRepresentationIntegrity.ComputeContentHash(ir);
        var canonicalIr = ir with
        {
            Hashes = ir.Hashes with { ContentHash = contentHash }
        };
        return inputs with
        {
            IrState = inputs.IrState with { Ir = canonicalIr },
            SerializerRequest = inputs.SerializerRequest with { PbirIrContentHash = contentHash },
            DeployableRequest = inputs.DeployableRequest with { PbirIrContentHash = contentHash }
        };
    }

    private static PbirVisualBinding CreateChartBinding(string visualId)
    {
        return CreateBinding(
            visualId,
            new PbirRoleProjectionBinding(
                Role: "Category",
                ProjectionOrder: 1,
                SourceSemanticToken: "Month",
                SemanticModelEntryRef: "column:Date.Month",
                QueryRef: "Date.Month",
                NativeQueryRef: "Month",
                Aggregation: "none",
                DisplayName: null,
                Format: null),
            new PbirRoleProjectionBinding(
                Role: "Y",
                ProjectionOrder: 1,
                SourceSemanticToken: "Revenue",
                SemanticModelEntryRef: "measure:Sales.Revenue",
                QueryRef: "Sales.Revenue",
                NativeQueryRef: "Revenue",
                Aggregation: "none",
                DisplayName: null,
                Format: null));
    }

    private static PbirVisualBinding CreateBinding(
        string visualId,
        params PbirRoleProjectionBinding[] projections)
    {
        return new PbirVisualBinding(visualId, projections);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "service-dotnet")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    internal sealed record ReadyDeployableInputs(
        PbirIntermediateRepresentationState IrState,
        PbirSerializerRequest SerializerRequest,
        PbirDeployableSerializerRequest DeployableRequest);
}
