#!/usr/bin/env node

import fs from 'fs';
import path from 'path';

function readJson(filePath) {
  return JSON.parse(fs.readFileSync(filePath, 'utf8'));
}

function stableStringify(value) {
  if (Array.isArray(value)) {
    return `[${value.map((item) => stableStringify(item)).join(',')}]`;
  }

  if (value && typeof value === 'object') {
    return `{${Object.keys(value).sort().map((key) => `${JSON.stringify(key)}:${stableStringify(value[key])}`).join(',')}}`;
  }

  return JSON.stringify(value);
}

function compareValue(label, left, right, failures) {
  const matches = stableStringify(left) === stableStringify(right);
  console.log(`${matches ? 'MATCH' : 'DIFF '}  ${label}`);
  if (!matches) {
    failures.push(label);
    console.log(`  left : ${stableStringify(left)}`);
    console.log(`  right: ${stableStringify(right)}`);
  }
}

function relativeLabel(filePath) {
  return path.relative(process.cwd(), filePath) || filePath;
}

function main() {
  const [, , leftPath, rightPath] = process.argv;
  if (!leftPath || !rightPath) {
    console.error('Usage: node scripts/compare-score-diagnostics.mjs <left.json> <right.json>');
    process.exit(1);
  }

  const left = readJson(leftPath);
  const right = readJson(rightPath);
  const failures = [];

  console.log(`Comparing diagnostics`);
  console.log(`- left : ${relativeLabel(leftPath)}`);
  console.log(`- right: ${relativeLabel(rightPath)}`);
  console.log('');

  compareValue('extensionVersion', left.extensionVersion, right.extensionVersion, failures);
  compareValue('backendVersion', left.backendVersion, right.backendVersion, failures);
  compareValue('resultSource', left.resultSource, right.resultSource, failures);
  compareValue('cachedPayload', left.cachedPayload, right.cachedPayload, failures);
  compareValue('reportFingerprint.fingerprint', left.reportFingerprint?.fingerprint, right.reportFingerprint?.fingerprint, failures);
  compareValue('reportFingerprint.fileCount', left.reportFingerprint?.fileCount, right.reportFingerprint?.fileCount, failures);
  compareValue('score', left.score, right.score, failures);
  compareValue('pageCount', left.pageCount, right.pageCount, failures);
  compareValue('issueCount', left.issueCount, right.issueCount, failures);
  compareValue('severityCounts', left.severityCounts, right.severityCounts, failures);
  compareValue('readinessScore', left.readinessScore, right.readinessScore, failures);
  compareValue('readinessBand', left.readinessBand, right.readinessBand, failures);
  compareValue('analyzerType', left.analyzerType, right.analyzerType, failures);
  compareValue('analyzerProfile', left.analyzerProfile, right.analyzerProfile, failures);
  compareValue('pageProcessingOrder', left.pageProcessingOrder, right.pageProcessingOrder, failures);
  compareValue('pageSnapshots', left.pageSnapshots, right.pageSnapshots, failures);
  compareValue('findings', left.findings, right.findings, failures);
  compareValue('evidenceCount', left.evidenceCount, right.evidenceCount, failures);
  compareValue('reportFingerprint.sourceFiles', left.reportFingerprint?.sourceFiles, right.reportFingerprint?.sourceFiles, failures);

  console.log('');
  compareValue('platform', left.platform, right.platform, failures);
  compareValue('architecture', left.architecture, right.architecture, failures);
  compareValue('backendTarget', left.backendTarget, right.backendTarget, failures);
  compareValue('backendRuntimeId', left.backendRuntimeId, right.backendRuntimeId, failures);

  console.log('');
  if (failures.length === 0) {
    console.log('Diagnostics match on all compared fields.');
    process.exit(0);
  }

  console.log(`Diagnostics differ on ${failures.length} field(s).`);
  process.exit(2);
}

main();
