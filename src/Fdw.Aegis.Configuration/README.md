# Fdw.Aegis.Configuration

Aegis configuration types.

A TypeCollection and its options — an extensible enum. Members are discovered at compile time and reachable by `ByName` / `ById` / `All`; a lookup that finds nothing returns a `NotFound` sentinel rather than null, so a caller never has to null-check a lookup.

A module initializer collects every `[TypeOption]` in every referenced assembly and dedupes them into the collection at load. Adding a member is a package reference plus a declaration — the collection itself is never edited.

## Collections

| Type | Kind | Purpose |
|---|---|---|
| `ApprovalPolicyTypes` | class | Registry of Aegis command approval-policy kinds (PreApproved/AdHoc). Pure type collection with no DI… |

## Options (2 declared)

| Type | Kind | Purpose |
|---|---|---|
| `AdHocApprovalPolicyType` | class | The ad-hoc policy kind: every invocation requires a fresh verdict rather than a standing pre-approval. |
| `PreApprovedApprovalPolicyType` | class | The pre-approved policy kind: the command's parameter allow-list is the whole approval contract — no… |

## Installation

```bash
dotnet add package Fdw.Aegis.Configuration --prerelease
```

## Dependencies

`Fdw.Aegis.Abstractions` · `Fdw.Collections` · `Fdw.Configuration` · `Fdw.Configuration.Abstractions` · `Fdw.Data.Abstractions` · `Fdw.Types.Abstractions`

Build-time only (generators and analyzers, not runtime dependencies): `Fdw.Collections.SourceGenerators` · `Fdw.Configuration.SourceGenerators` · `Fdw.Data.SourceGenerators`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
