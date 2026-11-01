using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Find translation failed with exception.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "FindTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FindTranslationFailedCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindTranslationFailedCode"/> class.
    /// </summary>
    public FindTranslationFailedCode()
        : base(91007, "FindTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate find: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
