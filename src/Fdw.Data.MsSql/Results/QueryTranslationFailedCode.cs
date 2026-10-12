using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Query translation failed with exception.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "QueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryTranslationFailedCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTranslationFailedCode"/> class.
    /// </summary>
    public QueryTranslationFailedCode()
        : base(90002, "QueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate query: {ErrorMessage}",
            isRetryable: false)
    {
    }
}