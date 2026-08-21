import fs from 'fs';
import path from 'path';

const root = process.cwd();
const manifestPath = path.join(root, 'config', 'protocol-contracts.json');
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
if (manifest.schemaVersion !== 'protocol-contracts/v1') {
  throw new Error(`Unsupported protocol contract manifest: ${manifest.schemaVersion}`);
}

for (const contract of manifest.contracts) {
  for (const source of contract.sources) {
    const sourcePath = path.resolve(root, source);
    if (!fs.existsSync(sourcePath)) {
      throw new Error(`${contract.id} references missing source: ${source}`);
    }
    const contents = fs.readFileSync(sourcePath, 'utf8');
    for (const literal of contract.requiredLiterals) {
      if (!contents.includes(literal)) {
        throw new Error(`${contract.id} is missing ${JSON.stringify(literal)} in ${source}`);
      }
    }
  }
}

console.log(`Protocol contract validation passed for ${manifest.contracts.length} contracts.`);
