import fs from 'fs';
import path from 'path';

const targets = process.argv.slice(2);

for (const target of targets) {
  const resolved = path.resolve(target);
  fs.rmSync(resolved, { recursive: true, force: true });
}
