import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(new URL('..', import.meta.url).pathname);
const fixtures = JSON.parse(fs.readFileSync(path.join(root, 'contracts', 'score-panel', 'v1', 'fixtures.json'), 'utf8'));
function isObject(value) { return value !== null && typeof value === 'object' && !Array.isArray(value); }
function validate(payload) {
  if (!isObject(payload) || payload.protocolVersion !== 1 || payload.schemaVersion !== 1 || typeof payload.type !== 'string') return false;
  if (payload.state !== undefined) {
    const state = payload.state;
    if (!isObject(state) || !isObject(state.result) || !Number.isInteger(state.selectedPageIndex) || state.selectedPageIndex < 0) return false;
    if (!Number.isInteger(state.result.pageCount) || state.result.pageCount < 0 || typeof state.result.compositeScore !== 'number' || state.result.compositeScore < 0 || state.result.compositeScore > 100) return false;
  }
  if (payload.mutation !== undefined) {
    const mutation = payload.mutation;
    if (!isObject(mutation) || !['preview', 'apply', 'rollback'].includes(mutation.operation) || typeof mutation.requestId !== 'string' || !mutation.requestId || typeof mutation.previewHash !== 'string' || !mutation.previewHash) return false;
    if (mutation.transactionId !== undefined && mutation.transactionId !== null && typeof mutation.transactionId !== 'string') return false;
  }
  return true;
}
let failed = 0;
for (const fixture of fixtures) {
  const actual = validate(fixture.payload);
  if (actual !== fixture.valid) { console.error(`${fixture.name}: expected ${fixture.valid}, got ${actual}`); failed++; }
}
if (failed) process.exit(1);
console.log(`Contract compatibility passed for ${fixtures.length} fixtures.`);
