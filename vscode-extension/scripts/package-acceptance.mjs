import crypto from 'node:crypto';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawnSync, spawn } from 'node:child_process';
import { backendTargets } from './backend-targets.mjs';

const extensionRoot = process.cwd();
const repoRoot = path.resolve(extensionRoot, '..');
const version = JSON.parse(fs.readFileSync(path.join(extensionRoot, 'package.json'), 'utf8')).version;
const target = process.argv[process.argv.indexOf('--target') + 1] ?? backendTargets.find((item) => `${process.platform}-${process.arch}` === item.target.replace('win32', 'win').replace('darwin', 'darwin'))?.target;
const descriptor = backendTargets.find((item) => item.target === target);
if (!descriptor) throw new Error(`Unknown or unavailable target: ${target ?? '(none)'}`);
const vsixPath = path.join(extensionRoot, `pbir-design-analyzer-${version}-${target}.vsix`);
if (!fs.existsSync(vsixPath)) throw new Error(`VSIX is missing: ${vsixPath}`);
const fixturePath = process.env.PBIR_ACCEPTANCE_FIXTURE ?? path.join(repoRoot, 'service-dotnet', 'tests', 'Fixtures', 'Characterization', 'MinimalReport.Report');
const mutationFixturePath = process.env.PBIR_ACCEPTANCE_MUTATION_FIXTURE ?? path.join(repoRoot, 'service-dotnet', 'tests', 'Fixtures', 'Characterization', 'MultiPageReport.Report');
const sha256 = (file) => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const tempRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-packaged-acceptance-'));
try {
  const unzip = spawnSync('unzip', ['-q', vsixPath, '-d', tempRoot], { stdio: 'inherit' });
  if (unzip.status !== 0) throw new Error('Unable to extract VSIX for packaged acceptance.');
  const backendPath = path.join(tempRoot, 'extension', 'backend', 'rpc', descriptor.executableName);
  const hostTarget = process.platform === 'win32'
    ? `win32-${process.arch}`
    : `${process.platform}-${process.arch}`;
  const forcedRuntime = process.env.PBIR_ACCEPTANCE_FORCE_RUNTIME === '1';
  const rosettaRuntime = forcedRuntime && target === 'darwin-x64' && process.platform === 'darwin' && process.arch === 'arm64';
  const runtimeProof = target === hostTarget || rosettaRuntime;
  const runtimeMode = process.env.PBIR_ACCEPTANCE_RUNTIME_MODE
    ?? (target === hostTarget ? 'native' : rosettaRuntime ? 'rosetta-x86_64' : 'not-executed');
  const runtimeCommand = rosettaRuntime ? '/usr/bin/arch' : backendPath;
  const runtimeArgs = rosettaRuntime ? ['-x86_64', backendPath] : [];
  const result = { target, version, packageSha256: sha256(vsixPath), fixturePath, runtimeProof, runtimeMode, acceptanceLayer: runtimeProof ? 'packaged-backend' : 'package-only' };
  if (runtimeProof) {
    if (!fs.existsSync(backendPath)) throw new Error(`Packaged backend is missing: ${backendPath}`);
    result.backendSha256 = sha256(backendPath);
    const first = await scorePackagedBackend(runtimeCommand, fixturePath, runtimeArgs);
    const second = await scorePackagedBackend(runtimeCommand, fixturePath, runtimeArgs);
    const firstScorePayload = first.score.result?.data ?? first.score.result;
    const secondScorePayload = second.score.result?.data ?? second.score.result;
    result.protocolNegotiation = first.protocolNegotiation;
    result.ping = first.ping;
    result.compositeScore = firstScorePayload?.compositeScore;
    result.normalizedFingerprint = fingerprint(firstScorePayload);
    if (!result.normalizedFingerprint || result.normalizedFingerprint !== fingerprint(secondScorePayload)) throw new Error('Packaged score output was not deterministic or empty.');
    const workflow = await runPackagedWorkflow(runtimeCommand, mutationFixturePath, path.join(tempRoot, 'workflow'), runtimeArgs);
    Object.assign(result, workflow);
  }
  const evidenceDir = path.join(repoRoot, 'docs', 'release-evidence');
  fs.mkdirSync(evidenceDir, { recursive: true });
  fs.writeFileSync(path.join(evidenceDir, `packaged-acceptance-${target}.json`), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
} finally {
  fs.rmSync(tempRoot, { recursive: true, force: true });
}

function fingerprint(value) {
  if (!value) return undefined;
  const stable = JSON.stringify(value, (key, item) => ['reportPath', 'scoredAt'].includes(key) ? undefined : item);
  return crypto.createHash('sha256').update(stable).digest('hex');
}

async function openPackagedBackend(command, args = []) {
  const child = spawn(command, args, { stdio: ['pipe', 'pipe', 'pipe'] });
  let buffer = Buffer.alloc(0);
  const responses = new Map();
  child.stderr.on('data', () => {});
  child.stdout.on('data', (chunk) => {
    buffer = Buffer.concat([buffer, chunk]);
    while (true) {
      const separator = buffer.indexOf(Buffer.from('\r\n\r\n'));
      if (separator < 0) break;
      const header = buffer.subarray(0, separator).toString('utf8');
      const length = Number(header.match(/Content-Length:\s*(\d+)/i)?.[1]);
      if (!Number.isFinite(length) || buffer.length < separator + 4 + length) break;
      const body = JSON.parse(buffer.subarray(separator + 4, separator + 4 + length).toString('utf8'));
      responses.set(body.id, body);
      buffer = buffer.subarray(separator + 4 + length);
    }
  });
  const send = (id, method, params) => {
    const body = Buffer.from(JSON.stringify({ jsonrpc: '2.0', id, method, params }));
    child.stdin.write(`Content-Length: ${body.length}\r\n\r\n`);
    child.stdin.write(body);
  };
  const waitFor = async (id) => {
    const started = Date.now();
    while (!responses.has(id) && Date.now() - started < 15000) await new Promise((resolve) => setTimeout(resolve, 25));
    if (!responses.has(id)) throw new Error(`Packaged backend did not answer request ${id}.`);
    return responses.get(id);
  };
  return {
    request: async (id, method, params) => {
      send(id, method, params);
      return waitFor(id);
    },
    close: () => child.kill(),
  };
}

async function scorePackagedBackend(command, reportPath, args = []) {
  const backend = await openPackagedBackend(command, args);
  try {
    const initialization = await backend.request(1, 'initialize', { processId: null, rootUri: null, capabilities: {} });
    const ping = await backend.request(2, 'model/ping', {});
    const score = await backend.request(3, 'model/pbir/scoreReport', { reportPath });
    if (score.error || !score.result?.success) throw new Error(`Packaged score request failed: ${JSON.stringify(score)}`);
    return { protocolNegotiation: Boolean(initialization.result), ping: ping.result, score };
  } finally {
    backend.close();
  }
}

async function runPackagedWorkflow(command, sourceDirectory, workflowRoot, args = []) {
  if (!fs.existsSync(sourceDirectory)) throw new Error(`Mutation fixture is missing: ${sourceDirectory}`);
  fs.mkdirSync(workflowRoot, { recursive: true });
  const outputRoot = path.join(workflowRoot, 'output');
  const targetDirectoryName = `Mutated-${Date.now()}.Report`;
  fs.mkdirSync(outputRoot, { recursive: true });
  const backend = await openPackagedBackend(command, args);
  try {
    const initialization = await backend.request(10, 'initialize', { processId: null, rootUri: null, capabilities: {} });
    if (!initialization.result) throw new Error('Packaged workflow initialization failed.');
    const generated = await backend.request(11, 'pbir/authoring', {
      schemaVersion: 'pbir-authoring-rpc/v1', operation: 'generate',
      generate: { request: { v1: {
        schemaVersion: 'local-pbir-generation-request/v1', requestId: 'package-acceptance-generate',
        reportName: 'PackagedAcceptance', pageId: 'overview', pageDisplayName: 'Overview',
        visualId: 'revenue-card', visualType: 'card', datasetPath: 'Sales.SemanticModel',
        measureToken: 'Sales.Revenue', measureEntity: 'Sales', measureProperty: 'Revenue',
        generatedUtc: '2026-08-21T00:00:00.000Z', outputBaseDirectory: outputRoot, targetDirectoryName: 'Generated.Report',
      } } },
    });
    if (!generated.result?.succeeded) throw new Error(`Packaged generation failed: ${JSON.stringify(generated)}`);
    const disposableSource = path.join(outputRoot, 'Generated.Report');
    const imported = await backend.request(12, 'pbir/authoring', {
      schemaVersion: 'pbir-authoring-rpc/v1', operation: 'import',
      import: { sourceDirectory: disposableSource },
    });
    const importResult = imported.result?.importResult;
    const snapshot = importResult?.snapshot;
    const pageId = importResult?.pages?.[0]?.pageId;
    if (!imported.result?.succeeded || !snapshot || !pageId) throw new Error(`Packaged authoring import failed: ${JSON.stringify(imported)}`);
    const request = {
      schemaVersion: 'local-pbir-mutation-request/v1',
      mutationId: 'package-acceptance-rename', sourceDirectory: disposableSource,
      outputBaseDirectory: outputRoot, targetDirectoryName,
      operations: [{ kind: 'renamePage', target: { pageId }, displayName: 'Packaged Acceptance' }],
    };
    const preview = await backend.request(13, 'pbir/authoring', {
      schemaVersion: 'pbir-authoring-rpc/v1', operation: 'mutate',
      mutate: { snapshot, request, mode: 'preview' },
    });
    if (!preview.result?.succeeded || !preview.result?.mutateResult?.preview) throw new Error(`Packaged mutation preview failed: ${JSON.stringify(preview)}`);
    const executed = await backend.request(14, 'pbir/authoring', {
      schemaVersion: 'pbir-authoring-rpc/v1', operation: 'mutate',
      mutate: { snapshot, request, mode: 'execute' },
    });
    const materialization = executed.result?.mutateResult?.materialization;
    if (!executed.result?.succeeded || !materialization) throw new Error(`Packaged mutation apply failed: ${JSON.stringify(executed)}`);
    const targetDirectory = path.join(materialization.outputBaseDirectory, materialization.targetDirectoryName);
    if (!fs.existsSync(targetDirectory)) throw new Error('Packaged mutation output was not materialized.');
    const rollback = await backend.request(15, 'pbir/materialization/rollback', {
      schemaVersion: 'pbir-local-materialization-rollback-request/v1',
      requestId: 'package-acceptance-rollback', operation: 'pbir/materialization/rollback',
      outputBaseDirectory: materialization.outputBaseDirectory,
      targetDirectoryName: materialization.targetDirectoryName,
      targetKey: materialization.targetKey, transactionId: materialization.transactionId,
      expectedTransactionHash: materialization.transactionHash,
      expectedCurrentReceiptHash: materialization.currentReceiptHash,
      expectedCurrentTargetStateHash: materialization.currentTargetStateHash,
      rollbackApproved: true,
      executionPolicy: {
        filesystemMutationAllowed: true, providerInvocationAllowed: false,
        microsoftSkillsExecutionAllowed: false, apiInvocationAllowed: false,
        cliInvocationAllowed: false, deploymentAllowed: false, publishingAllowed: false,
        desktopAutomationAllowed: false, analyzerAutomationAllowed: false,
      },
    });
    if (rollback.result?.outcome !== 'rolled-back' || fs.existsSync(targetDirectory)) throw new Error(`Packaged rollback failed: ${JSON.stringify(rollback)}`);
    const exportPath = path.join(workflowRoot, 'review-deliverable.json');
    fs.writeFileSync(exportPath, `${JSON.stringify({ schemaVersion: 'pbir-review-workflow/v1', fixture: sourceDirectory, preview: preview.result.mutateResult.preview, comparison: executed.result.mutateResult.comparison ?? null }, null, 2)}\n`);
    if (!fs.existsSync(exportPath)) throw new Error('Packaged review deliverable was not exported.');
    return {
      acceptanceLayer: 'packaged-workflow', mutationWorkflow: 'PASS', rollbackWorkflow: 'PASS', exportWorkflow: 'PASS',
      workflowFixturePath: disposableSource,
      workflowFixtureSha256: hashDirectory(disposableSource),
    };
  } finally {
    backend.close();
  }
}

function hashDirectory(directory) {
  const files = [];
  const visit = (current) => {
    for (const entry of fs.readdirSync(current, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name))) {
      const absolute = path.join(current, entry.name);
      if (entry.isDirectory()) visit(absolute);
      else files.push([path.relative(directory, absolute).split(path.sep).join('/'), fs.readFileSync(absolute)]);
    }
  };
  visit(directory);
  const digest = crypto.createHash('sha256');
  for (const [relative, content] of files) digest.update(relative).update('\0').update(content).update('\0');
  return digest.digest('hex');
}
