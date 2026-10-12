using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Query translation failed with exception.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "QueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryTranslationFailedCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTranslationFailedCode"/> class.
    /// </summary>
    public QueryTranslationFailedCode()
        : base(91000, "QueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate query: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
