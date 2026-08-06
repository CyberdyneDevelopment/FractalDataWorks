using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Batch insert translation failed with exception.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "BatchInsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BatchInsertTranslationFailedCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchInsertTranslationFailedCode"/> class.
    /// </summary>
    public BatchInsertTranslationFailedCode()
        : base(91004, "BatchInsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate batch insert: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
