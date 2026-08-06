using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An unexpected exception during SQLite batch insert translation.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "BatchInsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BatchInsertTranslationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchInsertTranslationFailedCode"/> class.
    /// </summary>
    public BatchInsertTranslationFailedCode()
        : base(21016, "BatchInsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite batch insert translation failed",
            isRetryable: false)
    {
    }
}
