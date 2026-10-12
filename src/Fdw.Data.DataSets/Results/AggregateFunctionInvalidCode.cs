using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// An aggregate measure's aggregateFunctionName is not a registered AggregationFunctions member.
/// Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "AggregateFunctionInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AggregateFunctionInvalidCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateFunctionInvalidCode"/> class.
    /// </summary>
    public AggregateFunctionInvalidCode()
        : base(20006, "AggregateFunctionInvalid",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: aggregateFunctionName '{aggregateFunctionName}' is not a registered AggregationFunctions member",
            isRetryable: false)
    {
    }
}
