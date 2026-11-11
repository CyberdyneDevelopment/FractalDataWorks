using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.PostgreSql.Translators;

/// <summary>
/// Translates <c>UpsertCommand</c> to one set-based PostgreSQL command (see <see cref="PostgreSqlUpsertTranslatorBase"/>): the changed
/// rows are updated and the new keys inserted for the whole batch, never a delete and an insert per row (FDW-4955).
/// </summary>
[TypeOption(typeof(PostgreSqlDataCommandTranslators), "Upsert", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlUpsertTranslator : PostgreSqlUpsertTranslatorBase
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlUpsertTranslator"/> class.</summary>
    public PostgreSqlUpsertTranslator()
        : base("Upsert")
    {
    }
}
