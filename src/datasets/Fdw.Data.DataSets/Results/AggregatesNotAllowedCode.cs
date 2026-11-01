using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// Aggregates were supplied for a data set whose kind has none: only a Simple data set defines aggregates of its own (FDW-1031). Caller-input validation failure: HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "AggregatesNotAllowed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AggregatesNotAllowedCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregatesNotAllowedCode"/> class.
    /// </summary>
    public AggregatesNotAllowedCode()
        : base(20014, "AggregatesNotAllowed",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: aggregates are not allowed when implementation is '{implementation}'",
            isRetryable: false)
    {
    }
}
