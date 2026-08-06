# Fdw.Data.RowSources.Delimited

A row source for delimited text — CSV and its relatives.

## Options (2)

| Type | Kind | Purpose |
|---|---|---|
| `DelimitedRowSourceType` | class | TypeOption for delimited (CSV / variable-length) stream row sources, backed by RecordParser. |
| `DelimitedRowWriterType` | class | TypeOption for delimited (CSV / variable-length) row writers, backed by RecordParser. The write-side… |

## Types (2)

| Type | Kind | Purpose |
|---|---|---|
| `DelimitedStreamRowSource` | class | Streaming delimited (CSV / variable-length) row source backed by RecordParser's raw reader. Produces… |
| `DelimitedStreamRowWriter` | class | Writes flat name→value rows as delimited (CSV / variable-length) text using RecordParser's driver. The… |

## Installation

```bash
dotnet add package Fdw.Data.RowSources.Delimited --prerelease
```

## Dependencies

`Fdw.Data.RowSources` · `Fdw.Data.RowSources.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
