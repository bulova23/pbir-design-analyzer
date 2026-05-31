# Intent Label Enrichment And Evidence Labels Plan

## Scope

Apply three tightly scoped improvements on top of the implemented issues-workspace foundation:

1. Rename `Framework Analysis` to `Design Framework Analysis`.
2. Rename `Screenshot Audit` to `AI Screenshot Audit`.
3. Strengthen inferred story / intent wording by using richer business-facing field and measure labels when PBIR metadata exposes them.

## Constraints

- Do not redesign the broader workspace architecture.
- Do not add a full semantic-model parser.
- Keep the implementation deterministic.
- Only use richer label metadata when it is already available in PBIR-exposed visual binding metadata.

## Implementation Steps

1. Update Evidence-section labels in the analyzer-score webview.
2. Enrich PBIR role-hint extraction to recognize business-facing aliases such as display labels, synonyms, aliases, and descriptions when present in role metadata objects.
3. Add a semantic-hint selector so story inference prefers concise human-facing labels over raw technical names or query refs.
4. Extend story-inference backend tests to cover enriched role metadata.
5. Update any webview text assertions impacted by the label changes.
6. Re-run focused webview/backend tests plus full validation.

## Expected Result

- Users see `Design Framework Analysis` and `AI Screenshot Audit` in Evidence.
- Inferred story/intent uses better metric and dimension names when PBIR provides richer role metadata.
- The system still falls back safely to existing visible-title and raw field-role hints when richer metadata is absent.
