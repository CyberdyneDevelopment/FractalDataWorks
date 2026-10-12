using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Failed to translate QUERY command.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "QueryTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QueryTranslationFailedCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryTranslationFailedCode"/> class.
    /// </summary>
    public QueryTranslationFailedCode()
        : base(91002, "QueryTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate REST query: {ErrorMessage}",
            isRetryable: false)
    {
    }
}