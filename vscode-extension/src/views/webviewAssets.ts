import * as vscode from 'vscode';
import * as fs from 'fs';
import * as path from 'path';

interface WebviewAssetsOptions {
  webview: vscode.Webview;
  extensionUri: vscode.Uri;
  entryFile: string;
  fallbackScriptFile: string;
  fallbackStyleFile?: string;
  manifestFileName?: string;
  devServerUrl?: string;
}

export interface WebviewAssetsResult {
  scriptUri: vscode.Uri | string;
  styleUris: Array<vscode.Uri | string>;
  usingDevServer: boolean;
  devServerOrigin?: string;
  devServerWebSocketOrigin?: string;
  missingAssets?: boolean;
}

interface ViteManifestEntry {
  file: string;
  css?: string[];
  isEntry?: boolean;
}

function loadManifest(manifestPath: string): Record<string, ViteManifestEntry> | undefined {
  if (!fs.existsSync(manifestPath)) {
    return undefined;
  }

  try {
    const raw = fs.readFileSync(manifestPath, 'utf8');
    return JSON.parse(raw) as Record<string, ViteManifestEntry>;
  } catch (error) {
    console.warn('Failed to parse Vite manifest', error);
    return undefined;
  }
}

function findManifestEntry(
  manifest: Record<string, ViteManifestEntry>,
  entryFile: string
): ViteManifestEntry | undefined {
  const normalizedEntry = entryFile.replace(/\\/g, '/');
  if (manifest[normalizedEntry]) {
    return manifest[normalizedEntry];
  }

  const entryFileName = path.basename(normalizedEntry);
  const matchKey = Object.keys(manifest).find((key) => {
    const normalizedKey = key.replace(/\\/g, '/');
    if (normalizedKey.endsWith(normalizedEntry)) {
      return true;
    }
    if (normalizedKey.endsWith(entryFileName)) {
      return true;
    }
    return manifest[key]?.isEntry && normalizedKey.includes(entryFileName);
  });

  return matchKey ? manifest[matchKey] : undefined;
}

export function resolveWebviewAssets(options: WebviewAssetsOptions): WebviewAssetsResult {
  const distPath = path.join(options.extensionUri.fsPath, 'webview-dist');
  const manifestFileName = options.manifestFileName || 'manifest.json';
  const manifestPath = path.join(distPath, manifestFileName);
  const manifest = loadManifest(manifestPath);
  const entry = manifest ? findManifestEntry(manifest, options.entryFile) : undefined;

  const candidateScript = entry?.file || options.fallbackScriptFile;
  const scriptPath = path.join(distPath, candidateScript);
  const hasLocalScript = fs.existsSync(scriptPath);

  const styleCandidates = (entry?.css || []).filter((file) =>
    fs.existsSync(path.join(distPath, file))
  );

  const fallbackStyleFile = options.fallbackStyleFile;
  const hasFallbackStyle =
    fallbackStyleFile && fs.existsSync(path.join(distPath, fallbackStyleFile));

  if (hasLocalScript) {
    const scriptUri = options.webview.asWebviewUri(
      vscode.Uri.joinPath(options.extensionUri, 'webview-dist', candidateScript)
    );

    const styleUris: Array<vscode.Uri | string> = [];
    if (styleCandidates.length > 0) {
      styleCandidates.forEach((styleFile) => {
        styleUris.push(
          options.webview.asWebviewUri(
            vscode.Uri.joinPath(options.extensionUri, 'webview-dist', styleFile)
          )
        );
      });
    } else if (hasFallbackStyle && fallbackStyleFile) {
      styleUris.push(
        options.webview.asWebviewUri(
          vscode.Uri.joinPath(options.extensionUri, 'webview-dist', fallbackStyleFile)
        )
      );
    }

    return {
      scriptUri,
      styleUris,
      usingDevServer: false,
      missingAssets: false,
    };
  }

  const devServer = options.devServerUrl || process.env.VITE_DEV_SERVER_URL;
  if (!devServer) {
    console.warn(`Webview assets not found at ${scriptPath} and no dev server is configured.`);
    return {
      scriptUri: '',
      styleUris: [],
      usingDevServer: false,
      missingAssets: true,
    };
  }

  const scriptUri = `${devServer}/${options.fallbackScriptFile}`;
  const styleUris = fallbackStyleFile ? [`${devServer}/${fallbackStyleFile}`] : [];
  const devUrl = new URL(devServer);
  const wsProtocol = devUrl.protocol === 'https:' ? 'wss:' : 'ws:';

  return {
    scriptUri,
    styleUris,
    usingDevServer: true,
    devServerOrigin: devUrl.origin,
    devServerWebSocketOrigin: `${wsProtocol}//${devUrl.host}`,
    missingAssets: false,
  };
}
