import { spawnSync } from 'child_process';

function run(command, args, cwd = process.cwd()) {
  const result = spawnSync(command, args, { cwd, stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}

const changed = spawnSync('git', ['diff', '@{upstream}...HEAD', '--name-only'], { encoding: 'utf8' }).stdout
  .split(/\r?\n/).filter(Boolean);

if (changed.some((file) => file.startsWith('service-dotnet/'))) {
  run('dotnet', ['test', 'service-dotnet/tests/Tests.csproj', '-c', 'Release']);
}
if (changed.some((file) => file.startsWith('vscode-extension/'))) {
  run('npm', ['run', 'compile'], 'vscode-extension');
  run('npm', ['test'], 'vscode-extension');
}
