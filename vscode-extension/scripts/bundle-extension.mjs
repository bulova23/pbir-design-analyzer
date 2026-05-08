import { build, context } from 'esbuild';

const watchMode = process.argv.includes('--watch');

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

if (watchMode) {
  const bundleContext = await context(config);
  await bundleContext.watch();
  console.log('[bundle-extension] Watching dist/extension.js');
} else {
  await build(config);
}
