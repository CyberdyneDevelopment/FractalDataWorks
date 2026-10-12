using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Http.Results;

/// <summary>
/// Failed to translate query to HTTP request.
/// </summary>
[TypeOption(typeof(DataHttpResultCodes), "QueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryTranslationFailedCode : DataHttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTranslationFailedCode"/> class.
    /// </summary>
    public QueryTranslationFailedCode()
        : base(90002, "QueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate query to HTTP request: {ErrorMessage}",
            isRetryable: false)
    {
    }
}