# Fdw.UI.Abstractions

The render-agnostic UI contracts: the page and component model, `IUIRenderer`, and the navigation descriptors a renderer consumes.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (64)

| Type | Kind | Purpose |
|---|---|---|
| `IActivityItem` | interface | An activity item for the recent activity feed. |
| `IActivitySeverity` | interface | Interface for activity severity levels. |
| `IBreadcrumbItem` | interface | Represents a breadcrumb item for navigation context. |
| `IBrowseColumnModel` | interface | A single column in an . Carries its own items and selection state so each column can load independently… |
| `IBrowseItem` | interface | A single item in an — the thing the user can highlight and select. Items carry a name/label and an… |
| `IBrowsePageModel` | interface | Miller-column browse page — N linked columns where selecting an item in column i loads / populates… |
| `ICanvasEdge` | interface | A directed edge connecting two nodes in the canvas graph. |
| `ICanvasEdgeType` | interface | Interface for canvas edge type options. |
| `ICanvasEditContext` | interface | Edit-mode command surface for a canvas. |
| `ICanvasModel` | interface | The top-level model for a node-graph canvas page. |
| `ICanvasNode` | interface | A node in the canvas graph. |
| `ICanvasNodeType` | interface | Interface for canvas node type options. |
| `ICanvasPort` | interface | A connection point on a canvas node through which edges attach. |
| `ICanvasRenderer` | interface | Core contract for canvas renderers. |
| `ICanvasRendererType` | interface | Interface for canvas renderer type options. |
| `IChartDataSource` | interface | Identifies the data source for a chart tile and carries optional query parameters. |

## Base types (41)

| Type | Kind | Purpose |
|---|---|---|
| `ActivitySeverities` | class | TypeCollection for activity severity levels. |
| `ActivitySeverityBase` | class | Base class for activity severity levels. |
| `CanvasEdgeTypeBase` | class | Base class for canvas edge types using the CRTP pattern. |
| `CanvasEdgeTypes` | class | TypeCollection for canvas edge types. |
| `CanvasNodeTypeBase` | class | Base class for canvas node types using the CRTP pattern. |
| `CanvasNodeTypes` | class | TypeCollection for canvas node types. |
| `CanvasRendererTypeBase` | class | Base class for canvas renderer type options using the CRTP pattern. |
| `CanvasRendererTypes` | class | TypeCollection for canvas renderer types — the enumerable renderer registry. |
| `ChartEncodingRoleBase` | class | Base class for chart encoding role type options using the CRTP pattern. |
| `ChartEncodingRoles` | class | TypeCollection for chart encoding roles — the enumerable registry of data-binding channels. |

## Models and supporting types (106)

| Type | Kind | Purpose |
|---|---|---|
| `AccordionDisplayMode` | class | Accordion/collapsible panels. |
| `AreaChartType` | class | Filled area chart — like a line chart but with the region below the line filled. |
| `BarChartType` | class | Vertical or horizontal bar chart for comparing values across categories. |
| `BlazorUIRendererType` | class | UI renderer type for Blazor rendering. |
| `BothRenderMode` | class | Both view and edit side-by-side. |
| `CalcInputNodeType` | class | A calculation graph input parameter node that receives a value fed into the calculation. |
| `CalcOperationNodeType` | class | A calculation graph operation step node that performs a computation on its inputs. |
| `CalcOutputNodeType` | class | A calculation graph output result node that captures the final computed value. |
| `CalculationNodeType` | class | A calculation chain node that groups a set of calculation operations. |
| `ChartEncoding` | class | Binds a single data field to a chart encoding role. |
| `CollectionDisplayModes` | class | TypeCollection for collection display modes. |
| `ColorEncodingRole` | class | Colour-coding channel — maps a field value to a colour scale or palette. |

## Installation

```bash
dotnet add package Fdw.UI.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
