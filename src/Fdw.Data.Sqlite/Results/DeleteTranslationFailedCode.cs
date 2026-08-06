using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An unexpected exception during SQLite delete translation.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "DeleteTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteTranslationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTranslationFailedCode"/> class.
    /// </summary>
    public DeleteTranslationFailedCode()
        : base(21013, "DeleteTranslationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite delete translation failed",
            isRetryable: false)
    {
    }
}
