using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Sqlite.Results;

/// <summary>
/// An unexpected exception during SQLite update translation.
/// </summary>
[TypeOption(typeof(SqliteDataResultCodes), "UpdateTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpdateTranslationFailedCode : SqliteDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTranslationFailedCode"/> class.
    /// </summary>
    public UpdateTranslationFailedCode()
        : base(21012, "UpdateTranslationFailed",
            ResultSeverities.ByName("Error"),
            "SQLite update translation failed",
            isRetryable: false)
    {
    }
}
