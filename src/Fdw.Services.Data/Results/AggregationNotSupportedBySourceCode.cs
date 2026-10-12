using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A query asked a data set to group and aggregate, and the connection its source reads through does not run
/// a GROUP BY, so it would return the rows ungrouped.
/// </summary>
/// <remarks>
/// Only a connection that says so (<c>IDataConnection.AggregatesInStore</c>) is sent an aggregation. HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "AggregationNotSupportedBySource", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AggregationNotSupportedBySourceCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="AggregationNotSupportedBySourceCode"/> class.</summary>
    public AggregationNotSupportedBySourceCode()
        : base(20006, "AggregationNotSupportedBySource",
            ResultSeverities.ByName("Error"),
            "Data set '{dataSet}' cannot be grouped: its source '{source}' does not run a grouped query",
            isRetryable: false)
    {
    }
}
