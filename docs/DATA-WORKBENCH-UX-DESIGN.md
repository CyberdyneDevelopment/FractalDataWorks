# FDW Reference UI — "Data Workbench" UX Design

**Status:** Approved design, in execution (branch `feature/data-workbench-ux`, based on the reorganized `feature/1.5.0-development`).
**Scope:** Redesign reference-ui (and the FDW UI packages it consumes) into a commercial-grade, end-to-end ETL authoring + exploration + visualization tool — built entirely on the existing render-mode-agnostic UI architecture.

---

## 1. Vision & Principles

reference-ui must let an operator build and navigate a **complete ETL flow** — connections → datastores → datasets → transformations/calculations → pipelines → schedules — define reusable pieces once and wire them together, **trace lineage** ("how was this report built?"), and **preview + visualize** the data, all on the headless component model with no free-text where a type/option exists, and at **arbitrary scale**.

Five rules (from commercial data tooling — ADF, dbt, Dagster, Fivetran, Looker):

1. **One graph, many lenses.** The domain *is* a graph. Build, run-status, and lineage are the *same* node-graph in different modes — not separate screens.
2. **Flow over forms.** The ETL lifecycle is the spine; authoring happens on a canvas, details in an inspector.
3. **Never free-text what's typed.** Every TypeCollection → a searchable picker; every parameter → a typed, validated input from metadata.
4. **Search-first at every scale.** Command palette + virtualized search/filter in lists *and* pickers. No fixed limits.
5. **Define once, reference everywhere.** Calculations, datasets, connections are named resources surfaced through one entity-picker.

---

## 2. Architecture — render-mode-agnostic logic, swappable rendering (the hard constraint)

This is **built on what already exists** in the reorg checkout, not a parallel architecture:

```
┌─ LOGIC (render-mode-agnostic, NO Blazor) ───────────────────────────┐
│  Domain providers (plain C#, already Blazor-free)                    │
│  + presenters → emit IComponentModel / IPageModel                    │
│    (Browse · List · Dashboard · Wizard  + NEW: Canvas, Report)       │
│  + ValidationResult (IEntityValidator<T>)                            │
│  packages: *.Components, UI.Abstractions(143f), Validation(.Abstr.)  │
├─ CONTRACT (the seam) ───────────────────────────────────────────────┤
│  IUIRenderer.Render(IComponentModel) / Prompt<T> / RenderPage         │
│  IRenderMode (View/Edit/Both) · CollectionDisplayModes · NavDescriptor│
│  + NEW: IChartRenderer + ChartTypes (charts as a renderer plugin)    │
├─ RENDERING (swappable plugins) ─────────────────────────────────────┤
│  Blazor (UI.Components)   Spectre/TUI (UI.Rendering.Spectre)   VSCode │
│  reference-ui = Blazor renderer + Tailwind theme skin ONLY           │
└─────────────────────────────────────────────────────────────────────┘
```

**What exists (build on):** `UI.Abstractions` (zero UI-framework deps): `ComponentBase<TSelf,TModel>`, `IComponentModel`/`IInputComponentModel<T>`, `IPageModel` (Browse/List/Dashboard/Wizard), `IRenderMode`, `CollectionDisplayModes`, the `IUIRenderer` seam. Renderers: Blazor (`BlazorUIRenderer`) + Spectre (`SpectreUIRenderer`). Metadata gen (`Configuration.UI.SourceGenerators`) emits Web+Blazor+TUI+FormModel from one `[ManagedConfiguration]`. Domain providers are plain C#. Validation contract `IEntityValidator<T>`. Nav is data-driven via `PageTypes` + `NavDescriptor`.

**What we add:** two new page-model types — **Canvas** (workbench/lineage graph) and **Report** (visualization/dashboard) — each a render-agnostic model + a Blazor renderer; a charts renderer seam (§8); searchable/virtualized pickers; validation wired into forms; a formal renderer registry.

**Gaps to close (completeness, not architecture):** Spectre coverage for complex components; client-side validation flow; a `ServiceTypeCollection<IUIRenderer>` registry; `TUI.Management` driven by `PageTypes`.

---

## 3. Information Architecture — data-driven, intent-based, scales

Replace the (pre-reorg) hardcoded sidebar with **`NavDescriptor`-driven nav** reorganized into four intent areas + a global command palette:

```
┌────────┬───────────────────────────────────────────────── ⌘K Search… ─┐
│ ◈ Home │  Workspace content                                    [user ▾] │
│ ▸ BUILD│   BUILD     → Workbench (canvas) + Resource library            │
│ ▸ EXPL.│   EXPLORE   → Catalog (search everything) + Lineage            │
│ ▸ OPER.│   OPERATE   → Runs, schedules, health, quality, alerts         │
│ ▸ ADMIN│   ADMIN     → users, roles, secrets, tenants, config (gated)   │
└────────┴─────────────────────────────────────────────────────────────┘
```

- **Command palette (⌘K)** — "New calculation", "Open pipeline X", "Run schedule Y", "Trace lineage of Z". A registry presenters publish (render-agnostic). Kills hunting.
- **Role-gating** = a `NavDescriptor` predicate → Admin hides itself for non-admins; no code edits to reorganize IA.
- **Scale:** every list/tree is `Virtualize`-backed with **server-side search/paging** (list endpoints gain `search`/`skip`/`take`/`sort`). Catalog is the at-scale entry point.

---

## 4. The Workbench — one component, two modes (Canvas page model)

Build == lineage in edit mode (`IRenderMode.Edit` vs `View`) — **the same component**.

```
┌── BUILD ▸ Workbench ───────────────────────────────────── ⌘K ── ▶Run ─┐
│ PALETTE / LIBRARY │            CANVAS                  │  INSPECTOR     │
│ [search ▾]        │   (Conn)→(Store)→(DataSet)         │  ┌──────────┐  │
│ ▾ Connections  ⌕  │                 │                  │  │ DataSet  │  │
│ ▾ DataSets     ⌕  │                 ▼                  │  │ Name  [] │  │
│ ▾ Calculations ⌕  │            (Calc: Tax)             │  │ Type ▾   │  │
│ ▾ Transforms   ⌕  │                 ▼                  │  │ Source ▾ │  │
│ + New ▾           │  (Pipeline)──writes──▶(Sink DS)    │  │ ✓ valid  │  │
│                   │   (Schedule ⟳ daily)              │  │[Save][Test]│ │
├───────────────────┴────────────────────────────────────┴───────────────┤
│ ▾ Validation (0 errors) · ▾ Preview (50 rows) · ▾ Run output            │
└─────────────────────────────────────────────────────────────────────────┘
```

- **Nodes are real config entities.** Edges express real references (`DataStore.ConnectionId`, `DataSet.Source→container`, `Pipeline.source/sink dataset`, `Schedule→Pipeline`, calc attached to dataset/transform). **Saving persists through the real domain providers** — replacing the filesystem-stub designer (`FileSystemDesignerPipelineStore`); what you build is runnable and shows in `GET /pipelines`.
- **Inspector** = metadata-driven editor for the selected node (from `[ManagedConfiguration]`), inline validation, **Schedule/Run on the pipeline node** (fixes schedule decoupling).
- Bottom dock: **validation**, **data preview**, **run output** — in context.
- **Lineage view** = the same canvas in `View` mode, focused on a node's upstream (§7).

---

## 4a. The Canvas — one render-agnostic graph for pipelines, lineage & calculations

The node-graph canvas is a **single reusable component** used in three places, all the same design — and, like charts (§8), it is render-agnostic with a **swappable renderer plugin** (a second reference-app showcase of "headless model + render layer"):

- **Pipelines** — build (Edit) + view
- **Lineage** — view (trace upstream/downstream), with search/filter/focus at scale
- **Calculations** — build/view the operation-step graph (inputs → operations → output); the menu+param form (§5) is the **node inspector** inside the canvas

```
ICanvasModel : IPageModel                  (UI.Abstractions — zero lib refs)
  Nodes  : id, NodeType (CanvasNodeTypes TypeCollection), label, ports, position, status, metadata
  Edges  : source→target (+ ports), EdgeType, label
  Mode   : IRenderMode (View | Edit)
  Layout : hint (dagre / topological / force) — renderer may honor or compute
  Selection + commands (add/connect/move/delete) — Edit mode only

ServiceTypeCollection<ICanvasRenderer>     (renderer registry — SAME pattern as charts)
  ICanvasRenderer.Render(ICanvasModel, IRenderContext) → RenderResult
  + capability flags: SupportsEditing, SupportsPorts, SupportsLargeGraphs, LayoutAlgorithms
  [ServiceTypeOption] BlazorDiagramsRenderer  (Blazor-native node/port/link editor — default for Edit)
  [ServiceTypeOption] CytoscapeRenderer       (JS interop — large lineage + layout algorithms; default for big View)
  [ServiceTypeOption] SvgCanvasRenderer       (hand-rolled SVG — zero-dep, fully themeable baseline)
  [ServiceTypeOption] SpectreCanvasRenderer   (TUI — tree/adjacency view of the same model)
```

- **Default by capability/mode:** Edit → an editor-capable renderer (Blazor.Diagrams); large View → a layout/perf renderer (Cytoscape); a **renderer dropdown** switches per-canvas to showcase interop (same UX as the chart-renderer dropdown).
- **Same model, three node-type sets** via `CanvasNodeTypes`: Connection/DataStore/DataSet/Calculation/Transform/Pipeline/Schedule (workbench); operation/input/output (calculations); any entity (lineage). Logic only emits `ICanvasModel`; renderers never leak into logic.

**Candidate libraries (ship as implementations, no-npm where possible):**
- **Blazor.Diagrams (Z.Blazor.Diagrams)** — MIT, native Blazor node/port/link editor → default editor.
- **Cytoscape.js** — MIT (JS interop), best for large graphs + many layout algorithms → lineage at scale.
- **hand-rolled SVG** — zero-dep, themeable (the existing lineage renderer, generalized) → baseline + TUI-parity story.
- **(optional) maxGraph** — Apache-2.0 (draw.io engine) for heavy diagramming.

Lands in **Phase 3 (Canvas)**. Phase 1's calc builder (the menu+param form) is unaffected — it becomes the node inspector; the calculation's step-graph is a canvas view.

---

## 5. Menu-driven, validated editors (+ calc fix, pickers, validation)

Every editor is the inspector or a focused wizard, generated from metadata. **Calculation builder** rebuilt to match the transform builder that already works:

```
 Calculation: "NetRevenue"
 ┌─────────────────────────────────────────────────────────────────────┐
 │ Type     ◉ Formula   ○ Windowed            (OptionPicker, searchable) │
 │ Operation  [ Multiply ▾ ⌕ ]   (OptionPicker over CalculationOperationTypes)
 │ ── Parameters (from OperationParameterDefinition) ──                  │
 │   Left   [ Amount   ▾⌕ ]  (Kind=Field → column picker, required)      │
 │   Right  [ 1.05      ]    (Kind=Scalar → number, ✗ "must be #")       │
 │   Output [ net_rev   ]    (required ✓)                                │
 │ ⚠ Right must be numeric                       [Save]  [Use in… ▾]     │
 └─────────────────────────────────────────────────────────────────────┘
```

- **Fix:** drop free-text; drive inputs from `ICalculationOperation.Parameters` (`OperationParameterDefinition.Kind` → Scalar=input, Field=column picker, FieldArray=multiselect, DataSet=entity-picker). Plumbing already exists; the builder just isn't using it.
- **Searchable/virtualized pickers everywhere:** upgrade `OptionPicker<T>` (type-ahead + grouping + `Virtualize`) and add an **`EntityPicker`** (pick existing connection/dataset/calc by name, server-side searchable, "create new" hook). Same component whether it lists 5 or 50,000.
- **Validation (currently missing in forms):** wire `IEntityValidator<T>` + metadata rules into generated forms → inline errors, **save blocked until valid**; map server-side `GenericResult` validation failures back to fields. "Use in…" exposes reuse seams (attach calc to dataset field / pipeline transform).

---

## 6. Explore — Catalog + Lineage at arbitrary scale

```
┌ EXPLORE ▸ Catalog ─── ⌕ "revenue"  [Type ▾][Owner ▾][Tag ▾] ─────────┐
│ ▦ Sales (DataSet)    MsSql · 42 cols · 3 consumers   [Open][Trace]    │
│ ▦ NetRevenue (Calc)  Formula · used by 4             [Open][Trace]    │
│ … virtualized, server-paged, full-text … (any N)                     │
└──────────────────────────────────────────────────────────────────────┘
   │ Trace ▼
┌ Lineage: "ExecRevenueReport" ─ ⌕node [▣filter][⊙focus][⤓export]──────┐
│  (Orders)─┐                                                            │
│  (Sales)──┼─▶(NetRevenue)─▶(NightlyLoad)─▶◎ ExecRevenueReport         │
│  (FxRate)─┘                                 └─▶(Dashboard: Exec)       │
│  ▸ expand fields  ▸ collapse to upstream-of-selected                   │
└────────────────────────────────────────────────────────────────────────┘
```

"**How was this report built?**" = from any dataset/report → **Trace** → canvas (view mode) focused on its upstream, with **search-to-node, type/status filters, focus+context, field-level expansion**. Graph API + config-derived capture already exist; the work is the at-scale viewer affordances.

---

## 7. Data preview + Report / Visualization / Dashboard builder (net-new)

New **Report** page model + composer:

```
┌ Report: "Exec Revenue" ───────────── Renderer[ApexCharts▾] [+Dataset][Save]┐
│ DATA            │  CANVAS (drag tiles)               │  TILE INSPECTOR     │
│ ▾ Sales      ⌕  │  ┌──────────┐ ┌────────────────┐   │  Chart [Bar ▾]      │
│   • amount      │  │ KPI $4.2M│ │  ▆▆▅▇  Bar      │   │  X [month ▾]        │
│ ▾ NetRevenue ⌕  │  └──────────┘ └────────────────┘   │  Y [amount ▾]       │
│   • net_revenue │  ┌───────────────────────────────┐ │  Series [region]    │
│ + join dataset… │  │ ▦ Grid (virtualized, paged)    │ │  Filter [+]         │
│                 │  └───────────────────────────────┘ │  Drill → Trace      │
└─────────────────┴──────────────────────────────────────┴────────────────────┘
```

- **Preview** uses the existing dataset-query/data-preview path (server-side paged + filtered grid).
- **Visualization** = chart tiles bound to dataset columns; **tie datasets together** via report-level joins.
- **Dashboard** = a saved arrangement of tiles across one or more datasets.
- Each tile can **Trace** to lineage — closing the loop from "what does this number show" to "how was it built."

---

## 8. Charts as pluggable renderers (showcase all four)

The chart library is **strictly a rendering engine** behind the seam — never tied to the logic. reference-ui **ships all four** with a runtime renderer dropdown (interoperability is a reference-app showcase feature).

```
IChartModel : IComponentModel              (UI.Abstractions — zero lib refs)
  ChartType   → ChartTypes TypeCollection (Bar/Line/Area/Pie/KPI/Heatmap/Geo/Sankey…)
                each IChartType: displayName, category, icon, required/optional encodings
  DataSource  → dataset ref + query (filters, aggregation, paging)
  Encodings   → X / Y / Series / Color / Size  bound to dataset fields
  Axes/Legend/Interactions + RendererHints (escape-bag for lib-specific extras)

ServiceTypeCollection<IChartRenderer>      (the renderer registry — FDW plugin model)
  IChartRenderer.RenderChart(IChartModel, IRenderContext) → RenderResult
  + capability flags (SupportsGeo, SupportsLargeSeries, SupportsCrossfilter…)
  + SupportedChartTypes  (which ChartTypes this renderer can draw)

  [ServiceTypeOption] ApexChartsRenderer   (Blazor — polished default)
  [ServiceTypeOption] EChartsRenderer      (Blazor — large data / exotic)
  [ServiceTypeOption] RadzenChartRenderer  (Blazor — zero-JS)
  [ServiceTypeOption] SyncfusionRenderer   (Blazor — max breadth)
  [ServiceTypeOption] SpectreChartRenderer (TUI — bar/breakdown)
```

**UI: two coupled, data-driven pickers**

```
Renderer   [ ApexCharts ▾ ]   ← OptionPicker over ServiceTypeCollection<IChartRenderer>
Chart type [ Bar ▾ ]          ← OptionPicker over ChartTypes, FILTERED to
                                 selectedRenderer.SupportedChartTypes
```

- Picking a renderer **re-filters the chart-type dropdown** to that renderer's `SupportedChartTypes` (ECharts → Sankey/Geo appear; Radzen → core set). Derived from the renderer, never coded into the UI.
- **Per-tile renderer selection** → drop two tiles on the same dataset, render one with ApexCharts and one with ECharts, side-by-side interop demo. The `IChartModel` is identical; only the renderer differs.
- **Graceful switch:** keep current chart type if still supported; else auto-select nearest + flag.
- **Library choices:** ApexCharts (MIT, default), Radzen (MIT, zero-JS), ECharts (Apache-2.0, large-data/exotic), Syncfusion (Community license, max breadth). All no-npm (NuGet static assets / referenced JS). The neutral model covers the common ~90%; lib-specific exotica via `RendererHints` or capability-gated `ChartType`s.

---

## 9. Verified defects folded into the build

- `configuration/types?category=` filters by **physical schema name** (conn/data/pipe), not `ServiceCategory` → empty dropdowns/palette (only Transform matches). Fix metadata-driven at the endpoint/lookup.
- `CalculatedDataSetProvider` injects server `ICalculationEntityService` → 500. Fix: use `ICalculationApiClient` (UI never opens ConfigurationDb).
- Health `IHealthState` DTO not deserializable → add `HealthStateJsonConverter` (mirror `PipelineStatusJsonConverter`); resolve via `HealthStates.ByName` (NotFound sentinel, no fallback).
- Pipeline "designer" is a `FileSystemDesignerPipelineStore` divorced from real pipelines → 404 (the "white `<`"). Fix via the Canvas writing real `PipelineConfiguration`.
- reference-api `/connect/token` is still a minimal-API `ConnectTokenHandler`; FDW's `ConnectTokenEndpoint` is now FastEndpoints → delete the handler, use the FDW endpoint.

---

## 10. Build plan (ordering, not estimates)

1. **Foundations & fixes** — searchable/virtualized `OptionPicker` + `EntityPicker`; wire validation into generated forms; fix config-types, calc-500, health-converter; rebuild calc builder; (reference-api) auth `/connect/token` FastEndpoints cutover.
2. **IA shell** — intent-based nav from `NavDescriptor` + command palette + role-gating; server-side search/paging on list endpoints.
3. **Canvas — `ICanvasModel` + `ICanvasRenderer` registry (§4a)** — one reusable graph for pipelines/lineage/calculations; ship Blazor.Diagrams + Cytoscape + SVG renderers + dropdown; workbench authoring over real config; inline Schedule/Run; designer→real-config.
4. **Explore** — catalog at scale + lineage viewer affordances (search/filter/focus/field-level).
5. **Visualize** — `Report`/`IChartModel`/`ChartTypes` + `IChartRenderer` registry + the 4 renderers + dropdown + preview grid + dashboards.
6. **Render-agnostic completeness** (parallel/optional) — Spectre coverage for new models; formal `ServiceTypeCollection<IUIRenderer>`.

---

## 11. Reuse vs build

| Reuse as-is | Extend | Build new |
|---|---|---|
| `IUIRenderer` seam, `IComponentModel`, Browse/List/Dashboard/Wizard page models, domain providers (Blazor-free), `PageTypes`/`NavDescriptor`, metadata form gen, `IEntityValidator<T>`, transform-builder pattern, lineage graph API | `OptionPicker`→searchable+virtualized; list endpoints→search/page/sort; nav→intent sections + ⌘K; validation→wired into forms; lineage→search/filter/focus/field-level | **Canvas** page model + renderer (workbench+lineage); **Report**/`IChartModel`/`ChartTypes` + `IChartRenderer` registry + 4 chart renderers + dropdown; **EntityPicker**; calc-builder rebuild; designer→real-config; the §9 fixes + auth cutover |
