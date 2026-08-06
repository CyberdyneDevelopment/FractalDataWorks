using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An unexpected exception during SQLite compound query translation.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "CompoundQueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompoundQueryTranslationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundQueryTranslationFailedCode"/> class.
    /// </summary>
    public CompoundQueryTranslationFailedCode()
        : base(21015, "CompoundQueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite compound query translation failed",
            isRetryable: false)
    {
    }
}
