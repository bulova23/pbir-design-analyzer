import * as crypto from 'crypto';
import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import type { VisualAuditFinding } from '../types';
import type { VisualAuditInput, VisualAuditProvider } from './VisualAuditProvider';

const SECRET_KEY = 'pbir-audit.openai-api-key';
const MODEL = 'gpt-4o';
const API_URL = 'https://api.openai.com/v1/chat/completions';

export class OpenAIVisualAuditProvider implements VisualAuditProvider {
  readonly providerName = 'OpenAI GPT-4o Vision';

  constructor(private readonly context: vscode.ExtensionContext) {}

  async isConfigured(): Promise<boolean> {
    const key = await this.context.secrets.get(SECRET_KEY);
    return Boolean(key?.trim());
  }

  async setApiKey(key: string): Promise<void> {
    await this.context.secrets.store(SECRET_KEY, key.trim());
  }

  async analyzeCapture(input: VisualAuditInput): Promise<VisualAuditFinding[]> {
    const apiKey = await this.context.secrets.get(SECRET_KEY);
    if (!apiKey) {
      throw new Error(
        'OpenAI API key not configured. Run "PBIR Design Analyzer: Configure Audit Provider" to add your key.',
      );
    }

    const { capture, pageName, pageScore } = input;

    if (!fs.existsSync(capture.storedPath)) {
      throw new Error(`Screenshot asset not found: ${capture.storedPath}`);
    }

    const imageData = fs.readFileSync(capture.storedPath);
    const base64Image = imageData.toString('base64');
    const mediaType = resolveMediaType(capture.fileName);
    const contextBlock = buildContextBlock(pageName, pageScore);

    const body = JSON.stringify({
      model: MODEL,
      max_tokens: 1024,
      messages: [
        {
          role: 'user',
          content: [
            {
              type: 'image_url',
              image_url: {
                url: `data:${mediaType};base64,${base64Image}`,
                detail: 'high',
              },
            },
            { type: 'text', text: buildPrompt(pageName, contextBlock) },
          ],
        },
      ],
    });

    const response = await fetch(API_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`,
      },
      body,
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`OpenAI API error (${response.status}): ${errorText}`);
    }

    const result = (await response.json()) as OpenAIResponse;
    const textContent = result.choices?.[0]?.message?.content ?? '';
    return parseFindings(textContent, pageName, capture.captureId);
  }
}

interface OpenAIResponse {
  choices: Array<{ message: { content: string } }>;
}

type MediaType = 'image/png' | 'image/jpeg' | 'image/webp' | 'image/gif';

function resolveMediaType(fileName: string): MediaType {
  const ext = path.extname(fileName).toLowerCase();
  if (ext === '.png') return 'image/png';
  if (ext === '.jpg' || ext === '.jpeg') return 'image/jpeg';
  if (ext === '.webp') return 'image/webp';
  if (ext === '.gif') return 'image/gif';
  return 'image/png';
}

function buildContextBlock(pageName: string, pageScore: unknown): string {
  const lines = [`Page name: ${pageName}`];
  if (pageScore && typeof pageScore === 'object' && 'compositeScore' in pageScore) {
    lines.push(
      `PBIR composite score: ${(pageScore as { compositeScore: number }).compositeScore.toFixed(1)}`,
    );
  }
  return lines.join('\n');
}

function buildPrompt(pageName: string, contextBlock: string): string {
  return `You are a Power BI report design auditor. Analyze this screenshot of the report page "${pageName}".

Context:
${contextBlock}

Identify up to 5 visual design issues. For each finding, output a JSON object on its own line with no surrounding text:
{"findingType":"objective|strongHeuristic|stylePreference","severity":"critical|warning|info","confidence":"high|medium|low","text":"<description>","recommendation":"<actionable fix>","regionHint":"<optional area>"}

Classification rules:
- objective: clearly visible issues — clipped text, overlapping visuals, error states, cut-off labels
- strongHeuristic: hierarchy, scan path, spacing, density, or visual balance problems
- stylePreference: polish and consistency observations

Output only the JSON lines. No preamble, no explanation.`;
}

function parseFindings(text: string, pageName: string, captureId: string): VisualAuditFinding[] {
  const findings: VisualAuditFinding[] = [];

  for (const line of text.split('\n')) {
    const trimmed = line.trim();
    if (!trimmed.startsWith('{')) {
      continue;
    }

    try {
      const parsed = JSON.parse(trimmed) as {
        findingType?: string;
        severity?: string;
        confidence?: string;
        text?: string;
        recommendation?: string;
        regionHint?: string;
      };

      if (!parsed.text || !parsed.findingType) {
        continue;
      }

      findings.push({
        findingId: crypto.randomUUID(),
        pageName,
        captureId,
        findingType: toFindingType(parsed.findingType),
        severity: toSeverity(parsed.severity),
        confidence: toConfidence(parsed.confidence),
        text: parsed.text,
        recommendation: parsed.recommendation,
        regionHint: parsed.regionHint,
      });
    } catch {
      // Skip malformed lines
    }
  }

  return findings;
}

function toFindingType(value: string): VisualAuditFinding['findingType'] {
  if (value === 'objective' || value === 'stylePreference') return value;
  return 'strongHeuristic';
}

function toSeverity(value: string | undefined): VisualAuditFinding['severity'] {
  if (value === 'critical' || value === 'info') return value;
  return 'warning';
}

function toConfidence(value: string | undefined): VisualAuditFinding['confidence'] {
  if (value === 'high' || value === 'low') return value;
  return 'medium';
}
