using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Sources list was null for predicate pushdown analysis.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "SourcesListNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourcesListNullCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourcesListNullCode"/> class.
    /// </summary>
    public SourcesListNullCode()
        : base(21018, "SourcesListNull", ResultSeverities.ByName("Error"),
            "Sources list cannot be null",
            isRetryable: false)
    {
    }
}