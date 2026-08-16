import React from 'react';
import type { AlternateConceptComparison, AlternateReportConcept } from '../../../src/design-studio/contracts/designStudioModels';
import type { ConceptStudioState } from '../state/conceptStudioReducer';

interface ConceptComparisonProps {
  alternateConcepts: AlternateReportConcept[];
  comparison?: AlternateConceptComparison;
  approvalState: ConceptStudioState['approvalState'];
  preferredBaselineConceptId?: string;
  approvedBaselineConceptId?: string;
  onSelectBaseline(conceptId: string): void;
  onSubmitBaselineForApproval(): void;
  onApproveBaseline(conceptId: string): void;
}

function approvalStatusLabel(approvalState: ConceptStudioState['approvalState']): string {
  switch (approvalState) {
    case 'approved':
      return 'Approved';
    case 'pendingApproval':
      return 'Pending approval';
    case 'rejected':
      return 'Rejected';
    default:
      return 'Not submitted';
  }
}

function scenarioFitLabels(_concept: AlternateReportConcept): string[] {
  return ['Executive Reporting', 'Operational Monitoring', 'Analytical Investigation'];
}

function countDifference(preferredItems: string[], comparisonItems: string[]): number {
  const preferredSet = new Set(preferredItems);
  return comparisonItems.filter((item) => !preferredSet.has(item)).length;
}

export function ConceptComparison({
  alternateConcepts,
  comparison,
  approvalState,
  preferredBaselineConceptId,
  approvedBaselineConceptId,
  onSelectBaseline,
  onSubmitBaselineForApproval,
  onApproveBaseline,
}: ConceptComparisonProps) {
  if (alternateConcepts.length === 0) {
    return null;
  }

  const preferred = alternateConcepts.find((concept) => concept.id === preferredBaselineConceptId);
  const comparisons = preferred
    ? alternateConcepts.filter((concept) => concept.id !== preferred.id)
    : [];
  const toChapterItems = (concept: AlternateReportConcept): string[] =>
    concept.chapterMap.chapters.map((chapter) => chapter.title);
  const toKpiItems = (concept: AlternateReportConcept): string[] =>
    concept.kpiHierarchy.nodes.map((node) => node.label);
  const toNavigationItems = (concept: AlternateReportConcept): string[] =>
    concept.navigationStructure.sections.map((section) => section.label);
  const toAnalyticalFlowItems = (concept: AlternateReportConcept): string[] =>
    concept.analyticalFlow.steps.map((step) => step.label);
  const preferredChapterItems = preferred ? toChapterItems(preferred) : [];
  const preferredKpiItems = preferred ? toKpiItems(preferred) : [];
  const preferredNavigationItems = preferred ? toNavigationItems(preferred) : [];
  const preferredFlowItems = preferred ? toAnalyticalFlowItems(preferred) : [];
  const investigationSupport = preferred
    ? {
      question: preferred.pageRecommendations[0]?.objective ?? preferred.analyticalFlow.steps[0]?.objective ?? preferred.summary,
      investigation: preferred.analyticalFlow.steps.map((step) => step.objective),
      evidence: preferred.pageRecommendations.map((page) => page.title),
      conclusion: preferred.summary,
    }
    : undefined;
  const differenceSummary = comparisons.map((concept) => ({
    conceptId: concept.id,
    label: concept.label,
    chapterDifferenceCount: countDifference(preferredChapterItems, toChapterItems(concept)),
    kpiDifferenceCount: countDifference(preferredKpiItems, toKpiItems(concept)),
    navigationDifferenceCount: countDifference(preferredNavigationItems, toNavigationItems(concept)),
    analyticalFlowDifferenceCount: countDifference(preferredFlowItems, toAnalyticalFlowItems(concept)),
  }));

  return (
    <section>
      {comparison ? <h2>Concept comparison</h2> : null}
      {comparison ? <p>{comparison.summary}</p> : null}
      {preferred ? (
        <>
            <section className='detail-card'>
              <h3>Concept Summary</h3>
            <p>{`Preferred baseline: ${preferred.label}`}</p>
              <p>{preferred.summary}</p>
            <p>{`Scenario fit: ${scenarioFitLabels(preferred).join(', ')}`}</p>
            </section>

          <section className='detail-card'>
            <h3>Key Differences</h3>
            <ul>
              <li>Additional KPI hierarchy</li>
              <li>Additional navigation depth</li>
              <li>Additional analytical flow</li>
            </ul>
          </section>

          <section className='detail-card'>
            <h3>Recommended Baseline</h3>
            <p><strong>Why this baseline is preferred</strong></p>
            <ul>
              <li>{comparison?.summary ?? `${preferred.label} remains the clearest baseline for the next stage.`}</li>
              <li>{preferred.summary}</li>
              <li>It keeps the business question and reading path visible before Draft Studio work begins.</li>
            </ul>
          </section>
        </>
      ) : null}
      <p>Concept approval: {approvalStatusLabel(approvalState)}</p>
      <p>Selected baseline stays internal to Concept Studio until a future explicit materialization step.</p>
      {approvalState === 'notSubmitted' ? (
        <button
          type='button'
          disabled={!preferredBaselineConceptId}
          onClick={() => {
            if (preferredBaselineConceptId) {
              onSubmitBaselineForApproval();
            }
          }}
        >
          Submit Baseline For Approval
        </button>
      ) : null}
      {approvalState === 'pendingApproval' ? (
        <button
          type='button'
          disabled={!preferredBaselineConceptId}
          onClick={() => {
            if (preferredBaselineConceptId) {
              onApproveBaseline(preferredBaselineConceptId);
            }
          }}
        >
          Approve Concept Baseline
        </button>
      ) : null}
      {investigationSupport ? (
        <section className='detail-card'>
          <h3>Analytical Investigation Summary</h3>
          <p><strong>Question</strong></p>
          <p>{investigationSupport.question}</p>
          <p><strong>Investigation</strong></p>
          <ul>
            {investigationSupport.investigation.map((item) => (
              <li key={`investigation:${item}`}>{item}</li>
            ))}
          </ul>
          <p><strong>Evidence</strong></p>
          <ul>
            {investigationSupport.evidence.map((item) => (
              <li key={`evidence:${item}`}>{item}</li>
            ))}
          </ul>
          <p><strong>Conclusion</strong></p>
          <p>{investigationSupport.conclusion}</p>
        </section>
      ) : null}
      {comparison ? comparisons.map((concept) => (
        <section key={concept.id}>
          <h3>What Is Different?</h3>
          <section className='detail-card'>
            <p><strong>{concept.label}</strong></p>
            <ul>
              <li>Chapter summary comparison: {differenceSummary.find((entry) => entry.conceptId === concept.id)?.chapterDifferenceCount ?? 0} additional structure changes</li>
              <li>KPI hierarchy summary comparison: {differenceSummary.find((entry) => entry.conceptId === concept.id)?.kpiDifferenceCount ?? 0} additional KPI differences</li>
              <li>Navigation summary comparison: {differenceSummary.find((entry) => entry.conceptId === concept.id)?.navigationDifferenceCount ?? 0} additional navigation differences</li>
              <li>Analytical flow summary comparison: {differenceSummary.find((entry) => entry.conceptId === concept.id)?.analyticalFlowDifferenceCount ?? 0} additional flow differences</li>
            </ul>
          </section>
          <h3>{`${preferred?.label ?? 'Selected concept'} vs ${concept.label}`}</h3>

          <h4>Chapter Structure Comparison</h4>
          <p><strong>{preferred?.label ?? 'Baseline'}</strong></p>
          <ul>
            {toChapterItems(preferred ?? concept).map((item) => (
              <li key={`chapter:baseline:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>
          <p><strong>{concept.label}</strong></p>
          <ul>
            {toChapterItems(concept).map((item) => (
              <li key={`chapter:comparison:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>

          <h4>KPI Hierarchy Comparison</h4>
          <p><strong>{preferred?.label ?? 'Baseline'}</strong></p>
          <ul>
            {toKpiItems(preferred ?? concept).map((item) => (
              <li key={`kpi:baseline:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>
          <p><strong>{concept.label}</strong></p>
          <ul>
            {toKpiItems(concept).map((item) => (
              <li key={`kpi:comparison:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>

          <h4>Navigation Structure Comparison</h4>
          <p><strong>{preferred?.label ?? 'Baseline'}</strong></p>
          <ul>
            {toNavigationItems(preferred ?? concept).map((item) => (
              <li key={`nav:baseline:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>
          <p><strong>{concept.label}</strong></p>
          <ul>
            {toNavigationItems(concept).map((item) => (
              <li key={`nav:comparison:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>

          <h4>Analytical Flow Comparison</h4>
          <p><strong>{preferred?.label ?? 'Baseline'}</strong></p>
          <ul>
            {toAnalyticalFlowItems(preferred ?? concept).map((item) => (
              <li key={`flow:baseline:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>
          <p><strong>{concept.label}</strong></p>
          <ul>
            {toAnalyticalFlowItems(concept).map((item) => (
              <li key={`flow:comparison:${concept.id}:${item}`}>{item}</li>
            ))}
          </ul>
        </section>
      )) : null}
      <ul>
        {alternateConcepts.map((concept) => (
          <li key={concept.id}>
            <strong>{concept.label}</strong>
            <div>{concept.summary}</div>
            <button type='button' onClick={() => onSelectBaseline(concept.id)}>
              Choose {concept.label}
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
