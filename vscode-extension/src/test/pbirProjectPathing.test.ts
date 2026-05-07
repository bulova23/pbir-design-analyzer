import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import { resolvePbirProjectPath } from '../analyzer/project/pathing';

describe('resolvePbirProjectPath', () => {
  let tempDir: string;
  let workspaceRoot: string;
  let reportRoot: string;

  beforeEach(() => {
    tempDir = fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-project-pathing-'));
    workspaceRoot = path.join(tempDir, 'PBITesting');
    reportRoot = path.join(workspaceRoot, 'Sales & Production.Report');

    fs.mkdirSync(path.join(reportRoot, 'definition', 'pages'), { recursive: true });
    fs.writeFileSync(path.join(reportRoot, 'definition.pbir'), '{}');
    fs.writeFileSync(path.join(reportRoot, 'definition', 'report.json'), '{}');
  });

  afterEach(() => {
    fs.rmSync(tempDir, { recursive: true, force: true });
  });

  it('returns the concrete report folder when a workspace root contains a PBIR report', () => {
    expect(resolvePbirProjectPath(workspaceRoot)).toBe(reportRoot);
  });

  it('returns the report folder when a PBIR report folder is selected directly', () => {
    expect(resolvePbirProjectPath(reportRoot)).toBe(reportRoot);
  });
});
