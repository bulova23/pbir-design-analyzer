import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptPath = fileURLToPath(import.meta.url);
const repoRoot = path.resolve(path.dirname(scriptPath), '..');

const candidates = [
  { path: 'service-dotnet/Services/Pbir/PbirScoringService.cs', classification: 'shipped scoring', targetOwner: 'scoring orchestrator' },
  { path: 'vscode-extension/webview-src/analyzer-score/App.tsx', classification: 'shipped workspace', targetOwner: 'workspace composition' },
  { path: 'vscode-extension/src/views/PbirScorePanel.ts', classification: 'shipped host', targetOwner: 'panel lifecycle/coordinators' },
  { path: 'vscode-extension/src/views/scoreResultPayload.ts', classification: 'shipped boundary', targetOwner: 'normalizer/projections' },
  { path: 'vscode-extension/src/views/scorePanelProtocol.ts', classification: 'shipped protocol', targetOwner: 'cohesive protocol boundary' },
  { path: 'vscode-extension/src/views/scorePanelMessageRouter.ts', classification: 'shipped routing', targetOwner: 'cohesive routing boundary' },
  { path: 'service-dotnet/Services/Discovery/RecommendationEngineService.cs', classification: 'deferred discovery', targetOwner: 'existing discovery domain' },
  { path: 'service-dotnet/Services/Discovery/LocalPbirGenerationProviderService.cs', classification: 'deferred provider', targetOwner: 'future provider program' },
  { path: 'service-dotnet/Services/Pbir/ScoreResultAssemblyService.cs', classification: 'shipped score boundary', targetOwner: 'authoritative result assembly' },
];

function parseArgs(argv) {
  const args = { root: repoRoot, manifest: null };
  for (let i = 0; i < argv.length; i += 1) {
    if (argv[i] === '--root') args.root = path.resolve(argv[++i]);
    else if (argv[i] === '--manifest') args.manifest = path.resolve(argv[++i]);
    else throw new Error(`Unknown argument: ${argv[i]}`);
  }
  return args;
}

function readManifest(manifestPath) {
  if (!manifestPath) return candidates;
  const parsed = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  if (!Array.isArray(parsed)) throw new Error('Manifest must be a JSON array');
  return parsed;
}

function sourceMetrics(source) {
  const declarations = [...source.matchAll(/^\s*(?:public|private|protected|internal)?\s*(?:static\s+)?(?:class|record|interface|enum|(?:async\s+)?[\w<>?[\], ]+\s+)[A-Za-z_]\w*\s*\(/gm)]
    .map((match) => match[0].trim()).sort();
  const imports = [...source.matchAll(/^\s*(?:import|using)\s+[^;]+;?/gm)].map((match) => match[0].trim()).sort();
  const mutableFields = [...source.matchAll(/^\s*(?:public|private|protected|internal)?\s*(?:static\s+)?(?!readonly\b)[\w<>?[\], ]+\s+_[A-Za-z_]\w*\s*(?:=|;)/gm)]
    .map((match) => match[0].trim()).sort();
  const sideEffects = ['File.', 'Directory.', 'Process.', 'Console.', 'window.', 'postMessage(', 'sendEvent(']
    .filter((token) => source.includes(token));
  return { declarations, imports, mutableFields, sideEffects };
}

function validateCandidates(root, entries) {
  const errors = [];
  const names = new Set();
  for (const entry of entries) {
    if (!entry || typeof entry.path !== 'string' || names.has(entry.path)) {
      errors.push(`invalid or duplicate manifest entry: ${entry?.path ?? '<unknown>'}`);
      continue;
    }
    names.add(entry.path);
    if (!fs.existsSync(path.join(root, entry.path))) errors.push(`missing candidate: ${entry.path}`);
  }
  return { errors, names };
}

function render(root, entries) {
  const { errors, names } = validateCandidates(root, entries);
  if (errors.length) throw new Error(errors.join('\n'));
  const rows = [...entries].sort((a, b) => a.path.localeCompare(b.path)).map((entry) => {
    const source = fs.readFileSync(path.join(root, entry.path), 'utf8');
    const metrics = sourceMetrics(source);
    return { ...entry, lineCount: source.split('\n').length, ...metrics };
  });
  return { rows, names };
}

const args = parseArgs(process.argv.slice(2));
const entries = readManifest(args.manifest);
const { rows, names } = render(args.root, entries);
const output = [
  '# Post-v1 Decomposition Baseline',
  '',
  'Repository root: <repository>',
  `Candidate count: ${rows.length}`,
  '',
  'The measurements below are advisory complexity signals. Candidate presence and disposition are enforced.',
  '',
  '| Candidate | Lines | Classification | Target owner | Declarations | Imports | Mutable fields | Side effects |',
  '|---|---:|---|---|---:|---:|---:|---|',
  ...rows.map((row) => `| ${row.path} | ${row.lineCount} | ${row.classification} | ${row.targetOwner} | ${row.declarations.length} | ${row.imports.length} | ${row.mutableFields.length} | ${row.sideEffects.join(', ') || 'none'} |`),
  '',
  '## Runtime composition and controls',
  '',
  '- Backend composition: `service-dotnet/RpcHost/Program.cs` constructs the shipped scoring, tree, governance, and project services; `AnalyzerRpcDispatcher.cs` routes scoring, tree, governance, materialization, and authoring boundaries.',
  '- Host composition: `vscode-extension/src/views/PbirScorePanel.ts` owns lifecycle and routes fire-and-forget messages through the validated protocol/router boundary.',
  '- Controls: backend architecture tests, score-panel contract validation, protocol tests, selected-page clamping tests, characterization/golden scripts, deterministic repeat, package acceptance, and mutation/rollback acceptance remain authoritative.',
  '',
  '## Candidate manifest',
  '',
  ...[...names].sort().map((name) => `- ${name}`),
  '',
].join('\n');

process.stdout.write(output);
