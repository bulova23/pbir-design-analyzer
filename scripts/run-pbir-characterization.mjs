import { spawnSync } from 'node:child_process';
const command = ['test', 'service-dotnet/tests/Tests.csproj', '-c', 'Release', '--filter', 'FullyQualifiedName~Characterization'];
for (let run = 1; run <= 2; run++) {
  const result = spawnSync('dotnet', command, { stdio: 'inherit', shell: process.platform === 'win32' });
  if (result.status !== 0) process.exit(result.status ?? 1);
  console.log(`PBIR characterization repeat ${run}/2 passed; goldens are read-only.`);
}
