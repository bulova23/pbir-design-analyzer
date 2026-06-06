if (process.env.PBIR_BUILD_WEBVIEW_ON_INSTALL === '1') {
  const { spawnSync } = await import('child_process');
  const result = spawnSync('npm', ['run', 'build:webview'], { stdio: 'inherit', shell: true });
  process.exit(result.status ?? 1);
}

console.log('[postinstall] Skipping webview build. Run `npm run build:webview` or `npm run build` when you need compiled assets.');
