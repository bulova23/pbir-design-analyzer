# PBIR Page Template Specification

The Phase 41 page-template catalog is deterministic and closed. All templates use a 1280 × 720 canvas and stable rectangles.

| Template | Composition |
| --- | --- |
| Executive Summary | Header; KPI Row with Kpi1 and Kpi2; Primary Analysis with PrimaryChart; Detail Grid with DetailTable; Filter Rail with RegionSlicer; Footer / Navigation |
| Overview | Header; Primary Analysis with PrimaryChart; Secondary Analysis with SecondaryChart; Filter Rail with Filter1; Footer / Navigation |
| Detail | Header; Filter Rail with Filter1; Detail Grid with DetailTable; Footer / Navigation |
| Comparison | Header; Primary Analysis with PrimaryChart and SecondaryChart; Footer / Navigation |

The catalog does not support nested sections, arbitrary containers, recursive layout graphs, or free-form page design. Automatic placement walks visual order and then the template's compatible unassigned slots. Visuals beyond the available canvas or serializer layout capacity are rejected by existing validation.

Navigation and slicer regions are metadata-bearing slots. They do not create additional visual families or a second layout engine.
