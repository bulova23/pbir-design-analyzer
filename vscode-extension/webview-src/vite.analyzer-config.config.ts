import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],

  build: {
    outDir: resolve(__dirname, '../webview-dist'),
    emptyOutDir: false,
    manifest: 'manifest.analyzer-config.json',

    lib: {
      entry: resolve(__dirname, 'analyzer-config/index.tsx'),
      name: 'AnalyzerConfigPanel',
      formats: ['iife'],
      fileName: () => 'analyzer-config.js',
    },

    rollupOptions: {
      output: {
        inlineDynamicImports: true,
        format: 'iife',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name && assetInfo.name.endsWith('.css')) {
            return 'analyzer-config.css';
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
