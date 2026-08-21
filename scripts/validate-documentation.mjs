import fs from 'node:fs';
import path from 'node:path';
const root = path.resolve(new URL('..', import.meta.url).pathname);
const read = (file) => fs.readFileSync(path.join(root, file), 'utf8');
const errors = [];
for (const file of ['README.md', 'docs/current-state/RELEASING.md', 'docs/product/scope.md', 'docs/release-evidence/v1.0-readiness-report.md']) {
  if (!fs.existsSync(path.join(root, file))) errors.push(`missing authoritative document: ${file}`);
}
const readme = read('README.md');
for (const match of readme.matchAll(/\]\(([^)#]+)(?:#[^)]+)?\)/g)) {
  const link = match[1];
  if (link.startsWith('http') || link.startsWith('mailto:')) continue;
  if (!fs.existsSync(path.resolve(root, link))) errors.push(`broken current README link: ${link}`);
}
const release = read('docs/current-state/RELEASING.md');
const targets = JSON.parse(read('vscode-extension/config/release-targets.json')).targets.map((item) => item.target);
for (const target of targets) if (!release.includes(target)) errors.push(`release guide omits target: ${target}`);
if (release.includes('docs/RELEASING.md')) errors.push('release guide contains stale docs/RELEASING.md authority');
if (errors.length) { console.error(errors.join('\n')); process.exit(1); }
console.log(`Documentation validation passed: ${targets.length} current targets and authoritative links verified.`);
