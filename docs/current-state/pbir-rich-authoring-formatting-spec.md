# PBIR Rich Authoring Formatting Specification

Phase 38 uses typed internal contracts rather than raw JSON formatting blobs. The v3 request is additive and optional: a Phase 36 or Phase 37 request can be submitted without authoring and produces the existing output shape.

## Contract Shape

Authoring is divided into report, page, and visual concerns:

- Report authoring contains Theme, ReportFilters, Metadata, Interaction, and Layout.
- Page authoring contains Background and page Filters.
- Visual authoring contains Card or Table formatting, visual Filters, Interaction, and Padding.
- Color, Padding, TextStyle, BoxStyle, and column formatting are strongly typed records.

The provider validates identifiers, color values, numeric ranges, duplicate filter fields, supported visual/formatting combinations, theme values, and layout values before calling the existing serializer.

## PBIR Mapping

The serializer maps supported values to schema-safe PBIR properties:

- report and page filterConfig values use categorical Version 2 equality filters;
- page backgrounds use the page background formatting object;
- Card and Table values use visual formatting objects and formatString properties;
- interactions use page visualInteractions and report settings where applicable;
- theme identity and deterministic palette values use report theme metadata and annotations.

The pinned offline schemas remain the acceptance boundary. A new formatting value cannot bypass serializer validation, materialization schema validation, or analyzer round-trip.

## Deterministic Rules

Filter arrays are ordered by filter identity. Palette values are ordered by hex value for metadata emission. Visual interactions are ordered by page, source, and target. Layout defaults use fixed dimensions, margin, and spacing; explicit Phase 37 layouts remain unchanged. Authoring values do not create random identifiers.

## Deliberate Deferrals

Advanced filter expressions, conditional formatting, chart bindings, bookmarks, drillthrough, custom visuals, semantic-model generation, DAX, and arbitrary theme resource packages are deferred. The next recommended architecture step is a generalized visual-binding model for chart categories, series, and axes.
