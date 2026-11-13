using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// The limits of a Graph data set read (rows, time) are configuration with the default declared on the kind's option; one that is not a positive number refuses the read and is never replaced by a number written in code (FDW-5291).
/// </summary>
/// <remarks>
/// The limits of a Graph data set read (rows, time) are configuration with the default declared on the kind's option; one that is not a positive number refuses the read and is never replaced by a number written in code (FDW-5291). HTTP 400.
/// </remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetLimitNotStated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetLimitNotStatedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetLimitNotStatedCode"/> class.</summary>
    public GraphDataSetLimitNotStatedCode()
        : base(21075, "GraphDataSetLimitNotStated",
            ResultSeverities.ByName("Error"),
            "The read limit '{limit}' of Graph data sets is not stated as a positive number ({value}), so data set '{dataSet}' cannot be read",
            isRetryable: false)
    {
    }
}
