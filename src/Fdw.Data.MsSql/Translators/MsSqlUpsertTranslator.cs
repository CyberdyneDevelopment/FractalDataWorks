using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql.Translators;

/// <summary>
/// Translates <c>UpsertCommand</c> to a set-based T-SQL batch (see <see cref="MsSqlUpsertTranslatorBase"/>).
/// </summary>
[TypeOption(typeof(MsSqlDataCommandTranslators), "Upsert", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlUpsertTranslator : MsSqlUpsertTranslatorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MsSqlUpsertTranslator"/> class.
    /// </summary>
    public MsSqlUpsertTranslator()
        : base("Upsert")
    {
    }
}
