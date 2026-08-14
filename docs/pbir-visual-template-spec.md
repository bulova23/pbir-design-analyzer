# PBIR Visual Template Specification

Phase 40 provides three deterministic, strongly typed templates.

| Template | Characteristics |
| --- | --- |
| Default | White chart background, visible axes and legends when explicitly configured, neutral title behavior |
| Executive | Light gray-blue background, Executive Summary title fallback, visible axes, bottom legends |
| Compact | White background, hidden axis and legend defaults, minimal title behavior |

Templates are selected by enum in v5 authoring and resolved from a static catalog. Explicit visual settings override template defaults. Templates do not generate identifiers or timestamps; they participate only in canonical input and artifact hashes through the normal serializer request.

The serializer projects template results through existing schema-safe PBIR primitives. It does not emit arbitrary custom visual-container objects.

