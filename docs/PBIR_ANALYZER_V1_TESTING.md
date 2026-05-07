# PBIR Analyzer V1 Testing

This repo now includes opt-in backend integration coverage against the real `PBITesting` fixture used by the PBIR analyzer plan.

The canonical product and behavior specification lives in [PBIR_ANALYZER_V1_SPEC.md](./PBIR_ANALYZER_V1_SPEC.md).

## Real Fixture Path

The integration tests look for the fixture in this order:

1. `PBIR_REAL_FIXTURE_PATH`
2. `~/Documents/GitHub/PBITesting/Sales & Production.pbip`

`PBIR_REAL_FIXTURE_PATH` may point either to the `.pbip` file itself or to the `PBITesting` directory that contains it.

## Run The Fixture Tests

```bash
dotnet test service-dotnet/tests/Tests.csproj --filter Category=PBITesting
```

Example with an explicit path override:

```bash
PBIR_REAL_FIXTURE_PATH="/Users/bcrowell/Documents/GitHub/PBITesting/Sales & Production.pbip" \
dotnet test service-dotnet/tests/Tests.csproj --filter Category=PBITesting
```

These tests verify:

- PBIR project detection against the real `Sales & Production.pbip`
- tree discovery for the 21-page, 172-visual report
- full-report scoring
- single-page scoring for `Net Sales`
- governance evaluation using a strict temporary workspace policy
- scoring on a fixture that contains bookmark references and custom visuals

## Verified Build And Package Flow

The current PBIR analyzer build path is:

```bash
cd vscode-extension
npm run build
npm run package
```

`npm run build` publishes the minimal packaged backend to `vscode-extension/backend/lsp`, then rebuilds the TypeScript extension and React webview bundles. `npm run package` runs that prepublish path again before creating `pbir-design-analyzer-0.1.9.vsix`.

## Manual Smoke Flow

1. Open VS Code on `/Users/bcrowell/Documents/GitHub/PBITesting`.
2. Start the extension from this repo.
3. Run `PBIR Analyzer: Open PBIP Project`.
4. Select `Sales & Production.pbip`.
5. Verify the PBIR tree loads.
6. Run `PBIR Analyzer: Score Report`.
7. Run `PBIR Analyzer: Configure Scoring` and verify score changes persist.
8. Run `PBIR Analyzer: Check Governance` with permissive and strict settings.

This document is intentionally focused on the PBIR analyzer fixture workflow and the verified local build/package path. Broader publishing cleanup is still tracked separately.
