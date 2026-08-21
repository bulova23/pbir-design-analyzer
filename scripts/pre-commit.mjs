import { spawnSync } from 'child_process';

function run(command, args, cwd = process.cwd()) {
  const result = spawnSync(command, args, { cwd, stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}

const staged = spawnSync('git', ['diff', '--cached', '--name-only', '--diff-filter=ACMR'], { encoding: 'utf8' })
  .stdout.split(/\r?\n/).filter(Boolean);
const extensionRoot = 'vscode-extension';
const changedTypeScript = staged.filter((file) => /^vscode-extension\/src\/.*\.(ts|tsx)$/.test(file));

run('git', ['diff', '--cached', '--check']);
if (changedTypeScript.length > 0) {
  run('npx', ['eslint', ...changedTypeScript.map((file) => file.slice(`${extensionRoot}/`.length)), '--no-warn-ignored'], extensionRoot);
}
if (staged.some((file) => file.startsWith('vscode-extension/') || file === 'README.md' || file.startsWith('docs/'))) {
  run('node', ['scripts/validate-release-contract.mjs'], extensionRoot);
  run('node', ['scripts/validate-protocol-contracts.mjs'], extensionRoot);
}
