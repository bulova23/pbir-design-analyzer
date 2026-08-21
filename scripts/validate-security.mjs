import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
const root = path.resolve(new URL('..', import.meta.url).pathname);
const policy = JSON.parse(fs.readFileSync(path.join(root, 'config', 'security-baseline.json'), 'utf8'));
const approved = new Map(policy.approvedExceptions.map((entry) => [entry.package, entry]));
const npm = spawnSync('npm', ['audit', '--audit-level=high', '--json'], { cwd: path.join(root, 'vscode-extension'), encoding: 'utf8' });
let audit;
try { audit = JSON.parse(npm.stdout); } catch { console.error(npm.stdout || npm.stderr); process.exit(1); }
const failures = [];
for (const [name, vulnerability] of Object.entries(audit.vulnerabilities ?? {})) {
  if (!['high', 'critical'].includes(vulnerability.severity)) continue;
  const exception = approved.get(name);
  if (!exception || exception.expires < new Date().toISOString().slice(0, 10)) failures.push(`npm ${vulnerability.severity} vulnerability requires remediation or approved exception: ${name}`);
}
const dotnet = spawnSync('dotnet', ['list', 'service-dotnet/tests/Tests.csproj', 'package', '--vulnerable', '--include-transitive'], { cwd: root, encoding: 'utf8' });
const dotnetOutput = `${dotnet.stdout}\n${dotnet.stderr}`;
for (const packageName of ['System.Net.Http', 'System.Text.RegularExpressions']) {
  if (dotnetOutput.includes(packageName) && !approved.has(packageName)) failures.push(`NuGet vulnerability requires approved exception: ${packageName}`);
}
if (failures.length) { console.error(failures.join('\n')); process.exit(1); }
console.log(`Security policy passed with ${Object.keys(audit.vulnerabilities ?? {}).length} npm advisories classified and approved exceptions enforced.`);
