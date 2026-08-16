import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],
  define: {
    'process.env.NODE_ENV': JSON.stringify('production'),
  },

  build: {
    outDir: resolve(__dirname, '../webview-dist'),
    emptyOutDir: false,
    manifest: 'manifest.analyzer-score.json',

    lib: {
      entry: resolve(__dirname, 'analyzer-score/index.tsx'),
      name: 'AnalyzerScorePanel',
      formats: ['iife'],
      fileName: () => 'analyzer-score.js',
    },

    rollupOptions: {
      output: {
        inlineDynamicImports: true,
        format: 'iife',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name && assetInfo.name.endsWith('.css')) {
            return 'analyzer-score.css';
          }

          return assetInfo.name || 'asset.[ext]';
        },
      },
    },

    sourcemap: true,
    minify: process.env.NODE_ENV === 'production' ? 'esbuild' : false,
    target: 'es2020',
  },

  resolve: {
    alias: {
      '@': resolve(__dirname, './'),
      '@compat': resolve(__dirname, '../src/webview/compat'),
      '@contracts': resolve(__dirname, '../src/webview/contracts'),
    },
  },
});
