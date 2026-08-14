# Phase 43 Lossless Authoring IR, Identity Preservation, and Round-Trip Fidelity

## Objective

Promote the shared PBIR intermediate representation from a generation-oriented projection to a bounded hybrid authoring representation. Imported schema-supported authoring state will remain available through a typed projection plus a validated opaque preservation envelope. Typed mutations will be merged into that envelope before schema validation and serialization.

Phase 43 remains backend-only. It does not expose RPC, add report features, or create a general JSON patch facility.

## Current loss matrix

The matrix is based on the Phase 42 reader, IR models, mutation executor, and deployable serializer rather than assumptions about PBIR.

| Construct | Current import | Current IR | Current serializer | Phase 43 classification |
| --- | --- | --- | --- | --- |
| Report definition and dataset reference | Not imported; dataset is supplied to serializer | Synthesized references only | Regenerated | OpaquePreserved for supported report envelope; typed dataset override remains generation-owned |
| Report identity | Synthesized from IR ID | No explicit imported identity | Derived indirectly | TypedSupported identity plus envelope ownership |
| Pages and page folders | Page order, folder name, display name | Page ID, page identity, order, display name; navigation fields synthesized empty | Folder identities regenerated | TypedSupported identity/order/name; opaque page properties preserved |
| Page formatting | Discarded | None | Regenerated/defaulted | OpaquePreserved |
| Page filters | Discarded | Only semantic filter labels | Regenerated/defaulted | OpaquePreserved; typed mutation only through modeled contract |
| Visual folders and identities | Folder name captured as logical ID | Visual ID, page ID, type, order | Folder identities derived from IR | TypedSupported identity and ownership |
| Visual layout | Position imported | Typed layout | Re-emitted from typed layout | TypedSupported |
| Visual bindings | Query projections imported | Typed bindings | Regenerated from bindings | TypedSupported; original query subtree retained for unrelated properties |
| Visual formatting | Discarded | None | Regenerated/defaulted | OpaquePreserved |
| Visual authoring properties | Discarded | Type and semantic intent only | Regenerated/defaulted | OpaquePreserved |
| Themes | Discarded | None | Not preserved | OpaquePreserved where pinned schema identifies a supported theme object |
| Report/page/visual filters | Discarded except semantic labels | Partial semantic labels only | Regenerated/defaulted | OpaquePreserved; typed filter operations remain bounded |
| Navigation and active page | Active page inferred; transitions/bookmarks synthesized | Partial typed navigation | Regenerated | TypedSupported targeted navigation plus opaque navigation metadata |
| Slicers | Visual type only | No slicer metadata | Regenerated as generic visual | TypedSupported for existing slicer identity/layout/binding; remaining metadata opaque |
| Unknown schema-supported properties | Discarded | None | Discarded | OpaquePreserved only after schema-lock eligibility check |
| Unsupported schema or construct | Closed-catalog diagnostic | No usable IR | Not serialized | Unsupported and fail closed |
| File ordering and JSON property ordering | File hashes only | Not represented | Canonical ordering | Preserved when safe through envelope metadata; canonical normalization reported otherwise |

## Authoring model

Add a bounded `PbirAuthoringEnvelope` to the shared IR state. The envelope models report, page, visual, navigation, theme, filter, slicer, and layout ownership explicitly. Each owned item contains:

- logical typed identity and imported PBIR identity/folder relationship;
- relative source file and schema URL/version;
- the original supported JSON subtree;
- a classification of `TypedSupported`, `OpaquePreserved`, or `Unsupported`;
- stable source hash and source ordering metadata where available.

The envelope is not a filesystem snapshot. It stores only definition JSON objects that are admitted by the pinned schema lock and that belong to known PBIR authoring owners. Unsupported files, schema versions, and structures remain diagnostics and cannot produce a ready authoring state.

The typed IR remains authoritative for fields used by validation, analysis, mutation, generation, and identity targeting. The envelope remains authoritative for untouched supported fields. Generated IR has no imported envelope and continues using existing deterministic identities and serializer output.

## Merge precedence

Introduce one focused `PbirAuthoringMergeService` between mutation execution and serialization:

1. Start with the imported envelope and its original subtrees.
2. Apply typed IR changes recorded by the validated mutation plan.
3. For each changed typed field, replace only the corresponding property/subtree in the owned JSON object.
4. Preserve all unrelated properties and their source ownership.
5. Reject a mutation when its target is not typed-supported or when its merged result cannot satisfy the pinned schema.
6. Return a resolved authoring representation consumed by the serializer.

The serializer will not contain field-level precedence decisions. It will emit resolved envelope content and generate only objects with no imported owner or objects explicitly added by a mutation.

There is no raw JSON replacement, JSON Pointer, JSON Patch, arbitrary subtree mutation, or opaque override path.

## Identity policy

Each page and visual exposes logical ID, imported PBIR identity, generated identity, and optional explicit override as separate values. Imported identity is selected for unchanged imported objects. New objects receive the existing deterministic provider identity. An explicit typed identity override is validated for uniqueness and ownership before it can be selected. Serializer folder paths and references are resolved from this identity policy, never recomputed from the IR ID for an imported object.

Report identity, navigation identity, slicer identity, binding identity, layout ownership, and formatting ownership follow the same imported-versus-generated distinction where the pinned schema provides an addressable identity. Where PBIR has no independent identity, the envelope records its owning file and property path instead of inventing one.

## Fidelity and diagnostics

Round-trip validation compares source and output by:

- byte-identical file hash;
- canonical semantic JSON equality;
- expected normalized difference;
- unexpected difference.

The result reports preserved, changed, and unsupported paths. Hashes are measured, never manipulated. An unchanged report may have canonical differences caused by serializer formatting or ordering; those differences must be listed explicitly. Any changed path outside the requested mutation is an unexpected difference and blocks a fidelity-ready result.

Mutation evidence will add identity, authoring-preservation, fidelity, hash-delta, and analyzer-before/after summaries while retaining the existing result contract compatibility. Analyzer comparison remains advisory evidence and does not become mutation authority.

## Compatibility and safety

Generation requests continue through the current typed-only path and must produce the same canonical artifacts. Existing Phase 42 mutation requests remain valid; operations that still lack a typed mutation contract continue to fail closed. The envelope cannot make unsupported content safe by itself. Schema validation runs after merge and before artifact readiness.

Unknown schema-supported fields are preserved only when their owning file and schema are in the pinned schema inventory. Unknown schema content outside that inventory is unsupported. This is the explicit boundary between preservation and accidental acceptance.

## Validation plan

Add fixture and focused tests for reader envelope capture, serializer round trips, imported identity stability, generated identity allocation, formatting/theme/filter/navigation/slicer preservation, single-field mutation isolation, unsupported-content diagnostics, absence of generic JSON mutation APIs, analyzer comparison, and hash categories. Run focused backend tests first, then the complete backend suite, .NET build, extension compile/build/tests, and `git diff --check`. Performance timings will compare import, planning, execution, serialization, and analyzer stages with Phase 42 observations without imposing a numeric regression threshold.

## Explicit limitations

Phase 43 does not model every PBIR field. Bookmarks, drillthrough, shared slicers, semantic-model generation, DAX generation, Desktop automation, hosted execution, RPC, VS Code commands, and provider security remain out of scope. Unsupported PBIR constructs continue to fail closed.

