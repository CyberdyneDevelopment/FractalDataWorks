# Fdw.Web.Analytics.Components

Headless components for analytics and promotions.

Headless Blazor components for this domain — 18 `.razor` component(s) plus their supporting types.

Components are headless: a provider owns the state and the calls, a context carries them, and the consumer supplies the markup through a `RenderFragment<TContext>`. Nothing here renders a fixed skin, so a host can present the same component in any visual language.

## Context, provider and log

| Type | Kind | Purpose |
|---|---|---|
| `AnalyticsDashboardContext` | class | Immutable context for the analytics dashboard widget. |
| `GaugeContext` | class | Immutable context for the Gauge headless provider. |
| `GaugeProviderLog` | class | MessageLogging methods for GaugeProvider operations. EventId range: 8900-8909 |
| `HealthDashboardContext` | class | Immutable context for the Health Dashboard headless provider. Aggregates system health and per-service… |
| `HealthDashboardProviderLog` | class | MessageLogging methods for HealthDashboardProvider operations. EventId range: 8930-8939 |
| `PromotionContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries both state snapshots and… |
| `PromotionProviderLog` | class | MessageLogging for PromotionProvider operations. EventId range: 4430-4444 |
| `PromotionReviewContext` | class | Immutable context object passed to the consumer RenderFragment by . Carries a single promotion request… |
| `PromotionReviewProviderLog` | class | MessageLogging for PromotionReviewProvider operations. EventId range: 4450-4464 |
| `SparklineContext` | class | Immutable context for the Sparkline headless provider. |
| `SparklineProviderLog` | class | MessageLogging methods for SparklineProvider operations. EventId range: 8910-8919 |
| `ThroughputContext` | class | Immutable context for the Throughput headless provider. Combines gauge and sparkline data for throughput… |
| `ThroughputProviderLog` | class | MessageLogging methods for ThroughputProvider operations. EventId range: 8920-8929 |
| `AnalyticsContext` | class | — |

## Installation

```bash
dotnet add package Fdw.Web.Analytics.Components --prerelease
```

## Dependencies

`Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Services.Abstractions` · `Fdw.UI.Components` · `Fdw.UI.Themes.Components` · `Fdw.Web.Analytics.Clients`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
