# Connectors

The **connectors** slice provides a uniform adapter family for reading from and writing to
heterogeneous sources — files, HTTP endpoints, Roslyn symbols — behind a single registry. Connectors are the runtime objects that ETL pipelines and
ad-hoc tooling resolve when they need to talk to a non-database source.

## Object model

| Type | Package | Role |
|---|---|---|
| `IConnector` | `Fdw.Connectors.Abstractions` | Non-generic marker; carries `Name`. |
| `IConnectorLocation` | `Fdw.Connectors.Abstractions` | An addressable target inside a connector (e.g. a file path, an HTTP URI). |
| `ISourceConnector<T>` / `ITargetConnector<T>` / `IRequestConnector<TReq, TResp>` | Abstractions | Typed access — read, write, request/response. |
| `IConnectorType` | `Fdw.Connectors` | TypeOption metadata (display name, factory wiring). |
| `IConnectorProvider` | `Fdw.Connectors` | Runtime registry — `GetByName(string) → IConnector`. |
| `IConnectorFactory` | `Fdw.Connectors` | Per-type factory that builds connector instances from configuration. |
| `ConnectorConfiguration` | `Fdw.Connectors` | `[ManagedConfiguration]` base for connector configs (ServiceCategory = "Connector"). |
| `ConnectorTypes` | `Fdw.Connectors` | `[MutableTypeCollection]` registry of all connector types. |

## Reference connector types (slice 2)

| Type | Package | Purpose |
|---|---|---|
| `FileTextConnectorType` | `Fdw.Connectors.LocalFile` | Read/write text files from a local DataPath. |
| `HttpTextConnectorType` | `Fdw.Connectors.Http` | GET/POST text bodies against an HTTP endpoint. |
| `RoslynSymbolTextConnectorType` | `Fdw.Connectors.RoslynSymbol` | Read source from Roslyn symbol references — used by source-generator-driven pipelines. |

Each ships its own `*ConnectorConfiguration` (a `[ManagedConfiguration]` partial), a
factory, the connector implementation, a `*ConnectorLocation`, and result codes.

## DataStore-backed addressing

Connectors do **not** store raw paths or URIs in configuration. They store the **DataStore
name + DataPath name** they resolve addresses against. The `FileTypeHandlers` TypeCollection
governs which file extensions are permitted on a given DataPath via `DataPathPolicy`. The
older `AllowedExtensions` property on file connectors is gone.

This makes LocalFs (and any other source) a first-class **DataStore** — connectors read paths
out of the same DataStore tree the gateway uses for database paths.

## Hosting

`ConnectorTypes` follows the standard three-phase ServiceTypeCollection pattern. Adding a connector implementation package (e.g. `Fdw.Connectors.Http`) is the registration intent — the `[ServiceTypeOption(typeof(ConnectorTypes), "...")]` is wired into DI by `Registration.SourceGenerators`.

```csharp
// Phase 1 — Configure + Register (before builder.Build())
ConnectorTypes.Configure(builder, loggerFactory);
ConnectorTypes.RegisterAdditionalServices(builder.Services);
ConnectorTypes.Register(builder.Services, loggerFactory);

var app = builder.Build();

// Phase 3 — Initialize (after Build)
ConnectorTypes.Initialize(app.Services, loggerFactory);
```

`Configure` binds the per-type IOptions sections (`Connectors:HttpText`, `Connectors:LocalFileText`, `Connectors:RoslynSymbolText`, ...). `Register` registers `DefaultConnectorProvider` as `IConnectorProvider` along with each type's factory. `Initialize` builds the connector instances from the bound options and populates the provider.

## Resolving a connector at runtime

```csharp
public sealed class IngestEndpoint
{
    private readonly IConnectorProvider _connectors;

    public IngestEndpoint(IConnectorProvider connectors) => _connectors = connectors;

    public async Task<IGenericResult<string>> ReadAsync(string connectorName, CancellationToken ct)
    {
        var connector = _connectors.GetByName(connectorName);
        if (connector is not ISourceConnector<string> source)
            return GenericResult.Failure<string>(ConnectorResultCodes.WrongShape);

        return await source.ReadAsync(location, ct).ConfigureAwait(false);
    }
}
```

## Pipelines integration

`Fdw.Services.Etl` consumes connectors via `IConnectorSourcePipelineFactory`,
which builds a pipeline step that pulls from any `ISourceConnector<T>`. This is the entry
point used by the reference-etl sample pipelines.

## Related

- [JSON-Driven Configuration Startup](03-05-JSON-Driven-Configuration.md)
- [Polymorphic Configuration Pattern](03-07-Polymorphic-Configuration-Pattern.md)
- [DataGateway Pattern](05-01-DataGateway-Pattern.md)
- [Building an ETL Server](14-02-Building-An-ETL-Server.md)
