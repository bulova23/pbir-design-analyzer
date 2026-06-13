import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import type { ExtensionContext } from 'vscode';
import {
  approveDesignBrief,
  loadDesignBriefState,
  saveDesignBriefDraft,
} from '../design-studio/state/designBriefStore';

function makeContext(tmpDir: string): ExtensionContext {
  return {
    globalStorageUri: { fsPath: tmpDir },
    secrets: {
      get: jest.fn(),
      store: jest.fn(),
      delete: jest.fn(),
    },
  } as unknown as ExtensionContext;
}

function makeTempDir(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'pbir-design-brief-test-'));
}

describe('designBriefStore', () => {
  it('returns undefined when a design thread has no persisted brief yet', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    await expect(loadDesignBriefState(context, 'thread-1')).resolves.toBeUndefined();
  });

  it('persists versioned studio-owned design briefs in extension global storage', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    const initial = await saveDesignBriefDraft(context, 'thread-1', {
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Where should we intervene first'],
      primaryKpis: ['Renewal rate'],
      dimensions: ['Region'],
      intendedStory: 'Lead with renewal risk, then explain drivers.',
      successCriteria: ['Manager can decide in under five minutes'],
      reportType: 'dashboard',
      navigationExpectations: 'Overview first, drill to region detail second.',
      consumptionContext: 'Weekly pipeline review',
      decisionCadence: 'Weekly',
      narrativeRisksOrConstraints: ['Do not over-emphasize small regions'],
      requiredEvidenceDomains: ['KPI trend', 'region variance'],
      targetAnalyzableSurfaceFamily: 'pbir',
    });

    const updated = await saveDesignBriefDraft(context, 'thread-1', {
      audience: 'Sales leaders',
      businessObjective: 'Reduce missed renewals',
      keyDecisions: ['Where should we intervene first'],
      primaryKpis: ['Renewal rate', 'At-risk pipeline'],
      dimensions: ['Region'],
      intendedStory: 'Lead with renewal risk, then explain drivers.',
      successCriteria: ['Manager can decide in under five minutes'],
      reportType: 'dashboard',
      navigationExpectations: 'Overview first, drill to region detail second.',
      consumptionContext: 'Daily intervention check',
      decisionCadence: 'Daily',
      narrativeRisksOrConstraints: ['Preserve segment comparability'],
      requiredEvidenceDomains: ['At-risk account list'],
      targetAnalyzableSurfaceFamily: 'pbir',
    });

    const reloaded = await loadDesignBriefState(context, 'thread-1');

    expect(initial.current.version).toBe(1);
    expect(updated.current.version).toBe(2);
    expect(updated.current.consumptionContext).toBe('Daily intervention check');
    expect(updated.current.decisionCadence).toBe('Daily');
    expect(updated.current.narrativeRisksOrConstraints).toEqual(['Preserve segment comparability']);
    expect(updated.current.requiredEvidenceDomains).toEqual(['At-risk account list']);
    expect(updated.current.targetAnalyzableSurfaceFamily).toBe('pbir');
    expect(updated.history.map((entry) => entry.version)).toEqual([1, 2]);
    expect(reloaded).toEqual(updated);
    expect(fs.existsSync(path.join(tmp, 'design-studio', 'threads'))).toBe(true);
    expect(fs.existsSync(path.join('/Users/me/Workspace', 'design-brief.json'))).toBe(false);
  });

  it('requires explicit approval before concept generation can proceed', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    const draft = await saveDesignBriefDraft(context, 'thread-1', {
      audience: 'Operations managers',
      businessObjective: 'Reduce inventory exceptions',
      keyDecisions: ['Which plants need action today'],
      primaryKpis: ['Exception count'],
      dimensions: ['Plant'],
      intendedStory: 'Open with exceptions, then show root causes.',
      successCriteria: ['Manager can identify a plant to contact immediately'],
      reportType: 'dashboard',
      navigationExpectations: 'Start at summary, then move to root-cause detail.',
    });

    expect(draft.validation.canGenerateConcepts).toBe(false);
    expect(draft.current.approvalState).toBe('pendingApproval');

    const approved = await approveDesignBrief(context, 'thread-1');

    expect(approved.current.version).toBe(2);
    expect(approved.current.approvalState).toBe('approved');
    expect(approved.current.lifecycleState).toBe('approved');
    expect(approved.validation.canGenerateConcepts).toBe(true);
    expect(approved.history.map((entry) => entry.version)).toEqual([1, 2]);
    expect(approved.history[0].brief.approvalState).toBe('pendingApproval');
    expect(approved.history[0].brief.lifecycleState).toBe('draft');
    expect(approved.history[1].brief.approvalState).toBe('approved');
    expect(approved.history[1].brief.lifecycleState).toBe('approved');
  });

  it('rejects approval when required fields are missing', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    await saveDesignBriefDraft(context, 'thread-1', {
      audience: 'Operations managers',
      businessObjective: '',
      keyDecisions: [],
      primaryKpis: [],
      dimensions: [],
      intendedStory: '',
      successCriteria: [],
      reportType: 'dashboard',
      navigationExpectations: '',
    });

    await expect(approveDesignBrief(context, 'thread-1')).rejects.toThrow(
      'Design Brief must be valid before approval.',
    );
  });

  it('supports missing optional design brief fields without breaking existing briefs', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    const saved = await saveDesignBriefDraft(context, 'thread-2', {
      audience: 'Operations managers',
      businessObjective: 'Reduce inventory exceptions',
      keyDecisions: ['Which plants need action today'],
      primaryKpis: ['Exception count'],
      dimensions: ['Plant'],
      intendedStory: 'Open with exceptions, then show root causes.',
      successCriteria: ['Manager can identify a plant to contact immediately'],
      reportType: 'dashboard',
      navigationExpectations: 'Start at summary, then move to root-cause detail.',
    });

    expect(saved.validation.isValid).toBe(true);
    expect(saved.current.consumptionContext).toBeUndefined();
    expect(saved.current.decisionCadence).toBeUndefined();
    expect(saved.current.narrativeRisksOrConstraints).toBeUndefined();
    expect(saved.current.requiredEvidenceDomains).toBeUndefined();
    expect(saved.current.targetAnalyzableSurfaceFamily).toBeUndefined();
  });

  it('does not treat persisted constraint fields as validation-required', async () => {
    const tmp = makeTempDir();
    const context = makeContext(tmp);

    const saved = await saveDesignBriefDraft(context, 'thread-3', {
      audience: 'Finance leads',
      businessObjective: 'Prioritize collections follow-up',
      keyDecisions: ['Which accounts need outreach now'],
      primaryKpis: ['Overdue balance'],
      dimensions: ['Region'],
      intendedStory: 'Start with exposure, then show the biggest drivers.',
      successCriteria: ['Lead can assign the next outreach action quickly'],
      reportType: 'dashboard',
      navigationExpectations: 'Overview first, then account detail.',
      consumptionContext: '   ',
      decisionCadence: '   ',
      narrativeRisksOrConstraints: ['   '],
      requiredEvidenceDomains: ['   '],
      targetAnalyzableSurfaceFamily: '   ',
    });

    expect(saved.validation.isValid).toBe(true);
    expect(saved.validation.errors).toEqual([
      {
        field: 'approvalState',
        message: 'Design Brief must be approved before concept generation can proceed.',
      },
    ]);
    expect(saved.current.consumptionContext).toBeUndefined();
    expect(saved.current.decisionCadence).toBeUndefined();
    expect(saved.current.narrativeRisksOrConstraints).toBeUndefined();
    expect(saved.current.requiredEvidenceDomains).toBeUndefined();
    expect(saved.current.targetAnalyzableSurfaceFamily).toBeUndefined();
  });
});
