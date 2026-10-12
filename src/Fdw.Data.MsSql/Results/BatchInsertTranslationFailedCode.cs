using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Batch insert translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "BatchInsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BatchInsertTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchInsertTranslationFailedCode"/> class.
    /// </summary>
    public BatchInsertTranslationFailedCode()
        : base(91000, "BatchInsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate batch insert: {ErrorMessage}",
            isRetryable: false)
    {
    }
}