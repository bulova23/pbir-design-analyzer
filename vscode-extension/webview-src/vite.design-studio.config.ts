import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'path';

export default defineConfig({
  plugins: [react()],

  build: {
    outDir: resolve(__dirname, '../webview-dist'),
    emptyOutDir: false,
    manifest: 'manifest.design-studio.json',

    lib: {
      entry: resolve(__dirname, 'design-studio/index.tsx'),
      name: 'DesignStudioPanel',
      formats: ['iife'],
      fileName: () => 'design-studio.js',
    },

    rollupOptions: {
      output: {
        inlineDynamicImports: true,
        format: 'iife',
        assetFileNames: (assetInfo) => {
          if (assetInfo.name && assetInfo.name.endsWith('.css')) {
            return 'design-studio.css';
          }

          return assetInfo.name || 'asset.[ext]';
        },
      },
    },

    sourcemap: true,
    minify: process.env.NODE_ENV === 'production' ? 'esbuild' : false,
    target: 'es2020',
  },
});
