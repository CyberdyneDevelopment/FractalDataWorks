using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Update translation failed with exception.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "UpdateTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpdateTranslationFailedCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTranslationFailedCode"/> class.
    /// </summary>
    public UpdateTranslationFailedCode()
        : base(91002, "UpdateTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate update: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
