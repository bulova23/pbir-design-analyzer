# Post-v1 Decomposition Baseline

Repository root: /Users/bcrowell/Documents/GitHub/pbir-design-analyzer/.worktrees/tasks1-4-remediation-20260909
Candidate count: 9

The measurements below are advisory complexity signals. Candidate presence and disposition are enforced.

| Candidate | Lines | Classification | Target owner | Declarations | Imports | Mutable fields | Side effects |
|---|---:|---|---|---:|---:|---:|---|
| service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs | 1974 | deferred provider | future provider program | 385 | 8 | 2 | Directory. |
| service-dotnet/Services/Discovery/RecommendationEngineService.cs | 2568 | deferred discovery | existing discovery domain | 368 | 1 | 0 | none |
| service-dotnet/Services/Pbir/PbirScoringService.cs | 9917 | shipped scoring | scoring orchestrator | 1219 | 7 | 35 | Console. |
| service-dotnet/Services/Pbir/ScoreResultAssemblyService.cs | 205 | shipped score boundary | authoritative result assembly | 8 | 1 | 2 | none |
| vscode-extension/src/views/PbirScorePanel.ts | 921 | shipped host | panel lifecycle/coordinators | 84 | 37 | 0 | window., postMessage(, sendEvent( |
| vscode-extension/src/views/scorePanelMessageRouter.ts | 174 | shipped routing | cohesive routing boundary | 5 | 4 | 0 | window. |
| vscode-extension/src/views/scorePanelProtocol.ts | 468 | shipped protocol | cohesive protocol boundary | 64 | 3 | 0 | none |
| vscode-extension/src/views/scoreResultPayload.ts | 1119 | shipped boundary | normalizer/projections | 180 | 14 | 0 | none |
| vscode-extension/webview-src/analyzer-score/App.tsx | 4574 | shipped workspace | workspace composition | 363 | 9 | 0 | window., postMessage( |

## Runtime composition and controls

- Backend composition: `service-dotnet/RpcHost/Program.cs` constructs the shipped scoring, tree, governance, and project services; `AnalyzerRpcDispatcher.cs` routes scoring, tree, governance, materialization, and authoring boundaries.
- Host composition: `vscode-extension/src/views/PbirScorePanel.ts` owns lifecycle and routes fire-and-forget messages through the validated protocol/router boundary.
- Controls: backend architecture tests, score-panel contract validation, protocol tests, selected-page clamping tests, characterization/golden scripts, deterministic repeat, package acceptance, and mutation/rollback acceptance remain authoritative.

## Candidate manifest

- service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs
- service-dotnet/Services/Discovery/RecommendationEngineService.cs
- service-dotnet/Services/Pbir/PbirScoringService.cs
- service-dotnet/Services/Pbir/ScoreResultAssemblyService.cs
- vscode-extension/src/views/PbirScorePanel.ts
- vscode-extension/src/views/scorePanelMessageRouter.ts
- vscode-extension/src/views/scorePanelProtocol.ts
- vscode-extension/src/views/scoreResultPayload.ts
- vscode-extension/webview-src/analyzer-score/App.tsx
