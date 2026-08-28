using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql.Tests.Fakes;

/// <summary>
/// Minimal ISqlDialect implementation for shared-base tests.
/// Uses double-quote quoting ("x"), @ parameter prefix, FALSE always-false predicate,
/// and LIMIT/OFFSET paging — independent of any real backend.
/// </summary>
internal sealed class FakeDialect : ISqlDialect
{
    public FakeDialect(bool supportsSchemaNamespace = true)
    {
        SupportsSchemaNamespace = supportsSchemaNamespace;
    }

    public string Name => "Fake";

    public bool SupportsSchemaNamespace { get; }

    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    public string ParameterPrefix => "@";

    public string AlwaysFalsePredicate => "FALSE";

    public string BuildPagingClause(IPagingExpression paging)
        => paging.Take.HasValue
            ? $"LIMIT {paging.Take.Value} OFFSET {paging.Skip}"
            : $"OFFSET {paging.Skip}";
}
