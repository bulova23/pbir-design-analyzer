import { spawn } from 'child_process';

const commands = process.argv.slice(2);
if (commands.length === 0) {
  throw new Error('Expected at least one command.');
}

const children = commands.map((command) => spawn(command, {
  stdio: 'inherit',
  shell: true,
}));

const terminate = () => {
  for (const child of children) {
    child.kill('SIGTERM');
  }
};

process.on('SIGINT', terminate);
process.on('SIGTERM', terminate);

const exitCode = await new Promise((resolve) => {
  let settled = false;
  let remaining = children.length;

  for (const child of children) {
    child.on('exit', (code) => {
      remaining -= 1;
      if (!settled && code && code !== 0) {
        settled = true;
        terminate();
        resolve(code);
        return;
      }

      if (!settled && remaining === 0) {
        settled = true;
        resolve(0);
      }
    });

    child.on('error', () => {
      if (!settled) {
        settled = true;
        terminate();
        resolve(1);
      }
    });
  }
});

process.exit(exitCode);
