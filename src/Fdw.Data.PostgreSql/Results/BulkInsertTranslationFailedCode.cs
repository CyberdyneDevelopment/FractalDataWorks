using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Bulk insert (COPY) translation failed with exception.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "BulkInsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BulkInsertTranslationFailedCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkInsertTranslationFailedCode"/> class.
    /// </summary>
    public BulkInsertTranslationFailedCode()
        : base(91005, "BulkInsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate bulk insert (COPY): {ErrorMessage}",
            isRetryable: false)
    {
    }
}
