import fs from 'fs';
import path from 'path';

const extensionRoot = process.cwd();
const repositoryRoot = path.resolve(extensionRoot, '..');
const contractPath = path.join(extensionRoot, 'config', 'release-targets.json');
const contract = JSON.parse(fs.readFileSync(contractPath, 'utf8'));
const packageJson = JSON.parse(fs.readFileSync(path.join(extensionRoot, 'package.json'), 'utf8'));
const lockJson = JSON.parse(fs.readFileSync(path.join(extensionRoot, 'package-lock.json'), 'utf8'));
const targetNames = contract.targets.map((target) => target.target);
const issues = [];

if (contract.schemaVersion !== 'release-targets/v1') {
  issues.push(`Unsupported release target contract: ${contract.schemaVersion}`);
}
if (new Set(targetNames).size !== targetNames.length || targetNames.some((target) => !target)) {
  issues.push('Release target identifiers must be unique and non-empty.');
}
if (packageJson.version !== lockJson.version || packageJson.version !== lockJson.packages?.['']?.version) {
  issues.push('package.json and package-lock.json versions are inconsistent.');
}

const requiredTargets = targetNames.flatMap((target) => [
  `pbir-design-analyzer-${packageJson.version}-${target}.vsix`,
  target,
]);
const documentationPaths = [
  path.join(repositoryRoot, 'README.md'),
  path.join(extensionRoot, 'README.md'),
  path.join(repositoryRoot, 'docs', 'current-state', 'RELEASING.md'),
];
for (const filePath of documentationPaths) {
  const content = fs.readFileSync(filePath, 'utf8');
  for (const required of requiredTargets) {
    if (!content.includes(required)) {
      issues.push(`${path.relative(repositoryRoot, filePath)} is missing release contract fact: ${required}`);
    }
  }
}

for (const workflow of ['ci.yml', 'release.yml']) {
  const filePath = path.join(repositoryRoot, '.github', 'workflows', workflow);
  const content = fs.readFileSync(filePath, 'utf8');
  if (!content.includes('fromJSON(needs.release-contract.outputs.targets)')) {
    issues.push(`${workflow} does not consume the release target contract output.`);
  }
}

if (issues.length > 0) {
  console.error('Release contract validation failed:');
  for (const issue of issues) console.error(`- ${issue}`);
  process.exit(1);
}

console.log(`Release contract validation passed for ${targetNames.length} targets at version ${packageJson.version}.`);
