# RC1 Competitive Analysis

## Conclusion

PBIR Design Analyzer should complement Microsoft’s authoring and delivery
surfaces rather than compete with them. The product opportunity is the
independent design-quality gate: versioned organizational policy, evidence,
review packets, and safe remediation for reports created by people, Microsoft
AI, consultants, or CI/CD.

## Capability comparison

| Surface | What it provides | RC1 relationship | Product boundary |
| --- | --- | --- | --- |
| Microsoft Fabric Skills | Reusable instructions for Fabric-aware AI coding tools; the current overview says the skills produce item definitions and data-movement code, but do not render finished Power BI reports. | Research input only; no external skill code or autonomous execution is imported. | Accept authored Fabric/Power BI outputs and evaluate them against design policy. |
| Power BI Report Design and Report Authoring skills | First-party guidance and PBIR authoring for pages, visuals, filters, slicers, themes, formatting, and validation. | A complementary authoring provider, not a replacement target. | Do not own generic natural-language report creation; independently check the result. |
| Fabric CLI | Authenticated command-line access for Fabric automation and service-principal workflows. | A future delivery/integration surface. | Add design-governance checks to existing automation rather than replace Fabric deployment. |
| PBI Lens | Optional rendered-review companion when a supported interface is available. | RC1 uses capability detection, human checklist review, typed screenshot evidence, and deterministic fallback. | Do not build a competing viewer or claim automated visual intelligence. |

## Strategic fit

Microsoft’s Power BI Agentic materials describe an increasingly complete
design, plan, author, validate, and publish path. The Power BI Report Authoring
skill specifically targets PBIR/PBIP report creation and modification, while
Skills for Fabric cover Fabric-aware coding workflows. The Fabric CLI provides
the authenticated automation path around Fabric resources. These are reasons
to narrow the product promise, not reasons to add another generic authoring
agent.

The complementary workflow is:

1. Microsoft or another authoring provider creates or edits the report.
2. PBIR Design Analyzer imports the repository snapshot and available
   semantic/rendered evidence.
3. Design Governance evaluates policy, score, findings, and readiness.
4. The reviewer receives evidence-backed issues and a deterministic remediation
   preview.
5. Fabric CLI, Git, or an existing delivery pipeline can consume the resulting
   quality gate after approval.

## Differentiation

- Provider-neutral review rather than provider-owned authoring.
- Stable findings and evidence rather than advice that disappears in chat.
- Organizational policy and waivers rather than only general design guidance.
- Deterministic preview/apply/rollback rather than autonomous mutation.
- CI/CD-ready outcomes rather than a desktop-only review conversation.
- Rendered observation as bounded evidence, kept separate from Visual
  Intelligence and mutation authority.

## Sources

- [Skills for Fabric overview](https://learn.microsoft.com/en-us/fabric/fundamentals/skills-for-fabric-overview)
- [Power BI Report Authoring skill](https://learn.microsoft.com/en-us/power-bi/developer/agentic/power-bi-report-authoring-skill-overview)
- [Power BI agentic capabilities](https://learn.microsoft.com/en-us/power-bi/developer/agentic/)
- [Fabric command line interface](https://learn.microsoft.com/en-us/rest/api/fabric/articles/fabric-command-line-interface)
- [PBI Lens integration notes](../../integrations/pbi-lens-rendered-evidence.md)
