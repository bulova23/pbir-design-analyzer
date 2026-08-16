import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import type { ExtensionContext } from 'vscode';
import type {
  ReviewWorkflowExportProfile,
  ReviewWorkflowMarkdownRenderOptions,
  ScoreResult,
  RenderedReviewPanelState,
} from '../analyzer/contracts/scorePanel';
import {
  buildReviewWorkflowExportData,
  exportReviewWorkflowAsHtml,
  exportReviewWorkflowAsJson,
  exportReviewWorkflowAsMarkdown,
  exportReviewWorkflowAsPdf,
} from '../analyzer/score/reviewWorkflowExport';
import { buildReviewPacketPreviewHtml, type ReviewPacketPreviewOptions } from '../analyzer/score/reviewPacketPreview';
import { chooseProfiledDocumentExportOptions } from '../analyzer/score/reviewWorkflowExportPrompts';
import { loadIntentFeedbackSession } from '../analyzer/intentFeedback/store';

type ExportDeps = {
  context: ExtensionContext;
  getReportPath: () => string;
  getCurrentResult: () => ScoreResult | undefined;
  getReviewPacketPreviewOptions: () => ReviewPacketPreviewOptions;
  getRenderedReview?: () => RenderedReviewPanelState | undefined;
  showWarningMessage?: typeof vscode.window.showWarningMessage;
  loadIntentFeedbackSession?: typeof loadIntentFeedbackSession;
  buildReviewWorkflowExportData?: typeof buildReviewWorkflowExportData;
  buildReviewPacketPreviewHtml?: typeof buildReviewPacketPreviewHtml;
  openExternal?: typeof vscode.env.openExternal;
  makeTempDir?: (prefix: string) => string;
  writeFile?: (filePath: string, content: string | Uint8Array, encoding?: BufferEncoding) => void;
  showQuickPick?: typeof vscode.window.showQuickPick;
  chooseProfiledDocumentExportOptions?: typeof chooseProfiledDocumentExportOptions;
  showSaveDialog?: typeof vscode.window.showSaveDialog;
  exportReviewWorkflowAsMarkdown?: typeof exportReviewWorkflowAsMarkdown;
  exportReviewWorkflowAsHtml?: typeof exportReviewWorkflowAsHtml;
  exportReviewWorkflowAsPdf?: typeof exportReviewWorkflowAsPdf;
  exportReviewWorkflowAsJson?: typeof exportReviewWorkflowAsJson;
  showInformationMessage?: typeof vscode.window.showInformationMessage;
  openTextDocument?: typeof vscode.window.showTextDocument;
  executeCommand?: typeof vscode.commands.executeCommand;
};

export function createScorePanelExportWorkflowService(deps: ExportDeps) {
  const showWarningMessage = deps.showWarningMessage ?? vscode.window.showWarningMessage;
  const loadIntentFeedbackSessionImpl = deps.loadIntentFeedbackSession ?? loadIntentFeedbackSession;
  const buildReviewWorkflowExportDataImpl = deps.buildReviewWorkflowExportData ?? buildReviewWorkflowExportData;
  const buildReviewPacketPreviewHtmlImpl = deps.buildReviewPacketPreviewHtml ?? buildReviewPacketPreviewHtml;
  const openExternal = deps.openExternal ?? vscode.env.openExternal;
  const makeTempDir = deps.makeTempDir ?? ((prefix: string) => fs.mkdtempSync(path.join(os.tmpdir(), prefix)));
  const writeFile = deps.writeFile ?? ((filePath: string, content: string | Uint8Array, encoding?: BufferEncoding) => {
    if (typeof content === 'string') {
      fs.writeFileSync(filePath, content, encoding ?? 'utf8');
      return;
    }

    fs.writeFileSync(filePath, content);
  });
  const showQuickPick = deps.showQuickPick ?? vscode.window.showQuickPick;
  const chooseProfiledDocumentExportOptionsImpl = deps.chooseProfiledDocumentExportOptions ?? chooseProfiledDocumentExportOptions;
  const showSaveDialog = deps.showSaveDialog ?? vscode.window.showSaveDialog;
  const exportReviewWorkflowAsMarkdownImpl = deps.exportReviewWorkflowAsMarkdown ?? exportReviewWorkflowAsMarkdown;
  const exportReviewWorkflowAsHtmlImpl = deps.exportReviewWorkflowAsHtml ?? exportReviewWorkflowAsHtml;
  const exportReviewWorkflowAsPdfImpl = deps.exportReviewWorkflowAsPdf ?? exportReviewWorkflowAsPdf;
  const exportReviewWorkflowAsJsonImpl = deps.exportReviewWorkflowAsJson ?? exportReviewWorkflowAsJson;
  const showInformationMessage = deps.showInformationMessage ?? vscode.window.showInformationMessage;
  const openTextDocument = deps.openTextDocument ?? vscode.window.showTextDocument;
  const executeCommand = deps.executeCommand ?? vscode.commands.executeCommand;

  return {
    async openReviewPacketPreview(): Promise<void> {
      const currentResult = deps.getCurrentResult();
      const reportPath = deps.getReportPath();
      if (!currentResult) {
        void showWarningMessage('Score the report before opening the review packet preview.');
        return;
      }

      const session = await loadIntentFeedbackSessionImpl(deps.context, reportPath);
      const reviewPacketPreview = buildReviewWorkflowExportDataImpl(currentResult, session.entries, undefined, deps.getRenderedReview?.());
      const html = buildReviewPacketPreviewHtmlImpl(
        reviewPacketPreview,
        reportPath,
        deps.getReviewPacketPreviewOptions(),
      );
      const tempDir = makeTempDir('pbir-review-preview-');
      const reportName = path.basename(reportPath).replace(/\.Report$/i, '');
      const profileSuffix = deps.getReviewPacketPreviewOptions().profile.toLowerCase();
      const tempFilePath = path.join(tempDir, `${reportName}-${profileSuffix}-preview.html`);

      writeFile(tempFilePath, html, 'utf8');
      await openExternal(vscode.Uri.file(tempFilePath));
    },
    async exportReviewWorkflow(): Promise<void> {
      const currentResult = deps.getCurrentResult();
      const reportPath = deps.getReportPath();
      if (!currentResult) {
        void showWarningMessage('Score the report before exporting the review summary.');
        return;
      }

      const session = await loadIntentFeedbackSessionImpl(deps.context, reportPath);
      const exportData = buildReviewWorkflowExportDataImpl(currentResult, session.entries, undefined, deps.getRenderedReview?.());
      const formatChoice = await showQuickPick(
        [
          { label: 'Markdown', description: 'Human-readable review summary (.md)' },
          { label: 'HTML', description: 'Styled consultant packet (.html)' },
          { label: 'PDF', description: 'Fixed-layout consultant packet (.pdf)' },
          { label: 'JSON', description: 'Machine-readable review workflow snapshot (.json)' },
        ],
        { placeHolder: 'Choose review export format' },
      );

      if (!formatChoice) {
        return;
      }

      const selectedFormat = formatChoice.label.toLowerCase();
      const isMarkdown = selectedFormat === 'markdown';
      const isHtml = selectedFormat === 'html';
      const isPdf = selectedFormat === 'pdf';
      let markdownProfile: ReviewWorkflowExportProfile = 'consultant';
      let markdownOptions: ReviewWorkflowMarkdownRenderOptions = {};

      if (isMarkdown || isHtml || isPdf) {
        const exportSelection = await chooseProfiledDocumentExportOptionsImpl(
          isMarkdown ? 'markdown' : isHtml ? 'html' : 'pdf',
          deps.getReviewPacketPreviewOptions(),
        );
        if (!exportSelection) {
          return;
        }

        markdownProfile = exportSelection.profile;
        markdownOptions = {
          templateVariant: exportSelection.templateVariant,
          branding: exportSelection.branding,
        };
      }

      const saveUri = await showSaveDialog({
        defaultUri: vscode.Uri.file(
          path.join(
            path.dirname(reportPath),
            `review-workflow-summary.${isMarkdown ? 'md' : isHtml ? 'html' : isPdf ? 'pdf' : 'json'}`,
          ),
        ),
        filters: isMarkdown
          ? { Markdown: ['md'] }
          : isHtml
            ? { HTML: ['html'] }
            : isPdf
              ? { PDF: ['pdf'] }
              : { JSON: ['json'] },
        saveLabel: 'Export',
      });

      if (!saveUri) {
        return;
      }

      if (isPdf) {
        writeFile(saveUri.fsPath, await exportReviewWorkflowAsPdfImpl(exportData, markdownProfile, markdownOptions));
      } else {
        const content = isMarkdown
          ? exportReviewWorkflowAsMarkdownImpl(exportData, markdownProfile, markdownOptions)
          : isHtml
            ? exportReviewWorkflowAsHtmlImpl(exportData, markdownProfile, markdownOptions)
            : exportReviewWorkflowAsJsonImpl(exportData);
        writeFile(saveUri.fsPath, content, 'utf8');
      }

      const openAction = 'Open File';
      const choice = await showInformationMessage(
        `Review workflow summary exported to ${path.basename(saveUri.fsPath)}`,
        openAction,
      );

      if (choice === openAction) {
        if (isPdf) {
          await executeCommand('vscode.open', saveUri);
        } else {
          await openTextDocument(saveUri);
        }
      }
    },
  };
}
