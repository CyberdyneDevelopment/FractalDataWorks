using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An unexpected exception during SQLite find translation.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "FindTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FindTranslationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindTranslationFailedCode"/> class.
    /// </summary>
    public FindTranslationFailedCode()
        : base(21014, "FindTranslationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite find translation failed",
            isRetryable: false)
    {
    }
}
