import { createScorePanelExportWorkflowService } from '../views/scorePanelExportWorkflowService';

describe('scorePanelExportWorkflowService', () => {
  it('warns when preview is requested before a score result exists', async () => {
    const showWarningMessage = jest.fn();
    const service = createScorePanelExportWorkflowService({
      showWarningMessage,
      loadIntentFeedbackSession: jest.fn(),
      buildReviewWorkflowExportData: jest.fn(),
      buildReviewPacketPreviewHtml: jest.fn(),
      openExternal: jest.fn(),
      makeTempDir: jest.fn(),
      writeFile: jest.fn(),
      showQuickPick: jest.fn(),
      chooseProfiledDocumentExportOptions: jest.fn(),
      showSaveDialog: jest.fn(),
      exportReviewWorkflowAsMarkdown: jest.fn(),
      exportReviewWorkflowAsHtml: jest.fn(),
      exportReviewWorkflowAsPdf: jest.fn(),
      exportReviewWorkflowAsJson: jest.fn(),
      showInformationMessage: jest.fn(),
      openTextDocument: jest.fn(),
      executeCommand: jest.fn(),
      getReportPath: () => '/Reports/Sales.Report',
      context: {} as never,
      getCurrentResult: () => undefined,
      getReviewPacketPreviewOptions: () => ({
        profile: 'consultant',
        templateVariant: 'default',
      } as never),
    });

    await service.openReviewPacketPreview();

    expect(showWarningMessage).toHaveBeenCalledWith(
      'Score the report before opening the review packet preview.',
    );
  });
});
