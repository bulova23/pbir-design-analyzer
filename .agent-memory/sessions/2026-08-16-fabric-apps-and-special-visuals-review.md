# Fabric App and Special Visual Capability Review

Date: 2026-08-16

## Question

Assess whether the current product can analyze analytical Fabric Apps and PBIR reports containing HTML/custom or Deneb visuals, and identify the practical limitations.

## Findings

- Fabric App support is real but advisory-only: surface discovery expects a local TypeScript repository with package.json/tsconfig/vite indicators, route/navigation artifacts, and analytics-looking TypeScript. The review analyzer extracts bounded TypeScript layout, navigation, design-token, screenshot filename, and semantic-model usage evidence.
- Fabric App Review does not execute the app, inspect a deployed/live Fabric App, validate runtime behavior, or reuse the PBIR scoring engine for app visuals. It is not a general Fabric App or frontend quality analyzer.
- PBIR scoring is metadata-first. Unknown/custom visual types are tolerated and remain visible in metadata/tree/governance classification, but most chart-intent and visual-family logic recognizes built-in Power BI types. Deneb/HTML/custom internals are not parsed as their own semantic visual domains.
- HTML/Deneb visuals can therefore contribute generic layout/position/count/title/field signals when those are available in PBIR JSON, but cannot currently receive reliable visual-specific review of HTML/CSS/JavaScript, Vega/Vega-Lite specifications, accessibility semantics, or rendered chart behavior.
- Manual screenshot attachment plus the optional visual-audit provider can review rendered appearance for any visual technology, but this is evidence-driven and not automatic; it does not create structured Deneb/HTML semantics or deterministic fixes.
- Visual Intelligence & Screenshot Analysis remains a separate roadmap epic. The current roadmap/spec explicitly calls for screenshot-to-finding linkage, overlays, reading-order/density/alignment/focus annotations, not a custom visual parser.

## Evidence inspected

- vscode-extension/src/analyzer/surfaces/{types,catalog,discovery,fabricAppDiscovery}.ts
- vscode-extension/src/analyzer/fabric/review/{fabricAppReviewAnalyzer,typescriptEvidence,navigationEvidence,designTokenEvidence,screenshotEvidence,semanticModelEvidence}.ts
- service-dotnet/Services/Pbir/{ReportModelLoader,PbirScoringService,PbirScoringFoundationModels}.cs
- docs/ROADMAP.md
- docs/superpowers/specs/2026-06-03-fabric-apps-analytics-review-design.md
- docs/superpowers/specs/2026-05-31-visual-intelligence-screenshot-analysis-design.md
- docs/current-state/PBIR_ANALYZER_V1_SPEC.md

## Conclusion

The product has moved beyond PBIR-only at the platform boundary, but not to parity across analytical surfaces or visual technologies. The honest v1.0 claim is: strong PBIR structural/design review, bounded advisory review of local analytical Fabric App source, and optional screenshot-based review. It is not yet a reliable Deneb/HTML/custom-visual semantic or runtime analyzer.
