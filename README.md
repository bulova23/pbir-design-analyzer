# PBIR Design Analyzer

![PBIR Design Analyzer logo](vscode-extension/resources/icon.png)

PBIR Design Analyzer is a focused VS Code extension for reviewing local Power BI PBIP/PBIR report projects before they are shared, governed, or published.

This repository contains only the public PBIR analyzer product surface:

- the VS Code extension in `vscode-extension/`
- the packaged .NET backend host in `service-dotnet/LspHost/`
- the PBIR scoring, governance, and analyzer support services in `service-dotnet/Services/Pbir/`
- PBIR-focused tests and public docs

## Product Scope

Included:

- open a local PBIP project or `.Report` folder
- inspect the PBIR tree for reports, pages, visuals, and theme references
- score a full report or a single page
- tune analyzer scoring and governance settings
- run governance checks against enterprise thresholds and rules

Not included in this public repo:

- TMDL authoring workflows
- Fabric live connection workflows
- AI/copilot features
- translation management
- monitoring dashboards
- PBIR report creation
- automated theme import or report-theme application workflows

## Build

Prerequisites:

- Node.js 18+
- .NET 8 SDK
- VS Code 1.93+

Build and package the extension:

```bash
cd vscode-extension
npm install
npm run build
npm run package
```

Run the PBIR-focused backend tests:

```bash
dotnet test service-dotnet/tests/Tests.csproj
```

## Docs

- [How To Use PBIR Design Analyzer](docs/HOW_TO_USE.md)
- [PBIR Analyzer V1 Spec](docs/PBIR_ANALYZER_V1_SPEC.md)
- [PBIR Analyzer V1 Testing](docs/PBIR_ANALYZER_V1_TESTING.md)
- [PBIR Troubleshooting](docs/PBIR_TROUBLESHOOTING.md)
- [Release Guide](docs/RELEASING.md)

## Feedback

Public bugs, feature requests, support questions, and documentation fixes should be submitted through the repository issue forms. See [CONTRIBUTING.md](CONTRIBUTING.md) for the intake and tracking workflow.

[Submit an issue or feature request](https://github.com/bulova23/pbir-design-analyzer/issues/new/choose)
