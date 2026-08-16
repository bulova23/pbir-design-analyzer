import * as fs from 'fs';
import * as path from 'path';

describe('Phase 34 materialization workflow scope', () => {
  const repositoryRoot = path.resolve(__dirname, '../../..');
  const coordinatorPath = path.join(repositoryRoot, 'vscode-extension/src/services/materialization/PbirMaterializationWorkflow.ts');
  const componentPath = path.join(repositoryRoot, 'vscode-extension/webview-src/design-studio/components/LocalPbirMaterializationWorkflow.tsx');

  it('keeps the new workflow route-only and free of direct filesystem/backend bypass authority', () => {
    const coordinator = fs.readFileSync(coordinatorPath, 'utf8');
    const component = fs.readFileSync(componentPath, 'utf8');

    expect(coordinator.match(/pbir\/materialization\/(?:preview|apply|recovery\/inspect)/g)?.sort()).toEqual([
      'pbir/materialization/apply',
      'pbir/materialization/preview',
      'pbir/materialization/recovery/inspect',
    ]);
    expect(coordinator).not.toMatch(/from ['"](?:fs|path)['"]|PbirMaterializationOrchestrationService|PbirDeployableMaterialization/);
    expect(component).not.toMatch(/from ['"](?:fs|path)['"]|PbirMaterializationOrchestrationService|PbirDeployableMaterialization/);
  });
});
