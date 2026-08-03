# Deterministic Modern PBIR Serializer — Repository Phase 29 Design

Date: 2026-07-26

Status: Implemented after explicit phase-boundary and implementation-plan approval. Repository Phase 29 is complete only through original roadmap Phase 4A serialization.

## Roadmap Mapping

Repository Phase 29 implements **Original Roadmap Phase 4A — Deterministic Modern PBIR Serialization**.

The original seven-phase roadmap remains:

1. Design Package Consumption Layer
2. Skills Prompt Generation
3. Generation Request Framework
4. PBIR Generation
5. Analyzer Handoff
6. Refinement Loop
7. Fabric App Generation

Repository Phases 21–28 decomposed prerequisites and review infrastructure that made Phase 4 safer but obscured the original sequence. Phase 29 is the first implementation slice inside original Phase 4:

- **Original Phase 4A:** deterministic modern PBIR serialization in memory — Repository Phase 29
- **Original Phase 4B:** safe local deployable PBIR materialization with preview/apply/rollback — next separate repository phase
- later original Phase 4 work: provider or Microsoft Skills execution, Desktop verification, deployment, and publishing — not authorized

Phase 29 stops after serialization. It does not complete all of original Phase 4.

## Decision Summary

Add a strict, backend-only compiler from canonical pbir-ir/v1 to an in-memory modern PBIR file inventory.

The compiler:

- consumes pbir-ir/v1 through pbir-serializer-request/v1
- accepts a new versioned modern-serializer request that locks target schemas, a safe relative semantic-model reference, a fixed layout profile, and explicit semantic-binding resolutions
- emits definition.pbir and the required definition/ hierarchy in memory
- emits definition/report.json but never root-level report.json
- validates the complete candidate before returning any artifact or manifest
- returns diagnostics and a rejected or blocked readiness state with no partial output on any unsupported or incomplete input
- performs no filesystem, provider, network, API, CLI, Microsoft Skills, Desktop, deployment, or Analyzer action

## Architectural Review Findings

Ranked by long-term risk.

### 1. Highest Risk: Treating Current pbir-ir/v1 Placeholders As Deployable Detail

The current IR can contain values such as auto dimensions, friendly KPI names without table ownership, and slot-based placement. Those values are sufficient for preview but are not automatically valid PBIR semantic queries.

Design response:

- keep pbir-ir/v1 stable
- require explicit request-side resolution of every semantic token to an immutable table/property/kind reference
- allow only the documented slot layout grammar and fixed layout profile
- reject auto, ambiguous, missing, or unused resolutions
- record unsupported IR sections in diagnostics

This means some IR currently produced by the upstream pipeline will be rejected until it carries resolvable intent. Rejection is preferable to inventing model references.

### 2. Highest Risk: Confusing Modern definition/report.json With PBIR-Legacy report.json

Modern PBIR requires report metadata at definition/report.json. PBIR-Legacy uses report.json at the report root. The two representations are mutually exclusive for this phase.

Design response:

- require definition/report.json
- reject and test for root-level report.json
- name path checks against normalized full relative paths, not only file names
- document the distinction in contracts, diagnostics, and current-state docs

### 3. High Risk: Schema Drift Through Live Downloads

Microsoft PBIR schemas evolve independently. Resolving latest schemas at runtime would make production output and tests nondeterministic.

Design response:

- pin exact schema URLs and a reviewed Microsoft schema repository commit
- keep production offline
- vendor the exact official schemas and required references as test fixtures with provenance and hashes
- require an intentional contract/version change to add a new schema version

### 4. High Risk: A Serializer That Returns Partial Output

Returning a few valid files alongside a failure makes later materialization ambiguous and unsafe.

Design response:

- build into a private candidate inventory
- validate paths, identities, cross-references, schema versions, content, hashes, and lineage
- expose artifact and manifest only after the whole candidate passes
- expose no artifact bytes or partial manifest when readiness is blocked or rejected

### 5. Medium Risk: Generalizing Too Early

A generic PBIR/PBIR-Legacy/PBIP serializer would combine incompatible formats and prematurely couple the future writer to serialization.

Design response:

- modern PBIR only
- in-memory inventory only
- no generic writer abstraction
- no reuse or widening of the preview-only writer
- no PBIP, semantic-model, resource, bookmark, mobile, or report-extension generation

## Considered Approaches

### A. Strict Request-Bound Compiler — Recommended

Keep pbir-ir/v1 unchanged. Add explicit model-binding and layout-profile inputs at the serializer request boundary. Compile only supported visual and navigation shapes and fail closed otherwise.

Benefits:

- preserves the canonical IR contract
- makes every emitted semantic reference traceable to caller-supplied input
- supports deterministic offline validation
- cleanly hands an immutable inventory to a future writer

Cost:

- existing placeholder-heavy IR may be rejected
- the supported visual subset is deliberately small

### B. Widen pbir-ir/v1 Before Serialization

Add typed coordinates and fully qualified semantic-model objects directly to pbir-ir/v1.

Rejected for Phase 29 because it would turn the first serializer into an upstream contract migration, increase backward-compatibility risk, and violate the instruction to consume canonical pbir-ir/v1 through the existing boundary.

### C. Emit Schema-Valid Skeleton Visuals

Emit visuals without complete queries or use default fields and layout values.

Rejected because schema validity alone does not preserve design intent. This approach would invent information and produce misleading deployable output.

## Contract Set

All contracts are backend-internal in Phase 29 but versioned because they form the future writer boundary.

### pbir-deployable-serializer-request/v1

Required fields:

- schemaVersion
- requestId
- serializerRequestRef
- serializerRequestSchemaVersion
- pbirIrRef
- pbirIrSchemaVersion
- pbirIrContentHash
- targetFormat: modernPbir
- definitionPropertiesSchemaVersion: 2.0.0
- definitionSchemaVersion: 1.0.0
- datasetReference
- layoutProfileId: modern-grid-1280x720/v1
- semanticModelInventory
- semanticModelInventoryRef
- semanticModelInventoryContentHash
- visualBindings
- executionPolicy

datasetReference supports only:

- byPath
- a normalized relative path
- forward-slash separators
- no empty segments, dot segments, parent traversal, URI scheme, drive prefix, absolute path, or control characters

The request explicitly keeps these false:

- filesystemMaterializationAllowed
- providerInvocationAllowed
- microsoftSkillsExecutionAllowed
- apiInvocationAllowed
- cliInvocationAllowed
- deploymentAllowed
- desktopAutomationAllowed
- analyzerAutomationAllowed

semanticModelInventory contains immutable entries:

- entryId
- token used by the IR
- entity/table name
- property name
- kind: measure or column

semanticModelInventoryRef and semanticModelInventoryContentHash identify the caller-supplied, immutable model snapshot used for reference validation. The serializer verifies the inventory hash before resolving any binding. It does not rescan a repository or query a live semantic model.

visualBindings contains:

- visualId
- one or more role projections
- each projection's role
- each projection's projectionOrder
- each projection's sourceSemanticToken
- each projection's semanticModelEntryRef
- each projection's explicit queryRef
- each projection's explicit nativeQueryRef
- each projection's aggregation, which must be none
- each projection's displayName, which must be null in Phase 29
- each projection's format, which must be null in Phase 29

Every source token must occur in the linked IR semantic record. Every resolved entity/property/kind must exist exactly once in the request inventory. Extra bindings and duplicate inventory identities are invalid.

The request-side semantic shape uses these exact property names:

```json
{
  "semanticModelInventory": {
    "schemaVersion": "pbir-semantic-model-inventory/v1",
    "inventoryRef": "modelInventory:sales",
    "entries": [
      {
        "entryId": "column:Date.Month",
        "token": "Month",
        "entity": "Date",
        "property": "Month",
        "kind": "column"
      },
      {
        "entryId": "measure:Sales.Revenue",
        "token": "Revenue",
        "entity": "Sales",
        "property": "Revenue",
        "kind": "measure"
      }
    ]
  },
  "semanticModelInventoryRef": "modelInventory:sales",
  "semanticModelInventoryContentHash": "{{64-lowercase-hex}}",
  "visualBindings": [
    {
      "visualId": "visual:revenue-by-month",
      "projections": [
        {
          "role": "Category",
          "projectionOrder": 1,
          "sourceSemanticToken": "Month",
          "semanticModelEntryRef": "column:Date.Month",
          "queryRef": "Date.Month",
          "nativeQueryRef": "Month",
          "aggregation": "none",
          "displayName": null,
          "format": null
        },
        {
          "role": "Y",
          "projectionOrder": 1,
          "sourceSemanticToken": "Revenue",
          "semanticModelEntryRef": "measure:Sales.Revenue",
          "queryRef": "Sales.Revenue",
          "nativeQueryRef": "Revenue",
          "aggregation": "none",
          "displayName": null,
          "format": null
        }
      ]
    }
  ]
}
```

### pbir-deployable-artifact/v1

Contains:

- schemaVersion
- artifactId
- targetFormat
- files
- lineage
- hashes

Each file contains:

- normalized relativePath
- contentType: application/json
- exact UTF-8 content
- byteLength
- hashSha256
- Microsoft schema URL and version
- source IR references

The artifact inventory is immutable and ordered by relative path using ordinal comparison.

### pbir-deployable-manifest/v1

Contains:

- schemaVersion
- manifestId
- artifactRef
- source references
- schema lock
- ordered file inventory
- supported features
- warnings
- unsupported sections
- lineage
- hashes

The manifest is suitable as input to a future deployable writer but carries no path root, overwrite policy, or materialization authority.

### pbir-deployable-validation/v1

Contains:

- schemaVersion
- isValid
- validatedFileCount
- schemaContractResults
- structuralValidationResults
- crossReferenceValidationResults
- hashValidationResults

schemaContractResults reports only exact schema URL/version locks, required supported-template properties, property types, forbidden properties, and supported-shape checks implemented by Phase 29. Full Microsoft Draft 7 JSON Schema evaluation is a build/test guarantee performed against pinned local fixtures with network resolution disabled; the runtime contract does not claim full JSON Schema evaluation.

### pbir-deployable-readiness/v1

Readiness values:

- incomplete — required contract or IR information is absent
- blocked — complete input is present but unsafe, unsupported, contradictory, or invalid
- readyForSerialization — request and IR passed preflight
- serialized — the complete candidate passed post-serialization validation

Only serialized may contain artifact and manifest.

### pbir-deployable-diagnostics/v1

Ordered diagnostic categories:

- missingRequiredFields
- unsupportedSchemaVersions
- unsupportedVisualTypes
- incompleteSemanticBindings
- invalidModelReferences
- invalidPaths
- duplicateIdentities
- invalidLayoutDefinitions
- invalidNavigationDefinitions
- schemaIncompatibilities
- hashViolations
- lineageViolations
- boundaryViolations
- warnings
- unsupportedSections

Diagnostics use stable codes plus human-readable messages. Ordering is code, path/reference, then message using ordinal comparison.

### pbir-deployable-lineage/v1

Contains:

- pbirIrRef
- pbirIrContentHash
- serializerRequestRef
- deployableSerializerRequestRef
- semanticModelInventoryRef
- semanticModelInventoryContentHash
- upstreamLineage copied from the IR
- immutableLineage containing every upstream reference plus artifact and manifest identities
- lineageHash

No lineage collection is mutated after construction.

### pbir-deployable-hashes/v1

Contains:

- inputHash
- fileSetHash
- artifactHash
- manifestHash
- lineageHash

Hash rules:

- SHA-256, lowercase hexadecimal
- hashes cover exact UTF-8 bytes
- inputHash covers canonical IR content hash plus canonical deployable serializer request content
- fileSetHash covers ordered relative path, byte length, and file hash tuples
- artifactHash covers schema version, artifact identity, target format, every generated-file field including exact content and sourceIrReferences, complete lineage, hashes schema version, inputHash, fileSetHash, and lineageHash; it excludes artifactHash and manifestHash
- manifestHash covers schema version, manifest identity, artifact reference, schema lock, complete file references, supported features, warnings, unsupported sections, complete lineage, hashes schema version, inputHash, fileSetHash, artifactHash, and lineageHash; it excludes manifestHash
- the artifact stores the resulting manifestHash only as a cross-reference, and postflight requires the artifact and manifest hash records to be identical
- timestamps are not generated implicitly; identical explicit inputs produce identical outputs

## Supported Modern PBIR Shape

For a request that passes validation, the minimum inventory is:

- definition.pbir
- definition/version.json
- definition/report.json
- definition/pages/pages.json
- definition/pages/[pageIdentity]/page.json for every page
- definition/pages/[pageIdentity]/visuals/[visualIdentity]/visual.json for every supported visual

Not emitted:

- report.json at the report root
- bookmarks
- mobile layouts
- report extensions
- static resources
- custom visuals
- semantic models
- PBIP project files
- .platform

### Deterministic Identities

- Page and visual object names are 20 lowercase hexadecimal characters.
- Names are derived from SHA-256 over a domain-separated tuple of IR id plus canonical source identity.
- Folder and document object names are identical.
- Collisions and duplicate source identities fail closed; no suffix is invented.

### Canonical JSON

- UTF-8 without BOM
- LF newline
- one trailing LF
- two-space indentation
- explicit property order
- ordinal ordering for maps and sets
- source order only where order is semantically meaningful and already canonical
- invariant-culture numbers
- Unicode strings must already be normalized to NFC; non-NFC input is rejected rather than rewritten
- Utf8JsonWriter with JavaScriptEncoder.UnsafeRelaxedJsonEscaping defines string escaping; quotation mark, reverse solidus, and control characters are escaped, while valid non-ASCII scalar values remain UTF-8
- no null or default-valued optional properties unless required by the locked schema

The templates below use double-braced names only as specification notation. Double braces are never emitted.

### Semantic Model Inventory Content Hash

semanticModelInventoryContentHash is computed before any model reference is resolved.

Validation before hashing:

- schemaVersion must equal pbir-semantic-model-inventory/v1
- inventoryRef must equal semanticModelInventoryRef
- entryId, token, entity, and property must be nonempty NFC strings
- kind must be exactly column or measure
- entryId must be unique using ordinal comparison
- token must be unique using ordinal comparison
- the tuple entity, property, kind must be unique using ordinal comparison
- duplicate entries are rejected; they are never deduplicated or overwritten

Canonical inventory bytes are one minified JSON object with this exact property order:

```json
{"schemaVersion":"pbir-semantic-model-inventory/v1","inventoryRef":"{{semanticModelInventoryRef}}","entries":[{"entryId":"{{entryId}}","token":"{{token}}","entity":"{{entity}}","property":"{{property}}","kind":"{{column|measure}}"}]}
```

Canonicalization rules:

- entries are sorted by entryId, then token, then entity, then property, then kind using StringComparer.Ordinal
- object member separator is a single comma byte 0x2C
- name/value separator is a single colon byte 0x3A
- no space, tab, CR, LF, BOM, or trailing byte is present
- strings use Utf8JsonWriter with JavaScriptEncoder.UnsafeRelaxedJsonEscaping
- encoding is UTF-8
- the content-hash field itself is not part of the canonical object
- SHA-256 is computed over exactly those bytes and rendered as 64 lowercase hexadecimal characters

An empty entries array is invalid and produces no hash-authorized serializer request.

For the concrete Month/Revenue inventory shown above, the canonical payload is 310 bytes and its expected semanticModelInventoryContentHash is:

```text
bc4f58184e62028614f7867e3927c5591f1b55c0104b3f70a9d85ed4e9516d29
```

### Exact Document Templates And Property Mappings

#### definition.pbir

Exact property order and shape:

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json",
  "version": "4.0",
  "datasetReference": {
    "byPath": {
      "path": "{{request.datasetReference.byPath.path}}"
    }
  }
}
```

Mapping:

- $schema is the locked serializer constant
- version is the locked serializer constant
- datasetReference.byPath.path is copied byte-for-byte as a JSON string from the already validated request value
- no byConnection or other property is emitted

#### definition/version.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json",
  "version": "1.0.0"
}
```

Both values are locked serializer constants.

#### definition/report.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json",
  "layoutOptimization": "None",
  "themeCollection": {}
}
```

Mapping:

- layoutOptimization is the fixed None value because Phase 29 emits no mobile layout
- themeCollection is the required empty object; no theme, resource, setting, filter, annotation, or formatting value is inferred

#### definition/pages/pages.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json",
  "pageOrder": [
    "{{generatedPageIdentityInIrOrder}}"
  ],
  "activePageName": "{{generatedIdentityForIrNavigationLandingPage}}"
}
```

Mapping:

- pageOrder contains every generated page identity sorted by IR Page.Order ascending; ties are invalid
- activePageName is the generated identity of the page whose PageId exactly equals IR Navigation.LandingPage
- no page name is inferred from display text

#### definition/pages/[pageIdentity]/page.json

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.0.0/schema.json",
  "name": "{{generatedPageIdentity}}",
  "displayName": "{{irPage.PageId}}",
  "displayOption": "FitToPage",
  "height": 720,
  "width": 1280
}
```

Mapping:

- name is the generated identity for IR Page.PageIdentity
- displayName is copied exactly from IR Page.PageId; IntendedPurpose is not substituted
- displayOption, height, and width are fixed by modern-grid-1280x720/v1
- IntendedPurpose is retained in the manifest unsupported-section diagnostics and lineage context; it is not converted to formatting or annotations

#### Shared Direct Field Projection

Every projection has this property order:

```json
{
  "field": {
    "{{Measure|Column}}": {
      "Expression": {
        "SourceRef": {
          "Entity": "{{inventory.entity}}"
        }
      },
      "Property": "{{inventory.property}}"
    }
  },
  "queryRef": "{{binding.queryRef}}",
  "nativeQueryRef": "{{binding.nativeQueryRef}}"
}
```

Rules:

- Measure is emitted only for an inventory entry whose kind is measure
- Column is emitted only for an inventory entry whose kind is column
- Entity and Property are copied exactly from the referenced inventory entry
- queryRef and nativeQueryRef are copied exactly from the role binding and must be nonempty, NFC, and unique within the visual
- aggregation must be explicitly none in the binding; no Aggregation expression is emitted
- displayName and format must be explicitly null in the binding; neither property is emitted
- active, hidden, fieldParameters, showAll, sortDefinition, options, objects, and visualContainerObjects are not emitted

#### Card visual.json

The supported IR VisualType is exactly card. The only role is Fields with exactly one measure projection.

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json",
  "name": "{{generatedVisualIdentity}}",
  "position": {
    "x": 24,
    "y": 24,
    "z": 0,
    "height": 328,
    "width": 400,
    "tabOrder": 0
  },
  "visual": {
    "visualType": "card",
    "query": {
      "queryState": {
        "Fields": {
          "projections": [
            {
              "field": {
                "Measure": {
                  "Expression": {
                    "SourceRef": {
                      "Entity": "Sales"
                    }
                  },
                  "Property": "Revenue"
                }
              },
              "queryRef": "Sales.Revenue",
              "nativeQueryRef": "Revenue"
            }
          ]
        }
      }
    }
  }
}
```

The position values shown are slot 1; other slots use the geometry table below.

#### Table visual.json

The supported IR VisualType is exactly table. The only role is Values with one or more explicitly ordered direct column or measure projections.

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json",
  "name": "{{generatedVisualIdentity}}",
  "position": {
    "x": 24,
    "y": 24,
    "z": 0,
    "height": 328,
    "width": 400,
    "tabOrder": 0
  },
  "visual": {
    "visualType": "table",
    "query": {
      "queryState": {
        "Values": {
          "projections": [
            {
              "field": {
                "Column": {
                  "Expression": {
                    "SourceRef": {
                      "Entity": "Date"
                    }
                  },
                  "Property": "Month"
                }
              },
              "queryRef": "Date.Month",
              "nativeQueryRef": "Month"
            },
            {
              "field": {
                "Measure": {
                  "Expression": {
                    "SourceRef": {
                      "Entity": "Sales"
                    }
                  },
                  "Property": "Revenue"
                }
              },
              "queryRef": "Sales.Revenue",
              "nativeQueryRef": "Revenue"
            }
          ]
        }
      }
    }
  }
}
```

The position shown is slot 1. The projection order is copied from binding projectionOrder: Month is 1 and Revenue is 2.

#### Clustered column visual.json

The supported IR VisualType is exactly clusteredColumnChart. Roles appear in the fixed order Category, then Y. Category has exactly one direct column projection. Y has one or more direct measure projections.

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json",
  "name": "{{generatedVisualIdentity}}",
  "position": {
    "x": 24,
    "y": 24,
    "z": 0,
    "height": 328,
    "width": 400,
    "tabOrder": 0
  },
  "visual": {
    "visualType": "clusteredColumnChart",
    "query": {
      "queryState": {
        "Category": {
          "projections": [
            {
              "field": {
                "Column": {
                  "Expression": {
                    "SourceRef": {
                      "Entity": "Date"
                    }
                  },
                  "Property": "Month"
                }
              },
              "queryRef": "Date.Month",
              "nativeQueryRef": "Month"
            }
          ]
        },
        "Y": {
          "projections": [
            {
              "field": {
                "Measure": {
                  "Expression": {
                    "SourceRef": {
                      "Entity": "Sales"
                    }
                  },
                  "Property": "Revenue"
                }
              },
              "queryRef": "Sales.Revenue",
              "nativeQueryRef": "Revenue"
            }
          ]
        }
      }
    }
  }
}
```

The position shown is slot 1. Series, Tooltips, sort, aggregation, formatting, and secondary axes are unsupported.

#### Line visual.json

The supported IR VisualType is exactly lineChart. Roles appear in the fixed order Category, then Y. Category has exactly one direct column projection. Y has one or more direct measure projections.

```json
{
  "$schema": "https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json",
  "name": "{{generatedVisualIdentity}}",
  "position": {
    "x": 24,
    "y": 24,
    "z": 0,
    "height": 328,
    "width": 400,
    "tabOrder": 0
  },
  "visual": {
    "visualType": "lineChart",
    "query": {
      "queryState": {
        "Category": {
          "projections": [
            {
              "field": {
                "Column": {
                  "Expression": {
                    "SourceRef": {
                      "Entity": "Date"
                    }
                  },
                  "Property": "Month"
                }
              },
              "queryRef": "Date.Month",
              "nativeQueryRef": "Month"
            }
          ]
        },
        "Y": {
          "projections": [
            {
              "field": {
                "Measure": {
                  "Expression": {
                    "SourceRef": {
                      "Entity": "Sales"
                    }
                  },
                  "Property": "Revenue"
                }
              },
              "queryRef": "Sales.Revenue",
              "nativeQueryRef": "Revenue"
            }
          ]
        }
      }
    }
  }
}
```

The position shown is slot 1. Series, Tooltips, sort, aggregation, formatting, and secondary axes are unsupported.

### Pages And Navigation

Supported:

- page order from IR Page.Order
- landing page through definition/pages/pages.json activePageName
- visible report pages
- 1280×720 canvas with FitToPage
- ordinary page-tab navigation
- sequential page transitions only when they exactly assert the same page order

Rejected:

- duplicate page ids, identities, or order values
- landing page not present
- nonsequential transitions
- bookmark state beyond the canonical page and landing markers
- drillthrough, tooltip pages, hidden pages, or navigation requiring buttons/bookmarks
- page filters that cannot be resolved exactly

### Layout

Supported placement grammar:

- page:[pageId]/slot:[positive integer]

modern-grid-1280x720/v1 is a fixed 3-column × 2-row grid:

- canvas: 1280 wide × 720 high
- supported slots: 1 through 6
- outer margins: 24 on left, top, right, and bottom
- horizontal gutter: 16
- vertical gutter: 16
- slot width: 400
- slot height: 328
- z and tabOrder are zero-based multiples of 1000 derived directly from slot

| Slot | x | y | width | height | z | tabOrder |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 24 | 24 | 400 | 328 | 0 | 0 |
| 2 | 440 | 24 | 400 | 328 | 1000 | 1000 |
| 3 | 856 | 24 | 400 | 328 | 2000 | 2000 |
| 4 | 24 | 368 | 400 | 328 | 3000 | 3000 |
| 5 | 440 | 368 | 400 | 328 | 4000 | 4000 |
| 6 | 856 | 368 | 400 | 328 | 5000 | 5000 |

The right edge of slots 3 and 6 is 1256, leaving the required 24 right margin. The bottom edge of slots 4–6 is 696, leaving the required 24 bottom margin.

A page may use fewer than six slots and may leave gaps. It may not contain more than six visuals. Slot is read only from IR Visual.Placement; z and tabOrder are not derived from list position. Sorting a page's visuals by IR Visual.Order must produce the same visual sequence as sorting by slot number, otherwise the input is contradictory and rejected.

Rejected:

- free-form placement
- duplicate slots
- placement page mismatch
- slot 0, a negative slot, or slot greater than 6
- more than six visuals on a page
- IR Visual.Order that contradicts slot order
- coordinates outside the fixed profile
- layout containers that omit or duplicate visual references
- responsive hints requiring a mobile artifact

### Visuals And Semantic Bindings

Initial visual allowlist:

- card
- table
- clusteredColumnChart
- lineChart

The allowlist is a serializer configuration constant with provenance, not a permissive pass-through.

Role requirements:

- card: exactly one measure projection
- table: one or more column or measure projections in Values
- clusteredColumnChart: exactly one category column plus one or more measure projections
- lineChart: exactly one category column plus one or more measure projections

### Exact IR, Inventory, And Role-Binding Mapping

Page mapping:

| Source | Validation | PBIR destination |
| --- | --- | --- |
| IR Page.PageIdentity | nonempty, ordinal-unique | input to deterministic page identity hash |
| IR Page.PageId | nonempty, ordinal-unique | page.json displayName |
| IR Page.Order | positive, unique, contiguous 1..page count | pages.json pageOrder |
| IR Page.NavigationBehavior | must equal pageTab | authorizes page-tab navigation; not emitted |
| IR Page.IntendedPurpose | nonempty | manifest unsupported-section diagnostic only |

Navigation mapping:

- Navigation.LandingPage must exactly match one Page.PageId and maps to pages.json activePageName through that page's generated identity.
- PageTransitions must contain exactly the consecutive Page.Order pairs. For each pair, FromPageId and ToPageId must match the two pages and Transition must equal fromPageId + "->" + toPageId.
- Bookmarks must contain only page:[pageId] for every declared page plus landing:[landingPage]. These are recognized canonical IR markers and do not produce bookmark files.
- DrillPaths must be empty.
- No other navigation value is ignored or converted.

Layout mapping:

- Each page has exactly one IR Layout.Container whose PageId matches the page.
- Container.VisualRefs must equal the ordinal set of Visual.VisualId values on the page, with no pageShell entry, omission, duplicate, or foreign visual.
- Visual.Placement must parse exactly as page:[Visual.PageId]/slot:[1..6].
- Layout.Spacing must equal the single-item list standard-8px-grid.
- Layout.Alignment must equal the two-item list deterministic-grid, visual-placement-preserved in that order.
- Layout.ResponsiveHints must equal preserve-page-order, preserve-visual-intent, allow-future-serializer-layout-adaptation in that order. The final value authorizes only modern-grid-1280x720/v1; it does not authorize mobile output.
- ContainerId and Purpose remain lineage/diagnostic context and are not emitted.

Visual-to-semantic linkage:

1. Visual.VisualId and Visual.PageId must be nonempty and ordinal-unique; PageId must match one page.
2. Visual.VisualType is copied exactly to visual.visualType after allowlist validation.
3. Visual.SemanticIntent must exactly match one Kpis entry in exactly one semantic record on the same page.
4. That semantic record must contain the exact relationship string visual:[Visual.VisualId]->semantic:[Visual.SemanticIntent].
5. All semantic relationship strings must use that visual-link grammar and point to declared visuals and KPI tokens. Filter, page, or invented relationship forms are rejected.
6. The semantic record's Filters must be empty and DrillBehavior must equal none.
7. Visual.InteractionModel must equal the single-item list none.
8. Each visual has exactly one VisualBinding record whose VisualId matches it; extra or duplicate bindings are rejected.

Projection resolution:

1. Bindings are grouped by their explicit role and sorted by projectionOrder ascending within the role. projectionOrder must be positive, contiguous from 1 within its role, and unique.
2. sourceSemanticToken must occur exactly once in the linked semantic record:
   - a token in Measures must reference an inventory entry of kind measure
   - a token in Dimensions must reference an inventory entry of kind column
   - tokens from Kpis, Filters, or Relationships cannot be projected
3. semanticModelEntryRef must match the entryId of the unique inventory entry whose token equals sourceSemanticToken.
4. Entity and Property are copied from that inventory entry into the direct Measure or Column expression.
5. role, queryRef, nativeQueryRef, aggregation, displayName, and format come only from the binding. The serializer never selects or rewrites them.
6. role must match the exact visual-specific role set described below.
7. aggregation must equal none, displayName must be null, and format must be null. Any other value is unsupported and blocks output.
8. queryRef and nativeQueryRef must be nonempty, NFC, and ordinal-unique within the visual. They are copied exactly; Entity.Property is not synthesized.

Visual-specific role contract:

| VisualType | Required roles | Cardinality | Allowed kind |
| --- | --- | --- | --- |
| card | Fields | exactly 1 | measure |
| table | Values | 1 or more | column or measure |
| clusteredColumnChart | Category, Y | Category exactly 1; Y 1 or more | Category column; Y measure |
| lineChart | Category, Y | Category exactly 1; Y 1 or more | Category column; Y measure |

No additional role is accepted. Role names are case-sensitive.

Supported semantic expressions:

- direct model column reference
- direct model measure reference

Rejected:

- unsupported or custom visual types
- auto dimensions
- implicit aggregation
- calculated columns or measures
- visual calculations
- field parameters
- hierarchies
- ambiguous token resolution
- filters, relationships, KPIs, drill behavior, or interactions that cannot be represented exactly by the supported subset

The serializer does not infer tables, fields, roles, aggregations, display names, sort order, filters, or formatting.

## Microsoft Schema Lock

Reviewed source commit:

- microsoft/json-schemas commit 34356d97e1218c79331780f8f5b77b03f2d13f35

Locked emitted-document schemas:

- definition.pbir:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definitionProperties/2.0.0/schema.json
  - file format version 4.0
- definition/version.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/versionMetadata/1.0.0/schema.json
  - report definition version 1.0.0
- definition/report.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/report/1.0.0/schema.json
- definition/pages/pages.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/pagesMetadata/1.0.0/schema.json
- page.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/page/1.0.0/schema.json
- visual.json:
  - https://developer.microsoft.com/json-schemas/fabric/item/report/definition/visualContainer/1.0.0/schema.json

Required local reference fixtures also include the locked 1.0.0 formatting-object and semantic-query schemas referenced by emitted schemas.

Official sources:

- [Power BI Desktop project report folder and modern PBIR structure](https://learn.microsoft.com/en-us/power-bi/developer/projects/projects-report)
- [Microsoft PBIR JSON schemas](https://github.com/microsoft/json-schemas/tree/main/fabric/item/report)
- [definition.pbir schema family](https://github.com/microsoft/json-schemas/tree/main/fabric/item/report/definitionProperties)
- [modern PBIR definition schema family](https://github.com/microsoft/json-schemas/tree/main/fabric/item/report/definition)

Production code never downloads these URLs. Schema upgrades require an explicit code, fixture, documentation, and compatibility change.

### Runtime Versus Test-Time Validation

Phase 29 deliberately separates two guarantees:

- runtime schema-contract validation:
  - exact $schema URL and version allowlist
  - exact supported-template required properties
  - exact supported-template property types and forbidden-property checks
  - modern root/definition path rules
  - cross-file identities and references
  - visual role/cardinality/kind rules
  - layout bounds, hashes, and lineage
  - reported only as schemaContractResults
- test-time full schema conformance:
  - complete Draft 7 evaluation of every emitted document
  - pinned local Microsoft fixtures and all required local references
  - network resolution disabled
  - a build/test assertion, not a serialized runtime result

Full runtime Draft 7 evaluation is not claimed in Phase 29.

## Serialization Flow

1. Validate pbir-ir/v1 state and its existing pbir-serializer-request/v1 reference/hash.
   Recompute the canonical IR content hash from the current IR fields so a stale validation result or post-validation mutation fails closed.
2. Validate pbir-deployable-serializer-request/v1 and all trust-boundary flags.
3. Normalize and validate paths, identities, ordering, layout slots, navigation, model inventory, and visual bindings.
4. Return incomplete or blocked with no candidate output if preflight fails.
5. Build a private ordered file candidate.
6. Validate every candidate document against the runtime schema-lock/template contract and deterministic subset validators.
7. Validate file paths, root-format exclusivity, cross-references, identities, byte lengths, and hashes.
8. Construct immutable lineage, artifact, and manifest.
9. Revalidate artifact and manifest self-consistency.
10. Return serialized with the complete artifact and manifest.

## Trust Boundary

The Phase 29 public method accepts in-memory records and returns in-memory records.

Forbidden dependencies and behavior:

- System.IO file or directory mutation
- preview-writer reuse
- process execution
- HTTP or other network clients
- provider registries or runtimes
- Microsoft Skills
- APIs
- CLIs
- deployment or publishing
- Power BI Desktop automation
- Analyzer Workspace launch or validation
- Design Studio execution controls

Trust-boundary tests are precise rather than word-based:

- reflection asserts the exact internal service entry point accepts only PbirIntermediateRepresentationState, PbirSerializerRequest, and PbirDeployableSerializerRequest and returns PbirDeployableSerializerState
- reflection asserts constructor and field dependency types are limited to PbirDeployableSerializerSafetyGate, PbirDeployableSerializerValidator, and PbirDeployableSerializerCanonicalJson
- reflection asserts no serializer method returns or accepts FileInfo, DirectoryInfo, FileSystemInfo, Stream, HttpClient, Process, provider/runtime interfaces, writer interfaces, or Analyzer/Design Studio service types
- project-reference checks prove Phase 29 adds no provider, CLI, network, Desktop, or extension-host dependency

Tests do not scan source for broad or incidental tokens. Contract names and negative authority fields including providerInvocationAllowed, apiInvocationAllowed, deploymentAllowed, and deployableSerializerRequestRef remain legal and required.

## Testing Strategy

Tests are written before implementation.

Positive coverage:

- byte-identical output and hashes for identical IR and request
- coherent required modern PBIR inventory
- no root-level report.json
- stable page/visual ids, ordering, paths, canonical JSON, hashes, and lineage
- card, table, clustered column, and line visual projections
- local schema-fixture validation for every emitted document
- unchanged preview serializer output when serializerImplementationAvailable is true

Fail-closed coverage:

- unsupported schemas or target format
- unsupported visual type
- missing, ambiguous, duplicate, extra, or wrong-kind semantic binding
- invalid model reference
- auto or invented semantic requirement
- unsafe dataset or artifact path
- duplicate page, visual, container, slot, or generated identity
- invalid navigation or unsupported bookmarks/drill behavior
- schema incompatibility
- tampered content, byte length, hash, file set, manifest, or lineage
- any forbidden execution flag

Boundary coverage:

- no filesystem writes or writer dependency
- no network, provider, Microsoft Skills, API, CLI, Desktop, deployment, publishing, or Analyzer surface
- rejected input returns null artifact and null manifest
- preview serializer continues to emit preview-only artifacts, rejects deployable requests, and gains no deployable authority after serializerImplementationAvailable becomes true

Full validation records actual counts for:

- backend xUnit tests
- all extension and webview Jest tests
- TypeScript compilation

## Documentation Changes At Implementation Close

- add modern PBIR serializer current-state documentation
- update PBIR IR and preview serializer current state without widening preview behavior
- update architecture-gap analysis
- update docs/ROADMAP.md with the repository-phase-to-original-roadmap mapping
- update the original roadmap plan with the Phase 4A/4B mapping
- update repository memory and the timestamped session note
- name the next separate phase as safe local deployable PBIR materialization with preview/apply/rollback controls

## Phase Exit Gate

Phase 29 is complete only when:

- all supported inputs produce deterministic, schema-valid in-memory modern PBIR artifacts
- every rejection returns no artifact and no manifest
- focused and full validation pass with actual counts recorded
- documentation maps Repository Phase 29 to original Phase 4A
- documentation names original Phase 4B as the next separate safe materialization phase

After the exit gate, stop. Do not begin materialization or any provider-execution work without a new goal.

## Downstream Phase 31 Integration

Repository Phase 31 was separately authorized on 2026-08-02. Its application orchestrator invokes this unchanged canonical serializer before every preview and apply. It does not reproduce Phase 29 serialization, validation, schema-lock, canonical JSON, identity, lineage, or hashing logic.
