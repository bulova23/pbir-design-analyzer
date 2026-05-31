import fs from 'fs';
import path from 'path';
import { build, context } from 'esbuild';

const watchMode = process.argv.includes('--watch');
const pdfkitDataSource = path.resolve('node_modules/pdfkit/js/data');
const pdfkitDataTarget = path.resolve('dist/data');

const config = {
  entryPoints: ['src/extension.ts'],
  outfile: 'dist/extension.js',
  bundle: true,
  format: 'cjs',
  platform: 'node',
  target: 'node18',
  external: ['vscode'],
  sourcemap: false,
  logLevel: 'info',
};

function copyPdfkitAssets() {
  fs.mkdirSync(path.dirname(pdfkitDataTarget), { recursive: true });
  fs.cpSync(pdfkitDataSource, pdfkitDataTarget, { recursive: true });
}

if (watchMode) {
  const bundleContext = await context(config);
  await bundleContext.watch();
  copyPdfkitAssets();
  console.log('[bundle-extension] Watching dist/extension.js');
} else {
  await build(config);
  copyPdfkitAssets();
}
