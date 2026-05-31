# Consultant Deliverables & Export Platform Design

Date: 2026-05-31

## Goal

Evolve the existing review packet preview and export workflows into a clearer consultant-facing deliverables platform without changing underlying scoring.

## Scope

Include:

- export profiles
- persona-aware export-summary wording
- smarter executive summary language polish
- branded consultant-ready PDF/export profiles
- export workspace redesign
- AI-generated executive narrative/commentary
- future DOCX/PDF architecture

## Architecture

Keep the current layering:

- scoring and findings remain authoritative
- export builders consume existing review packet and workspace state
- export presentation becomes a dedicated downstream workflow

Recommended new layers:

- export presentation adapter
- profile-aware export summary builder
- packet renderer abstraction for HTML/PDF/future DOCX

## Data Flow

`ScoreResult`
`+ normalized findings`
`+ overview summary`
`+ fix plan`
`+ review packet state`
`-> export presentation adapter`
`-> profile-aware packet model`
`-> HTML/PDF/DOCX renderers`

## UX Flow

1. User completes review in the score panel.
2. User enters a clearer export workspace.
3. User chooses profile, audience tone, and deliverable format.
4. User previews summary emphasis and packet branding.
5. User exports consultant-ready deliverables.

## Test Strategy

- export profile selection tests
- summary wording emphasis tests
- packet rendering tests
- preview/export alignment tests
- regression tests for existing Markdown/HTML/PDF behavior

## Non-Goals

- no scoring changes
- no score mutation from export choices
- no re-implementation of the full score panel

## Dependencies

- current review packet preview/export pipeline
- stable overview/fix-plan outputs
- stable persona presentation metadata
