import * as vscode from 'vscode';
import { AnthropicVisualAuditProvider } from './AnthropicVisualAuditProvider';
import { OpenAIVisualAuditProvider } from './OpenAIVisualAuditProvider';
import type { VisualAuditProvider } from './VisualAuditProvider';

const ACTIVE_PROVIDER_KEY = 'pbir-audit.active-provider';

type ProviderChoice = 'anthropic' | 'openai';

export function createActiveProvider(context: vscode.ExtensionContext): VisualAuditProvider {
  const choice = context.globalState.get<ProviderChoice>(ACTIVE_PROVIDER_KEY, 'anthropic');
  return choice === 'openai'
    ? new OpenAIVisualAuditProvider(context)
    : new AnthropicVisualAuditProvider(context);
}

export async function runProviderSetupFlow(
  context: vscode.ExtensionContext,
): Promise<VisualAuditProvider | undefined> {
  const picked = await vscode.window.showQuickPick(
    [
      {
        label: 'Anthropic Claude Vision',
        description: 'claude-haiku · Recommended',
        id: 'anthropic' as ProviderChoice,
      },
      {
        label: 'OpenAI GPT-4o Vision',
        description: 'gpt-4o · Requires OpenAI API key',
        id: 'openai' as ProviderChoice,
      },
    ],
    {
      title: 'Select AI Audit Provider',
      placeHolder: 'Choose which AI provider to use for screenshot analysis',
    },
  );

  if (!picked) {
    return undefined;
  }

  const keyConfig: Record<ProviderChoice, { title: string; prompt: string }> = {
    anthropic: {
      title: 'Configure Anthropic API Key',
      prompt: 'Enter your Anthropic API key. Stored in VS Code SecretStorage — never written to disk.',
    },
    openai: {
      title: 'Configure OpenAI API Key',
      prompt: 'Enter your OpenAI API key. Stored in VS Code SecretStorage — never written to disk.',
    },
  };

  const { title, prompt } = keyConfig[picked.id];
  const key = await vscode.window.showInputBox({
    title,
    prompt,
    password: true,
    ignoreFocusOut: true,
    validateInput: (v) => (v.trim().length > 0 ? undefined : 'API key is required.'),
  });

  if (!key) {
    return undefined;
  }

  await context.globalState.update(ACTIVE_PROVIDER_KEY, picked.id);

  const provider =
    picked.id === 'openai'
      ? new OpenAIVisualAuditProvider(context)
      : new AnthropicVisualAuditProvider(context);

  await provider.setApiKey(key);
  void vscode.window.showInformationMessage(`${picked.label} configured. Visual Audit is ready.`);
  return provider;
}
