# Power BI Agent Skills Reference Review

Date: 2026-06-02

Reference source reviewed:

- `data-goblin/power-bi-agentic-development`
  - https://github.com/data-goblin/power-bi-agentic-development

Local architecture constraints preserved:

- scoring remains authoritative
- normalized findings remain the shared issue model
- AI may improve proposal quality
- AI may not bypass deterministic preview, apply, rollback, mutation validation, or re-analysis

## 1. Relevant Skills

The most relevant ideas are pattern-level, not copy-and-import candidates:

- `pbip`, `tmdl`, and `pbir-format`
  - useful as reference categories for file-format guidance, project structure awareness, and edit-surface boundaries
- `pbip-validator` and the PBIR/TMDL/report-binding hooks
  - strong match for deterministic pre-apply and post-apply validation workflows
- `pbir-cli`
  - relevant as a future inspiration for report-shaping utilities inside a controlled deterministic pipeline
- `pbi-report-design` and `review-report`
  - relevant as inspiration for Phase 3 proposal enrichment and future Report Design Studio critique patterns
- reviewer agents such as `deneb-reviewer`, `svg-reviewer`, `r-reviewer`, and `python-reviewer`
  - useful mainly as a specialization pattern for future advisory review surfaces, not for current fix execution

## 2. What Should Be Adopted Now

- Add AGENTS.md guidance that external Power BI agent skills may be used as research input, but only as reference material and never as drop-in execution logic.
- Add AGENTS.md guidance that any AI-generated proposal touching PBIR, TMDL, themes, or bindings must map back to deterministic mutation contracts before it is previewable or applicable.
- Carry forward the repo's hook pattern into local PBIR/TMDL validation planning:
  - deterministic schema/structure checks before apply
  - deterministic binding/path checks after mutation planning
  - explicit machine-readable failure classes surfaced in preview/apply results
- Use the specialization pattern in future Phase 3 proposal enrichment:
  - separate proposal-enrichment prompts by domain such as layout, titles, themes, navigation, and visual scripting
  - keep those enrichers advisory-only and downstream from findings

## 3. What Should Be Deferred

- Future Phase 3 AI Proposal Enrichment:
  - specialized advisory enrichers for report-design critique, theme refinement, and narrative improvement
  - optional reviewer-style enrichment for Deneb, SVG, R, or Python visuals when those surfaces are in scope
- Future Report Design Studio:
  - domain-specific advisory panels for layout/design critique
  - specialized review passes for theme consistency and visual-language quality
- Future PBIR/TMDL validation workflows:
  - a dedicated validator stage that classifies failures as schema, binding, unsupported-surface, or rollback-coverage issues
  - optional CLI-assisted validation where it improves deterministic confidence without owning mutation authority

These are worth deferring because the current product still benefits more from shipping the hardened trust loop and then layering proposal quality improvements above it.

## 4. What Should Not Be Adopted

- Do not import or vendor the external skills, agents, hooks, or prompt files into this product.
  - the source repo is GPL-3.0 and its README explicitly says you do not have license to copy the skills into your own tools without attribution and link-back
  - this project should treat it as reference material, not embedded product logic
- Do not adopt autonomous agent execution for report mutation.
- Do not let reviewer agents write directly to PBIR, TMDL, DAX, or model artifacts.
- Do not replace the current AI Fix architecture with a plugin marketplace or hook-driven execution model.
- Do not expand current deterministic fixes into broad TMDL/model authoring until there is a separate spec for semantic-surface safety.

## 5. Roadmap Effect

This review does not change roadmap order, but it sharpens later epics:

- AI Fix Phase 3 should focus on better proposal generation, rationale quality, and domain-specific advisory enrichment only.
- Report Design Studio should use specialized critique patterns as advisory UX modules, not mutation engines.
- PBIR/TMDL validation should become a clearer deterministic subsystem that supports the trust boundary already defined in Phase 1 and Phase 2.
- Advanced AI refactoring should remain gated behind deterministic mutation contracts and should not absorb marketplace-style autonomous editing.

Recommended roadmap note:

- treat external Power BI agent ecosystems as research inputs for proposal quality and validation coverage
- do not treat them as an execution substrate for this product
