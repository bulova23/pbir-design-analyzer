import React from 'react';
import type { AlternateConceptComparison, AlternateReportConcept } from '../../../src/design-studio/contracts/designStudioModels';

interface ConceptComparisonProps {
  alternateConcepts: AlternateReportConcept[];
  comparison?: AlternateConceptComparison;
  preferredBaselineConceptId?: string;
  approvedBaselineConceptId?: string;
  onSelectBaseline(conceptId: string): void;
  onApproveBaseline(conceptId: string): void;
}

export function ConceptComparison({
  alternateConcepts,
  comparison,
  preferredBaselineConceptId,
  approvedBaselineConceptId,
  onSelectBaseline,
  onApproveBaseline,
}: ConceptComparisonProps) {
  if (alternateConcepts.length === 0 || !comparison) {
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
  const investigationSupport = preferred
    ? {
      question: preferred.pageRecommendations[0]?.objective ?? preferred.analyticalFlow.steps[0]?.objective ?? preferred.summary,
      investigation: preferred.analyticalFlow.steps.map((step) => step.objective),
      evidence: preferred.pageRecommendations.map((page) => page.title),
      conclusion: preferred.summary,
    }
    : undefined;

  return (
    <section>
      <h2>Concept comparison</h2>
      <p>{comparison.summary}</p>
      {preferred ? (
        <p>Preferred baseline: {preferred.label}</p>
      ) : null}
      <p>Draft Studio approval: {approvedBaselineConceptId ? 'Approved' : 'Not approved'}</p>
      <p>Selected baseline stays internal to Concept Studio until a future explicit materialization step.</p>
      <button
        type='button'
        disabled={!preferredBaselineConceptId}
        onClick={() => {
          if (preferredBaselineConceptId) {
            onApproveBaseline(preferredBaselineConceptId);
          }
        }}
      >
        Approve for Draft Studio
      </button>
      {comparisons.map((concept) => (
        <section key={concept.id}>
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
      ))}
      {investigationSupport ? (
        <section>
          <h3>Analytical Investigation Support</h3>
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
