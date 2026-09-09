import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const script = path.resolve(path.dirname(fileURLToPath(import.meta.url)), 'report-decomposition-baseline.mjs');
const root = path.resolve(path.dirname(script), '..');
const run = (args = []) => execFileSync(process.execPath, [script, ...args], { cwd: root, encoding: 'utf8' });

const first = run();
assert.equal(first, run(), 'baseline output must be byte-identical across runs');
assert.match(first, /Candidate count: 9/);

const temporaryRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-baseline-'));
try {
  const manifest = path.join(temporaryRoot, 'manifest.json');
  fs.writeFileSync(manifest, JSON.stringify([{ path: 'missing.cs', classification: 'test', targetOwner: 'test' }]));
  assert.throws(() => run(['--root', temporaryRoot, '--manifest', manifest]), /missing candidate/);
} finally {
  fs.rmSync(temporaryRoot, { recursive: true, force: true });
}

console.log('decomposition baseline script tests passed');
