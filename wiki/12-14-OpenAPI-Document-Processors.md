# OpenAPI Document Processors

FDW uses NSwag `IDocumentProcessor` implementations to enrich the OpenAPI spec at document generation time. These processors run after FastEndpoints registers all routes, modifying the spec before Scalar renders it.

**Stack:** FastEndpoints.Swagger (NSwag) + Scalar.AspNetCore

## ValuesFromSchemaDocumentProcessor

Resolves `[ValuesFrom]` attributes on configuration DTO properties and injects TypeCollection values as `enum` constraints in the OpenAPI schema. Scalar renders these as dropdown selectors.

### How it works

1. Iterates all `ConfigurationTypes.All()` (source-generated from `[ManagedConfiguration]` classes)
2. Reads each type's `ValuesFromReferences` (tracked by the configuration source generator)
3. Resolves the referenced TypeCollection via `TypeCollection.All()` reflection
4. Matches schema properties by name in the NSwag document
5. Injects TypeOption names as `Enumeration` values on the property schema

### Registration

```csharp
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.DocumentProcessors.Add(new ValuesFromSchemaDocumentProcessor());
    };
});
```

### What it covers

Any property decorated with `[ValuesFrom]` that appears in a request or response schema:

```csharp
// Type-safe reference (compile-time)
[ValuesFrom(typeof(ConnectionTypes))]
public string ServiceOptionType { get; set; } = string.Empty;

// String-based reference (cross-assembly, no direct dependency)
[ValuesFrom("CalculationTypes")]
public string CalculationType { get; set; } = string.Empty;

// Custom display property
[ValuesFrom(typeof(EnvironmentTypes), DisplayProperty = "DisplayName")]
public string EnvironmentType { get; set; } = string.Empty;
```

All three forms are resolved automatically. No per-endpoint configuration needed.

### Adding [ValuesFrom] to a new property

1. Add the attribute to the configuration property:
   ```csharp
   [ValuesFrom(typeof(MyTypes))]
   public string MyTypeName { get; set; } = string.Empty;
   ```
2. Ensure the TypeCollection is discoverable (loaded assembly at runtime)
3. The processor picks it up automatically on next Swagger document generation

## DataSetQueryDocumentProcessor

Enriches the DataSet query endpoint with per-dataset documentation. Requires deferred initialization because `IDataSetProvider` isn't available until after the Phase 3 `PlatformServices.Initialize(app.Services, loggerFactory)` sweep — which runs `DataSetProvider`'s own `Initialize` in Group order.

### How it works

1. Resolves `IDataSetProvider` from the service provider (set after `app.Build()`)
2. Finds the query operation by path pattern (`/datasets/` + `/query`)
3. Enriches the `DataSetName` path parameter with an enum of all dataset names
4. Builds per-dataset markdown field tables in the operation description
5. Clones the operation per dataset so each appears as its own Scalar sidebar entry with dataset-specific query parameters

### Registration

```csharp
var dataSetQueryDocProcessor = new DataSetQueryDocumentProcessor();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.DocumentProcessors.Add(dataSetQueryDocProcessor);
    };
});

var app = builder.Build();
// Phase 3 — one PlatformServices sweep runs every domain's Initialize (incl. DataSetProvider's).
PlatformServices.Initialize(app.Services, loggerFactory);
// Why: IDataSetProvider is only available after DataSetProvider's Initialize has run inside discovery.
dataSetQueryDocProcessor.Initialize(app.Services);
```

### What Scalar shows

Each dataset gets its own expandable entry:

```
GET /api/v1/datasets/NflPlayers/query
  - PlayerId (integer) — Primary Key
  - Name (string) — Indexed
  - Position (string) — Indexed
  - Team (string) — Indexed
  - skip (integer)
  - take (integer)

GET /api/v1/datasets/NflGames/query
  - GameId (integer) — Primary Key
  - HomeTeam (string) — Indexed
  - GameDate (string)
  - skip (integer)
  - take (integer)
```

### Cascading multi-parameter cloning

When a route has multiple dependent path parameters (e.g., `/api/v1/{Organization}/{DataSetName}/query`), nest the clone loops. Each outer loop resolves the parent, each inner loop gets its children:

```csharp
var organizations = GetAllOrganizations();
foreach (var org in organizations)
{
    var orgDataSets = GetDataSetsForOrganization(org);
    foreach (var dataSet in orgDataSets)
    {
        var path = genericPath
            .Replace("{Organization}", org.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{DataSetName}", dataSet.Name, StringComparison.OrdinalIgnoreCase);

        var clonedOperation = new OpenApiOperation
        {
            Summary = $"Query {org.Name}/{dataSet.Name}",
            OperationId = $"QueryDataSet_{org.Name}_{dataSet.Name}",
            // ...
        };

        // Skip both {Organization} and {DataSetName} from original params
        // Use Tags($"DataSets - {org.Name}") to group in Scalar sidebar
        // Add field-specific query params for this dataset
    }
}
```

This generates `orgs x datasets` operations. Use tags to keep the Scalar sidebar organized. For 3+ parameters, add another nesting level following the same pattern.

## Writing a custom document processor

### Template

```csharp
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

public sealed class MyDocumentProcessor : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        // context.Document.Paths — all registered routes
        // context.Document.Definitions — all schema definitions
        // context.Document.Tags — document-level tags

        // Modify operations:
        foreach (var (path, pathItem) in context.Document.Paths)
        {
            foreach (var (method, operation) in pathItem)
            {
                // operation.Parameters — path/query/header params
                // operation.Description — markdown rendered by Scalar
                // operation.Tags — sidebar grouping
            }
        }

        // Modify schemas:
        foreach (var (name, schema) in context.Document.Definitions)
        {
            foreach (var (propName, propSchema) in schema.Properties)
            {
                // propSchema.Type — JsonObjectType.String/Integer/etc.
                // propSchema.Enumeration — add allowed values here
            }
        }
    }
}
```

### Registration

```csharp
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.DocumentProcessors.Add(new MyDocumentProcessor());
    };
});
```

### Deferred initialization

If the processor needs services that aren't available until after `app.Build()`:

```csharp
public sealed class MyProcessor : IDocumentProcessor
{
    private IServiceProvider? _serviceProvider;

    public void Initialize(IServiceProvider sp) => _serviceProvider = sp;

    public void Process(DocumentProcessorContext context)
    {
        if (_serviceProvider is null) return;
        var myService = _serviceProvider.GetService<IMyService>();
        // ...
    }
}
```

Call `processor.Initialize(app.Services)` after `app.Build()` and service initialization.

## Key NSwag types

| Type | Purpose |
|------|---------|
| `IDocumentProcessor` | Modify the entire OpenAPI document |
| `DocumentProcessorContext` | Access to `Document`, `Settings`, `SchemaResolver` |
| `OpenApiDocument` | Root document with `Paths`, `Definitions`, `Tags` |
| `OpenApiPathItem` | One path, keyed by HTTP method |
| `OpenApiOperation` | One operation with `Parameters`, `Responses`, `Description` |
| `OpenApiParameter` | Path/query/header parameter with `Schema`, `Kind` |
| `JsonSchema` | NJsonSchema type — `Type`, `Properties`, `Enumeration` |
| `JsonObjectType` | `String`, `Integer`, `Number`, `Boolean`, `Object`, `Array` |
