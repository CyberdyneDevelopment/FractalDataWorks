# Naming Conventions

FractalDataWorks follows consistent naming conventions across projects and namespaces.

## Project Naming

| Pattern | Example |
|---------|---------|
| `{Solution}.{Concept}` | `Reference.TypeCollections` |
| `{Solution}.{Layer}` | `Reference.Domain` |
| `FractalDataWorks.{Feature}` | `FractalDataWorks.Collections` |
| `FractalDataWorks.{Feature}.Abstractions` | `FractalDataWorks.Commands.Abstractions` |

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
- `FractalDataWorks.Collections.csproj` -> `namespace FractalDataWorks.Collections`

## Folder-Based Namespaces

Subfolders add to the namespace. From [`FractalDataWorks.Commands.Abstractions`](../src/FractalDataWorks.Commands.Abstractions/):

```
FractalDataWorks.Commands.Abstractions/
|-- Logging/           -> FractalDataWorks.Commands.Abstractions.Logging
|-- Messages/          -> FractalDataWorks.Commands.Abstractions.Messages
|-- Clauses/           -> FractalDataWorks.Commands.Abstractions.Clauses
```

## File Naming

| Type | Convention | Actual Example |
|------|------------|----------------|
| Logger | `{Domain}Logger.cs` | [`CommandLogger.cs`](../src/FractalDataWorks.Commands.Abstractions/Logging/CommandLogger.cs) |
| TypeCollection | `{Type}s.cs` | [`PaymentMethods.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/PaymentMethods.cs) |
| TypeOption | `{Name}{Type}.cs` | [`CreditCardPayment.cs`](../samples/ReferenceSolution/concepts/01-type-collections/src/Reference.TypeCollections/Basic/Options/CreditCardPayment.cs) |
| Endpoint | `{Type}Endpoint.cs` | [`QueryEndpoint.cs`](../src/FractalDataWorks.Web.RestEndpoints/Base/QueryEndpoint.cs) |
| Validator | `{Type}Validator.cs` | [`MsSqlConfigurationValidator.cs`](../src/FractalDataWorks.Services.Connections.MsSql/MsSqlConfigurationValidator.cs) |

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

Logger classes reside in a `Logging/` subfolder within their domain package:

```
FractalDataWorks.Commands.Abstractions/
|-- Logging/
    |-- CommandLogger.cs
```

## Next Steps

Return to [Home](../Home.md) to continue with more advanced topics.
