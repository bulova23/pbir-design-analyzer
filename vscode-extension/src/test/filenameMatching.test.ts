import {
  extractStateName,
  matchFilenameToPages,
  normalizeFilename,
  normalizePageName,
} from '../analyzer/audit/filenameMatching';

describe('normalizeFilename', () => {
  it('strips extension', () => {
    expect(normalizeFilename('Overview.png')).toBe('overview');
  });

  it('strips leading numeric prefix', () => {
    expect(normalizeFilename('01 Overview.png')).toBe('overview');
    expect(normalizeFilename('02-Sales Detail.png')).toBe('sales detail');
    expect(normalizeFilename('03_Net Sales.png')).toBe('net sales');
  });

  it('strips state suffix when preceded by " - "', () => {
    expect(normalizeFilename('Net Sales - Default.png')).toBe('net sales');
    expect(normalizeFilename('01 Overview - Bookmark1.png')).toBe('overview');
  });

  it('collapses non-alphanumeric to single spaces', () => {
    expect(normalizeFilename('Sales & Production.png')).toBe('sales production');
  });

  it('lowercases result', () => {
    expect(normalizeFilename('EXECUTIVE SUMMARY.png')).toBe('executive summary');
  });
});

describe('normalizePageName', () => {
  it('lowercases and collapses punctuation', () => {
    expect(normalizePageName('Sales & Production')).toBe('sales production');
    expect(normalizePageName('Net Sales')).toBe('net sales');
  });
});

describe('extractStateName', () => {
  it('returns undefined when no state suffix', () => {
    expect(extractStateName('Overview.png')).toBeUndefined();
    expect(extractStateName('01 Overview.png')).toBeUndefined();
  });

  it('extracts state from " - StateName" suffix', () => {
    expect(extractStateName('Net Sales - Default.png')).toBe('Default');
    expect(extractStateName('01 Overview - Bookmark1.png')).toBe('Bookmark1');
  });
});

describe('matchFilenameToPages', () => {
  const pages = ['Overview', 'Sales Detail', 'Net Sales', 'Executive Summary'];

  it('returns exact match with score 1.0', () => {
    const result = matchFilenameToPages('Overview.png', pages);
    expect(result).toBeDefined();
    expect(result!.pageName).toBe('Overview');
    expect(result!.score).toBe(1.0);
  });

  it('matches file with leading number to page', () => {
    const result = matchFilenameToPages('01 Overview.png', pages);
    expect(result).toBeDefined();
    expect(result!.pageName).toBe('Overview');
  });

  it('matches file with state suffix', () => {
    const result = matchFilenameToPages('Net Sales - Default.png', pages);
    expect(result).toBeDefined();
    expect(result!.pageName).toBe('Net Sales');
  });

  it('matches multi-word page by word overlap', () => {
    const result = matchFilenameToPages('02-Sales Detail.png', pages);
    expect(result).toBeDefined();
    expect(result!.pageName).toBe('Sales Detail');
  });

  it('returns undefined for no matching page', () => {
    const result = matchFilenameToPages('completely unrelated image.png', pages);
    expect(result).toBeUndefined();
  });

  it('returns undefined for empty page list', () => {
    const result = matchFilenameToPages('Overview.png', []);
    expect(result).toBeUndefined();
  });

  it('picks best match when multiple candidates exist', () => {
    const localPages = ['Sales', 'Sales Detail', 'Other'];
    const result = matchFilenameToPages('Sales Detail.png', localPages);
    expect(result).toBeDefined();
    expect(result!.pageName).toBe('Sales Detail');
  });

  it('handles realistic Power BI screenshot export names', () => {
    const result1 = matchFilenameToPages('Executive Summary.jpg', pages);
    expect(result1?.pageName).toBe('Executive Summary');

    const result2 = matchFilenameToPages('03 Net Sales - Default.png', pages);
    expect(result2?.pageName).toBe('Net Sales');
  });
});
