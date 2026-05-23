import React from 'react';
import type {
  ConfigPanelHostToWebviewMessage,
  ConfigPanelStatus,
  ConfigPanelWebviewToHostMessage,
} from '../../src/analyzer/contracts/configPanel';
import type {
  AudiencePreset,
  DesignAnalyzerConfig,
  GovernanceRule,
  GovernanceRuleValue,
  NavigationScoringConfig,
  ScoringFramework,
} from '../../src/analyzer/config/types';
import { applyAudiencePreset } from '../../src/analyzer/config/presets';

interface ConfigVsCodeApi {
  postMessage(message: ConfigPanelWebviewToHostMessage): void;
}

declare function acquireVsCodeApi(): ConfigVsCodeApi;

function calculateWeightSummary(config: DesignAnalyzerConfig | null): {
  isValid: boolean;
  totalWeight: number;
  message: string;
} {
  if (!config) {
    return {
      isValid: false,
      totalWeight: 0,
      message: 'Waiting for analyzer configuration…',
    };
  }

  if (
    !Number.isFinite(config.navigationScoring.weight) ||
    config.navigationScoring.weight < 0 ||
    config.navigationScoring.weight > 100
  ) {
    return {
      isValid: false,
      totalWeight: 0,
      message: 'Navigation scoring weight must be between 0% and 100%.',
    };
  }

  const totalWeight = config.frameworks
    .filter((framework) => framework.enabled)
    .reduce((sum, framework) => sum + framework.weight, 0);

  if (totalWeight !== 100) {
    return {
      isValid: false,
      totalWeight,
      message: `Enabled frameworks must total 100%. Current total: ${totalWeight}%.`,
    };
  }

  return {
    isValid: true,
    totalWeight,
    message: 'Enabled frameworks total 100%.',
  };
}

function formatStatusClass(status?: ConfigPanelStatus): string {
  if (!status) {
    return 'status-banner';
  }

  return `status-banner status-banner-${status.level}`;
}

function cloneConfig(config: DesignAnalyzerConfig): DesignAnalyzerConfig {
  return {
    ...config,
    frameworks: config.frameworks.map((framework) => ({ ...framework })),
    governance: config.governance.map((rule) => ({ ...rule })),
    navigationScoring: { ...config.navigationScoring },
    appliedAudiencePresetId: config.appliedAudiencePresetId,
  };
}

function updateFramework(
  config: DesignAnalyzerConfig,
  frameworkId: string,
  updater: (framework: ScoringFramework) => ScoringFramework,
): DesignAnalyzerConfig {
  return {
    ...config,
    frameworks: config.frameworks.map((framework) =>
      framework.id === frameworkId ? updater(framework) : framework,
    ),
    appliedAudiencePresetId: undefined,
  };
}

function updateGovernanceRule(
  config: DesignAnalyzerConfig,
  ruleId: string,
  value: GovernanceRuleValue,
): DesignAnalyzerConfig {
  return {
    ...config,
    governance: config.governance.map((rule) =>
      rule.id === ruleId ? { ...rule, value } : rule,
    ),
    appliedAudiencePresetId: undefined,
  };
}

function updateNavigationScoring(
  config: DesignAnalyzerConfig,
  updater: (navigationScoring: NavigationScoringConfig) => NavigationScoringConfig,
): DesignAnalyzerConfig {
  return {
    ...config,
    navigationScoring: updater(config.navigationScoring),
    appliedAudiencePresetId: undefined,
  };
}

function getStep(value: GovernanceRuleValue): string | undefined {
  if (typeof value !== 'number') {
    return undefined;
  }

  return Number.isInteger(value) ? '1' : '0.01';
}

function renderGovernanceInput(
  rule: GovernanceRule,
  onChange: (value: GovernanceRuleValue) => void,
): React.ReactNode {
  if (typeof rule.value === 'boolean') {
    return (
      <label className="toggle-field">
        <input
          checked={rule.value}
          onChange={(event) => onChange(event.target.checked)}
          type="checkbox"
        />
        <span>{rule.value ? 'Enabled' : 'Disabled'}</span>
      </label>
    );
  }

  if (typeof rule.value === 'number') {
    return (
      <input
        className="governance-input"
        onChange={(event) => onChange(Number(event.target.value))}
        step={getStep(rule.value)}
        type="number"
        value={rule.value}
      />
    );
  }

  return (
    <input
      className="governance-input"
      onChange={(event) => onChange(event.target.value)}
      type="text"
      value={rule.value}
    />
  );
}

export default function App(): JSX.Element {
  const [config, setConfig] = React.useState<DesignAnalyzerConfig | null>(null);
  const [presets, setPresets] = React.useState<AudiencePreset[]>([]);
  const [status, setStatus] = React.useState<ConfigPanelStatus | undefined>();
  const vscodeApiRef = React.useRef<ConfigVsCodeApi | null>(null);

  if (!vscodeApiRef.current) {
    vscodeApiRef.current = acquireVsCodeApi();
  }

  React.useEffect(() => {
    const handleMessage = (event: MessageEvent<ConfigPanelHostToWebviewMessage>) => {
      const message = event.data;
      if (!message || typeof message !== 'object' || !('type' in message)) {
        return;
      }

      if (message.type === 'configState') {
        setConfig(cloneConfig(message.config));
        setStatus(message.status);
        if (message.presets) {
          setPresets(message.presets);
        }
      } else if (message.type === 'error') {
        setStatus({
          level: 'error',
          message: message.message,
        });
      }
    };

    window.addEventListener('message', handleMessage);
    vscodeApiRef.current?.postMessage({ type: 'webviewReady' });

    return () => {
      window.removeEventListener('message', handleMessage);
    };
  }, []);

  const weightSummary = React.useMemo(() => calculateWeightSummary(config), [config]);

  const enabledFrameworks = React.useMemo(
    () => config?.frameworks.filter((framework) => framework.enabled) ?? [],
    [config],
  );
  const optionalFrameworks = React.useMemo(
    () => config?.frameworks.filter((framework) => !framework.enabled) ?? [],
    [config],
  );

  const handleSave = () => {
    if (!config) {
      return;
    }

    if (!weightSummary.isValid) {
      setStatus({
        level: 'error',
        message: weightSummary.message,
      });
      return;
    }

    vscodeApiRef.current?.postMessage({
      type: 'saveConfig',
      config,
    });
  };

  const handleReset = () => {
    if (!window.confirm('Reset analyzer scoring and governance settings to defaults?')) {
      return;
    }

    vscodeApiRef.current?.postMessage({ type: 'resetConfig' });
  };

  const handlePresetSelect = (presetId: string) => {
    if (!config) {
      return;
    }
    if (presetId === '') {
      setConfig({ ...config, appliedAudiencePresetId: undefined });
      return;
    }
    const preset = presets.find((entry) => entry.id === presetId);
    if (!preset) {
      return;
    }
    setConfig(applyAudiencePreset(config, preset));
    setStatus({
      level: 'success',
      message: `Applied "${preset.name}" preset. Save to persist, or continue tuning individual fields.`,
    });
  };

  return (
    <main className="page-shell">
      <section className="hero-card">
        <div className="hero-copy">
          <p className="eyebrow">PBIR Design Analyzer</p>
          <h1>Design Analyzer Configuration</h1>
          <p className="hero-text">
            Tune which frameworks affect report scoring, then adjust local Enterprise
            Governance scoring defaults. Workspace publish governance is configured separately.
          </p>
        </div>
        <div className={`weight-pill ${weightSummary.isValid ? 'weight-pill-valid' : 'weight-pill-invalid'}`}>
          <span className="weight-pill-label">Enabled Weight</span>
          <strong>{weightSummary.totalWeight}%</strong>
        </div>
      </section>

      {status?.message ? (
        <section className={formatStatusClass(status)} role="status">
          {status.message}
        </section>
      ) : null}

      {!config ? (
        <section className="section-card loading-card">
          <p>Loading analyzer configuration…</p>
        </section>
      ) : (
        <>
          {presets.length > 0 ? (
            <section className="section-card preset-card">
              <div className="section-header">
                <div>
                  <p className="section-kicker">Audience</p>
                  <h2>Preset</h2>
                </div>
                <p className="section-caption">
                  Apply a starting bundle of thresholds; tune individual fields afterward.
                </p>
              </div>
              <label className="preset-control">
                <span>Audience preset</span>
                <select
                  className="preset-select"
                  onChange={(event) => handlePresetSelect(event.target.value)}
                  value={config.appliedAudiencePresetId ?? ''}
                >
                  <option value="">No preset (custom)</option>
                  {presets.map((preset) => (
                    <option key={preset.id} value={preset.id}>
                      {preset.name}
                    </option>
                  ))}
                </select>
              </label>
              {config.appliedAudiencePresetId
                ? (() => {
                    const active = presets.find(
                      (preset) => preset.id === config.appliedAudiencePresetId,
                    );
                    return active?.description ? (
                      <p className="preset-description">{active.description}</p>
                    ) : null;
                  })()
                : null}
            </section>
          ) : null}

          <section className="section-card">
            <div className="section-header">
              <div>
                <p className="section-kicker">Scoring</p>
                <h2>Enabled Frameworks</h2>
              </div>
              <p className={`section-caption ${weightSummary.isValid ? 'caption-valid' : 'caption-invalid'}`}>
                {weightSummary.message}
              </p>
            </div>

            <div className="framework-grid">
              {enabledFrameworks.map((framework) => (
                <article className="framework-card framework-card-enabled" key={framework.id}>
                  <div className="framework-header">
                    <div>
                      <h3>{framework.name}</h3>
                      {framework.optional ? <span className="optional-badge">Optional</span> : null}
                    </div>
                    <label className="toggle-field">
                      <input
                        checked={framework.enabled}
                        onChange={() =>
                          setConfig((current) =>
                            current
                              ? updateFramework(current, framework.id, (item) => ({
                                  ...item,
                                  enabled: !item.enabled,
                                  weight: !item.enabled ? item.weight : 0,
                                }))
                              : current,
                          )
                        }
                        type="checkbox"
                      />
                      <span>{framework.enabled ? 'On' : 'Off'}</span>
                    </label>
                  </div>

                  <p className="framework-description">{framework.description}</p>

                  <div className="framework-controls">
                    <label className="weight-control">
                      <span>Weight</span>
                      <input
                        max={100}
                        min={0}
                        onChange={(event) =>
                          setConfig((current) =>
                            current
                              ? updateFramework(current, framework.id, (item) => ({
                                  ...item,
                                  weight: Number(event.target.value),
                                }))
                              : current,
                          )
                        }
                        type="number"
                        value={framework.weight}
                      />
                    </label>
                    {framework.reference ? (
                      <a className="framework-link" href={framework.reference} rel="noreferrer" target="_blank">
                        Reference
                      </a>
                    ) : null}
                  </div>
                </article>
              ))}
            </div>
          </section>

          <section className="section-card">
            <div className="section-header">
              <div>
                <p className="section-kicker">Scoring</p>
                <h2>Optional Frameworks</h2>
              </div>
              <p className="section-caption">
                Keep the active mix narrow. Enable additional frameworks only when they add signal.
              </p>
            </div>

            <div className="framework-grid framework-grid-compact">
              {optionalFrameworks.map((framework) => (
                <article className="framework-card framework-card-disabled" key={framework.id}>
                  <div className="framework-header">
                    <div>
                      <h3>{framework.name}</h3>
                      <span className="optional-badge">Off</span>
                    </div>
                    <button
                      className="secondary-button"
                      onClick={() =>
                        setConfig((current) =>
                          current
                            ? updateFramework(current, framework.id, (item) => ({
                                ...item,
                                enabled: true,
                                weight: item.weight === 0 ? 5 : item.weight,
                              }))
                            : current,
                        )
                      }
                      type="button"
                    >
                      Enable
                    </button>
                  </div>
                  <p className="framework-description">{framework.description}</p>
                </article>
              ))}
            </div>
          </section>

          <section className="section-card">
            <div className="section-header">
              <div>
                <p className="section-kicker">Navigation</p>
                <h2>Navigation Treatment</h2>
              </div>
              <p className="section-caption">
                Modern Power BI reports rely on buttons, slicers, and other navigation controls.
                Score them as functional ink instead of letting them count like full data visuals.
              </p>
            </div>

            <article className="framework-card framework-card-enabled">
              <div className="framework-header">
                <div>
                  <h3>Navigation Scoring</h3>
                  <p className="framework-description">
                    When enabled, common navigation controls contribute a reduced weight to
                    complexity-oriented scoring and are excluded from Data-Ink Ratio.
                  </p>
                </div>
                <label className="toggle-field">
                  <input
                    checked={config.navigationScoring.enabled}
                    onChange={() =>
                      setConfig((current) =>
                        current
                          ? updateNavigationScoring(current, (navigationScoring) => ({
                              ...navigationScoring,
                              enabled: !navigationScoring.enabled,
                            }))
                          : current,
                      )
                    }
                    type="checkbox"
                  />
                  <span>{config.navigationScoring.enabled ? 'Reduced Weight' : 'Legacy Weight'}</span>
                </label>
              </div>

              <div className="framework-controls">
                <label className="weight-control">
                  <span>Navigation Weight</span>
                  <input
                    max={100}
                    min={0}
                    onChange={(event) =>
                      setConfig((current) =>
                        current
                          ? updateNavigationScoring(current, (navigationScoring) => ({
                              ...navigationScoring,
                              weight: Number(event.target.value),
                            }))
                          : current,
                      )
                    }
                    type="number"
                    value={config.navigationScoring.weight}
                  />
                </label>
                <p className="framework-description navigation-help">
                  {config.navigationScoring.enabled
                    ? `Navigation controls currently count at ${config.navigationScoring.weight}% of a standard data visual.`
                    : 'Navigation controls currently use legacy full-weight treatment.'}
                </p>
              </div>
            </article>
          </section>

          <section className="section-card">
            <div className="section-header">
              <div>
                <p className="section-kicker">Governance</p>
                <h2>Analyzer Governance Defaults</h2>
              </div>
              <button
                className="secondary-button"
                onClick={() => vscodeApiRef.current?.postMessage({ type: 'openGovernanceJson' })}
                type="button"
              >
                Open Defaults JSON
              </button>
            </div>

            <p className="section-caption">
              These values affect the optional Enterprise Governance scoring framework in your
              saved analyzer profile. They do not enable or control workspace publish blocking.
            </p>

            <div className="governance-grid">
              {config.governance.map((rule) => (
                <article className="governance-card" key={rule.id}>
                  <div className="governance-header">
                    <div>
                      <h3>{rule.name}</h3>
                      <p className="governance-meta">
                        {rule.severity ? `Severity: ${rule.severity}` : 'Advisory'}
                        {rule.adminOnly ? ' · Admin' : ''}
                      </p>
                    </div>
                  </div>
                  {rule.description ? <p className="framework-description">{rule.description}</p> : null}
                  {renderGovernanceInput(rule, (value) =>
                    setConfig((current) => (current ? updateGovernanceRule(current, rule.id, value) : current))
                  )}
                </article>
              ))}
            </div>
          </section>

          <section className="action-row">
            <button
              className="primary-button"
              disabled={!weightSummary.isValid}
              onClick={handleSave}
              type="button"
            >
              Save Configuration
            </button>
            <button className="secondary-button" onClick={handleReset} type="button">
              Reset to Defaults
            </button>
          </section>
        </>
      )}
    </main>
  );
}
