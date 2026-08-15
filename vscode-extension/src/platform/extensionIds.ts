export const PBIR_COMMANDS = {
  getTree: 'pbirAnalyzer.getTree',
  openProject: 'pbirAnalyzer.openProject',
  openDesignStudio: 'pbirAnalyzer.openDesignStudio',
  openLocalPbirMaterialization: 'pbirAnalyzer.openLocalPbirMaterialization',
  refreshReports: 'pbirAnalyzer.refreshReports',
  scoreReport: 'pbirAnalyzer.scoreReport',
  openAnalyzerWorkspaceHandoff: 'pbirAnalyzer.openAnalyzerWorkspaceHandoff',
  copyScoreDiagnostics: 'pbirAnalyzer.copyScoreDiagnostics',
  configureScoring: 'pbirAnalyzer.configureScoring',
  checkGovernance: 'pbirAnalyzer.checkGovernance',
  exportGovernanceReport: 'pbirAnalyzer.exportGovernanceReport',
  exportReviewWorkflow: 'pbirAnalyzer.exportReviewWorkflow',
  uploadScreenshots: 'pbirAnalyzer.uploadScreenshots',
  attachScreenshot: 'pbirAnalyzer.attachScreenshot',
  configureAuditProvider: 'pbirAnalyzer.configureAuditProvider',
  generateReport: 'pbirAnalyzer.generateReport',
  importReport: 'pbirAnalyzer.importReport',
  analyzeAuthoringReport: 'pbirAnalyzer.analyzeAuthoringReport',
  renamePage: 'pbirAnalyzer.renamePage',
} as const;

export const LEGACY_PBIR_COMMAND_ALIASES: Record<string, string> = {
  'pbir.getTree': PBIR_COMMANDS.getTree,
  'pbir.refreshTree': PBIR_COMMANDS.refreshReports,
  'pbir.scoreReport': PBIR_COMMANDS.scoreReport,
  'pbir.copyScoreDiagnostics': PBIR_COMMANDS.copyScoreDiagnostics,
  'pbir.governanceCheck': PBIR_COMMANDS.checkGovernance,
  'pbir.exportGovernanceReport': PBIR_COMMANDS.exportGovernanceReport,
  'pbir.exportReviewWorkflow': PBIR_COMMANDS.exportReviewWorkflow,
  'pbir.uploadScreenshots': PBIR_COMMANDS.uploadScreenshots,
  'pbir.attachScreenshot': PBIR_COMMANDS.attachScreenshot,
  'pbir.configureAuditProvider': PBIR_COMMANDS.configureAuditProvider,
};

export const PBIR_VIEW_IDS = {
  explorer: 'pbirAnalyzer.explorer',
} as const;

export const PBIR_CONFIG_SECTIONS = {
  canonical: 'pbirAnalyzer',
  legacy: 'powerbi-modeling',
} as const;
