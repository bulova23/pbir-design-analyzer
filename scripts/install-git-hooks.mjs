import fs from 'fs';
import path from 'path';
import { spawnSync } from 'child_process';

const root = process.cwd();
const hooksPath = path.join(root, '.githooks');
fs.mkdirSync(hooksPath, { recursive: true });
for (const name of ['pre-commit', 'pre-push']) {
  const target = path.join(hooksPath, name);
  fs.writeFileSync(target, `#!/bin/sh\nexec node scripts/${name}.mjs\n`);
  fs.chmodSync(target, 0o755);
}
const result = spawnSync('git', ['config', 'core.hooksPath', '.githooks'], { cwd: root, stdio: 'inherit' });
if (result.status !== 0) process.exit(result.status ?? 1);
console.log('Installed repository fast pre-commit and moderate pre-push hooks at .githooks.');
