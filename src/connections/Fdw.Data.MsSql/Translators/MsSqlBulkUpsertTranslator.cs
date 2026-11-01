using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Translates <c>BulkUpsertCommand</c> to a set-based T-SQL batch (see <see cref="MsSqlUpsertTranslatorBase"/>).
/// The rows travel as one parameter and are applied in set-based statements, so a large load is one round trip.
/// </summary>
[TypeOption(typeof(MsSqlDataCommandTranslators), "BulkUpsert", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlBulkUpsertTranslator : MsSqlUpsertTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlBulkUpsertTranslator"/> class.
    /// </summary>
    public MsSqlBulkUpsertTranslator()
        : base("BulkUpsert")
    {
    }
}
