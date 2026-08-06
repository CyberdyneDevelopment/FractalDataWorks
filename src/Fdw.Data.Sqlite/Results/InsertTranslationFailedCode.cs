using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An unexpected exception during SQLite insert translation.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "InsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InsertTranslationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertTranslationFailedCode"/> class.
    /// </summary>
    public InsertTranslationFailedCode()
        : base(21011, "InsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite insert translation failed",
            isRetryable: false)
    {
    }
}
