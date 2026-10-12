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
public string Implementation { get; set; } = string.Empty;

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

## AuthorizationMetadataOperationProcessor

An operation processor, not a document processor. At generation it stamps every operation with who
may call it, read from the endpoint's own authorization metadata:

- `x-fdw-anonymous`: `true` when the endpoint carries `IAllowAnonymous`.
- `x-fdw-policies`: the `IAuthorizeData` policy names, which in FDW are permission names
  (`workspaces:query`). FastEndpoints' own `epPolicy:` policies are left out.

FastEndpoints writes only its bearer scheme (`JWTBearerAuth`) into an operation's `security`, never the
policy, so without these stamps a generated document cannot say who may call what. An operation that
was not described from an ASP.NET Core endpoint fails generation instead of going unstamped.

```csharp
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.DocumentName = "v1";
        s.OperationProcessors.Add(new AuthorizationMetadataOperationProcessor());
    };
});
```

## API reference documents (FDW-919)

The document is **generated once per process** and never served as-is: `UseSwaggerGen()` is not
called. NSwag's middleware kept the document its first caller got and handed it to every caller after:
an admin opening the reference after an anonymous visitor saw the anonymous document.

Instead each reference is a document endpoint derived from `ApiReferenceDocumentEndpointBase<TRequest>`
(`Fdw.Web.RestEndpoints.ApiReference`):

1. Generates the document through `IOpenApiDocumentGenerator` the first time, and keeps the JSON.
2. Parses a copy for this request into an `ApiReferenceDocument`.
3. Calls the subclass's `Filter(document, caller, request, ct)`, where `ApiReferenceCaller` is read
   from the request's claims (signed in, platform admin, `perm` claims).
4. Drops tags and schemas no remaining operation uses, and removes the stamps.

`ApiReferenceDocument` offers `KeepAdmitted(caller)` (the per-caller cut, from the stamps),
`KeepPaths`, `Operation`/`AddOperation` (clone a template operation), `ReplaceTags`, `SetTitle` and
`DescribeBearerSchemes`.

| Document endpoint base | Route | Cut |
|---|---|---|
| `ServiceReferenceDocumentEndpointBase` | `/openapi/reference` | every operation the caller may call |
| `DataReferenceDocumentEndpointBase` | `/openapi/data` | one `POST /{workspace}/{dataSet}` per data set of each workspace the caller may read |
| `WorkspaceReferenceDocumentEndpointBase` | `/openapi/data/{WorkspaceName}` | the same, one workspace; 404 when unreadable |

Pages are options of the `ApiReferencePages` TypeCollection (`ApiReferencePageBase`): a route, a title,
who may open it, the document URL for a request, and the links it offers a caller.
`ListApiReferencePagesEndpointBase` (`GET /reference/pages`) lists the links a caller may follow.

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

### Nothing caller- or configuration-specific during generation

The document is generated once per process and every API reference is cut from it, so a processor
sees no caller and runs once. Anything that depends on who is asking, or on configuration that can
change (data sets, workspaces), belongs in a reference's `Filter`, not in a processor.

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
