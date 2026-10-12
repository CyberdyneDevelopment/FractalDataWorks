# Fdw.Data.RowSources.FixedWidth.Abstractions

Contracts for fixed-width row sources — the column layout a reader is driven by.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Models and supporting types (5)

| Type | Kind | Purpose |
|---|---|---|
| `FixedWidthField` | class | Defines one fixed-width field: its name, position, width, and padding rules. Built at runtime from the… |
| `FixedWidthRowSourceOptions` | class | Options for fixed-width (fixed-length) row reading. The per-field offsets/widths drive RecordParser's… |
| `FixedWidthRowWriterOptions` | class | Options for fixed-width (fixed-length) row writing. The write-side mirror of . The per-field… |
| `FixedWidthStreamRowSource` | class | Streaming fixed-width (fixed-length) row source backed by RecordParser's fixed-length raw reader.… |
| `FixedWidthStreamRowWriter` | class | Writes flat name→value rows as fixed-width (fixed-length) text using RecordParser's driver. The… |

## Installation

```bash
dotnet add package Fdw.Data.RowSources.FixedWidth.Abstractions --prerelease
```

## Dependencies

`Fdw.Data.RowSources.Abstractions`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
