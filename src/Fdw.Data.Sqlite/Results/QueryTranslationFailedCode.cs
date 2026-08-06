using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An unexpected exception during SQLite query translation.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "QueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryTranslationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTranslationFailedCode"/> class.
    /// </summary>
    public QueryTranslationFailedCode()
        : base(21010, "QueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite query translation failed",
            isRetryable: false)
    {
    }
}
