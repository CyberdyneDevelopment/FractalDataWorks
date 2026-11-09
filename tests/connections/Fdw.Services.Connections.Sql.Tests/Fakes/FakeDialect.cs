using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Sql.Tests.Fakes;

/// <summary>
/// Minimal ISqlDialect implementation for shared-base tests.
/// Uses double-quote quoting ("x"), @ parameter prefix, FALSE always-false predicate,
/// and LIMIT/OFFSET paging — independent of any real backend.
/// </summary>
internal sealed class FakeDialect : ISqlDialect, IKeyListSpillDialect
{
    public FakeDialect(
        bool supportsSchemaNamespace = true,
        string likeMetaCharacters = "%_[",
        char likeEscapeCharacter = '!',
        string patternMatchOperator = "LIKE")
    {
        SupportsSchemaNamespace = supportsSchemaNamespace;
        LikeMetaCharacters = likeMetaCharacters;
        LikeEscapeCharacter = likeEscapeCharacter;
        PatternMatchOperator = patternMatchOperator;
    }

    public string Name => "Fake";

    public bool SupportsSchemaNamespace { get; }

    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    public string ParameterPrefix => "@";

    public char LikeEscapeCharacter { get; }

    public string LikeMetaCharacters { get; }

    public string PatternMatchOperator { get; }

    public string AlwaysFalsePredicate => "FALSE";

    public KeyListSpillForm KeyListSpill(string uniqueId, string sourceTable, string columnName, string jsonParameter)
        => new($"PREPARE {uniqueId} FROM {sourceTable} USING {jsonParameter}; ", $"SELECT v FROM keys_{uniqueId}", $" CLEAN {uniqueId};");

    public string KeyListJson(string columnName, IReadOnlyList<object?> values) => KeyListJsonWriter.Scalars(values);

    public string BuildPagingClause(IPagingExpression paging)
        => paging.Take.HasValue
            ? $"LIMIT {paging.Take.Value} OFFSET {paging.Skip}"
            : $"OFFSET {paging.Skip}";
}
