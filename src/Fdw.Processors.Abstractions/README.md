# Fdw.Processors.Abstractions

Contracts for stateless command processors.

Implementations live in the sibling packages; this one holds the interfaces, base types and models they agree on, so a consumer can depend on the contract without taking the implementation.

## Interfaces (2)

| Type | Kind | Purpose |
|---|---|---|
| `IAsyncProcessor<TCommand, TContext>` | interface | Core interface for asynchronous command processors. Use when processing requires async operations (token… |
| `IProcessor<TCommand, TContext>` | interface | Core interface for synchronous command processors. Processors transform a command using the provided… |

## Base types (3)

| Type | Kind | Purpose |
|---|---|---|
| `AsyncProcessorBase<TCommand, TContext, TBase>` | class | Base class for asynchronous processors using the CRTP pattern. |
| `ProcessorBase<TCommand, TContext, TBase>` | class | Base class for synchronous processors using the CRTP pattern. |
| `ProcessorCollectionBase<TBase, TInterface>` | class | Base class for processor TypeCollections. Use with the [TypeCollection] attribute for source generation. |

## Models and supporting types (1)

| Type | Kind | Purpose |
|---|---|---|
| `ProcessorChain<TCommand>` | class | Composes multiple processors into a chainable pipeline. Supports Railway-Oriented error handling - stops… |

## Installation

```bash
dotnet add package Fdw.Processors.Abstractions --prerelease
```

## Dependencies

`Fdw.Collections` · `Fdw.Results`

---

Part of **[FractalDataWorks](https://github.com/CyberdyneDevelopment/FractalDataWorks)** `1.0.0-rc.1`. Licensed under Apache-2.0.

<!-- generated from source at 4d75c3fab on 2026-08-04; regenerate rather than hand-edit -->
