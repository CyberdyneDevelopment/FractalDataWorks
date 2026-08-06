using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Bulk insert translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "BulkInsertTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class BulkInsertTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkInsertTranslationFailedCode"/> class.
    /// </summary>
    public BulkInsertTranslationFailedCode()
        : base(91001, "BulkInsertTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate bulk insert: {ErrorMessage}",
            isRetryable: false)
    {
    }
}