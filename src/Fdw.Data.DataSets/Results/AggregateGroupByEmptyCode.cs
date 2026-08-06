using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// An aggregate measure's groupByFieldNames is empty or contains an empty element. Caller-input
/// validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "AggregateGroupByEmpty", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AggregateGroupByEmptyCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateGroupByEmptyCode"/> class.
    /// </summary>
    public AggregateGroupByEmptyCode()
        : base(20007, "AggregateGroupByEmpty",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: aggregate '{aggregateColumnName}' has an empty groupByFieldNames element",
            isRetryable: false)
    {
    }
}
