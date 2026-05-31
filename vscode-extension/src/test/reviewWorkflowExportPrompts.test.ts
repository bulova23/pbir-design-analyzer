import * as vscode from 'vscode';
import { chooseProfiledDocumentExportOptions } from '../analyzer/score/reviewWorkflowExportPrompts';

describe('chooseProfiledDocumentExportOptions', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('collects branded consultant metadata when the branded template is chosen', async () => {
    (vscode.window.showQuickPick as jest.Mock)
      .mockResolvedValueOnce({
        label: 'Consultant',
        value: 'consultant',
      })
      .mockResolvedValueOnce({
        label: 'Branded consultant template',
        value: 'brandedConsultant',
      });

    (vscode.window.showInputBox as jest.Mock)
      .mockResolvedValueOnce('Contoso Finance')
      .mockResolvedValueOnce('Northwind BI Advisory')
      .mockResolvedValueOnce('FY26 Executive Dashboard Review')
      .mockResolvedValueOnce('Client Confidential');

    const options = await chooseProfiledDocumentExportOptions('pdf');

    expect(options).toEqual({
      profile: 'consultant',
      templateVariant: 'brandedConsultant',
      branding: {
        clientName: 'Contoso Finance',
        reviewerName: 'Northwind BI Advisory',
        engagementName: 'FY26 Executive Dashboard Review',
        confidentiality: 'Client Confidential',
      },
    });
  });

  it('returns an executive profile without asking for branding metadata', async () => {
    (vscode.window.showQuickPick as jest.Mock).mockResolvedValueOnce({
      label: 'Executive',
      value: 'executive',
    });

    const options = await chooseProfiledDocumentExportOptions('html');

    expect(options).toEqual({
      profile: 'executive',
      templateVariant: 'standard',
    });
    expect(vscode.window.showInputBox).not.toHaveBeenCalled();
  });

  it('uses the active preview profile as the default export option', async () => {
    (vscode.window.showQuickPick as jest.Mock).mockResolvedValueOnce({
      label: 'Executive',
      value: 'executive',
    });

    await chooseProfiledDocumentExportOptions('html', {
      profile: 'executive',
      templateVariant: 'standard',
    });

    expect(vscode.window.showQuickPick).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({
          label: 'Executive',
          value: 'executive',
          description: expect.stringContaining('Current preview'),
        }),
      ]),
      expect.objectContaining({
        placeHolder: expect.stringContaining('Executive'),
      }),
    );
    expect((vscode.window.showQuickPick as jest.Mock).mock.calls[0][0][0]).toEqual(
      expect.objectContaining({
        label: 'Executive',
        value: 'executive',
      }),
    );
  });

  it('uses the active consultant template as the default export option', async () => {
    (vscode.window.showQuickPick as jest.Mock)
      .mockResolvedValueOnce({
        label: 'Consultant',
        value: 'consultant',
      })
      .mockResolvedValueOnce({
        label: 'Branded consultant template',
        value: 'brandedConsultant',
      });

    (vscode.window.showInputBox as jest.Mock)
      .mockResolvedValueOnce('Contoso Finance')
      .mockResolvedValueOnce('Northwind BI Advisory')
      .mockResolvedValueOnce('FY26 Executive Dashboard Review')
      .mockResolvedValueOnce('Client Confidential');

    await chooseProfiledDocumentExportOptions('pdf', {
      profile: 'consultant',
      templateVariant: 'brandedConsultant',
    });

    expect(vscode.window.showQuickPick).toHaveBeenNthCalledWith(
      2,
      expect.arrayContaining([
        expect.objectContaining({
          label: 'Branded consultant template',
          value: 'brandedConsultant',
          description: expect.stringContaining('Current preview'),
        }),
      ]),
      expect.objectContaining({
        placeHolder: expect.stringContaining('Branded consultant template'),
      }),
    );
    expect((vscode.window.showQuickPick as jest.Mock).mock.calls[1][0][0]).toEqual(
      expect.objectContaining({
        label: 'Branded consultant template',
        value: 'brandedConsultant',
      }),
    );
  });
});
