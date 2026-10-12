# Fdw.Services.Quality

Data quality: rules, their evaluation, and what a failed rule blocks.

Registration lives in the option, not in a host's `Program.cs`. Each option carries three replaceable phase bodies — **Configure**, **Register**, **Initialize** — where the first two take and return the `IHostApplicationBuilder` and the third takes and returns the `IServiceProvider`. Referencing this package is what enlists its options: a module initializer collects and dedupes them at assembly load.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `QualityServiceTypes` | class | ServiceTypeCollection for quality domain service types. |

## Options (1 declared)

| Type | Kind | Purpose |
|---|---|---|
| `DefaultQualityServiceType` | class | Default quality service type that registers quality, catalog, and promotion services with the dependency… |

Shipped options are reference implementations, not canon — a consumer adds a kind by declaring its own option against this collection, in its own assembly.

## Configuration

Configuration classes are `[ManagedConfiguration]`: they generate their own DDL, validation and UI form metadata, and are read back as rows rather than from JSON.

| Type | Kind | Purpose |
|---|---|---|
| `DataSetAnnotationConfiguration` | class | Configuration for DataSet metadata annotations. Stored in catalog.DataSetAnnotation table. |
| `DataSetAnnotationFieldBusinessNameConfiguration` | class | Configuration for a data set annotation field business name. Child of DataSetAnnotationConfiguration. |
| `DataSetAnnotationFieldDescriptionConfiguration` | class | Configuration for a data set annotation field description. Child of DataSetAnnotationConfiguration. |
| `DataSetAnnotationTagConfiguration` | class | Configuration for a data set annotation tag. Child of DataSetAnnotationConfiguration. |
| `EnvironmentApproverConfiguration` | class | Configuration for an individual environment approver. Child of EnvironmentConfiguration. |
| `EnvironmentConfiguration` | class | Configuration for deployment environments. Stored in quality.Environment table. |
| `GlossaryTermConfiguration` | class | Configuration for business glossary terms. Stored in catalog.GlossaryTerm table. |
| `GlossaryTermLinkedDataSetConfiguration` | class | Configuration for a glossary term linked data set. Child of GlossaryTermConfiguration. |
| `GlossaryTermRelationConfiguration` | class | Configuration for a glossary term relation. Child of GlossaryTermConfiguration. |
| `PromotionRequestConfiguration` | class | Configuration for promotion requests between environments. Stored in quality.PromotionRequest table. |
| `PromotionRequestItemConfiguration` | class | Configuration for an individual promotion request item. Child of PromotionRequestConfiguration. |
| `QualityRuleConfiguration` | class | Configuration for quality validation rules. Stored in quality.QualityRule table. |
| `QualityRuleReferenceValueConfiguration` | class | Configuration for an individual quality rule reference value. Child of QualityRuleConfiguration. |

## Installation

```bash
dotnet add package Fdw.Services.Quality --prerelease
```

## Dependencies

`Fdw.Configuration` · `Fdw.Data.Abstractions` · `Fdw.MessageLogging.Abstractions` · `Fdw.Results` · `Fdw.Results.Abstractions` · `Fdw.Services` · `Fdw.Services.Abstractions` · `Fdw.Services.Quality.Abstractions` · `Fdw.Validation`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators` · `Fdw.MessageLogging.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
