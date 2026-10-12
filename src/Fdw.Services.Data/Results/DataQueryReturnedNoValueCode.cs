using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A data query reported success but carried no row collection.
/// </summary>
/// <remarks>
/// An empty result is an empty collection. A missing one is the gateway contradicting itself, and
/// reading it as "no rows" would tell a caller the data set is empty when nobody knows that. HTTP 500.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "DataQueryReturnedNoValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataQueryReturnedNoValueCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataQueryReturnedNoValueCode"/> class.</summary>
    public DataQueryReturnedNoValueCode()
        : base(91007, "DataQueryReturnedNoValue",
            ResultSeverities.ByName("Error"),
            "Query of data set '{dataSet}' reported success but returned no rows collection",
            isRetryable: false)
    {
    }
}
