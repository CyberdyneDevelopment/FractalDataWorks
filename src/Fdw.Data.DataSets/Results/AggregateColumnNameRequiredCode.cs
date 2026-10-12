using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// An aggregate measure is missing its aggregateColumnName or inputFieldName. Caller-input
/// validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "AggregateColumnNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AggregateColumnNameRequiredCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateColumnNameRequiredCode"/> class.
    /// </summary>
    public AggregateColumnNameRequiredCode()
        : base(20005, "AggregateColumnNameRequired",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: aggregate is missing aggregateColumnName or inputFieldName",
            isRetryable: false)
    {
    }
}
