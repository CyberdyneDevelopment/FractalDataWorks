using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Filter decomposition failed.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "FilterDecompositionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FilterDecompositionFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterDecompositionFailedCode"/> class.
    /// </summary>
    public FilterDecompositionFailedCode()
        : base(90002, "FilterDecompositionFailed", ResultSeverities.ByName("Error"),
            "Failed to decompose filter: {Error}",
            isRetryable: false)
    {
    }
}