using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Delete translation failed with exception.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "DeleteTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteTranslationFailedCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTranslationFailedCode"/> class.
    /// </summary>
    public DeleteTranslationFailedCode()
        : base(91003, "DeleteTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate delete: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
