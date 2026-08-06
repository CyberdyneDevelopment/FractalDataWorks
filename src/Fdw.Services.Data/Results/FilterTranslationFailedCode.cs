using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Filter translation failed.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "FilterTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FilterTranslationFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterTranslationFailedCode"/> class.
    /// </summary>
    public FilterTranslationFailedCode()
        : base(91002, "FilterTranslationFailed", ResultSeverities.ByName("Error"),
            "Failed to translate filter: {Error}",
            isRetryable: false)
    {
    }
}