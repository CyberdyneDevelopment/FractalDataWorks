# Transformations Service Domain

The Transformations domain applies data transformations — calculations, aggregations, pivots, and lookups — using the standard service domain pattern. Each transformation type is a `ServiceTypeOption` in the `TransformationTypes` collection, dispatched by the `TransformationEngine`.

## Service Type Options

| Name | Package | Purpose |
|------|---------|---------|
| `Calculation` | `.Transformations.Calculation` | Computed columns, formula evaluation |
| `Aggregation` | `.Transformations.Aggregation` | Group-by, sum, count, min/max |
| `Pivot` | `.Transformations.Pivot` | Row-to-column pivot/unpivot |
| `Lookup` | `.Transformations.Lookup` | Key-based value enrichment |

`DataCleaning` (`.Transformations.DataCleaning`) is the reference pattern for adding new types.

## Package Structure

```
Services.Transformations.Abstractions/   # Interfaces, base classes
Services.Transformations/               # TransformationTypes, TransformationEngine, commands
Services.Transformations.Calculation/   # CalculationTransformationType + factory
Services.Transformations.Aggregation/   # AggregationTransformationType + factory
Services.Transformations.Pivot/         # PivotTransformationType + factory
Services.Transformations.Lookup/        # LookupTransformationType + factory
```

## Registration

Each transformation implementation package contains a `[ServiceTypeOption(typeof(TransformationTypes), "<name>")]` class. With `Fdw.Registration.SourceGenerators` in the entry-point app, the emitted `[ModuleInitializer]` registers every referenced `[ServiceTypeOption]` at assembly load — **adding the package reference IS the registration intent**.

`TransformationTypes` is an ordinary `[ServiceTypeCollection]`, so its three-phase methods run inside
the single `PlatformServices.Configure`/`Register`/`Initialize` sweep — there is no
hand-written `TransformationTypes.Configure(...)` call in `Program.cs`:

```csharp
// Phase 1 — Configure + Register (before builder.Build()) — one sweep, all domains.
PlatformServices.Configure(builder, loggerFactory);
PlatformServices.Register(builder.Services, loggerFactory);

var app = builder.Build();

// Phase 3 — Initialize (after Build) — one sweep, dependency-safe Group order.
PlatformServices.Initialize(app.Services, loggerFactory);
```

See [20-02 Service Startup Order](20-02-Service-Startup-Order.md) for the full Program.cs shape across all service domains.

## Configuration

Each type reads from `Transformations:{TypeName}` in appsettings:

```json
{
  "Transformations": {
    "Calculation": [
      {
        "Name": "DerivedRevenue",
        "IsEnabled": true
      }
    ],
    "Aggregation": [
      {
        "Name": "MonthlySummary",
        "IsEnabled": true
      }
    ]
  }
}
```

## Engine Dispatch Pattern

The `TransformationEngine` dispatches requests by `TransformationCategory`. Use `TransformationsCommands` to build requests — the category discriminator is set automatically:

```csharp
// Create a typed request
var request = TransformationsCommands.Calculation(
    inputData: myDataset,
    configurationName: "DerivedRevenue");

// Engine looks up TransformationTypes.ByName(request.TransformationCategory)
// and dispatches to the matching factory
var result = await engine.Transform<MyOutput>(request, context, ct);
```

## TransformationsCommands

Factory methods for each transformation type:

```csharp
// Calculation — formula/derived column evaluation
var req = TransformationsCommands.Calculation(data, "DerivedRevenue");

// Aggregation — group-by summary
var req = TransformationsCommands.Aggregation(data, "MonthlySummary");

// Pivot — rows to columns
var req = TransformationsCommands.Pivot(data, "SalesByRegion");

// Lookup — key-based enrichment
var req = TransformationsCommands.Lookup(data, "ProductCatalog");

// DataCleaning — null handling, trimming, normalization
var req = TransformationsCommands.DataCleaning(data, "StandardizePipeline");
```

All commands share the same signature:
```csharp
TransformationsCommands.{Type}(
    inputData,
    configurationName,
    inputType = "object",   // MIME type hint
    outputType = "object",
    timeout = null)
```

## ITransformationsService

Services implement `ITransformationsService` for command execution:

```csharp
// Command-pattern execution
var result = await transformationsService.Execute<MyOutput>(command, ct);

// Direct transform
var result = await transformationsService.Transform<MyOutput>(request, context, ct);

// Metrics
var metrics = await transformationsService.GetTransformationMetrics(ct);
```

## Adding a New Transformation Type

1. Create a `ServiceTypeOption` class extending `TransformationTypeBase<,,>`:

```csharp
[ServiceTypeOption(typeof(TransformationTypes), "MyCustom")]
public sealed class MyCustomTransformationType
    : TransformationTypeBase<IGenericTransformation, IMyCustomFactory, MyCustomConfiguration>
{
    public MyCustomTransformationType() : base(
        name: "MyCustom",
        inputType: typeof(object),
        outputType: typeof(object),
        supportsStreaming: false,
        supportedContainers: Array.Empty<IDataContainerType>(),
        category: "MyCustom")
    { }

    public override IServiceCollection RegisterRequiredServices(IServiceCollection services)
    {
        services.AddSingleton<IMyCustomFactory, MyCustomFactory>();
        return services;
    }

    public override void RegisterFactory(IFdwServiceProvider<IGenericTransformation, ITransformationConfiguration> provider, IServiceProvider services)
    {
        var factory = services.GetRequiredService<IMyCustomFactory>();
        provider.Register(Name, factory);

        // Configuration is provided by the domain's DefaultConfigurationProvider<T>
        // which holds the per-domain ConfigurationProvider cache internally
    }

    public override void Configure(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<List<MyCustomConfiguration>>(
            configuration.GetSection("Transformations:MyCustom"));
    }
}
```

2. Add a factory command to `TransformationsCommands` and a `[ManagedConfiguration]` class.

The source generator discovers the `[ServiceTypeOption]` attribute and registers it in `TransformationTypes` automatically.

## See Also

- [Service Domains Overview](06-01-Service-Domains-Overview.md)
- [Creating a Service Domain](06-02-Creating-Service-Domain.md)
- [Connections Service Domain](06-03-Connections-Service-Domain.md) - reference pattern
