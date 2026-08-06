# DataNode Core Split (FDW-572)

The DataStore object model — the `[ManagedConfiguration]` POCOs and the navigable `IDataNode`
tree that `IDataGateway` routes against — was extracted out of the server-side
`Fdw.Services.Connections` / `Fdw.Services.Data` cores into two **connection-agnostic** packages.
The goal: the thin UI can build the exact same `IDataStore` → `IDataPath` → `IDataContainer` →
`IDataField` trees from API-fetched configs **without referencing any server core, connection
factory, or `IConfigurationGateway`**.

## The two new packages

| Package | Contents | Depends on |
|---|---|---|
| `Fdw.Data.Configuration` | The 8 `[ManagedConfiguration]` DataStore-family POCOs: `DataStoreConfiguration`, `DataPathConfiguration`, `DataContainerConfiguration`, `DataContainerFieldConfiguration`, `DataContainerKeyConfiguration`, `DataContainerKeyFieldConfiguration`, `DataPathPolicyConfiguration`, `FileTypeHandlerOverrideConfiguration`. | Configuration abstractions only — no connection/gateway deps |
| `Fdw.Data.DataNodes` | The concrete nodes (`DataStore`, `DataPath`, `DataField`, `ContainerComposition`, `ContainerKey`, `ContainerKeyField`), the builders (`DataStoreBuilderBase`, `GenericDataStoreBuilder`, `GenericContainerPath`), `IDataStoreBuilderSelector`, and the pure `ConfiguredDataStoreProvider`. | `Fdw.Data.Configuration`, `Fdw.Data.Abstractions` — **no** `IDataConnectionProvider`, **no** `IConfigurationGateway` |

> **Namespaces were kept.** The POCOs moved OUT of `Fdw.Services.Connections` but their namespace
> stayed `Fdw.Services.Connections`, so no consumer `using` changed. The move is a project-boundary
> change, not a rename.

## `ConfiguredDataStoreProvider` — the pure core

`ConfiguredDataStoreProvider : IDataStoreProvider` (in `Fdw.Data.DataNodes`) resolves a DataStore as
the canonical navigable `IDataNode` tree from **two injected dependencies only**:

```csharp
public ConfiguredDataStoreProvider(
    ILogger<ConfiguredDataStoreProvider>? logger,
    IServiceConfigurationProvider<DataStoreConfiguration> configurationProvider,  // config source
    IDataStoreBuilderSelector builderSelector);                                   // per-transport dispatch
```

It has **no** dependency on `IDataConnectionProvider` or `IConfigurationGateway` (both excluded from
`Fdw.Data.DataNodes`), so it never merges ConfigurationDb's own gateway-owned stores and never
resolves a connection directly. Config reads go through the abstract
`IServiceConfigurationProvider<DataStoreConfiguration>`; transport dispatch goes through
`IDataStoreBuilderSelector` — both supplied by the caller, which *can* reference the excluded
packages. Each store is built once by its selected `IDataStoreBuilder` from the cascaded config
(Paths → Containers → Fields); `Path(name)` / `Container(name)` dot-walk the built tree per the
[DataNode navigation contract](#see-also).

## `ConfigurationGatewayDataStoreProvider` — the server-side provider

`ConfigurationGatewayDataStoreProvider : IDataStoreProvider` (in `Fdw.Services.Data`) is the
server-side provider that replaced the old `DataStoreProvider`. It:

- carries the discovered three-phase `Configure` / `Register` / `Initialize` (marked
  `[PlatformServiceProvider(ServiceCategory = "DataStore", Group = 8)]` — see
  [12-01 Creating a Server](12-01-Creating-A-Server.md));
- keeps the `IConfigurationGateway.DataStores` shortcut so ConfigurationDb's own bounded schema tree
  (built from `configurationSchema.json`) is returned directly rather than recursing back into the
  gateway;
- **delegates** all non-gateway store composition and per-transport build to the pure
  `ConfiguredDataStoreProvider`, then **merges** ConfigurationDb's gateway-owned stores into the
  full-tree result so any endpoint targeting either set resolves;
- has **no** `IDataConnectionProvider` constructor dependency.

Its companion `DataStoreTypesBuilderSelector` (also in `Fdw.Services.Data`) implements
`IDataStoreBuilderSelector` by dispatching to the module-init-populated `DataStoreTypes` collection.

## Builders take the response format explicitly

`GenericDataStoreBuilder`'s constructor now takes an `IFormatType defaultResponseFormat` (no string
ctor):

```csharp
public GenericDataStoreBuilder(IFormatType defaultResponseFormat, ILogger? logger = null)
```

Each transport's `SupplyBuilder` passes `ConnectionTypes.ByName(...).DefaultResponseFormat`.
`ContainerComposition.ResolveFormat(cfg, IFormatType)` reads a container's own `Format`
discriminator, falling back to that supplied default — format is config-driven per container, not a
hardcoded string.

## UI side — same trees, zero server-core references

`Fdw.Data.Components` (the thin UI's data package) gained:

- `ClientsDataStoreConfigurationProvider` — an `IServiceConfigurationProvider<DataStoreConfiguration>`
  backed by API-fetched configs instead of a gateway;
- `GenericBuilderSelector` — an `IDataStoreBuilderSelector`;
- `DataStoreProviderClientType` — a `[ServiceTypeOption(ApiClientTypes)]`.

With those three, the UI constructs the identical `IDataNode` trees from configs it fetched over
HTTP — building on the same `Fdw.Data.DataNodes` core the server uses, with no reference to any
server core, connection factory, or `IConfigurationGateway`.

## See Also

- [05-01 DataGateway Pattern](05-01-DataGateway-Pattern.md) — how `IDataGateway` routes against the tree
- [DataNode navigation skill] — `Provider.Get(name) → Path(name) → Container(name) → Field(name)`, all `IGenericResult<T>`, fail-loud on miss
- [12-01 Creating a Server](12-01-Creating-A-Server.md) — the PlatformServices sweep that drives `ConfigurationGatewayDataStoreProvider`'s three phases
- [13-01 Headless UI Pattern](13-01-Headless-UI-Pattern.md) — the per-domain `*.Components` isolation the UI side observes
