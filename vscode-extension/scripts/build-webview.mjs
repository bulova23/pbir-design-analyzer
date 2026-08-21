import { spawn } from 'child_process';
import path from 'path';
import fs from 'fs';

const watchMode = process.argv.includes('--watch');
fs.rmSync(path.resolve('webview-dist'), { recursive: true, force: true });

const configs = [
  'webview-src/vite.analyzer-config.config.ts',
  'webview-src/vite.analyzer-score.config.ts',
  'webview-src/vite.design-studio.config.ts',
];

function runVite(configPath) {
  return spawn(
    process.platform === 'win32' ? 'npx.cmd' : 'npx',
    ['vite', 'build', ...(watchMode ? ['--watch'] : []), '--config', configPath],
    {
      stdio: 'inherit',
      shell: process.platform === 'win32',
    },
  );
}

if (watchMode) {
  const children = configs.map(runVite);
  const terminate = () => {
    for (const child of children) {
      child.kill('SIGTERM');
    }
  };

  process.on('SIGINT', terminate);
  process.on('SIGTERM', terminate);

  await Promise.all(children.map((child) => new Promise((resolve, reject) => {
    child.on('exit', (code) => (code === 0 || code === null ? resolve() : reject(new Error(`vite exited with code ${code}`))));
    child.on('error', reject);
  })));
} else {
  for (const configPath of configs) {
    const child = runVite(configPath);
    const exitCode = await new Promise((resolve, reject) => {
      child.on('exit', (code) => resolve(code ?? 1));
      child.on('error', reject);
    });
    if (exitCode !== 0) {
      process.exit(exitCode);
    }
  }
}
