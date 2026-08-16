import { spawnSync } from 'child_process';

if (process.env.PBIR_SKIP_VSCE_PREPUBLISH === '1') {
  process.exit(0);
}

const steps = [
  ['npm', ['run', 'compile']],
  ['npm', ['run', 'bundle:extension']],
  ['npm', ['run', 'build:webview']],
];

for (const [bin, args] of steps) {
  const result = spawnSync(bin, args, {
    stdio: 'inherit',
    shell: process.platform === 'win32',
  });

  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}
