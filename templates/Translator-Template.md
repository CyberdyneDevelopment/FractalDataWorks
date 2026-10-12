# Template: Creating a New Translator

**Purpose**: Translates universal IDataCommand to domain-specific command (e.g., QueryCommand → SqlCommand, QueryCommand → HttpRequestMessage)

---

## File Location

```
src/Fdw.Data.{TypeSystem}/
└── Translators/
    ├── {TypeSystem}DataCommandTranslatorBase.cs    ← Base class (create once)
    ├── {TypeSystem}DataCommandTranslators.cs       ← TypeCollection (create once)
    ├── {TypeSystem}QueryTranslator.cs              ← Specific translators
    ├── {TypeSystem}InsertTranslator.cs
    ├── {TypeSystem}UpdateTranslator.cs
    └── {TypeSystem}DeleteTranslator.cs
```

**Examples:**
- `src/Fdw.Data.MsSql/Translators/MsSqlQueryTranslator.cs`
- `src/Fdw.Data.Http/Translators/RestQueryTranslator.cs`

---

## Template Code - Base Class

```csharp
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using {NativeCommandNamespace};  // e.g., Microsoft.Data.SqlClient

namespace Fdw.Data.{TypeSystem}.Translators;

/// <summary>
/// Base class for {TypeSystem} data command translators.
/// Returns {TCommand} objects ready for execution.
/// </summary>
public abstract class {TypeSystem}DataCommandTranslatorBase
    : DataCommandTranslatorBase<{TCommand}>
{
    protected {TypeSystem}DataCommandTranslatorBase(int id, string name)
        : base(id, name, "{DomainName}")  // e.g., "MsSql", "Rest", "GraphQL"
    {
    }

    // Add helper methods for building queries
    protected static {TCommand} CreateCommand(string commandText)
    {
        return new {TCommand}(commandText);
    }

    // Add parameter building helpers
    // Add WHERE clause builders
    // Add JOIN builders, etc.
}
```

---

## Template Code - Query Translator

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using {NativeCommandNamespace};

namespace Fdw.Data.{TypeSystem}.Translators;

/// <summary>
/// Translates QueryCommand to {TypeSystem} SELECT statement/request.
/// </summary>
[TypeOption(typeof({TypeSystem}DataCommandTranslators), "{TypeSystem}Query")]
public sealed class {TypeSystem}QueryTranslator : {TypeSystem}DataCommandTranslatorBase
{
    public {TypeSystem}QueryTranslator()
        : base(id: 1, name: "{TypeSystem}Query")
    {
    }

    public override Task<IGenericResult<{TCommand}>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate container
            if (container == null)
            {
                return Task.FromResult(
                    GenericResult<{TCommand}>.Failure("Container cannot be null"));
            }

            // 2. Build native query using container.Schema
            var nativeCommand = BuildSelectStatement(command, container);

            return Task.FromResult(GenericResult<{TCommand}>.Success(nativeCommand));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                GenericResult<{TCommand}>.Failure($"Translation failed: {ex.Message}"));
        }
    }

    private static {TCommand} BuildSelectStatement(IDataCommand command, IStorageContainer container)
    {
        var query = new StringBuilder();

        // Build SELECT clause from container.Schema.Fields
        query.Append("SELECT ");
        query.Append(string.Join(", ", container.Schema.Fields.Select(f => FormatFieldName(f.Name))));

        // Build FROM clause from container.Path
        query.Append($" FROM {FormatContainerName(container)}");

        // Build WHERE clause from command.Metadata["Filter"]
        // Build ORDER BY from command.Metadata["Ordering"]
        // Build paging, etc.

        return CreateCommand(query.ToString());
    }

    private static string FormatFieldName(string fieldName)
    {
        // Type-system-specific formatting
        // SQL: [FieldName]
        // REST: fieldName
        // GraphQL: fieldName
        return $"[{fieldName}]";  // Example for SQL
    }

    private static string FormatContainerName(IStorageContainer container)
    {
        // Extract table/endpoint name from container.Path
        return container.Name;
    }
}
```

---

## Requirements

### 1. Inherit from Correct Base

**Option A**: Type-system-specific base (recommended)
```csharp
public sealed class MsSqlQueryTranslator : MsSqlDataCommandTranslatorBase
```

**Option B**: Generic base
```csharp
public sealed class MsSqlQueryTranslator : DataCommandTranslatorBase<SqlCommand>
```

### 2. Use Container Schema

**Always use container.Schema for query building:**
```csharp
// ✅ DO - Schema-driven
foreach (var field in container.Schema.Fields)
{
    query.Append($"[{field.Name}]");
}

// ❌ DON'T - Hardcoded
query.Append("SELECT Id, Name, Email");  // Wrong!
```

### 3. Return Railway-Oriented Results

```csharp
// ✅ Success
return GenericResult<SqlCommand>.Success(sqlCommand);

// ✅ Failure
return GenericResult<SqlCommand>.Failure("Translation failed: ...");

// ❌ Exception
throw new TranslationException();  // Don't throw!
```

### 4. Handle Command Metadata

Commands carry metadata in `command.Metadata` dictionary:
```csharp
var filter = command.Metadata?.TryGetValue("Filter", out var filterObj) == true
    ? filterObj as IFilterExpression
    : null;

var ordering = command.Metadata?.TryGetValue("Ordering", out var orderObj) == true
    ? orderObj as IOrderingExpression
    : null;
```

---

## Integration Checklist

- [ ] Create base translator class for type system
- [ ] Implement query translator (most important)
- [ ] Add [TypeOption(typeof({TypeSystem}DataCommandTranslators), "Name")]
- [ ] Use container.Schema.Fields for SELECT clause
- [ ] Use container.Path for FROM/endpoint
- [ ] Handle Filter, Projection, Ordering, Paging from metadata
- [ ] Return IGenericResult (never throw exceptions)
- [ ] Test: QueryCommand → translated command

---

## Common Patterns

### SQL Translators
- Return `SqlCommand` with parameterized queries
- Use `[FieldName]` for identifiers
- Build WHERE clauses with SqlParameters (SQL injection prevention)
- Support JOIN, GROUP BY, OFFSET/FETCH

### REST Translators
- Return `HttpRequestMessage`
- Build OData query parameters ($filter, $select, $orderby, $top, $skip)
- Handle endpoint path parameters
- Set headers, authentication

### GraphQL Translators
- Return GraphQL query object
- Build query with selections and arguments
- Handle nested field requests
- Support variables

---

## Testing

**Test the translator**:
```csharp
[Fact]
public async Task ShouldTranslateSimpleQuery()
{
    var translator = new MsSqlQueryTranslator();
    var command = new QueryCommand<Customer>("Customers");
    var container = new TableContainer("Customers", path, schema, FormatTypes.Tabular);

    var result = await translator.Translate(command, container, CancellationToken.None);

    result.IsSuccess.ShouldBeTrue();
    result.Value.CommandText.ShouldContain("SELECT");
    result.Value.CommandText.ShouldContain("FROM");
}
```

**DON'T test every translator variation** - just verify translation works.

---

**See**: `src/Fdw.Data.MsSql/Translators/` for complete working examples
