using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Sqlite.Translators;

namespace Fdw.Services.Connections.Sqlite;

/// <summary>
/// Translates <c>BulkUpsertCommand</c> to set-based SQLite statements (see <see cref="SqliteUpsertTranslatorBase"/>): the changed rows are
/// updated and the new keys inserted for the whole batch, never a delete and an insert per row (FDW-4955).
/// </summary>
[TypeOption(typeof(SqliteDataCommandTranslators), "BulkUpsert", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SqliteBulkUpsertTranslator : SqliteUpsertTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="SqliteBulkUpsertTranslator"/> class.</summary>
    public SqliteBulkUpsertTranslator()
        : base("BulkUpsert")
    {
    }
}
