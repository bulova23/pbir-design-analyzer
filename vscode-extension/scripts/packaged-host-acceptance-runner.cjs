const assert = require('node:assert/strict');
const fs = require('node:fs');
const vscode = require('vscode');

async function run() {
  const extensionId = process.env.EXPECTED_EXTENSION_ID;
  const extension = vscode.extensions.getExtension(extensionId);
  assert.ok(extension, `Installed extension ${extensionId} was not found.`);
  assert.equal(extension.packageJSON.version, process.env.EXPECTED_VERSION);
  assert.equal(fs.existsSync(process.env.EXPECTED_BACKEND_PATH), true, 'Packaged backend entrypoint is missing.');
  await extension.activate();
  assert.equal(extension.isActive, true, 'Installed extension did not activate.');
  console.log(JSON.stringify({
    extensionId,
    version: extension.packageJSON.version,
    activated: extension.isActive,
    backendExists: true,
  }));
}

module.exports = { run };
