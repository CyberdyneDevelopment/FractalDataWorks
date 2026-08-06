# Naming Conventions

FractalDataWorks follows consistent naming conventions across projects and namespaces.

## Project Naming

| Pattern | Example |
|---------|---------|
| `{Solution}.{Concept}` | `Reference.TypeCollections` |
| `{Solution}.{Layer}` | `Reference.Domain` |
| `FractalDataWorks.{Feature}` | `Fdw.Collections` |
| `FractalDataWorks.{Feature}.Abstractions` | `Fdw.Commands.Abstractions` |

From Reference Solution ([`concepts/`](../samples/ReferenceSolution/concepts/)):
- `Reference.TypeCollections` - TypeCollection demonstrations
- `Reference.ServiceTypes` - ServiceType demonstrations
- `Reference.MessageLogging` - Logging demonstrations

## Namespace Conventions

Namespaces match project names by default:

```xml
<RootNamespace>$(MSBuildProjectName)</RootNamespace>
```

This means:
- `Reference.TypeCollections.csproj` -> `namespace Reference.TypeCollections`
- `Fdw.Collections.csproj` -> `namespace Fdw.Collections`

## Folder-Based Namespaces

Subfolders add to the namespace. From [`Fdw.Commands.Abstractions`](../src/Fdw.Commands.Abstractions/):

```
Fdw.Commands.Abstractions/
|-- Logging/           -> Fdw.Commands.Abstractions.Logging
|-- Messages/          -> Fdw.Commands.Abstractions.Messages
|-- Clauses/           -> Fdw.Commands.Abstractions.Clauses
```

## File Naming

| Type | Convention | Actual Example |
|------|------------|----------------|
| MessageLogging | `{Domain}Log.cs` | [`EtlLog.cs`](../src/Fdw.Services.Etl/Logging/EtlLog.cs) |
| LoggerMessage | `{Domain}Logger.cs` | [`CommandLogger.cs`](../src/Fdw.Commands.Abstractions/Logging/CommandLogger.cs) |
| TypeCollection | `{Type}s.cs` | [`PaymentMethods.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs) |
| TypeOption | `{Name}{Type}.cs` | [`CreditCardPayment.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CreditCardPayment.cs) |
| Endpoint | `{Type}Endpoint.cs` (or `{Type}EndpointBase.cs` for generic bases) | [`ListConnectionsEndpointBase.cs`](../src/Fdw.Services.Connections.Endpoints/ListConnectionsEndpointBase.cs) |
| Configuration | `{Type}Configuration.cs` | [`MsSqlConnectionConfiguration.cs`](../src/Fdw.Services.Connections.MsSql/MsSqlConnectionConfiguration.cs) |

## Class Naming

| Pattern | Convention | Actual Example |
|---------|------------|----------------|
| Base classes | `{Type}Base` | [`PaymentMethodBase`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethodBase.cs) |
| Collections | `{Type}s` | [`PaymentMethods`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs) |
| Interfaces | `I{Type}` | [`IPaymentMethod`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/IPaymentMethod.cs) |
| Options | `{Name}{Type}` | [`CreditCardPayment`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CreditCardPayment.cs) |

## TypeCollection Component Names

TypeCollections follow a specific four-component pattern. From [`PaymentMethods`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/):

| Component | Naming | File |
|-----------|--------|------|
| Interface | `I{Type}` | `IPaymentMethod.cs` |
| Base class | `{Type}Base` | `PaymentMethodBase.cs` |
| Collection | `{Type}s` | `PaymentMethods.cs` |
| Option | `{Name}{Type}` | `CreditCardPayment.cs` |

## Logger Class Location

Logger classes reside in a `Logging/` subfolder within their domain package. Two naming patterns exist:

- **`{Domain}Log.cs`** -- Uses `[MessageLogging]` attribute, returns `IGenericMessage` (preferred for new code)
- **`{Domain}Logger.cs`** -- Uses `[LoggerMessage]` attribute, returns `void` (used in projects without MessageLogging reference)

```
Fdw.Services.Etl/
|-- Logging/
    |-- EtlLog.cs              <-- [MessageLogging] pattern

Fdw.Commands.Abstractions/
|-- Logging/
    |-- CommandLogger.cs        <-- [LoggerMessage] pattern
```

## Next Steps

Return to [Home](Home.md) to continue with more advanced topics.
