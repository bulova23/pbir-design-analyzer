import fs from 'node:fs';
import path from 'node:path';
const root = path.resolve(new URL('..', import.meta.url).pathname);
const [fixtureId, reason, reviewer] = process.argv.slice(2);
if (!fixtureId || !reason || !reviewer) { console.error('Usage: node scripts/approve-pbir-golden-change.mjs <fixture-id> <reason> <reviewer/reference>'); process.exit(1); }
const output = path.join(root, 'docs', 'release-evidence', 'golden-change-approvals.jsonl');
fs.mkdirSync(path.dirname(output), { recursive: true });
fs.appendFileSync(output, `${JSON.stringify({ fixtureId, reason, reviewer, approvedAt: new Date().toISOString() })}\n`);
console.log(`Recorded approval metadata for ${fixtureId}; expected output was not changed.`);
