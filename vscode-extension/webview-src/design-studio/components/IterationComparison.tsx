import React from 'react';
import type { ClosedLoopIterationComparison } from '../../../src/design-studio/contracts/designStudioModels';

interface IterationComparisonProps {
  comparison: ClosedLoopIterationComparison;
}

function ChangeList(props: {
  title: string;
  items: string[];
}) {
  return (
    <section>
      <h3>{props.title}</h3>
      {props.items.length > 0 ? (
        <ul>
          {props.items.map((item) => (
            <li key={`${props.title}:${item}`}>{item}</li>
          ))}
        </ul>
      ) : (
        <p>No changes recorded.</p>
      )}
    </section>
  );
}

export function IterationComparison({ comparison }: IterationComparisonProps) {
  return (
    <section>
      <h2>Comparison</h2>
      <p>{comparison.summary}</p>
      <ChangeList title='Concept Changes' items={comparison.conceptChanges} />
      <ChangeList title='Draft Changes' items={comparison.draftChanges} />
      <ChangeList title='Analyzer Output Changes' items={comparison.analyzerOutputChanges} />
      <ChangeList title='Recommendation Changes' items={comparison.recommendationChanges} />
      <ChangeList title='Validation Status Changes' items={comparison.validationStatusChanges} />
    </section>
  );
}
